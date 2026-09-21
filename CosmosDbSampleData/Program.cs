using SecretLibrary;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosDbSampleData
{
    internal class Program
    {
        static string SecretConnectionString = "AzureCosmo-SampleDbConnectionString";
        static string SecretAccountUrl = "AzureCosmo-SampleDbAccountUrl";
        static string AzureCosmoPrimaryKey = "AzureCosmo-SampleKey";


        static async Task Main(string[] args)
        {
            //官方文档中推荐复用CosmosClient实例，而不是每次操作都创建新的实例
            CosmosClient client = await ConnectUsingConnectionString();
            //CosmosClient client = await ConnectUsingPrimaryKey();
            await VerifyExist(client);
            await QueryOne(client);
            await QueryMultiple(client);
            await QueryMultipleWithSql(client);
            await QueryWithParameter(client);
            await QueryWithPaginate(client);
            await UpsertItem(client);
            await CreateItem(client);
            await Task.Delay(2000); // 等待2秒，确保数据已经写入
            await DeleteItem(client);
        }

        /// <summary>
        /// 使用连接字符串连接到Cosmos DB
        /// </summary>
        /// <returns></returns>
        static async Task<CosmosClient> ConnectUsingConnectionString()
        {
            string? connectionString = OneDriveSecretFileHelper.getJsonConfig(SecretConnectionString);
            CosmosClient client = new CosmosClient(connectionString);
            //获取账户属性，验证连接是否成功
            AccountProperties account = await client.ReadAccountAsync();
            Console.WriteLine($"成功连接！账户 ID 为: {account.Id}");
            return client;
        }

        /// <summary>
        /// 使用cosmos db account url和主密钥连接到Cosmos DB
        /// </summary>
        /// <returns></returns>
        static async Task<CosmosClient> ConnectUsingPrimaryKey()
        {
            DefaultAzureCredential credential = new DefaultAzureCredential();
            string? accountUrl = OneDriveSecretFileHelper.getJsonConfig(SecretAccountUrl);
            string? primaryKey = OneDriveSecretFileHelper.getJsonConfig(AzureCosmoPrimaryKey);
            CosmosClient client = new CosmosClient(accountUrl, primaryKey);
            //获取账户属性，验证连接是否成功
            AccountProperties account = await client.ReadAccountAsync();
            Console.WriteLine($"成功连接！账户 ID 为: {account.Id}");
            return client;
        }

        /// <summary>
        /// 验证数据库和容器是否存在
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task VerifyExist(CosmosClient client)
        {
            //获取数据库对象（即使不存在也会被获取到）
            Database databaseTest = client.GetDatabase("随便什么名称");
            Console.WriteLine($"成功获取数据库！数据库名称为: {databaseTest.Id}");
            //验证云端是否存在该数据库
            Database database = client.GetDatabase("SampleDB");
            try
            {
                DatabaseProperties databaseProperties = await database.ReadAsync();
                Console.WriteLine($"数据库存在！数据库名称为: {databaseProperties.Id}");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("数据库不存在！");
            }
            //获取容器对象（相当于表）（即使不存在也会被获取到）
            Container containerTest = database.GetContainer("无论什么名称，该容器是否存在，都会被获取到。");
            Console.WriteLine($"成功获取容器！容器名称为: {containerTest.Id}");
            //验证云端是否存在该容器
            Container container = database.GetContainer("SampleContainer");
            try
            {
                ContainerProperties containerProperties = await container.ReadContainerAsync();
                Console.WriteLine($"容器存在！容器名称为: {containerProperties.Id}");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("容器不存在！");
            }
        }

        static async Task QueryOne(CosmosClient client)
        {
            Console.WriteLine("\n01 根据ID和Partition Key读取一条记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            ItemResponse<ItemModel> itemResponse = await container.ReadItemAsync<ItemModel>
                ("027D0B9A-F9D9-4C96-8213-C8546C4AAE71", new PartitionKey("26C74104-40BC-4541-8EF5-9892F7F03D72"));
            Console.WriteLine("读取成功，成功获取到指定商品：" + itemResponse.Resource.name);
        }

        /// <summary>
        /// 根据条件查询多条记录（使用LINQ）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task QueryMultiple(CosmosClient client)
        {
            Console.WriteLine("\n02 根据条件查询多条记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            var queryable = container.GetItemLinqQueryable<ItemModel>()
                .Where(item => item.categoryName == "Components, Pedals");
            double totalRu = 0;
            using (FeedIterator<ItemModel> feedIterator = queryable.ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<ItemModel> response = await feedIterator.ReadNextAsync();
                    // 累加请求单位 (RU)
                    totalRu += response.RequestCharge;
                    foreach (var item in response)
                    {
                        Console.WriteLine($"商品ID: {item.id}, 商品类别: {item.categoryName}, 商品名称: {item.name}");
                    }
                }
            }
            Console.WriteLine($"总请求单位 (RU): {totalRu}");
        }

        /// <summary>
        /// 根据条件查询多条记录（使用raw SQL语句）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task QueryMultipleWithSql(CosmosClient client)
        {
            Console.WriteLine("\n03 根据条件查询多条记录（使用SQL语句）");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            string sqlQueryText = "SELECT * FROM c WHERE c.categoryName = 'Components, Pedals'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            using (FeedIterator<ItemModel> feedIterator = container.GetItemQueryIterator<ItemModel>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<ItemModel> response = await feedIterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        Console.WriteLine($"商品ID: {item.id}, 商品类别: {item.categoryName}, 商品名称: {item.name}");
                    }
                }
            }
        }

        /// <summary>
        /// 根据参数查询多条记录（使用raw SQL语句和参数化查询，避免SQL注入风险）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task QueryWithParameter(CosmosClient client)
        {
            Console.WriteLine("\n06 根据参数查询记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            //使@符号作为参数占位符，避免SQL注入风险
            string sqlQueryText = "SELECT * FROM c WHERE c.categoryName = @categoryName";
            //使用参数化查询，避免SQL注入风险
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText).WithParameter("@categoryName", "Components, Pedals");
            using (FeedIterator<ItemModel> feedIterator = container.GetItemQueryIterator<ItemModel>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<ItemModel> response = await feedIterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        Console.WriteLine($"商品ID: {item.id}, 商品类别: {item.categoryName}, 商品名称: {item.name}");
                    }
                }
            }
        }

        /// <summary>
        /// 分页查询记录（使用raw SQL语句和分页选项，每页返回指定数量的记录）
        /// 好处：
        /// 1. 显著降低内存占用与崩溃风险。
        /// 2. 防止请求超时，提升响应速度。Cosmos DB 对单个 REST 请求有执行时间限制（通常为 5 秒）
        /// 3. 平滑 RU（ Request Units，吞吐量）消耗，避免限流。分页查询可以将大查询拆分为多个小查询，每个小查询消耗的 RU 更少，从而降低限流风险。
        /// 4. Cosmos DB 的分页基于 Continuation Token（延续令牌）。这是一个纯字符串（保存了游标信息），服务端不需要维持任何会话状态（Stateless）。
        ///    你可以把这个 Token 发给前端，前端在下一页请求时再传回后端，非常适合分布式和无状态的服务架构（如 RESTful API、Serverless / Azure Functions）。
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task QueryWithPaginate(CosmosClient client)
        {
            Console.WriteLine("\n07 分页查询记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            string sqlQueryText = "SELECT * FROM c WHERE c.categoryName = 'Components, Pedals'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            //设置分页选项，每页返回3条记录
            QueryRequestOptions paginateOptions = new QueryRequestOptions
            {
                MaxItemCount = 3 // 每页返回的最大记录数
            };
            int pageNumber = 1;
            using (FeedIterator<ItemModel> feedIterator = container.GetItemQueryIterator<ItemModel>(queryDefinition, requestOptions:paginateOptions))
            {
                while (feedIterator.HasMoreResults)
                {
                    Console.WriteLine($"第 {pageNumber} 页:");
                    FeedResponse<ItemModel> response = await feedIterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        Console.WriteLine($"商品ID: {item.id}, 商品类别: {item.categoryName}, 商品名称: {item.name}");
                    }
                    pageNumber++;
                }
            }
        }

        /// <summary>
        /// 更新或插入一条记录，如果记录存在则更新，否则插入（必须指定id和partition key，Cosmos DB并不会自动生成index）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task UpsertItem(CosmosClient client)
        {
            Console.WriteLine("\n04 更新或插入一条记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            ItemModel item = new ItemModel
            {
                id = "491834BE-AAA5-419D-B166-44B93F20EBA7",
                categoryId = "26C74104-40AC-4541-8EF5-9892F7F03D72",
                categoryName = "Components, Pedals",
                sku = "CS-9183",
                name = "Updated Product Name",
                description = "This item is updated by C# client.",
                price = 99.99,
                tags = new List<TagModel>()
            };
            //使用UpsertItemAsync方法，必须指定id和Partition Key的值
            ItemResponse<ItemModel> response = await container.UpsertItemAsync(item);
            Console.WriteLine("更新或插入成功！ ID=" + response.Resource.id);
        }

        /// <summary>
        /// 创建一条记录，如果记录已存在则会报错（必须指定id和partition key，Cosmos DB并不会自动生成index）
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task CreateItem(CosmosClient client)
        {
            Console.WriteLine("\n05 创建一条记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            ItemModel item = new ItemModel
            {
                id = "26C74104-40AC-4541-8EF5-9802F7F03D72",
                categoryId = "26C74104-40AC-4541-8EF5-5892F7F03D02",
                categoryName = "Components, Pedals",
                sku = "CS-9184",
                name = "New Product Name",
                description = "This is a new product created by C# client.",
                price = 199.99,
                tags = new List<TagModel>()
            };
            //这里显示传入了Partition Key的值，确保创建时指定了正确的分区键
            ItemResponse<ItemModel> response = await container.CreateItemAsync(item, new PartitionKey(item.categoryId));
            Console.WriteLine("创建成功！ ID=" + response.Resource.id);
        }

        static async Task DeleteItem(CosmosClient client)
        {
            Console.WriteLine("\n06 删除一条记录");
            Database database = client.GetDatabase("SampleDB");
            Container container = database.GetContainer("SampleContainer");
            string itemIdToDelete = "26C74104-40AC-4541-8EF5-9802F7F03D72";
            string partitionKeyValue = "26C74104-40AC-4541-8EF5-5892F7F03D02"; // 这里需要指定正确的分区键值
            try
            {
                ItemResponse<ItemModel> response = await container.DeleteItemAsync<ItemModel>(itemIdToDelete, new PartitionKey(partitionKeyValue));
                //成功返回204 No Content状态码，表示删除成功
                Console.WriteLine("删除成功！ StatusCode=" + (int)response.StatusCode + " " + response.StatusCode);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("删除失败，记录不存在！");
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"删除失败，Cosmos DB异常: {ex.StatusCode} - {ex.Message}");
            }
        }
    }
}
