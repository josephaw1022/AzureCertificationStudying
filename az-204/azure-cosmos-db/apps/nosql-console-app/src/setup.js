'use strict';
import 'dotenv/config';

import { CosmosClient } from '@azure/cosmos';
import { faker } from '@faker-js/faker';
import fs from 'fs';
import path from 'path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DB_ID = 'appdb';

// Containers to create: name, partition key, how many docs
const CONTAINERS = [
  { id: 'orders', pk: '/tenantId', count: 2000 },
  { id: 'products', pk: '/category', count: 1500 },
  { id: 'users', pk: '/orgId', count: 1000 },
  { id: 'events', pk: '/type', count: 1500 },
  { id: 'logs', pk: '/level', count: 1200 },
];

// Stored procedures/triggers/udfs folders
const TRIGGERS_DIR = path.join(__dirname, '..', 'triggers');
const SPROC_DIR = path.join(__dirname, '..', 'sproc');
const UDFS_DIR = path.join(__dirname, '..', 'udfs');

// Bulk chunk size per partition for sproc inserts
const BULK_BATCH_SIZE = 200;
// Pre-triggers to apply on single-document creates
const PRE_TRIGGERS_ON_CREATE = ['trgPreValidateToDoItemTimestamp'];
// Post-triggers to apply on create
const POST_TRIGGERS_ON_CREATE = ['post_audit'];
// -------------------------------------------

(async () => {


  const client = new CosmosClient({
    endpoint: process.env.COSMOS_ENDPOINT,
    key: process.env.COSMOS_KEY,
  });

  console.log('Connecting to emulator...');
  const { database } = await client.databases.createIfNotExists({ id: DB_ID });
  console.log(`DB ready: ${DB_ID}`);

  for (const def of CONTAINERS) {
    const container = await ensureContainer(database, def.id, def.pk);

    // Register UDFs, triggers, sprocs from files
    await upsertUdfsFromDir(container, UDFS_DIR);
    await upsertTriggersFromDir(container, TRIGGERS_DIR);
    await upsertSprocsFromDir(container, SPROC_DIR);

    // Seed (mix of single creates using pre-trigger + bulk sproc per partition)
    console.log(`[seed] Generating ${def.count.toLocaleString()} docs for '${def.id}'...`);
    const docs = generateDocs(def.id, def.pk, def.count);
    // 1) Do a small handful as single creates (to demo pre/post triggers)
    for (const d of docs.slice(0, 10)) {
      await container.items.create(d, {
        preTriggerInclude: PRE_TRIGGERS_ON_CREATE,
        postTriggerInclude: POST_TRIGGERS_ON_CREATE,
      });
    }
    // 2) Bulk insert the rest per partition via stored procedure
    await bulkInsertByPartition(container, def.pk, docs.slice(10));

    // Demo: query with UDF + sproc page
    await demoQueries(container, def.id);
  }

  console.log('All done.');
})().catch(err => {
  console.error(err);
  process.exit(1);
});

// ----------------- helpers -----------------

async function ensureContainer(database, id, pkPath) {
  const def = { id, partitionKey: { kind: 'Hash', version: 2, paths: [pkPath] } };
  try {
    const resp = await database.containers.createIfNotExists(def, { throughput: 400 });
    console.log(`[container] ${id} (${pkPath}) @ 400 RU/s`);
    return resp.container;
  } catch {
    const resp = await database.containers.createIfNotExists(def);
    console.log(`[container] ${id} (${pkPath}) @ serverless/no-throughput`);
    return resp.container;
  }
}


async function upsertUdfsFromDir(container, dir) {
  if (!fs.existsSync(dir)) return;
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.js'));
  for (const f of files) {
    const mod = (await import(pathToFileURL(path.join(dir, f)).href)).default;
    try {
      await container.scripts.userDefinedFunctions.create({ id: mod.id, body: mod.body });
    } catch (e) {
      if (e.code === 409) {
        await container.scripts.userDefinedFunction(mod.id).replace({ id: mod.id, body: mod.body });
      } else { throw e; }
    }
    console.log(`[udf] upserted ${mod.id}`);
  }
}

async function upsertTriggersFromDir(container, dir) {
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.js'));
  for (const f of files) {
    const mod = (await import(pathToFileURL(path.join(dir, f)).href)).default;
    try {
      await container.scripts.triggers.create({
        id: mod.id,
        triggerType: mod.triggerType,
        triggerOperation: mod.triggerOperation,
        body: mod.body,
      });
    } catch (e) {
      if (e.code === 409) {
        await container.scripts.trigger(mod.id).replace({
          id: mod.id,
          triggerType: mod.triggerType,
          triggerOperation: mod.triggerOperation,
          body: mod.body,
        });
      } else { throw e; }
    }
    console.log(`[trigger] upserted ${mod.id}`);
  }
}

