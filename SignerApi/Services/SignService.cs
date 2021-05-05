using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NETCore.Encrypt;
using Newtonsoft.Json.Linq;
using SignerApi.Models;
using SignerApi.Util;
using VirusTotalNet;
using VirusTotalNet.Results;

namespace SignerApi.Services
{
    /// <summary>
    /// Periodically checks DB for entries to be signed
    /// </summary>
    public class SignService : BackgroundService
    {
        private readonly Serilog.ILogger _l;
        private readonly IApiActivityService _asvc;
        private readonly IConfiguration _conf;

        public SignService(Serilog.ILogger logger, IServiceScopeFactory factory, IConfiguration conf)
        {
            _l = logger;
            _conf = conf;
            _asvc = factory.CreateScope().ServiceProvider.GetRequiredService<IApiActivityService>();
        }
        

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Read configs
            int checkInterval = Int32.Parse(_conf.GetSection("SignService")["checkIntervalSeconds"]);

            _l.Debug($"Signing Service started; will check for new entries each {checkInterval} seconds.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var signItems = _asvc.getItemsToBeSigned();
                foreach (var ac in signItems)
                {
                    _l.Debug($"Fetched item {ac.UniqueKey} for signing.");
                    ac.Status = ApiActivity.ApiStatus.Signing;
                    ac.Message = "Signing..";
                    _asvc.addUpdateApiActivity(ac);
                    if (signFile(ac))
                    {
                        ac.Status = ApiActivity.ApiStatus.Ready;
                        // ac.Message already filled by output parser
                        _asvc.addUpdateApiActivity(ac);
                        _l.Information(ac.ToString());
                    }
                    else
                    {
                        // ac.Message Error already filled by output parser
                        ac.Status = ApiActivity.ApiStatus.Error;
                        _asvc.addUpdateApiActivity(ac);
                        _l.Error(ac.ToString());
                    }
                    
                }




                await Task.Delay(1000 * checkInterval, stoppingToken);
            }

        }


        private bool signFile(ApiActivity ac)
        {
            // ---- SIGN FILE

            // prepare run
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Directory.GetCurrentDirectory() + "/lib/signtool.exe";
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            if (ac.EncCertPw != null && ac.EncCertPw.Length > 0)
            {
                // Read secrets --> decrpyt certPW
                JObject secretsConfig = JObject.Parse(System.IO.File.ReadAllText(@"secrets.json")); //secrets.json file not checked in. .gitignore
                var aesKey = (string)secretsConfig["aesKey"];
                var certPw = EncryptProvider.AESDecrypt(ac.EncCertPw, aesKey);
                psi.Arguments = $"sign /debug /v /fd sha256 /f \"{ac.SystemCertFilename}\" /p {certPw} \"{ac.SystemOfficeFilename}\"";
            }
            else
            {
                psi.Arguments = $"sign /debug /v /fd sha256 /f \"{ac.SystemCertFilename}\" \"{ac.SystemOfficeFilename}\"";
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
            ac = SignToolOutputParser.parseSignToolOutput(SignToolOutputParser.SignToolOperation.Sign, ac, stdOut.ToString(), stdErr.ToString());
            if (ac.Status == ApiActivity.ApiStatus.Ready)
            {
                return true;
            }
            else
            {
                return false;
            }



        }
           
    }
}
  