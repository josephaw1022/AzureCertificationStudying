package com.example;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.BlobOutput;
import com.microsoft.azure.functions.annotation.Cardinality;
import com.microsoft.azure.functions.annotation.EventHubTrigger;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import jakarta.inject.Inject;
import org.jboss.logging.Logger;


import java.util.List;
import java.util.Optional;

/**
 * Azure Functions with HTTP Trigger integrated with Quarkus
 */
public class Function {

    @Inject
    GreetingService service;

    @Inject
    Logger logger;

    /**
     * This function listens at endpoint "/api/HttpExample". Two ways to invoke it
     * using "curl" command in bash:
     * 1. curl -d "HTTP Body" {your host}/api/HttpExample
     * 2. curl "{your host}/api/HttpExample?name=HTTP%20Query"
     */
    @FunctionName("HttpExampleQuarkus")
    public HttpResponseMessage run(
            @HttpTrigger(name = "req", methods = { HttpMethod.GET,
                    HttpMethod.POST }, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            final ExecutionContext context) {
        logger.info("Java HTTP trigger processed a request.");

        // Parse query parameter
        final String query = request.getQueryParameters().get("name");
        final String name = request.getBody().orElse(query);

        if (name == null) {
            return request.createResponseBuilder(HttpStatus.BAD_REQUEST)
                    .body("Please pass a name on the query string or in the request body").build();
        } else {
            return request.createResponseBuilder(HttpStatus.OK).body(service.greeting(name)).build();
        }
    }

    @FunctionName("Eh1TriggerJavaQuarkus")
    public void eh1Trigger(
            @EventHubTrigger(name = "messages2", eventHubName = "eh1", consumerGroup = "cg1", connection = "EventHubConnection", cardinality = Cardinality.MANY, dataType = "string") List<String> messages,
            @BlobOutput(name = "outputBlob", path = "eventhub-output/{rand-guid}.txt", connection = "AzureWebJobsStorage") OutputBinding<String> outputBlob,
            final ExecutionContext context) {

        logger.info("Eh1TriggerJavaQuarkus got batch size: " + messages.size());

        for (String body : messages) {
            logger.info("Body: " + body);

            // Write each message as a blob
            outputBlob.setValue(body);
        }
    }

}
