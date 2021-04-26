using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using MacroGoat.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MacroGoat.Controllers
{
    /// <summary>
    /// For Signing Files
    /// </summary>
    public class SignController : Controller
    {

        private readonly IConfiguration _conf;
        private readonly IWebHostEnvironment _webHostEnv;

        public SignController(IConfiguration conf, IWebHostEnvironment webHostEnv)
        {
            _conf = conf;
            _webHostEnv = webHostEnv;
        }

        /// <summary>
        /// For signing files adhoc without any preconfig.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult AdHocSigner()
        {
            return View();
        }


        

    }
}
