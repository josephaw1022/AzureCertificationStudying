using System;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DotnetBasedFunctions;

public class EventHubTrigger
{
    private readonly ILogger<EventHubTrigger> _logger;

    public EventHubTrigger(ILogger<EventHubTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(EventHubTrigger))]
    public void Run([EventHubTrigger("samples-workitems", Connection = "EventHubConnection")] EventData[] events)
    {
        foreach (EventData @event in events)
        {
            _logger.LogInformation("Event Body: {body}", @event.Body);
            _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
        }
    }


    [Function("EhTrigger")]
    public void Run2([EventHubTrigger("eh1", ConsumerGroup = "cg1", Connection = "EventHubConnection")] EventData[] events, FunctionContext ctx)
    {
        foreach (var e in events)
        {
            _logger.LogInformation("Event Body: {body}", e.Body);
            _logger.LogInformation("Event Content-Type: {contentType}", e.ContentType);
        }
    }

}