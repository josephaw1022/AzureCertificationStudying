using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;


var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.Development.json", optional: true)
    .Build();

var endpoint = configuration["Cosmos:Endpoint"] ?? "https://localhost:8081/";
var key = configuration["Cosmos:Key"];

Console.WriteLine($"cosmos.endpoint: {endpoint}");
Console.WriteLine($"cosmos.key: {(string.IsNullOrEmpty(key) ? "<missing>" : $"...{key[^6..]}")}");


CosmosClientOptions options = new()
{
    ConnectionMode = ConnectionMode.Gateway,
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }),
    AllowBulkExecution = true,
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    },
    // Optional: keep the client pinned to the emulator endpoint, avoids any discovery chatter
    LimitToEndpoint = true
};

using CosmosClient client = new(endpoint, key, clientOptions: options);




try
{
    var swPing = Stopwatch.StartNew();
    // lightweight call: list databases (no creation yet)
    var iter = client.ReadAccountAsync(); // resolves the addresses; ok for “ping”
    await iter;
    swPing.Stop();
    Console.WriteLine($"cosmos.ping.ok in {swPing.ElapsedMilliseconds} ms");
}
catch (CosmosException ex)
{
    Console.WriteLine("cosmos.ping.failed");
    Console.WriteLine($"status: {(int)ex.StatusCode} {ex.StatusCode}");
    Console.WriteLine(ex.Diagnostics?.ToString() ?? "<no diagnostics>");
    throw;
}
catch (Exception ex)
{
    Console.WriteLine($"cosmos.ping.failed: {ex.GetType().Name} {ex.Message}");
    throw;
}


var domainLayout = new (string identifier, string[] containers)[]
{
    ("mealProgramCore", new[]{
        "householdApplications","studentEligibility","eligibilityDeterminations","benefitIssuance"
    }),
    ("menuPlanning", new[]{
        "menus","menuCycles","recipes","nutrientProfiles","ingredients"
    }),
    ("kitchenOperations", new[]{
        "productionRecords","prepTasks","wasteLogs","holdingTemperatures","kitchenAssets"
    }),
    ("inventory", new[]{
        "inventoryItems","inventoryBatches","stockOnHand","inventoryTransactions","reorderPoints"
    }),
    ("procurement", new[]{
        "vendors","bids","contracts","purchaseOrders","deliveries"
    }),
    ("pointOfSale", new[]{
        "schoolCafeterias","terminalDevices","posTransactions","refunds","tenders"
    }),
    ("payments", new[]{
        "familyAccounts","accountBalances","recharges","paymentInstruments"
    }),
    ("claims", new[]{
        "dailyCounts","editChecks","claimSummaries","reimbursementSubmissions","stateResponses"
    }),
    ("compliance", new[]{
        "reviewFindings","correctiveActions","policyDocuments","trainingRecords"
    }),
    ("allergenHealth", new[]{
        "allergyProfiles","dietOrders","accommodations"
    }),
    ("rosters", new[]{
        "students","guardians","enrollments","households"
    }),
    ("communications", new[]{
        "bulletins","messageTemplates","outbox","deliveries"
    }),
    ("identityAccessManagement", new[]{
        "roles","roleHierarchy","permissions","groups","groupMemberships","assignments"
    }),
    ("workflows", new[]{
        "workflowDefinitions","workflowInstances","tasks","approvals"
    }),
    ("governance", new[]{
        "auditLogs","dataPolicies","legalHolds","dataExports"
    }),
    ("geospatial", new[]{
        "schools","geofences","deviceLocations","locationEvents"
    }),
    ("analytics", new[]{
        "menuComplianceViews","posDailyAggregates","inventorySnapshots"
    })
};

var tenants = new[] { "district-01", "district-42", "district-99" };
var schools = new[] { "hs-01", "hs-07", "ms-03", "es-12" };
var rnd = new Random();

int Concurrency() => Environment.ProcessorCount * 8;


int CountFor(string container) => rnd.Next(50, 101);

