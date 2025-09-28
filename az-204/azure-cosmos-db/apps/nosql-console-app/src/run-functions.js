import 'dotenv/config';
import { CosmosClient } from '@azure/cosmos';
import { faker } from '@faker-js/faker';


const client = new CosmosClient({
  endpoint: process.env.COSMOS_ENDPOINT,
  key: process.env.COSMOS_KEY,
});

const RUN = process.env.RUN_TAG || Date.now().toString(36);


const db = client.database('appdb');

const defs = [
  { id: 'orders',   pkPath: '/tenantId', samplePk: 'tenant-0' },
  { id: 'products', pkPath: '/category', samplePk: 'tools' },
  { id: 'users',    pkPath: '/orgId',    samplePk: 'org-0' },
  { id: 'events',   pkPath: '/type',     samplePk: 'audit' },
  { id: 'logs',     pkPath: '/level',    samplePk: 'info' }
];




for (const { id, pkPath, samplePk } of defs) {
  const c = db.container(id);
  const pkField = pkPath.slice(1);
  const pk = `${samplePk}#${RUN}`;     // <-- unique logical partition per run

  console.log(`\n=== ${id} === (pk=${pk})`);

  const docs = [
    { id: faker.string.uuid(), [pkField]: pk, kind: 'bulk' },
    { id: faker.string.uuid(), [pkField]: pk, kind: 'bulk' }
  ];
  console.table(docs);

  // BULK (try array then JSON string if your sproc expects it)
  try {
    await c.scripts.storedProcedure('sproc_bulk_insert').execute(pk, [docs]);
    console.log('sproc_bulk_insert: OK');
  } catch (e1) {
    try {
      await c.scripts.storedProcedure('sproc_bulk_insert').execute(pk, [JSON.stringify(docs)]);
      console.log('sproc_bulk_insert(JSON): OK');
    } catch (e2) {
      console.log('sproc_bulk_insert failed:', (e2.body && e2.body.message) || e2.message);
    }
  }

  // UPSERT sproc
  try {
    const doc = { id: `upsert-${Date.now()}-${Math.random().toString(36).slice(2)}`, [pkField]: pk, kind: 'upsert' };
    const { resource } = await c.scripts.storedProcedure('sproc_upsert').execute(pk, [doc]);
    console.log('sproc_upsert:', resource?.id ?? 'OK');
  } catch (e) {
    console.log('sproc_upsert failed:', e.code || e.message);
  }

  // PAGED sproc
  try {
    const { resource } = await c.scripts.storedProcedure('sproc_paged_query').execute(pk, [null, 5]);
    console.log(`sproc_paged_query: items=${resource?.items?.length ?? 0} cont=${!!resource?.continuation}`);
  } catch (e) {
    console.log('sproc_paged_query failed:', e.code || e.message);
  }

  // UDF demo only for products
  if (id === 'products') {
    try {
      const { resources } = await c.items.query({
        query: 'SELECT TOP 3 c.id, c.price, udf.udf_tax(c.price, @r) AS withTax FROM c WHERE c.category=@cat',
        parameters: [{ name: '@r', value: 0.0825 }, { name: '@cat', value: pk }]
      }).fetchAll();
      console.log('udf_tax: rows=', resources.length);
    } catch (e) {
      console.log('udf_tax failed:', e.code || e.message);
    }
  }

  // Triggers (at most one pre-trigger)
  try {
    const doc = { id: `trg-${Date.now()}-${Math.random().toString(36).slice(2)}`, [pkField]: pk, note: 'trigger-create' };
    const opts = {};
    if (id === 'orders') {
      opts.preTriggerInclude = ['trgPreValidateToDoItemTimestamp'];
      opts.postTriggerInclude = ['post_audit'];
    } else if (id === 'products') {
      opts.preTriggerInclude = ['pre_stamp'];
    }
    await c.items.create(doc, opts);
    console.log('triggers: create OK');
  } catch (e) {
    console.log('triggers failed:', e.code || e.message);
  }
}

