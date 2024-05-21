using DirectScale.Disco.Extension.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })                
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders().AddDirectScaleLogger(configuration =>
                    {
                        configuration.LogLevel = LogLevel.Information;
                    });
                });
    }
}
