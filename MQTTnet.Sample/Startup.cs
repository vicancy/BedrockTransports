using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class Startup
    {
        // In class _Startup_ of the ASP.NET Core 2.0 project.

        public void ConfigureServices(IServiceCollection services)
        {
            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithoutDefaultEndpoint()
                .Build();
            services.AddMqttConnectionHandler()
                .AddConnections();
            services.AddControllers();
            services
                .AddHostedMqttServer(mqttServerOptions);

            // tricky part
            services.AddHostedService<TestHost>();
        }

        class TestHost : IHostedService
        {
            public TestHost(MqttHostedServer inner)
            {
                inner.StartedHandler = new MqttServerStartedHandlerDelegate(async args =>
                {
                    Console.WriteLine("Mqtt Host started");
                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload("Mqtt is awesome")
                        .WithTopic("message");
                    _ = Task.Run(async () =>
                    {
                        //while (true)
                        //{
                        //    try
                        //    {
                        //        await inner.PublishAsync(msg.Build());
                        //        msg.WithPayload("Mqtt is still awesome at " + DateTime.Now);
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        Console.WriteLine(e);
                        //    }
                        //    finally
                        //    {
                        //        await Task.Delay(TimeSpan.FromSeconds(2));
                        //    }
                        //}
                    });
                });
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Console.WriteLine("TestHost started");
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        // In class _Startup_ of the ASP.NET Core 2.0 project.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use((context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Request.Path = "/Index.html";
                }

                return next();
            });

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/node_modules",
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "node_modules"))
            });

            app.UseRouting();

            app.UseEndpoints(builder =>
            {
                builder.MapConnectionHandler<MqttConnectionHandler>("/mqtt",
                    options =>
                    {
                        options.WebSockets.SubProtocolSelector =
                            MQTTnet.AspNetCore.ApplicationBuilderExtensions.SelectSubProtocol;
                    });
            });
        }
    }
}
