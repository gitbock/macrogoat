using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SignerApi.Data;

namespace SignerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();


            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .CreateBootstrapLogger();

            
            Log.Information("Configuring Services in Program");
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                DbContext dbcontext = services.GetRequiredService<DataContext>();

                try
                {
                    // Ensure proper sqlite DB is created
                    dbcontext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred seeding the DB {ex.Message}");
                }
            }
            // finally run after seeding
            host.Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Use serilog as replacement for MS Logging
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration, sectionName: "DotNetCoreLogging")
                    .ReadFrom.Services(services)
                    .WriteTo.Console()) //change writeto.debug for showing in visual studio (?)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