string Pick(params string[] values) => values[rnd.Next(values.Length)];
string Id() => Guid.NewGuid().ToString("N");
DateTime UtcWithinDays(int days) => DateTime.UtcNow.AddMinutes(-rnd.Next(days * 24 * 60));
int Int(int min, int max) => rnd.Next(min, max + 1);
decimal Money(decimal min, decimal max) => Math.Round((decimal)rnd.NextDouble() * (max - min) + min, 2);
string Grade() => Pick("K", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12");
string Channel() => Pick("email", "sms", "push");
string Severity() => Pick("Low", "Medium", "High");
string MonthKey(DateTime? d = null) { var dt = d ?? DateTime.UtcNow; return $"{dt:yyyyMM}"; }

object MakeDoc(string db, string container)
{
    var tenantId = Pick(tenants);
    var schoolId = Pick(schools);
    var studentId = $"stu_{rnd.Next(1000000):D7}";
    var hhId = $"hh_{rnd.Next(1000000):D7}";
    var now = UtcWithinDays(60);


    if (db == "procurement" && container == "deliveries")
    {
        return new
        {
            id = Id(),
            tenantId,
            locationId = $"loc_{Int(1, 50)}",
            poId = $"po_{Id()}",
            arrivedAt = now,
            issues = Pick("None", "Short", "Damaged"),
            partitionKey = $"{tenantId}#loc_{Int(1, 50)}"
        };
    }

    if (db == "communications" && container == "deliveries")
    {
        return new
        {
            id = Id(),
            tenantId,
            recipientId = $"rcp_{Id()}",
            channel = Channel(),
            status = Pick("Sent", "Failed", "Queued"),
            ts = now,
            partitionKey = $"{tenantId}#rcp"
        };
    }



    object body = container switch
    {
        // mealProgramCore
        "householdApplications" => new
        {
            id = Id(),
            tenantId,
            applicationId = Id(),
            householdId = hhId,
            submittedAt = now,
            members = Int(1, 6),
            incomeMonthly = Money(500, 6000),
            status = Pick("Pending", "Verified", "Denied"),
            partitionKey = $"{tenantId}#{hhId}"
        },
        "studentEligibility" => new
        {
            id = Id(),
            tenantId,
            studentId,
            eligibility = Pick("Free", "Reduced", "Paid"),
            effectiveOn = now.Date,
            verifiedBy = $"user_{Int(100, 999)}",
            partitionKey = $"{tenantId}#{studentId}"
        },
        "eligibilityDeterminations" => new
        {
            id = Id(),
            tenantId,
            applicationId = Id(),
            method = Pick("DirectCert", "Paper", "Online"),
            result = Pick("Free", "Reduced", "Denied"),
            decidedAt = now,
            partitionKey = $"{tenantId}#{hhId}"
        },
        "benefitIssuance" => new
        {
            id = Id(),
            tenantId,
            studentId,
            benefit = Pick("Free", "Reduced"),
            issuedAt = now,
            issuer = $"user_{Int(100, 999)}",
            partitionKey = $"{tenantId}#{studentId}"
        },

        // menuPlanning
        "menus" => new
        {
            id = Id(),
            tenantId,
            schoolId,
            serviceDate = now.Date,
            meal = Pick("Breakfast", "Lunch", "Snack"),
            items = Enumerable.Range(0, Int(2, 6)).Select(_ => $"recipe_{Id()}").ToArray(),
            partitionKey = $"{tenantId}#{schoolId}"
        },
        "menuCycles" => new { id = Id(), tenantId, cycleId = $"cycle_{Id()}", startsOn = now.Date.AddDays(-7), endsOn = now.Date.AddDays(21), partitionKey = $"{tenantId}#cycle" },
        "recipes" => new { id = Id(), tenantId, recipeId = $"recipe_{Id()}", name = $"Recipe {Int(1, 9999)}", servings = Int(25, 200), partitionKey = $"{tenantId}#recipe" },
        "nutrientProfiles" => new { id = Id(), tenantId, recipeId = $"recipe_{Id()}", calories = Int(150, 900), sodiumMg = Int(50, 1200), partitionKey = $"{tenantId}#recipe" },
        "ingredients" => new { id = Id(), tenantId, ingredientId = $"ing_{Id()}", name = $"Ingredient {Int(1, 9999)}", allergens = new[] { Pick("None", "Milk", "Soy", "Wheat", "Egg") }, partitionKey = $"{tenantId}#ingredient" },

        // kitchenOperations
        "productionRecords" => new
        {
            id = Id(),
            tenantId,
            schoolId,
            serviceDate = now.Date,
            meal = Pick("Breakfast", "Lunch"),
            planned = Int(100, 1200),
            prepared = Int(100, 1200),
            served = Int(80, 1100),
            leftovers = Int(0, 50),
            partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}"
        },
        "prepTasks" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, task = $"Prep {Int(1, 999)}", owner = $"staff_{Int(100, 999)}", dueAt = now.AddHours(2), partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "wasteLogs" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, reason = Pick("Overproduction", "Quality", "Expired"), qty = Int(1, 30), partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "holdingTemperatures" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, item = $"Item {Int(1, 500)}", tempF = Int(120, 190), checkedAt = now, partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "kitchenAssets" => new { id = Id(), tenantId, schoolId, assetTag = $"asset_{Id()}", type = Pick("Oven", "Warmer", "Fridge"), status = Pick("OK", "Maintenance", "Down"), partitionKey = $"{tenantId}#{schoolId}" },

        // inventory
        "inventoryItems" => new { id = Id(), tenantId, itemId = $"item_{Id()}", name = $"Item {Int(1, 99999)}", uom = Pick("ea", "lb", "case"), partitionKey = $"{tenantId}#item" },
        "inventoryBatches" => new { id = Id(), tenantId, itemId = $"item_{Id()}", lot = $"lot{Int(1000, 9999)}", expiresOn = now.AddDays(Int(10, 180)).Date, partitionKey = $"{tenantId}#item" },
        "stockOnHand" => new { id = Id(), tenantId, locationId = $"loc_{Int(1, 50)}", itemId = $"item_{Id()}", qty = Int(0, 5000), partitionKey = $"{tenantId}#loc_{Int(1, 50)}" },
        "inventoryTransactions" => new { id = Id(), tenantId, locationId = $"loc_{Int(1, 50)}", ts = now, type = Pick("Issue", "Receive", "Adjust"), qty = Int(-200, 200), partitionKey = $"{tenantId}#loc_{Int(1, 50)}" },
        "reorderPoints" => new { id = Id(), tenantId, locationId = $"loc_{Int(1, 50)}", itemId = $"item_{Id()}", min = Int(10, 200), max = Int(200, 2000), partitionKey = $"{tenantId}#loc_{Int(1, 50)}" },

        // procurement
        "vendors" => new { id = Id(), tenantId, vendorId = $"ven_{Id()}", name = $"Vendor {Int(1, 9999)}", partitionKey = $"{tenantId}#vendor" },
        "bids" => new { id = Id(), tenantId, bidId = $"bid_{Id()}", category = Pick("Dairy", "Produce", "Meat", "Dry"), dueOn = now.AddDays(14).Date, partitionKey = $"{tenantId}#bid" },
        "contracts" => new { id = Id(), tenantId, contractId = $"con_{Id()}", vendorId = $"ven_{Id()}", startsOn = now.Date, endsOn = now.AddMonths(12).Date, partitionKey = $"{tenantId}#contract" },
        "purchaseOrders" => new { id = Id(), tenantId, poId = $"po_{Id()}", vendorId = $"ven_{Id()}", orderedAt = now, total = Money(100, 25000), partitionKey = $"{tenantId}#po" },

        // pointOfSale
        "schoolCafeterias" => new { id = Id(), tenantId, schoolId, name = $"{schoolId} Cafeteria", capacity = Int(100, 1200), partitionKey = $"{tenantId}#{schoolId}" },
        "terminalDevices" => new { id = Id(), tenantId, schoolId, deviceId = $"term_{Id()}", status = Pick("Active", "Inactive"), partitionKey = $"{tenantId}#{schoolId}" },
        "posTransactions" => new { id = Id(), tenantId, schoolId, studentId, amount = Money(0, 8), tender = Pick("Cash", "Account", "Card"), ts = now, partitionKey = $"{tenantId}#{MonthKey(now)}" },
        "refunds" => new { id = Id(), tenantId, schoolId, studentId, amount = Money(1, 8), reason = Pick("Duplicate", "Cancel", "Adjustment"), ts = now, partitionKey = $"{tenantId}#{schoolId}" },
        "tenders" => new { id = Id(), tenantId, schoolId, name = Pick("Cash", "Account", "Card"), active = true, partitionKey = $"{tenantId}#{schoolId}" },

        // payments
        "familyAccounts" => new { id = Id(), tenantId, householdId = hhId, primaryGuardian = $"guardian_{Int(1000, 9999)}", partitionKey = $"{tenantId}#{hhId}" },
        "accountBalances" => new { id = Id(), tenantId, householdId = hhId, balance = Money(-50, 300), updatedAt = now, partitionKey = $"{tenantId}#{hhId}" },
        "recharges" => new { id = Id(), tenantId, householdId = hhId, amount = Money(5, 200), method = Pick("Card", "ACH", "Cash"), ts = now, partitionKey = $"{tenantId}#{hhId}" },
        "paymentInstruments" => new { id = Id(), tenantId, householdId = hhId, last4 = $"{Int(1000, 9999)}", brand = Pick("VISA", "MC", "AMEX"), partitionKey = $"{tenantId}#{hhId}" },

        // claims
        "dailyCounts" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, breakfast = Int(0, 800), lunch = Int(0, 1500), snack = Int(0, 400), partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "editChecks" => new { id = Id(), tenantId, claimId = $"claim_{MonthKey(now)}", status = Pick("Pass", "Warn", "Fail"), details = $"Check {Int(1, 99)}", partitionKey = $"{tenantId}#claim" },
        "claimSummaries" => new { id = Id(), tenantId, claimMonth = MonthKey(now), totalMeals = Int(1000, 80000), partitionKey = $"{tenantId}#{MonthKey(now)}" },
        "reimbursementSubmissions" => new { id = Id(), tenantId, claimMonth = MonthKey(now), submittedAt = now, amount = Money(1000, 250000), partitionKey = $"{tenantId}#{MonthKey(now)}" },
        "stateResponses" => new { id = Id(), tenantId, claimMonth = MonthKey(now), status = Pick("Accepted", "Clarification", "Rejected"), partitionKey = $"{tenantId}#{MonthKey(now)}" },

        // compliance
        "reviewFindings" => new { id = Id(), tenantId, reviewId = $"rev_{Id()}", area = Pick("MealPattern", "Counting", "Verification", "CivilRights"), severity = Severity(), partitionKey = $"{tenantId}#rev" },
        "correctiveActions" => new { id = Id(), tenantId, reviewId = $"rev_{Id()}", action = $"Action {Int(1, 999)}", dueOn = now.AddDays(30).Date, partitionKey = $"{tenantId}#rev" },
        "policyDocuments" => new { id = Id(), tenantId, policyId = $"pol_{Id()}", title = $"Policy {Int(1, 9999)}", updatedAt = now, partitionKey = $"{tenantId}#policy" },
        "trainingRecords" => new { id = Id(), tenantId, staffId = $"staff_{Int(1000, 9999)}", topic = Pick("Allergen", "Sanitation", "POS", "CivilRights"), completedAt = now, partitionKey = $"{tenantId}#staff" },

        // allergenHealth
        "allergyProfiles" => new { id = Id(), tenantId, studentId, allergens = new[] { Pick("Milk", "Soy", "Wheat", "Peanut", "None") }, notes = $"Note {Int(1, 999)}", partitionKey = $"{tenantId}#{studentId}" },
        "dietOrders" => new { id = Id(), tenantId, studentId, diet = Pick("GlutenFree", "LactoseFree", "Vegetarian", "Diabetic"), startsOn = now.Date, partitionKey = $"{tenantId}#{studentId}" },
        "accommodations" => new { id = Id(), tenantId, studentId, accommodation = $"Accomod {Int(1, 99)}", partitionKey = $"{tenantId}#{studentId}" },

        // rosters
        "students" => new { id = Id(), tenantId, studentId, name = $"Student {Int(1, 999999)}", grade = Grade(), schoolId, partitionKey = $"{tenantId}#{studentId}" },
        "guardians" => new { id = Id(), tenantId, studentId, name = $"Guardian {Int(1, 999999)}", phone = $"+1-555-{Int(100, 999)}-{Int(1000, 9999)}", partitionKey = $"{tenantId}#{studentId}" },
        "enrollments" => new { id = Id(), tenantId, schoolId, studentId, enrolledAt = now.Date, status = Pick("Enrolled", "Pending", "Exited"), partitionKey = $"{tenantId}#{schoolId}" },
        "households" => new { id = Id(), tenantId, householdId = hhId, address = $"{Int(10, 9999)} Main St", city = "Springfield", state = "TX", zip = $"{Int(10000, 99999)}", partitionKey = $"{tenantId}#{hhId}" },

        // communications
        "bulletins" => new { id = Id(), tenantId, schoolId, title = $"Bulletin {Int(1, 9999)}", body = $"Message {Int(1, 999999)}", postedAt = now, partitionKey = $"{tenantId}#{schoolId}" },
        "messageTemplates" => new { id = Id(), tenantId, templateId = $"tmpl_{Id()}", channel = Channel(), content = $"Template {Int(1, 9999)}", partitionKey = $"{tenantId}#tmpl" },
        "outbox" => new { id = Id(), tenantId, channel = Channel(), recipientId = $"rcp_{Id()}", payload = $"Body {Int(1, 99999)}", queuedAt = now, partitionKey = $"{tenantId}#{Channel()}" },

        // identityAccessManagement
        "roles" => new { id = Id(), tenantId, roleId = $"role_{Id()}", name = Pick("Director", "Manager", "Cashier", "Cook", "Auditor"), partitionKey = $"{tenantId}#role" },
        "roleHierarchy" => new { id = Id(), tenantId, parentRoleId = $"role_{Id()}", childRoleId = $"role_{Id()}", partitionKey = $"{tenantId}#role" },
        "permissions" => new { id = Id(), tenantId, permissionId = $"perm_{Id()}", action = Pick("Read", "Write", "Approve", "Manage"), resource = Pick("Menu", "Inventory", "Claims", "POS"), partitionKey = $"{tenantId}#perm" },
        "groups" => new { id = Id(), tenantId, groupId = $"grp_{Id()}", name = $"Group {Int(1, 9999)}", partitionKey = $"{tenantId}#grp" },
        "groupMemberships" => new { id = Id(), tenantId, groupId = $"grp_{Id()}", principalId = $"user_{Int(1000, 9999)}", partitionKey = $"{tenantId}#grp" },
        "assignments" => new { id = Id(), tenantId, principalId = $"user_{Int(1000, 9999)}", roleId = $"role_{Id()}", partitionKey = $"{tenantId}#user" },

        // workflows
        "workflowDefinitions" => new { id = Id(), tenantId, definitionId = $"wfdef_{Id()}", name = $"Flow {Int(1, 999)}", version = Int(1, 10), partitionKey = $"{tenantId}#wfdef" },
        "workflowInstances" => new { id = Id(), tenantId, instanceId = $"wfinst_{Id()}", definitionId = $"wfdef_{Id()}", status = Pick("Running", "Completed", "Failed"), startedAt = now, partitionKey = $"{tenantId}#wfinst" },
        "tasks" => new { id = Id(), tenantId, instanceId = $"wfinst_{Id()}", name = $"Task {Int(1, 9999)}", assignee = $"user_{Int(1000, 9999)}", dueAt = now.AddDays(3), partitionKey = $"{tenantId}#wfinst" },
        "approvals" => new { id = Id(), tenantId, instanceId = $"wfinst_{Id()}", approver = $"user_{Int(1000, 9999)}", outcome = Pick("Approved", "Rejected", "NeedsInfo"), decidedAt = now, partitionKey = $"{tenantId}#wfinst" },

        // governance
        "auditLogs" => new { id = Id(), tenantId, month = MonthKey(now), actor = $"user_{Int(1000, 9999)}", action = $"action_{Int(1, 20)}", ts = now, partitionKey = $"{tenantId}#{MonthKey(now)}" },
        "dataPolicies" => new { id = Id(), tenantId, name = $"Policy {Int(1, 9999)}", retentionDays = Int(30, 3650), partitionKey = $"{tenantId}" },
        "legalHolds" => new { id = Id(), tenantId, caseId = $"case_{Id()}", appliedAt = now, partitionKey = $"{tenantId}#case" },
        "dataExports" => new { id = Id(), tenantId, exportId = $"exp_{Id()}", status = Pick("Queued", "Running", "Complete", "Failed"), partitionKey = $"{tenantId}#exp" },

        // geospatial
        "schools" => new { id = Id(), tenantId, schoolId, name = $"{schoolId}", address = $"{Int(10, 9999)} Main St", partitionKey = $"{tenantId}" },
        "geofences" => new { id = Id(), tenantId, schoolId, name = $"Fence {Int(1, 99)}", partitionKey = $"{tenantId}#{schoolId}" },
        "deviceLocations" => new { id = Id(), tenantId, deviceId = $"dev_{Id()}", lat = Math.Round(29 + rnd.NextDouble(), 6), lng = Math.Round(-98 - rnd.NextDouble(), 6), ts = now, partitionKey = $"{tenantId}#dev" },
        "locationEvents" => new { id = Id(), tenantId, deviceId = $"dev_{Id()}", kind = Pick("Enter", "Exit", "Dwell"), ts = now, partitionKey = $"{tenantId}#{MonthKey(now)}" },

        // analytics
        "menuComplianceViews" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, compliance = Pick("Meets", "Near", "Below"), partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "posDailyAggregates" => new { id = Id(), tenantId, schoolId, serviceDate = now.Date, transactions = Int(50, 3000), revenue = Money(100, 25000), partitionKey = $"{tenantId}#{schoolId}#{now:yyyy-MM-dd}" },
        "inventorySnapshots" => new { id = Id(), tenantId, locationId = $"loc_{Int(1, 50)}", month = MonthKey(now), value = Money(1000, 500000), partitionKey = $"{tenantId}#loc_{Int(1, 50)}" },

        _ => new { id = Id(), tenantId, partitionKey = $"{tenantId}#misc" }
    };

    return body;
}

