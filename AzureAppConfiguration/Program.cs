using Azure.Core.Diagnostics;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using SecretLibrary;
using System.Diagnostics.Tracing;
using System.Net;

namespace AzureAppConfiguration
{
    internal class Program
    {

        static void Main(string[] args)
        {
            //需要放在连接Azure服务之前。
            DebugAzureIdentity();

            var client = ConnectByEntraId();
            //var client = ConnectByAccessKeyConnectionString();
            ReadConfigFromAzureAppConfiguration(client);
        }

        /// <summary>
        /// 使用Azure Entra ID的DefaultAzureCredential来连接Azure App Configuration服务。
        /// 需要在Azure App Configuration服务的Access Control中为用户或者应用分配“Data Reader”角色。
        /// </summary>
        /// <returns></returns>
        static ConfigurationClient ConnectByEntraId()
        {
            string endpoint = OneDriveSecretFileHelper.getJsonConfig("2026AzurePractice", "AzureAppConfigurationEndPoint");

            //这里使用DefaultAzureCredential来连接Azure App Configuration服务，但是速度特别慢，他在凭据链条中会尝试很多种方式来获取凭据，导致连接速度慢。
            //可能和当前使用Visual Studio版本，项目中引用的NuGet包版本有关，或者是Azure SDK的版本有关。
            //ConfigurationClient client = new ConfigurationClient(new Uri(endpoint), new DefaultAzureCredential());

            //这里使用VisualStudioCredential来连接Azure App Configuration服务，并指定TenantId，速度快很多。
            var credential = new VisualStudioCredential(
                new VisualStudioCredentialOptions
                {
                    TenantId = "ffebec8e-67d4-4904-8488-cbccce5e6e83"
                });
            var client = new ConfigurationClient(new Uri(endpoint), credential);

            return client;
        }

        /// <summary>
        /// 使用Azure App Configuration的Access Key连接字符串来连接Azure App Configuration服务。
        /// 这个速度是最快的。
        /// </summary>
        /// <returns></returns>
        static ConfigurationClient ConnectByAccessKeyConnectionString()
        {
            string connectionString = OneDriveSecretFileHelper.getJsonConfig("2026AzurePractice", "AzureAppConfigurationAccessKeyConnectionString");
            ConfigurationClient client = new ConfigurationClient(connectionString);
            return client;
        }

        /// <summary>
        /// 指定key和lable，读取一个app configuration项。
        /// </summary>
        /// <param name="client"></param>
        static void ReadConfigFromAzureAppConfiguration(ConfigurationClient client)
        {
            //因为当前这个key有两个的label，“Development”和“Production”，所以必须指定label来获取对应的配置值。
            //否则会抛出异常：404，The configuration setting with key 'Practice:ConsoleApp:JsonConfig' and label 'null' was not found.
            ConfigurationSetting setting = client.GetConfigurationSetting("Practice:ConsoleApp:JsonConfig", "Development");
            string jsonValue = setting.Value;
            Console.WriteLine("Value:\n" + setting.Value);
            Console.WriteLine("Description: " + setting.Description);
            Console.WriteLine("Label: " + setting.Label);
            Console.WriteLine("LastModified: " + setting.LastModified);
            Console.WriteLine("ContentType: " + setting.ContentType);
        }


        /// <summary>
        /// 启用Azure Identity库的调试日志输出，方便调试身份验证问题。
        /// 输出的日志可以查看为什么连接缓慢，或者连接失败。
        /// </summary>
        static void DebugAzureIdentity()
        {
            using var listener = new AzureEventSourceListener(
                (args, message) =>
                {
                    if (args.EventSource.Name == "Azure-Identity")
                        Console.WriteLine(message);
                },
                EventLevel.Informational);
        }
    }
}
