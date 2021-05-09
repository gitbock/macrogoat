using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace MacroGoat.Util
{
    /// <summary>
    /// Classe seeds Default user and Roles into DB
    /// </summary>
    public static class GDBSeed
    {
        /// <summary>
        /// Default roles to be created on startup
        /// </summary>
        static string[] DefaultRoles =
        {
            "SuperAdmin",
            "Signer",
            "ZertAdmin"
        };

        /// <summary>
        /// Creates default roles
        /// </summary>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public static async Task SeedDefaultRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var r in DefaultRoles)
            {
                if (! await roleManager.RoleExistsAsync(r))
                {
                    await roleManager.CreateAsync(new IdentityRole(r));
                }
            }
        }


        public static async Task SeedDefaultSuperAdmin(UserManager<GUser> usrmgr)
        {
            var allSuperAdminUsers = usrmgr.GetUsersInRoleAsync("SuperAdmin");
            if(allSuperAdminUsers.Result.Count == 0)
            {   
                
                




                //var profileImgDir = GHelper.getProfilePicturesWebserverDir();
                // no Super Admin User exists -> create one
                GUser sa = new GUser
                {
                    Email = "superadmin@example.com",
                    UserName = "superadmin@example.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    FirstName = "Super",
                    LastName = "Admin"                    
                };

                try
                {
                    //Because it's a static class cannot use dependency injection
                    // read config file once
                    //IConfigurationBuilder builder = new ConfigurationBuilder()
                    //.SetBasePath(Directory.GetCurrentDirectory())
                    //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    //IConfigurationRoot conf = builder.Build();


                }
                catch (Exception ex)
                {

                }


                await usrmgr.CreateAsync(sa, "MacroGoat#22");
                await usrmgr.AddToRoleAsync(sa, "SuperAdmin");
                
            }

        }

    }
}
