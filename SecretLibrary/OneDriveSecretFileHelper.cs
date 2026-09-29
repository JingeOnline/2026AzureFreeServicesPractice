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
        /// 返回指定的key对应的值
        /// </summary>
        /// <param name="keys">将多个层级的key按照顺序传入</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">传入的keys参数为null或空数组</exception>
        public static string? getJsonConfig(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                throw new ArgumentException("Keys cannot be null or empty.", nameof(keys));
            }
            else
            {
                IConfigurationSection? section= config.Value.GetSection(keys[0]);
                if(section == null)
                {
                    throw new ArgumentException($"Key '{keys[0]}' not found in the configuration.", nameof(keys));
                }
                if(keys.Length > 1)
                {
                    for(int i = 1; i < keys.Length; i++)
                    {
                        section = section.GetSection(keys[i]);
                        if(section == null)
                        {
                            throw new ArgumentException($"Key '{keys[i]}' not found in the configuration.", nameof(keys));
                        }
                    }
                }
                return section.Value;
            }
        }
    }
}
