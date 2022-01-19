using System;
using App1.WebApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace App1.WebApi
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
            services.AddControllers();
            services.AddHttpClient();
            services.AddOpenTelemetryTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation()
                    .AddXRayTraceId()
                    .AddHttpClientInstrumentation()
                    .AddSource(nameof(PublishMessageController))
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App1"))
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
