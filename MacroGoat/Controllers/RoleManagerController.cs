using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MacroGoat.Controllers
{
    public class RoleManagerController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        
        public RoleManagerController(RoleManager<IdentityRole> rm)
        {
            _roleManager = rm;
        }

        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (roleName != null)
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            }
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> DeleteRole(List<string> roleId)
        {
            if (roleId != null)
            {
                foreach (var role in roleId)
                {
                    var deleteRole = await _roleManager.FindByIdAsync(role);
                    if (deleteRole != null)
                    {
                        await _roleManager.DeleteAsync(deleteRole);
                    }
                }


            }
            return RedirectToAction("Index");
        }
    }
}
