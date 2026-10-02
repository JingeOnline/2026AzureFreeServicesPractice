using Azure.Identity;
using Azure.Messaging.ServiceBus;
using AzureServiceBus.Shared;
using SecretLibrary;

namespace AzureServiceBus.ClientB
{
    /// <summary>
    /// 使用Entra ID进行身份验证连接Azure Service Bus，发送和接收消息。
    /// </summary>
    internal class Program
    {
        static string QueueName = "practiceprojectqueue";
        static string ServiceBusNamespace = OneDriveSecretFileHelper.getJsonConfig("2026AzurePractice:AzureServiceBusNamespace")!;
        static string UserName = "ClientB";

        static async Task Main(string[] args)
        {
            //使用Entra ID进行身份验证，需要在Access Control(IAM)中为Entra ID用户分配Azure Service Bus的访问权限。
            await using ServiceBusClient client = new(ServiceBusNamespace, new DefaultAzureCredential());
            ServiceBusSender sender = client.CreateSender(QueueName);
            ServiceBusReceiver receiver = client.CreateReceiver(QueueName);
            while (true)
            {
                Console.WriteLine("请输入要发送的消息内容，按下回车发送消息（输入exit退出，输入peek查看消息，输入receive仅接收ClientA的消息）：");
                Console.WriteLine("第一次操作的时候需要等待一段时间...因为需要时间来建立连接和认证。之后操作会更快。");
                string message = Console.ReadLine();
                if (message.ToLower() == "exit")
                {
                    break;
                }
                else if (message.ToLower() == "peek")
                {
                    await Peek(client);
                    continue;
                }
                else if (message.ToLower() == "receive")
                {
                    await ReceiveMessageAsync(receiver);
                    continue;
                }
                await SendMessageAsync(sender, message);
                Console.WriteLine("消息已发送：" + message);
            }
        }

        static async Task SendMessageAsync(ServiceBusSender sender, string message)
        {
            string jsonMessage = JsonHelper.CreateJsonMessage(UserName, message);
            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(jsonMessage);
            await sender.SendMessageAsync(serviceBusMessage);
        }


        /// <summary>
        /// Peek模式查看消息
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        static async Task Peek(ServiceBusClient client)
        {
            await using ServiceBusReceiver receiver = client.CreateReceiver(QueueName);

            var messages = await receiver.PeekMessagesAsync(maxMessages: 30);
            if (messages.Count == 0)
            {
                Console.WriteLine("Queue中没有消息。");
            }
            else
            {
                foreach (ServiceBusReceivedMessage message in messages)
                {
                    Console.WriteLine(
                        $"[{message.SequenceNumber}] [{message.EnqueuedTime}] {message.Body}");
                }

            }
        }

        /// <summary>
        /// 手动接收消息，且只接收ClientA发送的消息
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        static async Task ReceiveMessageAsync(ServiceBusReceiver receiver)
        {
            IEnumerable<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(maxMessages: 30);
            if (messages.Any())
            {
                int lucyMessageCount = 0;
                foreach (ServiceBusReceivedMessage message in messages)
                {
                    string body = message.Body.ToString();
                    MessageModel? messageModel = JsonHelper.DeserializeJsonMessage(body);
                    if (messageModel.Sender == "ClientA")
                    {
                        Console.WriteLine();
                        Console.WriteLine(new string(' ', 70) + $"[{messageModel.Sender}] [{messageModel.Time}]");
                        Console.WriteLine(new string(' ', 70) + $"{messageModel.Content}");
                        //手动完成消息接收
                        await receiver.CompleteMessageAsync(message);
                        lucyMessageCount++;
                    }
                }
                if(lucyMessageCount==0)
                {
                    Console.WriteLine("Queue中没有ClientA发送的消息。");
                }
            }
            else
            {
                Console.WriteLine("Queue中没有消息。");
            }
        }
    }
}
