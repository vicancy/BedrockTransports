using BedrockTransports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Client.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var shutdownTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                shutdownTokenSource.Cancel();
            };

            var token = shutdownTokenSource.Token;

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var clientFactory = new AzureSignalRConnectionFactory(loggerFactory, true);
            var clientEndPoint = new AzureSignalREndPoint("Endpoint=http://localhost;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;Port=8080;Version=1.0"
                , "chat", AzureSignalREndpointType.Client, true);

            var clientTask = ClientTest.RunAsync(clientEndPoint);

            Console.WriteLine("Started client");
            await clientTask;
        }


        public static class ClientTest
        {
            public static async Task RunAsync(EndPoint endpoint)
            {
                try
                {
                    var factory = new MqttFactory();
                    var client = factory.CreateMqttClient();
                    var clientOptions =
                        new MqttClientOptionsBuilder().
                            WithWebSocketServer(o=>
                            {
                                if (!(endpoint is AzureSignalREndPoint azEndpoint))
                                {
                                    throw new NotSupportedException($"{endpoint} is not supported");
                                }

                                o.Uri = azEndpoint.Uri.ToString().Replace("https://", "wss://");//"localhost:5001/mqtt"; // azEndpoint.Uri.AbsoluteUri;
                                //o.Uri = "localhost:5001/mqtt"; // azEndpoint.Uri.AbsoluteUri;
                                o.RequestHeaders = new Dictionary<string, string>();
                                o.RequestHeaders["Authorization"] = $"Bearer {azEndpoint.AccessToken}";
                            }).Build();


                    client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                    {
                        Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                        Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                        Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                        Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                        Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                        Console.WriteLine();
                    });

                    client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                    {
                        Console.WriteLine("### CONNECTED WITH SERVER ###");

                        await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());

                        Console.WriteLine("### SUBSCRIBED ###");
                    });

                    client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(async e =>
                    {
                        Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        try
                        {
                            await client.ConnectAsync(clientOptions);
                        }
                        catch
                        {
                            Console.WriteLine("### RECONNECTING FAILED ###");
                        }
                    });

                    try
                    {
                        await client.ConnectAsync(clientOptions);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                    }

                    Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                    while (true)
                    {
                        Console.ReadLine();

                        await client.SubscribeAsync(new TopicFilter { Topic = "test", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

                        var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("A/B/C")
                            .WithPayload("Hello World")
                            .WithAtLeastOnceQoS()
                            .Build();

                        await client.PublishAsync(applicationMessage);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }
}
