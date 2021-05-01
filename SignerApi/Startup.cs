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

namespace SignerApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

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

            app.UseHttpsRedirection();

            app.UseRouting();

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
