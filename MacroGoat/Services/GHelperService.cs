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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacroGoat.Services
{
    public class GHelperService
    {
        private readonly IConfiguration _conf;
        private readonly IWebHostEnvironment _webenv;

        //own settings file in memory
        public MgSettings MgSettings { get; set; }


        // Logger object can be saved later
        public ILogger l { get; set; }

        public GHelperService(IConfiguration conf, IWebHostEnvironment webenv)
        {
            _conf = conf;
            _webenv = webenv;
            readMgSettings();
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
            return MgSettings.FileSettings.ProfilePicturePath.Value;

        }


        /// <summary>
        /// returns absolut system path of profilepictures dir. Use for file system operations
        /// </summary>
        /// <returns>path</returns>
        public  string getProfilePicturesSystemDir()
        {
            return Path.Combine(_webenv.WebRootPath, getProfilePicturesWebserverDir());
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

        /// <summary>
        /// Reads the own settings file and holds it in local variable
        /// </summary>
        private void readMgSettings()
        {
            //check if secrets file already present and create if not
            string settingsFilename = "mgsettings.json";
            if (File.Exists(settingsFilename))
            {
                //read secrets config
                this.MgSettings = JsonConvert.DeserializeObject<MgSettings>(File.ReadAllText(settingsFilename));
                if (this.MgSettings == null)
                {
                    logError($"Settings File {settingsFilename} could not be parsed properly!");
                }
            }
            else
            {
                logError($"Settings File {settingsFilename} not found!");
            }

        }

        
        public void writeMgSettings(MgSettings newSettings)
        {
            //Description must not be overwritten. Better way??
            newSettings.DeleteFilesAfterSign.Description = MgSettings.DeleteFilesAfterSign.Description;
            newSettings.FileSettings.ProfilePicturePath.Description = MgSettings.FileSettings.ProfilePicturePath.Description;
            newSettings.ApiSettings.AdhocSignerURL.Description = MgSettings.ApiSettings.AdhocSignerURL.Description;
            newSettings.ApiSettings.SignerURL.Description = MgSettings.ApiSettings.SignerURL.Description;
            newSettings.ApiSettings.VerifyURL.Description = MgSettings.ApiSettings.VerifyURL.Description;

            
            using (StreamWriter file = File.CreateText(@"mgsettings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, newSettings);
            }

            //after writing re-read to update in-memory settings
            readMgSettings();
        }

        

    }
}
