using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignerApi.Models;
using SignerApi.Util;

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
            int checkInterval = Int32.Parse(_conf.GetSection("AnalyseService")["checkIntervalSeconds"]);
            _l.Debug($"Analyse Service will check for new entries each {checkInterval} seconds.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _l.Debug("Analyse Service checking DB for new items to be analysed...");
                var analyseItems = _asvc.getItemsToBeAnalysed();
                foreach (var ac in analyseItems)
                {
                    _l.Debug($"Fetched item {ac.UniqueKey} for analysing. Setting status to {ApiActivity.ApiResult.InProgress}...");
                    ac.Result = ApiActivity.ApiResult.InProgress;
                    _asvc.addUpdateApiActivity(ac);
                }




                await Task.Delay(1000 * checkInterval, stoppingToken);
            }


            
            

            // new Analyser Service start e.g. VirusTotal

            // scan

            // update AC

            // Sign

            // create download link in AC

        }
    }
}
