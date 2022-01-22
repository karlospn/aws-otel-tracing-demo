using System;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace App4.SqsConsumer.HostedService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    services.AddAWSService<IAmazonSQS>(new AWSOptions
                    {
                        Region = Amazon.RegionEndpoint.EUWest1
                    });

                    services.AddAWSService<IAmazonDynamoDB>(new AWSOptions
                    {
                        Region = Amazon.RegionEndpoint.EUWest1
                    });

                    services.AddOpenTelemetryTracing(builder =>
                    {
                        var provider = services.BuildServiceProvider();
                        IConfiguration config = provider
                                .GetRequiredService<IConfiguration>();

                        builder.AddAspNetCoreInstrumentation()
                            .AddXRayTraceId()
                            .AddAWSInstrumentation()
                            .AddSource(nameof(Worker))
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App4"))
                            .AddOtlpExporter(opts =>
                            {
                                opts.Endpoint = new Uri(hostContext.Configuration["Otlp:Endpoint"]);
                            });
                    });
                });
    }
}
