using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NETCore.Encrypt;
using Newtonsoft.Json.Linq;
using Serilog.Context;
using SignerApi.Data;
using SignerApi.Models;
using SignerApi.Services;
using SignerApi.Util;

namespace SignerApi.Controllers
{
    [Route("api/Signer")]
    [Produces("application/json")]
    [ApiController]
    public class SignerApiController : ControllerBase
    {
        private readonly IConfiguration _conf;
        private readonly IWebHostEnvironment _webHostEnv;
        private readonly Serilog.ILogger _l;
        private readonly IApiActivityService _asvc;
        private readonly IHttpContextAccessor _httpctx;


        public SignerApiController(IConfiguration conf, IWebHostEnvironment webHostEnv, Serilog.ILogger logger, IApiActivityService asvc, IHttpContextAccessor httpctx )
        {
            _conf = conf;
            _webHostEnv = webHostEnv;
            _l = logger;
            _asvc = asvc;
            _httpctx = httpctx;
        }


        [HttpPost]
        [Route("Verify")]
        public IActionResult CheckOfficeFile(IFormFile officeFile)
        {
            ApiActivity ac = new ApiActivity();
            ac.ClientIPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            ac.Operation = ApiActivity.ApiOperation.Verify;
            ac.Message = $"Check Office File started";
            ac.Status = ApiActivity.ApiStatus.Verifying;
            ac.StatusUrl = GHelper.generateUrl(GHelper.UrlType.StatusUrl, ac, _httpctx);
            ac.DownloadUrl = GHelper.generateUrl(GHelper.UrlType.DownloadUrl, ac, _httpctx);
            _asvc.addUpdateApiActivity(ac);
            
            
            if (officeFile != null )
            {
                ac.UserOfficeFilename = officeFile.FileName;
                //check valid file extension
                _l.Debug("Checking for valid file extensions...");
                string officeFileExt = Path.GetExtension(officeFile.FileName.ToLowerInvariant());
                if (!GHelper.fileHasAllowedExtension(GHelper.ExtensionType.OfficeFile, officeFileExt))
                {
                    ac.Message = $"Office File extension {officeFileExt} not valid!";
                    ac.Status = ApiActivity.ApiStatus.Error;
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.Message);
                    return Content(ac.getWebresult());
                }


                // check magic number file types
                _l.Debug("Checking for magic number of file...");
                if (!(GHelper.fileHasValidFormat(GHelper.ExtensionType.OfficeFile, officeFile.OpenReadStream())))
                {
                    ac.Message = $"Office File {officeFile.FileName} not a valid office file!";
                    ac.Status = ApiActivity.ApiStatus.Error;
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.Message);
                    return Content(ac.getWebresult());
                }

                //save office file
                string uniFilenameOfficeFile = GHelper.createUniqueFileName(officeFile.FileName);
                string systemFolderOfficeFile = GHelper.getOfficeFilesSystemDir(_webHostEnv, _conf);
                string systemFileNameOfficeFile = Path.Combine(systemFolderOfficeFile, uniFilenameOfficeFile);
                ac.SystemOfficeFilename = systemFileNameOfficeFile;

                // create dir if not exist
                System.IO.Directory.CreateDirectory(systemFolderOfficeFile);
                _l.Debug($"Saving file to {systemFileNameOfficeFile}");
                using (var fileStream = new FileStream(systemFileNameOfficeFile, FileMode.Create))
                {
                    officeFile.CopyTo(fileStream);
                }



                //verify file
                

                // prepare run
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _webHostEnv.ContentRootPath + @"\lib\signtool.exe";
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                //use quoted filename otherwise systempath is revealed in error message!!
                psi.Arguments = $"verify /pa /debug /v \"{systemFileNameOfficeFile}\"";
                _l.Debug($"Executing {psi.FileName} {psi.Arguments}...");

                // execute run
                StringBuilder stdOut = new StringBuilder();
                StringBuilder stdErr = new StringBuilder();
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
                
                while (!p.StandardOutput.EndOfStream)
                {
                    stdOut.AppendLine(p.StandardOutput.ReadLine());
                }
                while (!p.StandardError.EndOfStream)
                {
                    stdErr.AppendLine(p.StandardError.ReadLine());
                }
                p.WaitForExit();
                _l.Debug("Process exited. Parsing...");

