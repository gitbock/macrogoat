using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MacroGoat.Util
{
    public class GHelper
    {
       
        /// <summary>
        /// returns relative path of profile pictures dir. Use within View / html
        /// </summary>
        /// <returns>path</returns>
        public static string getProfilePicturesWebserverDir(IConfiguration conf)
        {
            string path = null;
            path = conf.GetSection("FileConfig")["ProfilePicturePath"];
            return "/" + path; 
        }


        /// <summary>
        /// returns absolut system path of profilepictures dir. Use for file system operations
        /// </summary>
        /// <returns>path</returns>
        public static string getProfilePicturesSystemDir(IWebHostEnvironment webHostEnv, IConfiguration conf)
        {
            var webProfilePicDir = conf.GetSection("FileConfig")["ProfilePicturePath"];
            string systemFolder = Path.Combine(webHostEnv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }

        /// <summary>
        /// Create unique filename for security concerns
        /// </summary>
        /// <param name="rawFilename">file name of uploaded file</param>
        /// <returns>non-enumerateable file name</returns>
        public static string createUniqueFileName(string rawFilename)
        {
            return Guid.NewGuid().ToString() + "_" + rawFilename;
        }

        
        /// <summary>
        /// Returns file path in system of cert files
        /// </summary>
        /// <param name="webHostEnv"></param>
        /// <param name="conf">application config with FilesConfig/CertFilesPath</param>
        /// <returns>System Path of cert files</returns>
        public static string getCertFilesSystemDir(IWebHostEnvironment webHostEnv, IConfiguration conf)
        {
            var webProfilePicDir = conf.GetSection("FileConfig")["CertFilesPath"];
            string systemFolder = Path.Combine(webHostEnv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }

        /// <summary>
        /// Returns file path in system of office files
        /// </summary>
        /// <param name="webHostEnv"></param>
        /// <param name="conf">application config with FilesConfig/OfficeFilesPath</param>
        /// <returns>System Path of Office files</returns>
        public static string getOfficeFilesSystemDir(IWebHostEnvironment webHostEnv, IConfiguration conf)
        {
            var webProfilePicDir = conf.GetSection("FileConfig")["OfficeFilesPath"];
            string systemFolder = Path.Combine(webHostEnv.WebRootPath, webProfilePicDir);
            return systemFolder;
        }


    }
}
