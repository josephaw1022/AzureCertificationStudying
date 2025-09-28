using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FamilyIndexProcessor;

public sealed class FamilyIndexWorker : BackgroundService
{
    private readonly CosmosClient _client;
    private readonly ILogger<FamilyIndexWorker> _logger;
    private ChangeFeedProcessor? _processor;

    public FamilyIndexWorker(CosmosClient client, ILogger<FamilyIndexWorker> logger)
    {
        _client = client;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        const string dbId = "FamilyDatabase";
        const string sourceId = "familyMembers";
        const string targetId = "familyInvites";
        const string leasesId = "leases";

        _logger.LogInformation("Ensuring database and containers exist");
        var db = (await _client.CreateDatabaseIfNotExistsAsync(dbId, cancellationToken: cancellationToken)).Database;
        var source = db.GetContainer(sourceId);
        var target = db.GetContainer(targetId);
        var leases = (await db.CreateContainerIfNotExistsAsync(new ContainerProperties(leasesId, "/id"), cancellationToken: cancellationToken)).Container;

        _logger.LogInformation("Building change feed processor");
        _processor = source
            .GetChangeFeedProcessorBuilder<JObject>(
                "familyIndexSync",
                async (items, ct) =>
                {
                    foreach (var doc in items)
                    {
                        var fid = (string?)doc["familyId"];
                        if (string.IsNullOrWhiteSpace(fid))
                        {
                            _logger.LogWarning("Document missing familyId: {Doc}", doc.ToString(Formatting.None));
                            continue;
                        }

                        var marker = new
                        {
                            id = $"fam-{fid}",
                            familyId = fid,
                            kind = "familyIndex",
                            ttl = -1,
                            updatedUtc = DateTime.UtcNow
                        };

                        _logger.LogInformation("Upserting familyIndex marker for familyId {FamilyId}", fid);
                        await target.UpsertItemAsync(marker, new PartitionKey(fid), cancellationToken: ct);
                    }
                })
            .WithInstanceName("familyIndexSync")
            .WithLeaseContainer(leases)
            .Build();

        _logger.LogInformation("Starting change feed processor");
        await _processor.StartAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FamilyIndexWorker running (with periodic cleanup)");

        // reconciliation cadence (tune as needed)
        var sweepInterval = TimeSpan.FromSeconds(30);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting reconciliation sweep");
                await RunReconciliationSweep(stoppingToken);
                _logger.LogInformation("End of reconciliation sweep");
                await Task.Delay(sweepInterval, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("FamilyIndexWorker stopping");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping change feed processor");
        if (_processor is not null)
        {
            await _processor.StopAsync();
        }
        _client.Dispose();
        await base.StopAsync(cancellationToken);
    }

    private async Task RunReconciliationSweep(CancellationToken ct)
    {
        const string dbId = "FamilyDatabase";
        const string sourceId = "familyMembers";
        const string targetId = "familyInvites";

        var db = _client.GetDatabase(dbId);
        var members = db.GetContainer(sourceId);
        var invites = db.GetContainer(targetId);

        _logger.LogInformation("Starting reconciliation sweep");

        // scan markers in familyInvites and remove those whose family has no members
        var markerQuery = new QueryDefinition("SELECT c.id, c.familyId FROM c WHERE c.kind = 'familyIndex'");
        using var it = invites.GetItemQueryIterator<JObject>(markerQuery);

        int checkedCount = 0, cleanedCount = 0;

        while (it.HasMoreResults && !ct.IsCancellationRequested)
        {
            var page = await it.ReadNextAsync(ct);
            foreach (var row in page)
            {
                checkedCount++;
                var fid = (string?)row["familyId"];
                var id  = (string?)row["id"];
                if (string.IsNullOrEmpty(fid) || string.IsNullOrEmpty(id))
                    continue;

                var anyLeft = await AnyMembersLeftAsync(members, fid, ct);
                if (!anyLeft)
                {
                    await DeleteMarkerAsync(invites, fid, ct);
                    await DeleteInvitesAsync(invites, fid, ct);
                    cleanedCount++;
                    _logger.LogWarning("⚠️ Cleaned up empty family {FamilyId}", fid);
                }
            }
        }

        _logger.LogInformation("Reconciliation sweep complete: checked={Checked}, cleaned={Cleaned}", checkedCount, cleanedCount);
    }

    private static async Task<bool> AnyMembersLeftAsync(Container members, string familyId, CancellationToken ct)
    {
        var q = new QueryDefinition("SELECT TOP 1 c.id FROM c");
        using var it = members.GetItemQueryIterator<JObject>(
            q,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(familyId) });

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync(ct);
            if (page.Count > 0) return true;
        }
        return false;
    }

    private static async Task DeleteMarkerAsync(Container invites, string familyId, CancellationToken ct)
    {
        var markerId = $"fam-{familyId}";
        try
        {
            await invites.DeleteItemAsync<JObject>(markerId, new PartitionKey(familyId), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
    }

    private static async Task DeleteInvitesAsync(Container invites, string familyId, CancellationToken ct)
    {
        var q = new QueryDefinition("SELECT c.id FROM c WHERE c.kind != 'familyIndex'");
        using var it = invites.GetItemQueryIterator<JObject>(
            q,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(familyId) });

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync(ct);
            foreach (var row in page)
            {
                var id = (string?)row["id"];
                if (!string.IsNullOrEmpty(id))
                {
                    await invites.DeleteItemAsync<JObject>(id, new PartitionKey(familyId), cancellationToken: ct);
                }
            }
        }
    }
}
