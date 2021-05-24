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
            var strCheckInterval = _conf.GetValue<string>("AnalyseService:CheckIntervalSeconds");
            _l.Information($"chekinterval {strCheckInterval}");
            int checkInterval = Int32.Parse(strCheckInterval);

            // Read secrets
            JObject secretsConfig = JObject.Parse(File.ReadAllText(@"secrets.json")); //secrets.json file not checked in. .gitignore
            var vtApiKey = (string)secretsConfig["ApiKeys"]["VirusTotal"];

            // Init analyserPlugins
            VirusTotal vt = new VirusTotal(vtApiKey);
            vt.UseTLS = true;
            


            _l.Debug($"Analyse Service started; will check for new entries each {checkInterval} seconds.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var analyseItems = _asvc.getItemsToBeAnalysed();
                foreach (var ac in analyseItems)
                {
                    _l.Debug($"Fetched item {ac.UniqueKey} for analysing.");
                    ac.Status = ApiActivity.ApiStatus.Analysing;
                    ac.Message = "Analysing";
                    _asvc.addUpdateApiActivity(ac);
                    //prepare filestream
                    using (Stream fs = File.OpenRead(ac.SystemOfficeFilename))
                    {
                        // new Analyser Service start e.g. VirusTotal
                        ac.Message = $"Checking Virustotal for known file {ac.UserOfficeFilename}. This can take up to 5 Min...";
                        _asvc.addUpdateApiActivity(ac);
                        _l.Information(ac.Message);

                        // first check if file already known to VT
                        var fileReport = await vt.GetFileReportAsync(fs);
                        _l.Information($"File Report requested for Resource {fileReport.Resource}");
                        

                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Queued)
                        {
                            // file already submitted but still scanned -> not scanning again
                            _l.Information($"File {ac.UserOfficeFilename} already submitted. Not scanning again. Resetting result to {ApiActivity.ApiStatus.QueuedAnalysis}");
                            ac.Status = ApiActivity.ApiStatus.QueuedAnalysis;
                            _asvc.addUpdateApiActivity(ac);
                        }

                        
                        
                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.NotPresent)
                        {
                            ScanResult scanResult = null;
                            // reset stream, otherwise it's posted from last position :(
                            fs.Seek(0, SeekOrigin.Begin);
                            //not known to VT -> start new scan
                            ac.Message = "File not know to VT yet. Starting Scan...";
                            _asvc.addUpdateApiActivity(ac);
                            _l.Information(ac.Message);
                            scanResult = await vt.ScanFileAsync(fs, ac.SystemOfficeFilename);
                            if (scanResult.ResponseCode == VirusTotalNet.ResponseCodes.ScanFileResponseCode.Queued)
                            {
                                // set to result queued to be picked up by loop next time; then Results should be already known 
                                // and can be retrieved.
                                ac.Message = $"File Queued for Analysis in VirusTotal with ScanID {scanResult.ScanId}.";
                                ac.Status = ApiActivity.ApiStatus.QueuedAnalysis;
                                _asvc.addUpdateApiActivity(ac);
                                _l.Information(ac.Message + $"Resetting result to {ApiActivity.ApiStatus.QueuedAnalysis}");

                            }
                            
                        }
                        if (fileReport.ResponseCode == VirusTotalNet.ResponseCodes.FileReportResponseCode.Present)
                        {
                            //Filereport here, check
                            _l.Information($"Filereport retrieved successfully. Checking if file file clean..");

                            // how many positives are OK?
                            int maxPositives = Int32.Parse(_conf["AnalyseService:SecurityPlugins:Virustotal:MaxPositives"]);

                            if(fileReport.Positives < maxPositives)
                            {
                                //file clean
                                ac.Message = $"File scanned by VT: File has {fileReport.Positives} of max {maxPositives} Positives. File clean! Queued for Signing";
                                ac.Status = ApiActivity.ApiStatus.QueuedSigning; //send to signing service
                                _asvc.addUpdateApiActivity(ac);
                                _l.Information(ac.Message);
                            }
                            else
                            {
                                //file infected
                                ac.Message = $"File scanned by VT: File has {fileReport.Positives} of max {maxPositives} Positives. File infected!! Cancel Signing";
                                ac.Status = ApiActivity.ApiStatus.Error;
                                _asvc.addUpdateApiActivity(ac);
                                _l.Warning(ac.Message);
                            }

                        }

                    }
                }




                await Task.Delay(1000 * checkInterval, stoppingToken);
            }


            
            

            

        }
    }
}
