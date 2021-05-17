using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FileSignatures;
using SignerApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using SignerApi.Controllers;
using SignerApi.Services;
using SignerApi.Util;
using AspNetCoreRateLimit;

namespace SignerApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public string CorsPolicy = "MacroGoatCorsPolicy";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // needed to load configuration from appsettings.json
            services.AddOptions();

            // needed to store rate limit counters and ip rules
            services.AddMemoryCache();

            //load general configuration from appsettings.json
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //load ip rules from appsettings.json
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            // inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            // Add framework services.
            services.AddMvc();

            // configuration (resolvers, counter key builders)
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Configure CORS to allow origins which can query API by appsettings.json entries
            var section = Configuration.GetSection("CORS:Origins");
            string[] corsOrigins = section.Get<string[]>();
            if (corsOrigins == null)
            {
                //default if not present in config
                corsOrigins = new string[] { "http://localhost", "https://localhost" };
            }
            services.AddCors(options =>
            {
                options.AddPolicy(name: CorsPolicy,
                                  builder =>
                                  {
                                      builder.WithOrigins(corsOrigins);
                                  });
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SignerApi", Version = "v1", Description = "Verify and signs Office files" });

                // Set the comments path for the Swagger JSON and UI.**
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

            });

            // Register own serilog Logger with different configuration as default dotnet core logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration, sectionName: "SignerApiLogging")
                .CreateLogger();
            //Log.logger special static Log object which can be used throughout application (?)
            services.AddSingleton<Serilog.ILogger>(Log.Logger); 

            // SQLite DB 
            services.AddDbContext<DataContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteCon")));

            // seed random AES key in config if not present yet
            GHelper.seedSecrets(Configuration);


            // Register own Database Service
            services.AddTransient<IApiActivityService, ApiActivityService>();

            //Register Analyser Background Service
            services.AddSingleton<IHostedService, AnalyseService>();

            //Register Signing Background Service
            services.AddSingleton<IHostedService, SignService>();

            // for accessing http context in custom components
            services.AddHttpContextAccessor();


            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SignerApi v1"));
            }

            // Rate Limiting Extension
            app.UseIpRateLimiting();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(CorsPolicy);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // for debugging swagger
            app.UseDeveloperExceptionPage();

            

        }
    }
}
