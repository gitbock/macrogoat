using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MacroGoat.Controllers
{
    public class AdminController : Controller
    {

        public AdminController()
        {

        }
        
        [Authorize(Roles = "SuperAdmin,CertAdmin")]
        public IActionResult CertManager()
        {
            return View();
        }


        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Settings()
        {
            return View();
        }
    }
}
