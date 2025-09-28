using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
namespace FamilyIndexProcessor;

public interface ICosmosService
{
    CosmosClient Client { get; }
}

public sealed class CosmosService : ICosmosService
{
    public CosmosClient Client { get; }

    public CosmosService(IConfiguration config)
    {
        var endpoint = config["Cosmos:Endpoint"]!;
        var key = config["Cosmos:Key"]!;

        Client = new CosmosClient(endpoint, key, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            AllowBulkExecution = false,
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
            LimitToEndpoint = true,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }),
        });
    }
}
