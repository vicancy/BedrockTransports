using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Adapter;
using MQTTnet.Channel;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using BedrockTransports;

namespace MQTTnet.Server.Sample
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

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MqttConnectionHandler>();
            serviceCollection.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttConnectionHandler>());
            serviceCollection.AddHostedMqttServer<MqttServerOptions>();
            var provider = serviceCollection.BuildServiceProvider();

            var mqttServer = provider.GetRequiredService<MqttHostedServer>();
            
            mqttServer.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                Console.WriteLine(
                    $"'{e.ClientId}' reported '{e.ApplicationMessage.Topic}' > '{Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? new byte[0])}'",
                    ConsoleColor.Magenta);
            });

            mqttServer.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e =>
            {
                Console.Write($"Client {e.ClientId} connected.");
            });

            _ = mqttServer.StartAsync(token);

            var handler = provider.GetRequiredService<MqttConnectionHandler>();

            var server = new AzureSignalRConnectionListenerFactory(loggerFactory, true);
            var endpoint = new AzureSignalREndPoint("Endpoint=http://localhost;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;Port=8080;Version=1.0;"
                , "chat", AzureSignalREndpointType.Server, true);
            
            // Connect to the server endpoint
            var listener = await server.BindAsync(endpoint);
            Console.WriteLine($"Listening on {endpoint}");

            var serverTask = RunServerAsync(handler, listener, token);

            Console.WriteLine("Server running, open client to see");

            await serverTask;

        }

        private static async Task RunServerAsync(MqttConnectionHandler handler, IConnectionListener listener, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var connection = await listener.AcceptAsync(cancellationToken);

                Console.WriteLine("New Connection " + connection.ConnectionId);

                _ = handler.OnConnectedAsync(connection);
            }

        }
    }
}
