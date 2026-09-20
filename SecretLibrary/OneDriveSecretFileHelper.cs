using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SecretLibrary
{
    /// <summary>
    /// A helper class to read secrets from a JSON file located in the user's OneDrive folder.
    /// </summary>
    public sealed class OneDriveSecretFileHelper
    {
        private static readonly string secretFileSubPath = "OneDrive\\脚本代码\\AccountSecrets.json";
        private static readonly string secretFilePath = GetSecretFilePath();

        //使用Lazy<T>创建的变量，其内部的匿名函数只会在第一次访问Value属性时执行一次，之后的访问将直接返回已经创建的实例。
        private static readonly Lazy<IConfiguration> config = new(() =>
            new ConfigurationBuilder().AddJsonFile(secretFilePath).Build());

        private static string GetSecretFilePath()
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userFolder, secretFileSubPath);
        }

        /// <summary>
        /// 返回指定的key对应的值，如果key不存在，则返回null
        /// </summary>
        /// <param name="key">读取嵌套的子元素的值的时候，中间用冒号连接</param>
        /// <returns></returns>
        public static string? getJsonConfig(string key)
        {
            var section = config.Value.GetSection(key);
            return section.Value;
        }

        /// <summary>
        /// 返回指定的key:subKey对应的值，如果该键不存在，则返回null
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subKey"></param>
        /// <returns></returns>
        public static string? getJsonConfig(string key, string subKey)
        {
            var section = config.Value.GetSection(key).GetSection(subKey);
            return section.Value;
        }
    }
}
