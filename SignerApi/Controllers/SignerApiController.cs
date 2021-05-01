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
        

        public SignerApiController(IConfiguration conf, IWebHostEnvironment webHostEnv, Serilog.ILogger logger, IApiActivityService asvc)
        {
            _conf = conf;
            _webHostEnv = webHostEnv;
            _l = logger;
            _asvc = asvc;
        }


        [HttpPost]
        [Route("CheckOfficeFile")]
        public IActionResult CheckOfficeFile(IFormFile officeFile)
        {
            ApiActivity ac = new ApiActivity();
            ac.ClientIPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            ac.Operation = ApiActivity.ApiOperation.Verify;
            ac.Message = $"Check Office File started with filename {officeFile.FileName}";
            ac.Status = ApiActivity.ApiStatus.InProgress;
            _asvc.addUpdateApiActivity(ac);
            
            
            if (officeFile != null )
            {
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
                psi.FileName = _webHostEnv.ContentRootPath + "/lib/signtool.exe";
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
                ac = SignToolOutputParser.parseSignToolOutput(SignToolOutputParser.SignToolOperation.Verify, ac, stdOut.ToString(), stdErr.ToString(), officeFile.FileName);
                _l.Debug($"Parsed result = {ac.ToString()}");
                _l.Debug($"Deleting file {systemFileNameOfficeFile}");
                // delete after verify
                System.IO.File.Delete(Path.Combine(systemFileNameOfficeFile));

                ac.UserOfficeFilename = officeFile.FileName;
                _asvc.addUpdateApiActivity(ac);
                
                return Content(ac.getWebresult());

            }
            else
            {
                var message = ("No Files submitted for Verifying!");
                _l.Debug(message);
                ac.Operation = ApiActivity.ApiOperation.Verify;
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = message;
                return Content(ac.getWebresult());
            }

        }


        /// <summary>
        /// Signs an office file by using the supplied cert file. Cert file needs to be *.pfx format
        /// </summary>
        /// <param name="officeFile">A docm, xlsm or similar office file</param>
        /// <param name="certFile">cert file used for signing the office file</param>
        /// <param name="certPw">if cert File is PW protected use this PW to use certificate</param>
        /// <returns>Signed Office File</returns>
        [HttpPost]
        [Route("SignOfficeFile")]
        public IActionResult SignOfficeFile(IFormFile officeFile, IFormFile certFile, [FromForm]string certPw)
        {
            //prepare svr (used for logging / error return as well)
            ApiActivity ac = new ApiActivity();
            ac.ClientIPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            ac.Operation = ApiActivity.ApiOperation.Sign;
            ac.Message = $"Starting signOfficeFile with {officeFile.FileName} and cert file {certFile.FileName}...";
            ac.UserOfficeFilename = officeFile.FileName;
            ac.UserCertFilename = certFile.FileName;
            _asvc.addUpdateApiActivity(ac);
            
            if (officeFile != null && certFile != null)
            {
                if (certPw != null)
                {
                    _l.Debug($"Provided cert PW = {certPw}");
                }
                else
                {
                    _l.Debug($"No cert PW provided!");
                }

                // ---- CHECKS

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
                if( certPw != null && certPw.Length > maxPwLength)
                {
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"Cert Pw exceeding max Length: {maxPwLength}!";
                    _l.Error(ac.Message);
                    _asvc.addUpdateApiActivity(ac);
                    return Content(ac.getWebresult());
                }

                // check non-malware files
                //##### todo

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
                ac.SystemCertFilename = systemFileNameCertFile;

                _asvc.addUpdateApiActivity(ac);

                // create dir if not exist
                System.IO.Directory.CreateDirectory(systemFolderCertFile);
                _l.Debug($"Saving cert file to {systemFileNameCertFile}");
                using (var fileStream = new FileStream(systemFileNameCertFile, FileMode.Create))
                {
                    certFile.CopyTo(fileStream);
                }

                // ---- SIGN FILE

                // prepare run
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _webHostEnv.ContentRootPath + "/lib/signtool.exe";
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                if(certPw != null && certPw.Length > 0)
                {
                    psi.Arguments = $"sign /debug /v /f \"{systemFileNameCertFile}\" /p {certPw} \"{systemFileNameOfficeFile}\"";
                }
                else
                {
                    psi.Arguments = $"sign /debug /v /f \"{systemFileNameCertFile}\" \"{systemFileNameOfficeFile}\"";
                }
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

                // parse result, prepare return json
                ac = SignToolOutputParser.parseSignToolOutput(SignToolOutputParser.SignToolOperation.Sign, ac, stdOut.ToString(), stdErr.ToString(), officeFile.FileName);
                if(ac.Status == ApiActivity.ApiStatus.Success)
                {

                    _l.Debug($"Deleting cert file {systemFileNameCertFile}");
                    // delete cert file after signing
                    System.IO.File.Delete(Path.Combine(systemFileNameCertFile));

                    //inject download link for user in result activity to be returned to user
                    ac.DownloadUrl = GHelper.generateUrl(GHelper.UrlType.DownloadUrl, ac, this);

                    // log activity, successful
                    ac.Status = ApiActivity.ApiStatus.Success;
                    ac.Message = $"Signed file ready download";
                    _asvc.addUpdateApiActivity(ac);

                    return Content(ac.getWebresult());
                }
                else
                {
                    // error signing file; cleanup
                    System.IO.File.Delete(Path.Combine(systemFileNameOfficeFile));
                    System.IO.File.Delete(Path.Combine(systemFileNameCertFile));
                    ac.Message = "Error signing file!";
                    ac.Status = ApiActivity.ApiStatus.Error;
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.ToString());
                    return Content(ac.getWebresult());
                }

                

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


        [HttpPost]
        [Route("RequestSigning")]
        public IActionResult RequestSigning(IFormFile officeFile, IFormFile certFile, [FromForm] string certPw)
        {
            var clientIp = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            var operation = ApiActivity.ApiOperation.RequestSigning;


            // CHECKS

            //prepare ac (used for logging / error return as well)
            ApiActivity ac = new ApiActivity();
            ac.Operation = ApiActivity.ApiOperation.RequestSigning;
            ac.ClientIPAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            ac.Status = ApiActivity.ApiStatus.InProgress;
            

            if (officeFile != null && certFile != null)
            {
                ac.UserOfficeFilename = officeFile.FileName;
                ac.UserCertFilename = certFile.FileName;
                ac.Message = $"Starting request Signing with {officeFile.FileName} and cert file {certFile.FileName}...";
                
                _asvc.addUpdateApiActivity(ac);
                if (certPw != null)
                {
                    _l.Debug($"Provided cert PW = \"{certPw}\"");
                }
                else
                {
                    _l.Debug($"No cert PW provided!");
                }

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
                ac.SystemCertFilename = systemFileNameCertFile;

                // create dir if not exist
                System.IO.Directory.CreateDirectory(systemFolderCertFile);
                _l.Debug($"Saving cert file to {systemFileNameCertFile}");
                using (var fileStream = new FileStream(systemFileNameCertFile, FileMode.Create))
                {
                    certFile.CopyTo(fileStream);
                }


                // START SECURITY ANALYSING THREAD
                ac.Status = ApiActivity.ApiStatus.Queued;
                ac.Message = "File queued for analysis";

                //Status Page for requesting current state
                ac.StatusUrl = GHelper.generateUrl(GHelper.UrlType.StatusUrl, ac, this);

                _asvc.addUpdateApiActivity(ac);


                // RETURN STATUS PAGE
                _l.Debug($"Returning Status page for Key {ac.UniqueKey}");
                return RedirectToAction("Status", new { key = ac.UniqueKey });


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
        /// Returns a File for requested FileKey.
        /// 
        /// </summary>
        /// <param name="filenameKey">GUID Filekey which was provided by Signer API while signing Office File</param>
        /// <returns>Filestream if file for Key exists or JSON error message</returns>
        [HttpGet]
        [Route("DownloadFile/{filenameKey}")]
        public IActionResult DownloadFile(string filenameKey)
        {
            ApiActivity ac = new ApiActivity();
            ac.Operation = ApiActivity.ApiOperation.Download;
            ac.Status = ApiActivity.ApiStatus.InProgress;
            ac.Message = $"Download of file {filenameKey} requested. Checking for file...";
            _asvc.addUpdateApiActivity(ac);

            //check if malicious content, GUID == 36 characters
            if (filenameKey.Length != 36)
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
                var activity = _asvc.getActivityByUniqueKey(filenameKey);
                var systemFileName = activity.SystemOfficeFilename;
                
                //check if file exists
                var storagePath = GHelper.getOfficeFilesSystemDir(_webHostEnv, _conf);
                var fullSystemFileName = Path.Combine(storagePath, systemFileName);
                if (System.IO.File.Exists(fullSystemFileName)){
                    _l.Debug($"Sytemfile {fullSystemFileName} found. Starting sending file...");
                    // send file for download
                    var content = new FileStream(fullSystemFileName, FileMode.Open);
                    var contentType = "APPLICATION/octet-stream";
                    // inject _signed.* in filename
                    var fileNameSigned = Path.GetFileNameWithoutExtension(activity.UserOfficeFilename) + "_signed" + Path.GetExtension(activity.UserOfficeFilename);
                    _l.Debug($"file {fullSystemFileName} found. Sending as {fileNameSigned}");
                    ac.Status = ApiActivity.ApiStatus.Success;
                    ac.Message = $"File {fileNameSigned} sent for download successfully!";
                    _asvc.addUpdateApiActivity(ac);
                    return File(content, contentType, fileNameSigned);
                }
                else
                {
                    //requested file in DB, but not found in filesystem
                    ac.Status = ApiActivity.ApiStatus.Error;
                    ac.Message = $"File for key {filenameKey} not found!";
                    _asvc.addUpdateApiActivity(ac);
                    _l.Error(ac.Message);
                    return Content(ac.getWebresult());
                }

            }
            catch (Exception)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"FileKey {filenameKey} is invalid!";
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
            ac.Status = ApiActivity.ApiStatus.InProgress;
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
            try
            {
                // get DB entry for updating
                ApiActivity requestedStatus = _asvc.getActivityByUniqueKey(key);

                ac.Message = $"entry found: returning status for key {key}";
                ac.Status = ApiActivity.ApiStatus.Success;
                _asvc.addUpdateApiActivity(ac);
                return Content(requestedStatus.getWebresult());
                

            }
            catch (Exception)
            {
                ac.Status = ApiActivity.ApiStatus.Error;
                ac.Message = $"Key {key} is invalid!";
                _asvc.addUpdateApiActivity(ac);
                _l.Error(ac.Message);
                return Content(ac.getWebresult());

            }




        }


    }
}
