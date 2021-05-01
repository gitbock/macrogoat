using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using SignerApi.Models;
using SignerApi.Util;
using VirusTotalNet;
using VirusTotalNet.Results;

namespace SignerApi.Services
{
    /// <summary>
    /// Periodically checks DB for entries to be analysed
    /// </summary>
    public class AnalyseService : BackgroundService
    {
        private readonly Serilog.ILogger _l;
        private readonly IApiActivityService _asvc;
        private readonly IConfiguration _conf;

        public AnalyseService(Serilog.ILogger logger, IServiceScopeFactory factory, IConfiguration conf)
        {
            _l = logger;
            _conf = conf;
            _asvc = factory.CreateScope().ServiceProvider.GetRequiredService<IApiActivityService>();
        }
        

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Read configs
            int checkInterval = Int32.Parse(_conf.GetSection("AnalyseService")["checkIntervalSeconds"]);

            // Read secrets
            JObject secretsConfig = JObject.Parse(File.ReadAllText(@"secrets.json")); //secrets.json file not checked in. .gitignore
            var vtApiKey = (string)secretsConfig["ApiKeys"]["VirusTotal"];

            // Init analyserPlugins
            VirusTotal vt = new VirusTotal(vtApiKey);
            vt.UseTLS = true;
            


            _l.Debug($"Analyse Service will check for new entries each {checkInterval} seconds.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _l.Debug("Analyse Service checking DB for new items to be analysed...");
                var analyseItems = _asvc.getItemsToBeAnalysed();
                foreach (var ac in analyseItems)
                {
                    _l.Debug($"Fetched item {ac.UniqueKey} for analysing.");

                    //prepare filestream
                    using (Stream fs = File.OpenRead(ac.SystemOfficeFilename))
                    {
                        // new Analyser Service start e.g. VirusTotal
                        ac.Message = $"Checking Virustotal for known file {ac.UserOfficeFilename}...";
                        _asvc.addUpdateApiActivity(ac);
                        _l.Debug(ac.Message);

                        // first check if file already known to VT
                        var fileReport = await vt.GetFileReportAsync(fs);
                        _l.Debug($"File Report requested for Resource {fileReport.Resource}");
                        ScanResult scanResult = null;

                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Queued)
                        {
                            // file already submitted but still scanned -> not scanning again
                            _l.Debug($"File {fileReport.SHA256} already submitted. Not scanning again...");
                        }

                        
                        
                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.NotPresent)
                        {
                            // reset stream, otherwise it's posted from last position :(
                            fs.Seek(0, SeekOrigin.Begin);
                            _l.Debug($"file not known to VT yet. ");
                            //not known to VT -> start new scan
                            ac.Message = "File not know to VT yet. Start Scan...";
                            _asvc.addUpdateApiActivity(ac);
                            _l.Debug(ac.Message);
                            scanResult = await vt.ScanFileAsync(fs, ac.SystemOfficeFilename);
                            if (scanResult.ResponseCode == VirusTotalNet.ResponseCodes.ScanFileResponseCode.Queued)
                            {
                                // set to result queued to be picked up by loop next time; then Results should be already known 
                                // and can be retrieved.
                                ac.Message = $"File Queued for scan with ScanID {scanResult.ScanId}. Resetting result to {ApiActivity.ApiStatus.Queued}";
                                ac.Status = ApiActivity.ApiStatus.Queued;
                                _asvc.addUpdateApiActivity(ac);
                                _l.Debug(ac.Message);

                            }
                            
                        }
                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Present)
                        {
                            //Filereport here, check
                            ac.Message = $"Filereport retrieved successfully. Resetting Status to {ApiActivity.ApiStatus.InProgress}"; 
                            ac.Status = ApiActivity.ApiStatus.InProgress;
                            _asvc.addUpdateApiActivity(ac);
                            _l.Debug(ac.Message);

                            // how many positives are OK?
                            int maxPositives = Int32.Parse(_conf["AnalyseService:SecurityPlugins:Virustotal:MaxPositives"]);

                            if(fileReport.Positives < maxPositives)
                            {
                                //file clean
                                ac.Message = $"file scanned by VT has {fileReport.Positives} of max {maxPositives}. File clean!";
                                _asvc.addUpdateApiActivity(ac);
                                _l.Debug(ac.Message);

                            }

                        }

                    }
                }




                await Task.Delay(1000 * checkInterval, stoppingToken);
            }


            
            

            

        }
    }
}
