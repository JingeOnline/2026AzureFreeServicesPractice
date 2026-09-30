using Azure.Messaging.ServiceBus;
using AzureServiceBus.Shared;
using Microsoft.Azure.Amqp.Framing;
using SecretLibrary;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace AzureServiceBus.ClientA
{
    internal class Program
    {
        static string QueueName = "practiceprojectqueue";
        static string ConnectionString = OneDriveSecretFileHelper.getJsonConfig("2026AzurePractice:AzureServiceBusConnectionString")!;
        static string UserName = "Lucy";
        static event Action ChangeModeToPeek;
        static bool IsProcessorStart = true;

        static async Task Main(string[] args)
        {
            //ServiceBusClient 实现了 IAsyncDisposable 接口，所以建议使用 await using
            await using ServiceBusClient client = new ServiceBusClient(ConnectionString);
            //创建一个发送端
            ServiceBusSender sender = client.CreateSender(QueueName);
            //创建一个接收端并启动
            await SetUpProcessor(client);
            //启动发送端
            await SetUpSender(sender, client);
        }

        static string CreateJsonMessage(string message)
        {
            MessageModel model = new MessageModel
            {
                Sender = UserName,
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

        static async Task SendMessageAsync(ServiceBusSender sender, string message)
        {
            string jsonMessage = CreateJsonMessage(message);
            ServiceBusMessage serviceBusMessage = new ServiceBusMessage(jsonMessage);
            await sender.SendMessageAsync(serviceBusMessage);
        }


        static async Task SetUpSender(ServiceBusSender sender, ServiceBusClient client)
        {
            Console.WriteLine("用户名：" + UserName + "  (CTRL+回车发送消息，运行过程中可以通过F1、F2键切换模式)");
            Console.WriteLine("按下【F1】，消息接收或关闭接收");
            Console.WriteLine("关闭接收后。按下【F2】，Peek消息");
            var input = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.F1)
                {
                    if (IsProcessorStart)
                    {
                        ChangeModeToPeek?.Invoke();
                        Console.WriteLine("已关闭接收消息");
                    }
                    else
                    {
                        await SetUpProcessor(client);
                        Console.WriteLine("已打开消息接收");
                    }
                    IsProcessorStart = !IsProcessorStart;

                }
                else if (key.Key == ConsoleKey.F2)
                {
                    Console.WriteLine("执行Peek动作");
                    await Peek(client);
                }
                //如果用户删除一个字符，input缓冲区也要删除一个字符
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input.Length--;
                        //空格：覆盖原来的字符，看起来就像删除了它。\b：光标再次向左移动，停在刚刚删除的位置，方便继续输入。
                        Console.Write(" \b");
                    }
                }
                //同时按下CTRL+回车，发送消息
                else if (key.Key == ConsoleKey.Enter && (key.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    await SendMessageAsync(sender, input.ToString());
                    input.Clear();
                }
                //按下回车键换行
                else if (key.Key == ConsoleKey.Enter)
                {
                    input.AppendLine();  // 缓冲区中保存换行
                    Console.WriteLine(); // 控制台显示换行
                }
                else
                {
                    input.Append(key.KeyChar);
                }
            }

        }

        static async Task SetUpProcessor(ServiceBusClient client)
        {
            ServiceBusProcessorOptions options = new()
            {
                //当message handler执行完毕，自动发送成功完成的反馈消息。
                //无论是否开启，一旦message handler抛出异常，processor会丢弃该消息，返回一个失败信号。
                AutoCompleteMessages = true,
                //允许多个线程操作（在当前程序中用不上）
                MaxConcurrentCalls = 2
            };
            ServiceBusProcessor processor = client.CreateProcessor(QueueName, options);
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
            //开始接收消息
            await processor.StartProcessingAsync();
            //当转换为Peek模式时，停止processor
            ChangeModeToPeek += async () => { await processor.StopProcessingAsync(); };
        }

        //处理接收到的消息
        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            MessageModel? messageModel = JsonSerializer.Deserialize<MessageModel>(body);
            Console.WriteLine();
            Console.WriteLine(new string(' ', 70) + $"[{messageModel.Sender}] [{messageModel.Time}]");
            Console.WriteLine(new string(' ', 70) + $"{messageModel.Content}");

            //如果ServiceBusProcessorOptions.AutoCompleteMessages=true，则默认会自动发送成功回复，不需要手动调用该方法。。
            //如果ServiceBusProcessorOptions.AutoCompleteMessages=false，需要显式的调用消息接收完成的方法，之后该消息才会从queue中删除。
            //如果没有调用该方法，则消息会在超过锁定期后，发送到其他客户端（也包括当前客户端），直到到达Max delivery count，变成死信。
            await args.CompleteMessageAsync(args.Message);
        }

        //处理接收消息过程中的异常
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        static async Task Peek(ServiceBusClient client)
        {
            await using ServiceBusReceiver receiver = client.CreateReceiver(QueueName);

            var messages = await receiver.PeekMessagesAsync(maxMessages: 30);

            foreach (ServiceBusReceivedMessage message in messages)
            {
                Console.WriteLine(
                    $"[{message.SequenceNumber}] {message.Body}");
            }

            if (messages.Count == 0)
            {
                Console.WriteLine("没有可查看的消息。");
            }
        }
    }
}
