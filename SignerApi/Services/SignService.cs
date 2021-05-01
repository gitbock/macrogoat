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


            await Task.Delay(1000 * checkInterval, stoppingToken);

        }
    }
}
