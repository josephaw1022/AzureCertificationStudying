"use strict";
import "dotenv/config";
import { CosmosClient } from "@azure/cosmos";

const domainLayout = [
  {
    identifier: "mealProgramCore",
    containers: [
      "householdApplications",
      "studentEligibility",
      "eligibilityDeterminations",
      "benefitIssuance"
    ]
  },
  {
    identifier: "menuPlanning",
    containers: [
      "menus",
      "menuCycles",
      "recipes",
      "nutrientProfiles",
      "ingredients"
    ]
  },
  {
    identifier: "kitchenOperations",
    containers: [
      "productionRecords",
      "prepTasks",
      "wasteLogs",
      "holdingTemperatures",
      "kitchenAssets"
    ]
  },
  {
    identifier: "inventory",
    containers: [
      "inventoryItems",
      "inventoryBatches",
      "stockOnHand",
      "inventoryTransactions",
      "reorderPoints"
    ]
  },
  {
    identifier: "procurement",
    containers: [
      "vendors",
      "bids",
      "contracts",
      "purchaseOrders",
      "deliveries"
    ]
  },
  {
    identifier: "pointOfSale",
    containers: [
      "schoolCafeterias",
      "terminalDevices",
      "posTransactions",
      "refunds",
      "tenders"
    ]
  },
  {
    identifier: "payments",
    containers: [
      "familyAccounts",
      "accountBalances",
      "recharges",
      "paymentInstruments"
    ]
  },
  {
    identifier: "claims",
    containers: [
      "dailyCounts",
      "editChecks",
      "claimSummaries",
      "reimbursementSubmissions",
      "stateResponses"
    ]
  },
  {
    identifier: "compliance",
    containers: [
      "reviewFindings",
      "correctiveActions",
      "policyDocuments",
      "trainingRecords"
    ]
  },
  {
    identifier: "allergenHealth",
    containers: [
      "allergyProfiles",
      "dietOrders",
      "accommodations"
    ]
  },
  {
    identifier: "rosters",
    containers: [
      "students",
      "guardians",
      "enrollments",
      "households"
    ]
  },
  {
    identifier: "communications",
    containers: [
      "bulletins",
      "messageTemplates",
      "outbox",
      "deliveries"
    ]
  },
  {
    identifier: "identityAccessManagement",
    containers: [
      "roles",
      "roleHierarchy",
      "permissions",
      "groups",
      "groupMemberships",
      "assignments"
    ]
  },
  {
    identifier: "workflows",
    containers: [
      "workflowDefinitions",
      "workflowInstances",
      "tasks",
      "approvals"
    ]
  },
  {
    identifier: "governance",
    containers: [
      "auditLogs",
      "dataPolicies",
      "legalHolds",
      "dataExports"
    ]
  },
  {
    identifier: "geospatial",
    containers: [
      "schools",
      "geofences",
      "deviceLocations",
      "locationEvents"
    ]
  },
  {
    identifier: "analytics",
    containers: [
      "menuComplianceViews",
      "posDailyAggregates",
      "inventorySnapshots"
    ]
  }
];


function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function withRetries(run) {
  let attempt = 0;
  let wait = 300;
  for (;;) {
    try {
      return await run();
    } catch (error) {
      const code = error?.code;
      const substatus = error?.substatus;
      if (code === 429 || code === 503 || substatus === 1007) {
        attempt += 1;
        if (attempt >= 8) throw error;
        const retryAfter =
          error?.retryAfterInMs ?? error?.retryAfterInMilliseconds ?? wait;
        await delay(retryAfter);
        wait = Math.min(wait * 2, 4000);
        continue;
      }
      throw error;
    }
  }
}

async function ensureDomain(cosmosClient, identifier) {
  const result = await withRetries(() =>
    cosmosClient.databases.createIfNotExists({ id: identifier })
  );
  console.log(identifier);
  await delay(600);
  return result.database;
}

async function ensureContainer(domain, identifier, partitionKeyPath) {
  const definition = {
    id: identifier,
    partitionKey: { kind: "Hash", version: 2, paths: [partitionKeyPath] },
  };
  const result = await withRetries(() =>
    domain.containers.createIfNotExists(definition)
  );
  console.log(`${domain.id}/${identifier}`);
  await delay(600);
  return result.container;
}

const cosmosClient = new CosmosClient({
  endpoint: process.env.COSMOS_ENDPOINT,
  key: process.env.COSMOS_KEY,
});

for (const domain of domainLayout) {
  const domainResource = await ensureDomain(cosmosClient, domain.identifier);
  for (const containerName of domain.containers) {
    await ensureContainer(domainResource, containerName, "/partitionKey");
  }
}

console.log("complete");
