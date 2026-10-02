using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AzureServiceBus.Shared
{
    public class JsonHelper
    {
        public static string CreateJsonMessage(string userName,string message)
        {
            MessageModel model = new MessageModel
            {
                Sender = userName,
                Time = DateTime.Now,
                Content = message
            };
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // 保留中文，避免输出为 \uXXXX
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(model, options);
        }

        public static MessageModel? DeserializeJsonMessage(string jsonMessage)
        {
            return JsonSerializer.Deserialize<MessageModel>(jsonMessage);
        }
    }
}
