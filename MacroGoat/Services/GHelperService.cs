using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MacroGoat.Services
{
    public class GHelperService
    {
        private readonly IConfiguration _conf;
        private readonly IWebHostEnvironment _webenv;

        // Logger object can be saved later
        public ILogger l { get; set; }

        public GHelperService(IConfiguration conf, IWebHostEnvironment webenv)
        {
            _conf = conf;
            _webenv = webenv;
        }

        public void logError(string msg)
        {
            if (l != null)
            {
                l.LogError(msg);
            }
        }

        public void logInfo(string msg)
        {
            if (l != null)
            {
                l.LogInformation(msg);
            }
        }


        /// <summary>
        /// returns relative path of profile pictures dir. Use within View / html
        /// </summary>
        /// <returns>path</returns>
        public string getProfilePicturesWebserverDir()
        {
            string path = null;
            path = _conf.GetSection("FileConfig")["ProfilePicturePath"];
            return "/" + path;
        }


        /// <summary>
        /// returns absolut system path of profilepictures dir. Use for file system operations
        /// </summary>
        /// <returns>path</returns>
        public  string getProfilePicturesSystemDir()
        {
            var webProfilePicDir = _conf.GetSection("FileConfig")["ProfilePicturePath"];
            string systemFolder = Path.Combine(_webenv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }

        

        /// <summary>
        /// Returns file path in system of cert files
        /// </summary>
        /// <param name="webHostEnv"></param>
        /// <param name="conf">application config with FilesConfig/CertFilesPath</param>
        /// <returns>System Path of cert files</returns>
        public string getCertFilesSystemDir()
        {
            var webProfilePicDir = _conf.GetSection("FileConfig")["CertFilesPath"];
            string systemFolder = Path.Combine(_webenv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }

        /// <summary>
        /// Returns file path in system of office files
        /// </summary>
        /// <param name="webHostEnv"></param>
        /// <param name="conf">application config with FilesConfig/OfficeFilesPath</param>
        /// <returns>System Path of Office files</returns>
        public string getOfficeFilesSystemDir()
        {
            var webProfilePicDir = _conf.GetSection("FileConfig")["OfficeFilesPath"];
            string systemFolder = Path.Combine(_webenv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }


        /// <summary>
        /// Default roles to be created on startup
        /// </summary>
        public static string[] DefaultRoles =
        {
            "SuperAdmin",
            "Signer",
            "CertAdmin"
        };

        /// <summary>
        /// Creates default roles
        /// </summary>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task SeedDefaultRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var r in DefaultRoles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                {
                    await roleManager.CreateAsync(new IdentityRole(r));
                }
            }
        }


        public async Task SeedDefaultSuperAdmin(UserManager<GUser> usrmgr)
        {
            var allSuperAdminUsers = usrmgr.GetUsersInRoleAsync("SuperAdmin");
            if (allSuperAdminUsers.Result.Count == 0)
            {
                // no Super Admin User exists -> create one
                GUser sa = new GUser
                {
                    Email = "superadmin@example.com",
                    UserName = "superadmin@example.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    FirstName = "Super",
                    LastName = "Admin",
                    ProfilePicture = "bock_schaedel.png"

                };

                await usrmgr.CreateAsync(sa, "MacroGoat#22");
                await usrmgr.AddToRoleAsync(sa, "SuperAdmin");

            }

        }


    }
}
