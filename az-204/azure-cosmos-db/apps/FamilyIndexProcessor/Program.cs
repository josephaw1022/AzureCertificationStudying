using FamilyIndexProcessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ICosmosService, CosmosService>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<ICosmosService>().Client);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<FamilyIndexWorker>();


var host = builder.Build();
await host.RunAsync();