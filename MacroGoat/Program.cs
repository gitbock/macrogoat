using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using MacroGoat.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MacroGoat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            // split Create from Run() to execute seeding with already injected services
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                // get services which were already registered ago by createHostBuilder and startup
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var helper = services.GetRequiredService<GHelperService>();
                
                // create default logging to be used througout helper service
                helper.l = loggerFactory.CreateLogger<Program>();

                try
                {
                    
                    var userManager = services.GetRequiredService<UserManager<GUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();


                    // Seed default roles and User User if not exist
                    helper.logInfo("Seeding default values to DB if needed... ");
                    await helper.SeedDefaultRoles(roleManager);
                    await helper.SeedDefaultSuperAdmin(userManager);
                    
                }
                catch (Exception ex)
                {
                    helper.logError($"An error occurred seeding the DB {ex.Message}");
                }
            }

            // finally run after seeding
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
