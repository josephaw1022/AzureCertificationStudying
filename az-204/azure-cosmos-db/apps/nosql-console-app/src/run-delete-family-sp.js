import "dotenv/config";
import { CosmosClient } from "@azure/cosmos";

const endpoint    = process.env.COSMOS_ENDPOINT || "https://127.0.0.1:8081/";
const key         = process.env.COSMOS_KEY      || "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
const databaseId  = process.env.COSMOS_DB       || "FamilyDatabase";
const containerId = process.env.COSMOS_MEMBERS  || "familyMembers";
const sprocId     = "deleteFamily";

const client = new CosmosClient({ endpoint, key });
const container = client.database(databaseId).container(containerId);

// Step 1: Get distinct familyIds
const query = "SELECT DISTINCT c.familyId FROM c";
const iterator = container.items.query(query);

const ids = [];
while (await iterator.hasMoreResults()) {
  const { resources } = await iterator.fetchNext();
  if (!resources) break;
  for (const r of resources) ids.push(r.familyId);
}

if (ids.length === 0) {
  console.error("No familyIds found.");
  process.exit(1);
}

// Step 2: Pick random familyId
const fid = ids[Math.floor(Math.random() * ids.length)];
console.log(`🎲 Selected random familyId: ${fid}`);

// Step 3: Run the stored procedure
const { resource: deletedCount } = await container
  .scripts
  .storedProcedure(sprocId)
  .execute(fid, [fid], { partitionKey: fid });

console.log(`✅ Deleted ${deletedCount} members for familyId ${fid}`);
