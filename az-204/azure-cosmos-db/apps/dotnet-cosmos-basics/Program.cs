using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Bogus;
using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos.Scripts;

static async Task<bool> MarkerExistsAsync(Container invites, string familyId, CancellationToken ct = default)
{
    var q = new QueryDefinition("SELECT VALUE 1 FROM c WHERE c.kind = 'familyIndex'");
    using var it = invites.GetItemQueryIterator<int>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(familyId) });
    while (it.HasMoreResults)
    {
        var page = await it.ReadNextAsync(ct);
        if (page.Count > 0) return true;
    }
    return false;
}

static async Task WaitForMarkerAsync(Container invites, string familyId, TimeSpan timeout, TimeSpan poll, CancellationToken ct = default)
{
    var stop = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < stop)
    {
        if (await MarkerExistsAsync(invites, familyId, ct)) return;
        await Task.Delay(poll, ct);
    }
    throw new TimeoutException($"Marker not found for familyId {familyId} within {timeout.TotalSeconds}s");
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var endpoint = configuration["Cosmos:Endpoint"] ?? "https://localhost:8081/";
var key = configuration["Cosmos:Key"];

var options = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }),
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    },
    LimitToEndpoint = true
};

using var cosmos = new CosmosClient(endpoint, key, options);
var db = await cosmos.CreateDatabaseIfNotExistsAsync("FamilyDatabase");

var familyInvitesContainerProperties = new ContainerProperties("familyInvites", "/familyId")
{
    DefaultTimeToLive = 60 * 60 * 24 * 7,
    IndexingPolicy = new IndexingPolicy
    {
        IndexingMode = IndexingMode.Consistent,
        Automatic = true,
        IncludedPaths =
        {
            new IncludedPath { Path = "/*" }
        },
        CompositeIndexes =
        {
            new Collection<CompositePath>
            {
                new CompositePath { Path = "/emailLower", Order = CompositePathSortOrder.Ascending },
                new CompositePath { Path = "/createdUtc", Order = CompositePathSortOrder.Descending }
            },
            new Collection<CompositePath>
            {
                new CompositePath { Path = "/familyId",   Order = CompositePathSortOrder.Ascending },
                new CompositePath { Path = "/emailLower", Order = CompositePathSortOrder.Ascending }
            }
        }
    },
    UniqueKeyPolicy = new UniqueKeyPolicy
    {
        UniqueKeys =
        {
            new UniqueKey { Paths = { "/familyId", "/email" } }
        }
    },
    ComputedProperties = new Collection<ComputedProperty>
    {
        new ComputedProperty
        {
            Name = "emailLower",
            Query = "SELECT VALUE LOWER(c.email) FROM c"
        }
    }
};

var familyInvitesContainer = (await db.Database.CreateContainerIfNotExistsAsync(familyInvitesContainerProperties)).Container;

string triggerBody = @"
function validateFamilyId() {
  var c = getContext();
  var coll = c.getCollection();
  var req = c.getRequest();
  var doc = req.getBody();
  var q = 'SELECT TOP 1 * FROM c WHERE c.kind = ""familyIndex""';
  var accepted = coll.queryDocuments(coll.getSelfLink(), q, { partitionKey: doc.familyId }, function (err, items) {
    if (err) throw err;
    if (!items || items.length === 0) throw new Error('invalid familyId');
  });
  if (!accepted) throw new Error('query not accepted');
}";
var triggerProps = new TriggerProperties
{
    Id = "validateFamilyId",
    Body = triggerBody,
    TriggerOperation = TriggerOperation.Create,
    TriggerType = TriggerType.Pre
};
try { await familyInvitesContainer.Scripts.CreateTriggerAsync(triggerProps); } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }

var familyMembersContainerProperties = new ContainerProperties("familyMembers", "/familyId")
{
    IndexingPolicy = new IndexingPolicy
    {
        IndexingMode = IndexingMode.Consistent,
        Automatic = true,
        IncludedPaths =
        {
            new IncludedPath { Path = "/*" }
        },
        CompositeIndexes =
        {
            new Collection<CompositePath>
            {
                new CompositePath { Path = "/address/state", Order = CompositePathSortOrder.Ascending },
                new CompositePath { Path = "/address/zip",   Order = CompositePathSortOrder.Ascending }
            },
            new Collection<CompositePath>
            {
                new CompositePath { Path = "/role", Order = CompositePathSortOrder.Ascending },
                new CompositePath { Path = "/age",  Order = CompositePathSortOrder.Descending }
            }
        }
    },
    ComputedProperties = new Collection<ComputedProperty>
    {
        new ComputedProperty
        {
            Name = "stateLower",
            Query = "SELECT VALUE LOWER(c.address.state) FROM c"
        },
        new ComputedProperty
        {
            Name = "isAdult",
            Query = "SELECT VALUE c.age >= 18 FROM c"
        }
    }
};

var familyMembersContainer = (await db.Database.CreateContainerIfNotExistsAsync(familyMembersContainerProperties)).Container;

