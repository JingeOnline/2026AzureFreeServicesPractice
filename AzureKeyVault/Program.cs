using Azure;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVault
{
    /// <summary>
    /// 在Visual Stuido中运行，程序会自动从Visual Studio中获取Azure AD的身份验证信息，使用这些信息来访问Azure Key Vault中的机密。
    /// 如果想要在其他环境中运行，需要确保环境中配置了Azure AD的身份验证信息，例如通过Azure CLI使用az login命令登录之后，再运行程序。
    /// </summary>
    internal class Program
    {
        //从创建的Azure Key Vault中获取的URL，其中key-vault-not-free是Key Vault的名称，其余部分是Azure Key Vault的域名，是固定的。
        static string VaultUrl = @"https://key-vault-not-free.vault.azure.net/";
        //要获取的机密名称
        static string SecretName = "TestSecret1";
        //要获取的机密版本ID
        static string SecretVersionId = "d3ff0f1b9e714962819b8ba739c04c87";

        static void Main(string[] args)
        {
            GetSecretFromKeyVault();
            Console.WriteLine();
            CreateSecretInKeyVault();
            Console.WriteLine();
            ListSecretsInKeyVault();
            Console.WriteLine();
            //DeleteSecretFromKeyVault();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// 从Azure Key Vault中获取指定名称和版本的机密，并输出其值
        /// </summary>
        static void GetSecretFromKeyVault()
        {
            //创建一个SecretClient实例
            var client = new SecretClient(new Uri(VaultUrl), new Azure.Identity.DefaultAzureCredential());
            //从Key Vault中获取指定名称的机密
            var secret = client.GetSecret(SecretName);
            //输出机密的值
            Console.WriteLine($"{SecretName} Value: {secret.Value.Value}");
            //从Key Vault中获取指定名称和版本的机密
            var secret2 = client.GetSecret(SecretName, SecretVersionId);
            Console.WriteLine($"{SecretName} (Version: {SecretVersionId}) Value: {secret2.Value.Value}");
        }

        /// <summary>
        /// 在Azure Key Vault中创建一个新的机密，并输出其名称、值和版本。如果该机密已存在，则会创建一个新的版本。
        /// 如果要创建的机密名称已经被删除，则会抛出异常。因为Azure Key Vault中删除的机密名称会被保留一段时间，期间无法再次创建同名机密。
        /// </summary>
        static void CreateSecretInKeyVault()
        {
            string secretName = "TestSecretFromConsoleApp";
            string secretValue = "This is a test secret created from code. " + DateTime.Now.ToString();
            //创建一个SecretClient实例
            var client = new SecretClient(new Uri(VaultUrl), new Azure.Identity.DefaultAzureCredential());
            //在Key Vault中创建一个新的机密
            var secret = client.SetSecret(secretName, secretValue);
            Console.WriteLine($"Created secret {secret.Value.Name} with value: {secret.Value.Value}, ");
            Console.WriteLine($"Version: {secret.Value.Properties.Version}");
        }

        /// <summary>
        /// 列出Azure Key Vault中所有机密的名称和更新时间
        /// </summary>
        static void ListSecretsInKeyVault()
        {
            //创建一个SecretClient实例
            var client = new SecretClient(new Uri(VaultUrl), new Azure.Identity.DefaultAzureCredential());
            //列出Key Vault中的所有机密
            Console.WriteLine("Listing secrets in Key Vault:");
            Pageable<SecretProperties> allSecrets = client.GetPropertiesOfSecrets();
            foreach (var secretProperties in allSecrets)
            {
                Console.WriteLine($"- {secretProperties.Name} - updated on {secretProperties.UpdatedOn}");
            }
        }

        /// <summary>
        /// 从Azure Key Vault中删除指定名称的机密，并等待删除操作完成
        /// </summary>
        static void DeleteSecretFromKeyVault()
        {
            string secretName = "TestSecretFromCode";
            //创建一个SecretClient实例
            var client = new SecretClient(new Uri(VaultUrl), new Azure.Identity.DefaultAzureCredential());
            //从Key Vault中删除指定名称的机密
            DeleteSecretOperation operation = client.StartDeleteSecret(secretName);
            operation.WaitForCompletion();
            Console.WriteLine($"Secret [{secretName}] have been deleted.");
        }
    }
}
