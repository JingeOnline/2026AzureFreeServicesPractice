using SecretLibrary;
using Microsoft.Azure.Cosmos;
using Azure.Identity;

namespace CosmosDbSampleData
{
    internal class Program
    {
        static string SecretConnectionString = "AzureCosmo-SampleDbConnectionString";
        static string SecretAccountUrl = "AzureCosmo-SampleDbAccountUrl";
        static string AzureCosmoPrimaryKey = "AzureCosmo-SampleKey";


        static async Task Main(string[] args)
        {
            CosmosClient client = await ConnectUsingPrimaryKey();
            await VerifyExist(client);
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

    }
}