async Task EnsureAndSeedAsync(string databaseId, string containerId)
{
    var swAll = Stopwatch.StartNew();
    Console.WriteLine($"seed.start {databaseId}/{containerId}");

    try
    {
        var swDb = Stopwatch.StartNew();
        var db = await client.CreateDatabaseIfNotExistsAsync(databaseId);
        swDb.Stop();
        Console.WriteLine($"db.ready {databaseId} in {swDb.ElapsedMilliseconds} ms");

        var swC = Stopwatch.StartNew();
        var props = new ContainerProperties(containerId, "/partitionKey");
        var container = await db.Database.CreateContainerIfNotExistsAsync(props);
        swC.Stop();
        Console.WriteLine($"container.ready {databaseId}/{containerId} in {swC.ElapsedMilliseconds} ms");

        var total = CountFor(containerId);
        var parallelism = Concurrency();
        Console.WriteLine($"seed.plan {databaseId}/{containerId} items={total} parallelism={parallelism}");

        using var throttler = new SemaphoreSlim(parallelism);
        var tasks = new List<Task>(total);
        var success = 0;
        var failed = 0;

        for (int i = 0; i < total; i++)
        {
            await throttler.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var doc = MakeDoc(databaseId, containerId);
                    var pk = (string)doc.GetType().GetProperty("partitionKey")!.GetValue(doc)!;
                    await container.Container.CreateItemAsync(doc, new PartitionKey(pk));
                    Interlocked.Increment(ref success);
                }
                catch (CosmosException ex)
                {
                    Interlocked.Increment(ref failed);
                    var delay = ex.RetryAfter.HasValue ? (int)ex.RetryAfter.Value.TotalMilliseconds : 200;
                    Console.WriteLine($"item.fail {databaseId}/{containerId} status={(int)ex.StatusCode} delay={delay}ms");
                    Console.WriteLine(ex.Diagnostics?.ToString() ?? "<no diagnostics>");

                    try
                    {
                        await Task.Delay(delay);
                        var doc2 = MakeDoc(databaseId, containerId);
                        var pk2 = (string)doc2.GetType().GetProperty("partitionKey")!.GetValue(doc2)!;
                        await container.Container.CreateItemAsync(doc2, new PartitionKey(pk2));
                        Interlocked.Increment(ref success);
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"item.retry.fail {databaseId}/{containerId}: {ex2.GetType().Name} {ex2.Message}");
                        if (ex2 is CosmosException c2) Console.WriteLine(c2.Diagnostics?.ToString() ?? "<no diagnostics>");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.WriteLine($"item.fail {databaseId}/{containerId}: {ex.GetType().Name} {ex.Message}");
                }
                finally
                {
                    throttler.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        swAll.Stop();
        Console.WriteLine($"seed.done {databaseId}/{containerId} ok={success} fail={failed} timeMs={swAll.ElapsedMilliseconds}");
    }
    catch (CosmosException ex)
    {
        Console.WriteLine($"seed.fatal {databaseId}/{containerId} status={(int)ex.StatusCode} {ex.StatusCode}");
        Console.WriteLine(ex.Diagnostics?.ToString() ?? "<no diagnostics>");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"seed.fatal {databaseId}/{containerId}: {ex.GetType().Name} {ex.Message}");
        throw;
    }
}


var swTotal = Stopwatch.StartNew();

foreach (var domain in domainLayout)
{
    Console.WriteLine($"domain.start {domain.identifier}");
    foreach (var container in domain.containers)
    {
        await EnsureAndSeedAsync(domain.identifier, container);
    }
    Console.WriteLine($"domain.done {domain.identifier}");
}

swTotal.Stop();
Console.WriteLine($"seeding complete in {swTotal.ElapsedMilliseconds} ms");


Console.WriteLine("seeding complete");
