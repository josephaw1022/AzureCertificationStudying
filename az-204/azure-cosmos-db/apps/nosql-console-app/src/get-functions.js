import 'dotenv/config';
import { CosmosClient } from '@azure/cosmos';


const client = new CosmosClient({
  endpoint: process.env.COSMOS_ENDPOINT,
  key: process.env.COSMOS_KEY,
});

const db = client.database('appdb');
const containers = ['events', 'logs', 'orders', 'products', 'users'];

for (const id of containers) {
  const c = db.container(id);
  console.log(`\n=== ${id} ===`);
  try {
    const [sps, triggers, udfs] = await Promise.all([
      c.scripts.storedProcedures.readAll().fetchAll(),
      c.scripts.triggers.readAll().fetchAll(),
      c.scripts.userDefinedFunctions.readAll().fetchAll()
    ]);
    console.log('stored procedures:', sps.resources.map(x => x.id));
    console.log('triggers:', triggers.resources.map(x => x.id));
    console.log('udfs:', udfs.resources.map(x => x.id));
  } catch (e) {
    console.log('scripts not supported:', e.code || e.message);
  }
}
