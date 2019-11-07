using BedrockTransports;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;

namespace MQTTnet.TestApp.AspNetCore2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    .ConfigureKestrel(o =>
                    {
                        o.ListenAnyIP(5001); // default http pipeline
                    });
                }).ConfigureServices((context, services) =>
                {
                    // This is a transport based on the AzureSignalR protocol, it gives you a full duplex mutliplexed connection over the 
                    // the internet
                    // Put your azure SignalR connection string in configuration
                    services.AddAzureSignalRListener("Endpoint=http://localhost;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;Port=8080;Version=1.0"
                    //services.AddAzureSignalRListener("Endpoint=http://localhost;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;Port=8080;Version=1.0;"
                , "chat",
                        builder => builder.UseConnectionHandler<MqttConnectionHandler>());

                });
    }
}
