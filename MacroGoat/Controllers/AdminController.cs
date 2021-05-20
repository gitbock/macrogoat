using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using MacroGoat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MacroGoat.Controllers
{
    public class AdminController : Controller
    {
        private readonly GHelperService _hlp;

        public AdminController(GHelperService hlp)
        {
            _hlp = hlp;
        }
       


        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Settings()
        {
            // get instance of MG settings to inject in View later
            return View(_hlp.MgSettings);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Settings(MgSettings settings)
        {
            // save new settings
            if (ModelState.IsValid)
            {
                _hlp.writeMgSettings(settings);
            }
            
            return View(settings);
        }
    }
}
