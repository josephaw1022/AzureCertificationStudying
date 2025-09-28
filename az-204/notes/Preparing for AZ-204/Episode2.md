# Episode 2 - Develop for Azure storage (15-20% of exam)


link to [video](https://learn.microsoft.com/en-us/shows/exam-readiness-zone/preparing-for-az-204-02-fy25)


2.1 - Develop Solutions that use Azure Cosmos DB

- performs operations on containers and items by using the sdk
- set the appropriate consistency level for operations
- implement changed feed notifications

2.2 Develop Solutions that use Azure Blob Storage

- set and retrieve properties and metadata
- perform operations on data by using the appropriate sdk
- implement storage policies and data lifecycle management






### 2.1


Know how to interact with the cosmos db via the dotnet sdk

```csharp
// Create a database
DatabaseResponse databaseResponse = await client.CreateDatabaseIfNotExistAsync(databaseId, 10000);

// Read a database by id
DatabaseResponse readResponse = await database.ReadAsync();

// Delete a database
DatabaseResponse deleteResponse = await database.DeleteAsync();


// understand syntax of the folowing
// - client creation process
// - how to read and write operations
// - syntax to get an id
// - commands needed to create items and then query them

```





Need to know consistency levels for operations

this is in order of strongest consistency to weakest consistency

- Strong
- Bounded Staleness
- Session
- Consistent prefix
- Eventual



you can implement change feed notifications with cosmos db too

remember the following key pointers to implement change feed notifications

- you can work with change feed using azure functions
- use your provisoned throughput to read from the change feed
- capture deletes by setting a "soft-delete" within your items in place of deletes
- synchronize changes from any point-in-time; there is no fixed data retention period for which changes are available
- process changes from large containers in parallel by multiple consumers





### 2.2


azure recommends using the latest version of azure storage client 12.x for all new applications


perform operations on data by using the appropriate sdk


**BlobServiceClient** - represents the storage account and provides operations to reterview and configure account properties and to work with blob containers in the storage account. 

**BlobContainerClient** - represents a specific blob container and provides operations to work with the containers the blobs within

**BlobClient** - represents a specific blob, provides operations to work with the blob itself such as upload, download, delete, and create snapshots.

**AppendBlobClient** -represents an append blob. provides operations specific to append blobs such as appending log data.

**BlockBlobClient** - represent a block blob, provides operations specific to block blobs such as staging and committing blocks of data.


Its also important to understand access tiers and storage policies and lifecycle management


- Hot - frequently accessed data
- Cool - less frequently accessed data
- Cold - rarely accessed or modified data
- Archive - rarely accessed data

can setup rules in a storage account to actually apply rules for changing access tiers and what not. so like after a week of inactivity, go from hot to cold, or something like that.


