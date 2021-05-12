using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Data;
using MacroGoat.Models;
using MacroGoat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace MacroGoat
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webenv)
        {
            Configuration = configuration;
            WebHostEnvironment = webenv;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();

            // SQLite DB 
            services.AddDbContext<DataContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteCon")));

            // initialize with custom User Model
            services.AddIdentity<GUser, IdentityRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders()
                ;

            // for sending mails by user creation / username change
            services.AddTransient<IEmailSender, GEmailSender>();

            // register own Helper Service. Can always be called executing own methods which need to use webenv/conf
            services.AddSingleton<GHelperService>(new GHelperService(Configuration, WebHostEnvironment));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // for sign in manager auto-logon
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