string deleteFamilySprocBody = @"
function deleteFamily(fid) {
  var c = getContext();
  var coll = c.getCollection();
  var resp = c.getResponse();
  var count = 0;

  var querySpec = {
    query: 'SELECT c._self FROM c WHERE c.familyId = @fid',
    parameters: [{ name: '@fid', value: fid }]
  };

  queryAndDelete();

  function queryAndDelete(continuation) {
    var accepted = coll.queryDocuments(
      coll.getSelfLink(),
      querySpec,
      { continuation: continuation, pageSize: 100, partitionKey: fid },
      function (err, docs, options) {
        if (err) throw err;
        if (!docs || docs.length === 0) {
          resp.setBody(count);
          return;
        }
        deleteBatch(docs, function () {
          if (options.continuation) {
            queryAndDelete(options.continuation);
          } else {
            resp.setBody(count);
          }
        });
      });

    if (!accepted) resp.setBody(count);
  }

  function deleteBatch(docs, callback) {
    if (docs.length === 0) {
      callback();
      return;
    }

    var doc = docs.shift();
    var accepted = coll.deleteDocument(
      doc._self,
      { partitionKey: fid },
      function (err) {
        if (err) throw err;
        count++;
        deleteBatch(docs, callback);
      });

    if (!accepted) resp.setBody(count);
  }
}";


var deleteFamilySproc = new StoredProcedureProperties
{
    Id = "deleteFamily",
    Body = deleteFamilySprocBody
};

try
{
    await familyMembersContainer.Scripts.CreateStoredProcedureAsync(deleteFamilySproc);
}
catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
{
    await familyMembersContainer.Scripts.ReplaceStoredProcedureAsync(deleteFamilySproc);
}



var faker = new Faker();

var uniqueFamilyCount = Random.Shared.Next(10, 30);
var totalMemberBatches = Random.Shared.Next(50, 150);

// var totalMemberBatches = 0;
// var uniqueFamilyCount = 0;


var familyIdPool = Enumerable.Range(0, uniqueFamilyCount)
    .Select(_ => Guid.NewGuid().ToString())
    .ToArray();

var rnd = new Random();

for (int i = 0; i < totalMemberBatches; i++)
{
    var familyId = familyIdPool[rnd.Next(familyIdPool.Length)];
    var sharedAddress = new
    {
        street = faker.Address.StreetAddress(),
        city = faker.Address.City(),
        state = faker.Address.StateAbbr(),
        zip = faker.Address.ZipCode()
    };

    var parent1 = new { id = Guid.NewGuid().ToString(), familyId, role = "parent", firstName = faker.Name.FirstName(), age = faker.Random.Int(35, 50), address = sharedAddress };
    var parent2 = new { id = Guid.NewGuid().ToString(), familyId, role = "parent", firstName = faker.Name.FirstName(), age = faker.Random.Int(35, 50), address = sharedAddress };
    var child1  = new { id = Guid.NewGuid().ToString(), familyId, role = "child",  firstName = faker.Name.FirstName(), age = faker.Random.Int(5, 18),  address = sharedAddress };
    var child2  = new { id = Guid.NewGuid().ToString(), familyId, role = "child",  firstName = faker.Name.FirstName(), age = faker.Random.Int(5, 18),  address = sharedAddress };
    var pet     = new { id = Guid.NewGuid().ToString(), familyId, role = "pet",    name = faker.Name.FirstName(), species = faker.Random.ArrayElement(new[] { "dog", "cat" }), address = sharedAddress };

    var batch = familyMembersContainer.CreateTransactionalBatch(new PartitionKey(familyId))
        .UpsertItem(parent1).UpsertItem(parent2)
        .UpsertItem(child1).UpsertItem(child2)
        .UpsertItem(pet);

    var resp = await batch.ExecuteAsync();
    if (!resp.IsSuccessStatusCode) throw new Exception($"Batch failed for {familyId}: {resp.StatusCode}");
}

var invitesPerFamily = 2;
foreach (var familyId in familyIdPool)
{
    await WaitForMarkerAsync(familyInvitesContainer, familyId, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(500));

    for (int i = 0; i < invitesPerFamily; i++)
    {
        var invite = new
        {
            id = Guid.NewGuid().ToString(),
            familyId,
            email = faker.Internet.Email(),
            invitedBy = faker.Name.FirstName(),
            roleRequested = faker.PickRandom(new[] { "parent", "child", "viewer" }),
            token = Guid.NewGuid().ToString("N"),
            createdUtc = DateTime.UtcNow
        };

        var opts = new ItemRequestOptions { PreTriggers = new List<string> { "validateFamilyId" } };
        await familyInvitesContainer.CreateItemAsync(invite, new PartitionKey(familyId), opts);
    }
}

var distinctFamilyIdQuery = new QueryDefinition("SELECT DISTINCT c.familyId FROM c");
var distinctFamilyIdIterator = familyMembersContainer.GetItemQueryIterator<dynamic>(distinctFamilyIdQuery);
var distinctFamilyIdResults = new List<string>();
while (distinctFamilyIdIterator.HasMoreResults)
{
    foreach (var row in await distinctFamilyIdIterator.ReadNextAsync())
        distinctFamilyIdResults.Add(row.familyId.ToString());
}
Console.WriteLine($"familyMembers distinct familyIds: {distinctFamilyIdResults.Count}");