                // parse result
                ac = SignToolOutputParser.parseSignToolOutput(SignToolOutputParser.SignToolOperation.Verify, ac, stdOut.ToString(), stdErr.ToString());
                _l.Debug($"Parsed result = {ac.ToString()}");
                _l.Debug($"Deleting file {systemFileNameOfficeFile}");
                // delete after verify
                System.IO.File.Delete(Path.Combine(systemFileNameOfficeFile));

                
                _asvc.addUpdateApiActivity(ac);
                
                return Content(ac.getWebresult());

            }
            else
            {
                var message = ("No Files submitted for Verifying!");
                _l.Warning(message);
                ac.Operation = ApiActivity.ApiOperation.Verify;
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = message;
                return Content(ac.getWebresult());
            }

        }


        /// <summary>
        /// Receives a office file, cert file, optional PW for signing. Also analysis can be requested before signning
        /// 
        /// </summary>
        /// <param name="officeFile">office file including macros to be signed</param>
        /// <param name="certFile">cert file in *.pfx or *.p12 format which should be used to sign office file</param>
        /// <param name="certPw">if cert is PW protected, use this PW to decrypt key in cert. PW will be temporary saved encrypted in DB</param>
        /// <param name="analyse">analysis requested before signing</param>
        /// <returns>JSON Status Page for this activity</returns>
        [HttpPost]
        [Route("RequestSigning")]
        public IActionResult RequestSigning(IFormFile officeFile, IFormFile certFile, [FromForm] string certPw, [FromForm] bool analyse)
        {
            

            //prepare ac (used for logging / error return as well)
            ApiActivity ac = new ApiActivity();
            ac.Operation = ApiActivity.ApiOperation.RequestSigning;
            ac.ClientIPAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            
            ac.StatusUrl = GHelper.generateUrl(GHelper.UrlType.StatusUrl, ac, _httpctx);
            ac.DownloadUrl = GHelper.generateUrl(GHelper.UrlType.DownloadUrl, ac, _httpctx);


            if (officeFile != null && certFile != null)
            {
                ac.UserOfficeFilename = officeFile.FileName;
                ac.UserCertFilename = certFile.FileName;
                ac.Message = $"Starting request Signing with {officeFile.FileName} and cert file {certFile.FileName}...";
                
                _asvc.addUpdateApiActivity(ac);
                if (certPw != null)
                {
                    _l.Debug($"Provided cert PW = \"{certPw}\"");
                    // Read secrets
                    JObject secretsConfig = JObject.Parse(System.IO.File.ReadAllText(@"secrets.json")); //secrets.json file not checked in. .gitignore
                    var aesKey = (string)secretsConfig["aesKey"];
                    var encryptedCertPw = EncryptProvider.AESEncrypt(certPw, aesKey);
                    // save pw encrypted in DB
                    ac.EncCertPw = encryptedCertPw;
                }
                else
                {
                    _l.Debug($"No cert PW provided!");
                }

                //------- CHECKS

                //check for valid file extension
                string officeFileExt = Path.GetExtension(officeFile.FileName.ToLowerInvariant());
                if (!GHelper.fileHasAllowedExtension(GHelper.ExtensionType.OfficeFile, officeFileExt))
                {

                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Office File extension {officeFileExt} not valid!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());
                }
                string certFileExt = Path.GetExtension(certFile.FileName.ToLowerInvariant());
                if (!GHelper.fileHasAllowedExtension(GHelper.ExtensionType.CertFile, certFileExt))
                {
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Certificate File extension {certFileExt} not valid!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());

                }

                // check magic number file types
                if (!(GHelper.fileHasValidFormat(GHelper.ExtensionType.OfficeFile, officeFile.OpenReadStream())))
                {
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Office File {officeFile.FileName} not a valid office file!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());
                }
                if (!(GHelper.fileHasValidFormat(GHelper.ExtensionType.CertFile, certFile.OpenReadStream())))
                {
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Cert File {certFile.FileName} not a valid cert file!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());
                }

                // check PW field
                int maxPwLength = Int32.Parse(_conf.GetSection("Security")["MaxCertPwLength"]);
                if (certPw != null && certPw.Length > maxPwLength)
                {
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Cert Pw exceeding max Length: {maxPwLength}!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());
                }

                // SAVE FILES
                //save office file with unique filename, not enumerable
                string uniFilenameOfficeFile = GHelper.createUniqueFileName(officeFile.FileName);
                string systemFolderOfficeFile = GHelper.getOfficeFilesSystemDir(_webHostEnv, _conf);
                string systemFileNameOfficeFile = Path.Combine(systemFolderOfficeFile, uniFilenameOfficeFile);
                ac.SystemOfficeFilename = systemFileNameOfficeFile;

                // create dir if not exist
                System.IO.Directory.CreateDirectory(systemFolderOfficeFile);
                _l.Debug($"Saving Office file to {systemFolderOfficeFile}");
                using (var fileStream = new FileStream(systemFileNameOfficeFile, FileMode.Create))
                {
                    officeFile.CopyTo(fileStream);

                }

                //save cert file with unique filename, not enumerable
                string uniFilenameCertFile = GHelper.createUniqueFileName(certFile.FileName);
                string systemFolderCertFile = GHelper.getCertFilesSystemDir(_webHostEnv, _conf);
                string systemFileNameCertFile = Path.Combine(systemFolderCertFile, uniFilenameCertFile);
                systemFileNameCertFile = systemFileNameCertFile.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                ac.SystemCertFilename = systemFileNameCertFile;

                // create dir if not exist
                System.IO.Directory.CreateDirectory(systemFolderCertFile);
                _l.Debug($"Saving cert file to {systemFileNameCertFile}");
                using (var fileStream = new FileStream(systemFileNameCertFile, FileMode.Create))
                {
                    certFile.CopyTo(fileStream);
                }

                if (analyse)
                {
                    // Queue foor ANALYSING
                    ac.Status = ApiActivity.ApiStatus.QueuedAnalysis;
                    ac.Message = "File queued for analysis";
                    _asvc.addUpdateApiActivity(ac);
                    _l.Debug("Analysis requested, queuing for analysis...");
                }
                else
                {
                    // Queue for SIGNING
                    ac.Status = ApiActivity.ApiStatus.QueuedSigning;
                    ac.Message = "File queued for signing";
                    _asvc.addUpdateApiActivity(ac);
                    _l.Debug("NO analysis requested, queuing for signing at once...");
                }


                // RETURN STATUS PAGE
                _l.Debug($"Returning Queued API Status for Key {ac.UniqueKey}");
                return Content(ac.getWebresult());


            }
            else
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = "Office File or Cert File not submitted. Both required for signing!";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }

        }


        /// <summary>
        /// Receives an office file, which is signed with settings and certificate tied to the profileName
        /// Profile is defined in appsettings.json file
        /// </summary>
        /// <param name="officeFile">office file including macros to be signed</param>
        /// <param name="profileName">ProfileName to be used for signing</param>
        /// <param name="analyse">analysis requested before signing</param>
        /// <returns>JSON Status Page for this activity</returns>
        [HttpPost]
        [Route("RequestSigning/{profileName}")]
        public IActionResult RequestSigning(IFormFile officeFile, [FromForm] bool analyse, string profileName)
        {
            //prepare ac (used for logging / error return as well)
            ApiActivity ac = new ApiActivity();
            ac.Operation = ApiActivity.ApiOperation.RequestSigning;
            ac.ClientIPAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            ac.StatusUrl = GHelper.generateUrl(GHelper.UrlType.StatusUrl, ac, _httpctx);
            ac.DownloadUrl = GHelper.generateUrl(GHelper.UrlType.DownloadUrl, ac, _httpctx);

            if (officeFile == null)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = "Office File not submitted. Required for signing!";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }

            ac.UserOfficeFilename = officeFile.FileName;
            ac.Message = $"Starting request Signing with {officeFile.FileName} and profile ID {profileName}...";
            
            //--- check if valid profile name was provided

            //check if secrets file present
            string secretFilename = "secrets.json";
            if (!System.IO.File.Exists(secretFilename))
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = "secrets file not found for reading profiles";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());

            }

            //read secrets config
            JObject jsonConfig = JObject.Parse(System.IO.File.ReadAllText(secretFilename));

            var profileCertFile = (string)jsonConfig["SigningProfiles"][profileName]["CertFile"];
            var profileCertPw = (string)jsonConfig["SigningProfiles"][profileName]["CertPw"];

            if( profileCertFile == null || profileCertPw == null)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"No certfile or certPW found for Profilename {profileName}";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }
            ac.UserCertFilename = profileCertFile;
            

            //check if cert file from settings is really on filesystem
            var systemCertFileName = Path.Combine(GHelper.getCertFilesSystemDir(_webHostEnv, _conf), ac.UserCertFilename);
            if (!System.IO.File.Exists(systemCertFileName)){
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"certfile {ac.SystemCertFilename} not found for Profilename {profileName}";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }
            ac.SystemCertFilename = systemCertFileName;

            // Save certPW encyrpted in AC, to be decrypted by signer service later. 
            // todo: better PW handling -> was already in cleartext in secrets file
            _l.Debug($"Provided cert PW = \"{profileCertPw}\"");
            // Read secrets
            JObject secretsConfig = JObject.Parse(System.IO.File.ReadAllText(@"secrets.json")); //secrets.json file not checked in. .gitignore
            var aesKey = (string)secretsConfig["aesKey"];
            var encryptedCertPw = EncryptProvider.AESEncrypt(profileCertPw, aesKey);
            // save pw encrypted in DB
            ac.EncCertPw = encryptedCertPw;


            //------- CHECKS

            //check for valid file extension
            string officeFileExt = Path.GetExtension(officeFile.FileName.ToLowerInvariant());
            if (!GHelper.fileHasAllowedExtension(GHelper.ExtensionType.OfficeFile, officeFileExt))
            {

                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"Office File extension {officeFileExt} not valid!";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }

            // check magic number file types
            if (!(GHelper.fileHasValidFormat(GHelper.ExtensionType.OfficeFile, officeFile.OpenReadStream())))
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"Office File {officeFile.FileName} not a valid office file!";
                _l.Error(ac.Message);
                _asvc.addUpdateApiActivity(ac);
                return Content(ac.getWebresult());
            }


            // SAVE FILES
            //save office file with unique filename, not enumerable
            string uniFilenameOfficeFile = GHelper.createUniqueFileName(officeFile.FileName);
            string systemFolderOfficeFile = GHelper.getOfficeFilesSystemDir(_webHostEnv, _conf);
            string systemFileNameOfficeFile = Path.Combine(systemFolderOfficeFile, uniFilenameOfficeFile);
            

            // create dir if not exist
            System.IO.Directory.CreateDirectory(systemFolderOfficeFile);
            _l.Debug($"Saving Office file to {systemFolderOfficeFile}");
            using (var fileStream = new FileStream(systemFileNameOfficeFile, FileMode.Create))
            {
                officeFile.CopyTo(fileStream);
            }
            ac.SystemOfficeFilename = systemFileNameOfficeFile;

            if (analyse)
            {
                // Queue for ANALYSING
                ac.Status = ApiActivity.ApiStatus.QueuedAnalysis;
                ac.Message = "File queued for analysis";
                _asvc.addUpdateApiActivity(ac);
                _l.Debug("Analysis requested, queuing for analysis...");
            }
            else
            {
                // Queue for SIGNING
                ac.Status = ApiActivity.ApiStatus.QueuedSigning;
                ac.Message = "File queued for signing";
                _asvc.addUpdateApiActivity(ac);
                _l.Debug("NO analysis requested, queuing for signing at once...");
            }

            // RETURN STATUS 
            _l.Debug($"Returning Queued API Status for Key {ac.UniqueKey}");
            return Content(ac.getWebresult());
        }
    


        /// <summary>
        /// Returns a File for requested key.
        /// 
        /// </summary>
        /// <param name="key">GUID key which was provided by Signer API while signing Office File</param>
        /// <returns>Filestream if file for Key exists or JSON error message</returns>
        [HttpGet]
        [Route("DownloadFile/{key}")]
        public IActionResult DownloadFile(string key)
        {
            ApiActivity ac = new ApiActivity();
            ac.Operation = ApiActivity.ApiOperation.Download;
            ac.Status = ApiActivity.ApiStatus.Ready;
            ac.Message = $"Download of file {key} requested. Checking for file...";
            _asvc.addUpdateApiActivity(ac);

            //check if malicious content, GUID == 36 characters
            if (key.Length != 36)
            {
                ac.Message = $"key provided is not a valid key!";
                ac.Status = ApiActivity.ApiStatus.Error;
                _asvc.addUpdateApiActivity(ac);
                _l.Error(ac.Message);
                return Content(ac.getWebresult());
            }

            //check if file ID exixts / file present
            try
            {
                var activity = _asvc.getActivityByUniqueKey(key);
                var systemFileName = activity.SystemOfficeFilename;

                //if file not ready (still queued or signing) no download!
                if(activity.Status != ApiActivity.ApiStatus.Ready)
                {
                    ac.Message = $"File Status of {key} is {activity.Status}. not ready for download yet!";
                    ac.Status = ApiActivity.ApiStatus.Error;
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.Message);
                    return Content(ac.getWebresult());
                }
                
                //check if file exists
                var storagePath = GHelper.getOfficeFilesSystemDir(_webHostEnv, _conf);
                var fullSystemFileName = Path.Combine(storagePath, systemFileName);
                if (System.IO.File.Exists(fullSystemFileName)){
                    _l.Debug($"System file {fullSystemFileName} found. Starting sending file...");
                    // send file for download
                    var content = new FileStream(fullSystemFileName, FileMode.Open);
                    var contentType = "APPLICATION/octet-stream";
                    // inject _signed.* in filename
                    var fileNameSigned = Path.GetFileNameWithoutExtension(activity.UserOfficeFilename) + "_signed" + Path.GetExtension(activity.UserOfficeFilename);
                    _l.Debug($"file {fullSystemFileName} found. Sending as {fileNameSigned}");
                    ac.Status = ApiActivity.ApiStatus.Ready;
                    ac.Message = $"File {fileNameSigned} sent for download successfully!";
                    _asvc.addUpdateApiActivity(ac);
                    return File(content, contentType, fileNameSigned);
                }
                else
                {
                    //requested file in DB, but not found in filesystem
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"File for key {key} not found!";
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.Message);
                    return Content(ac.getWebresult());
                }

            }
            catch (Exception)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"FileKey {key} is invalid!";
                _asvc.addUpdateApiActivity(ac);
                _l.Error(ac.Message);
                return Content(ac.getWebresult());
                
            }

            


        }



        /// <summary>
        /// Returns Status for requested key.
        /// 
        /// </summary>
        /// <param name="key">GUID key which was provided by Signer API while requesting Office File Signing</param>
        /// <returns>JSON status message</returns>
        [HttpGet]
        [Route("Status/{key}")]
        public IActionResult Status(string key)
        {

            ApiActivity ac = new ApiActivity(); // new entry -> Status requested 
            ac.Operation = ApiActivity.ApiOperation.Status;
            ac.Status = ApiActivity.ApiStatus.Ready;
            ac.Message = $"Status requested for key {key}, unique Key for this status request {ac.UniqueKey}";
            _asvc.addUpdateApiActivity(ac);
            _l.Debug(ac.Message);
            //check if malicious content, GUID == 36 characters
            if (key.Length != 36)
            {
                ac.Message = $"key provided is not a valid key!";
                ac.Status = ApiActivity.ApiStatus.Error;
                _asvc.addUpdateApiActivity(ac);
                _l.Error(ac.Message);
                return Content(ac.getWebresult());
            }

            //check if ID exixts / activity present
            // get DB entry for updating
            ApiActivity requestedStatus = _asvc.getActivityByUniqueKey(key);
            if(requestedStatus == null)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"Key {key} is invalid!";
                _asvc.addUpdateApiActivity(ac);
                _l.Error(ac.Message);
                return Content(ac.getWebresult());
            }
            else
            {
                ac.Message = $"file found: returning status for key {key}";
                ac.Status = ApiActivity.ApiStatus.Ready;
                _asvc.addUpdateApiActivity(ac);
                return Content(requestedStatus.getWebresult());
            }
            
                
                

           
                

            




        }


    }
}
