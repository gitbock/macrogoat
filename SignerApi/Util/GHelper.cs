using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileSignatures;
using FileSignatures.Formats;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SignerApi.Data;
using SignerApi.Models;

namespace SignerApi.Util
{
    /// <summary>
    /// Multiple Used helper methods
    /// </summary>
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
            string systemFolder = Path.Combine(webHostEnv.ContentRootPath, webProfilePicDir);
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
            string systemFolder = Path.Combine(webHostEnv.ContentRootPath, webProfilePicDir);
            return systemFolder;
        }


        public enum ExtensionType
        {
            OfficeFile,
            CertFile
        }

        /// <summary>
        /// Checks if file extension is valid for extension type
        /// </summary>
        /// <param name="extype">which file typ to check</param>
        /// <param name="fileExtension">the extension to check against file type</param>
        /// <returns></returns>
        public static bool fileHasAllowedExtension(ExtensionType extype, string fileExtension)
        {
            List<string> allowedExtensions = new List<string>();
            if(extype == ExtensionType.OfficeFile)
            {
                allowedExtensions.Add(".xls");
                allowedExtensions.Add(".xlsm");
                allowedExtensions.Add(".xlsx");
                allowedExtensions.Add(".doc");
                allowedExtensions.Add(".docx");
                allowedExtensions.Add(".docm");
                
                
            }
            if (extype == ExtensionType.CertFile)
            {
                allowedExtensions.Add(".pfx");
                allowedExtensions.Add(".p12");
            }

            return allowedExtensions.Contains(fileExtension);
        }


        /// <summary>
        /// Checks magic number of office file or cert file
        /// </summary>
        /// <param name="extype">Set desired extension type</param>
        /// <param name="fs">filestream to check</param>
        /// <returns></returns>
        public static bool fileHasValidFormat(ExtensionType extype, Stream fs)
        {
            bool valid = false;
            var fileinspector = new FileFormatInspector();
            FileFormat fileFormat;

            if(extype == ExtensionType.OfficeFile) { 
                using (fs)
                {
                    fileFormat = fileinspector.DetermineFileFormat(fs);
                    if (fileFormat is OfficeOpenXml)
                    {
                        valid = true;
                    }
                }
            }


            // cert file format not known by magic number, so at least check if not type of following files
            // like a blacklist
            if (extype == ExtensionType.CertFile)
            {
                using (fs)
                {
                    fileFormat = fileinspector.DetermineFileFormat(fs);
                    if ( (!(fileFormat is OfficeOpenXml)) &&
                         (!(fileFormat is Image)) &&
                         (!(fileFormat is Executable))
                        )
                    {
                        valid = true;
                    }
                }
            }


            return valid;
        }

        public enum UrlType
        {
            DownloadUrl,
            StatusUrl
        }
        
        public static string generateUrl(UrlType urltype, ApiActivity ac, ControllerBase contr )
        {
            var scheme = contr.Url.ActionContext.HttpContext.Request.Scheme;
            var host = contr.Url.ActionContext.HttpContext.Request.Host;
            var apiPath = contr.HttpContext.Request.Path.Value.Substring(0, contr.HttpContext.Request.Path.Value.LastIndexOf("/"));

            if (urltype == UrlType.DownloadUrl)
            {
                var downloadApiPath = $"{apiPath}/DownloadFile";
                return  $"{scheme}://{host}{downloadApiPath}/{ac.UniqueKey}";
            }
            if (urltype == UrlType.StatusUrl)
            {
                var statusApiPath = $"{apiPath}/Status";
                return  $"{scheme}://{host}{statusApiPath}/{ac.UniqueKey}";
            }
            return "URL could not be generated";
        }


    }
}
