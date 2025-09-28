'use strict';
import 'dotenv/config';
import { CosmosClient } from '@azure/cosmos';

const cosmosClient = new CosmosClient({
  endpoint: process.env.COSMOS_ENDPOINT,
  key: process.env.COSMOS_KEY
});

const { resources: databases } = await cosmosClient.databases.readAll().fetchAll();

await Promise.all(
  databases.map(db => {
    console.log(`deleting database: ${db.id}`);
    return cosmosClient.database(db.id).delete();
  })
);

console.log('all databases removed');
