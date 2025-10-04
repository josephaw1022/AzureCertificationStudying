# Cosmos DB - Azure's no-sql database service

this is Azure's flagship nosql database service that is intended to be the alternative to relational database services.

#### Resource Hierarchy


First you create a Cosmos Account. You can have up to 50 cosmos accounts in a single azure subscription.
That account must be associated with a single cosmos api. This is a rough equivalent of a postgres cluster or sql server which itself contains multiple databases.


then you create a database within that account.


and within the database, you create containers which are essentially just tables.


in when you create a container, you must define a partition key. partition keys are used to decided how which data goes where when it horizontally scales.



#### Consistency Levels 

- Strong
- Bounded Staleness
- Session
- Consistent Prefix
- Eventual



Strong - guarunteed to read latest writes on every read.

Bounded Staleness - either k number of versions and or the time interval reads can lag behind writes (whichever comes first if both are configured)

Session Consistency - guarunteed to be able to read your writes and writes-follow-reads guaruntees.

Consistent Prefix - Basically guaruntees writes will be read in order, but no guaruntee on when

Eventual - no guaruntee of order or time frame of when this consistency occurs



#### Apis for cosmsos

- Nosql - recommended no-sql api
- Mongodb - another recommended no-sql api
- postgres - postgres with citus 
- cassandra - columnar data
- gremlin - graph data
- table - for those using table api in storage account



RU - throughput unit for cosmos db


provisioned throughput mode - set ru for database and or container

serverless mode - you dont provision any throughput