async function upsertSprocsFromDir(container, dir) {
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.js'));
  for (const f of files) {
    const mod = (await import(pathToFileURL(path.join(dir, f)).href)).default;
    try {
      await container.scripts.storedProcedures.create({ id: mod.id, body: mod.body });
    } catch (e) {
      if (e.code === 409) {
        await container.scripts.storedProcedure(mod.id).replace({ id: mod.id, body: mod.body });
      } else { throw e; }
    }
    console.log(`[sproc] upserted ${mod.id}`);
  }
}

function generateDocs(containerId, pkPath, count) {
  const pkName = pkPath.replace(/^\//, '');
  const out = [];
  for (let i = 0; i < count; i++) {
    const doc = { id: faker.string.uuid() };
    switch (containerId) {
      case 'orders':
        doc[pkName] = `tenant-${i % 10}`;
        doc.orderNumber = faker.number.int({ min: 10000, max: 99999 });
        doc.customer = {
          id: faker.string.uuid(),
          name: faker.person.fullName(),
          email: faker.internet.email(),
        };
        doc.items = Array.from({ length: faker.number.int({ min: 1, max: 5 }) }, () => ({
          sku: faker.string.alphanumeric(8).toUpperCase(),
          name: faker.commerce.productName(),
          qty: faker.number.int({ min: 1, max: 3 }),
          price: Number(faker.commerce.price({ min: 5, max: 300 })),
        }));
        doc.total = doc.items.reduce((s, it) => s + it.qty * it.price, 0);
        doc.status = faker.helpers.arrayElement(['new', 'paid', 'fulfilled', 'cancelled']);
        doc.timestamp = faker.date.recent({ days: 30 }).toISOString(); // used by sample pre-trigger
        break;

      case 'products':
        doc[pkName] = faker.helpers.arrayElement(['tools', 'toys', 'outdoors', 'kitchen']);
        doc.sku = faker.string.alphanumeric(10).toUpperCase();
        doc.name = faker.commerce.productName();
        doc.description = faker.commerce.productDescription();
        doc.price = Number(faker.commerce.price({ min: 3, max: 800 }));
        doc.tags = faker.helpers.arrayElements(['alpha', 'beta', 'pro', 'lite', 'clearance'], { min: 1, max: 3 });
        doc.timestamp = faker.date.recent({ days: 90 }).toISOString();
        break;

      case 'users':
        doc[pkName] = `org-${i % 15}`;
        doc.username = faker.internet.username();
        doc.email = faker.internet.email();
        doc.fullName = faker.person.fullName();
        doc.role = faker.helpers.arrayElement(['admin', 'manager', 'user', 'viewer']);
        doc.timestamp = faker.date.recent({ days: 120 }).toISOString();
        break;

      case 'events':
        doc[pkName] = faker.helpers.arrayElement(['audit', 'metric', 'security', 'system']);
        doc.message = faker.hacker.phrase();
        doc.severity = faker.helpers.arrayElement(['low', 'medium', 'high']);
        doc.value = faker.number.float({ min: 0, max: 100, precision: 0.01 });
        doc.timestamp = faker.date.recent({ days: 7 }).toISOString();
        break;

      case 'logs':
        doc[pkName] = faker.helpers.arrayElement(['trace', 'debug', 'info', 'warn', 'error']);
        doc.message = faker.lorem.sentence();
        doc.context = { file: faker.system.fileName(), line: faker.number.int({ min: 1, max: 500 }) };
        doc.timestamp = faker.date.recent({ days: 3 }).toISOString();
        break;

      default:
        doc[pkName] = 'default';
        doc.timestamp = new Date().toISOString();
        break;
    }
    out.push(doc);
  }
  return out;
}

async function bulkInsertByPartition(container, pkPath, docs) {
  const concurrency = 16;
  let idx = 0, ok = 0;

  async function worker() {
    for (; ;) {
      const i = idx++;
      if (i >= docs.length) break;
      const doc = docs[i];
      for (; ;) {
        try {
          await container.items.create(doc);
          ok++;
          break;
        } catch (e) {
          if (e.code === 429) {
            const wait = e.retryAfterInMs ?? 100;
            await new Promise(r => setTimeout(r, wait));
            continue;
          }
          throw e;
        }
      }
    }
  }

  await Promise.all(Array.from({ length: concurrency }, worker));
  console.log(`[bulk] Inserted ${ok.toLocaleString()} docs into '${container.id}'`);
}

async function demoQueries(container) {
  const iter = container.items.query({ query: 'SELECT TOP 5 * FROM c' }, { maxItemCount: 5 });
  const { resources } = await iter.fetchAll();
  console.log(`[query] sample 5 docs:`, resources);
}


function pickAnyPk(containerId) {
  switch (containerId) {
    case 'products': return 'tools';
    case 'orders': return 'tenant-0';
    case 'users': return 'org-0';
    case 'events': return 'audit';
    case 'logs': return 'info';
    default: return 'default';
  }
}
