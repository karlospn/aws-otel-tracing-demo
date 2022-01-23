using System;
using App3.WebApi.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.S3;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;

namespace App3.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IS3Repository, S3Repository>();
            services.AddTransient<ISqsRepository, SqsRepository>();

            services.AddAWSService<IAmazonS3>(new AWSOptions
            {
                Region = Amazon.RegionEndpoint.EUWest1
            });

            services.AddAWSService<IAmazonSQS>(new AWSOptions
            {
                Region = Amazon.RegionEndpoint.EUWest1
            });

            services.AddControllers().AddNewtonsoftJson();
            services.AddOpenTelemetryTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation()
                    .AddSource(nameof(SqsRepository))
                    .AddXRayTraceId()
                    .AddAWSInstrumentation()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App3"))
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(Configuration["Otlp:Endpoint"]);
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync("Ok");
                });
                endpoints.MapControllers();
            });
        }
    }
}
