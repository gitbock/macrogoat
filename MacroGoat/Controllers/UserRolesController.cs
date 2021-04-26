using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MacroGoat.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly UserManager<GUser> _usrmgr;
        private readonly RoleManager<IdentityRole> _rolemgr;

        public UserRolesController(UserManager<GUser> usrmgr, RoleManager<IdentityRole> rolemgr)
        {
            _rolemgr = rolemgr;
            _usrmgr = usrmgr;
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var allUsers = _usrmgr.Users.ToList<GUser>();
            
            //holds all users incl. roles for viewing in View
            List<UserRolesViewModel> allUsersMV = new List<UserRolesViewModel>();

            foreach (var user in allUsers)
            {
                UserRolesViewModel urvm = new UserRolesViewModel();
                urvm.FirstName = user.FirstName;
                urvm.LastName = user.LastName;
                urvm.UserId = user.Id;
                urvm.UserName = user.UserName;
                urvm.Email = user.Email;

                urvm.Roles = new List<string>(await _usrmgr.GetRolesAsync(user));
                allUsersMV.Add(urvm);
            }


            return View(allUsersMV);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        // shows all Roles by user
        public async Task<IActionResult> Manage(string userId)
        {
            
            // to make selected user available in view later
            ViewBag.UserId = userId;
            
            var user = await _usrmgr.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            ViewBag.UserName = user.UserName;

            List<IdentityRole> allRawRoles = _rolemgr.Roles.ToList();

            List<ManageUserRolesViewModel> allManageRoles = new List<ManageUserRolesViewModel>();
            foreach (var r in allRawRoles)
            {
                allManageRoles.Add(new ManageUserRolesViewModel
                {
                    Role = r.Name,
                    RoleId = r.Id,
                    Selected = await _usrmgr.IsInRoleAsync(user, r.Name) ? true : false
                });
                
            }
            return View(allManageRoles);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> managedRoles, string userId)
        {
            if(managedRoles == null)
            {
                ViewBag.ErrorMessage = $"No Roles to manage";
                return View("NotFound");
            }

            GUser user = await _usrmgr.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User {userId} not found for managing its roles";
                return View("NotFound");
            }

            foreach (var mr in managedRoles)
            {
                if (mr.Selected)
                {
                    await _usrmgr.AddToRoleAsync(user, mr.Role);
                }
                else
                {
                    await _usrmgr.RemoveFromRoleAsync(user, mr.Role);
                }
            }

            
            return RedirectToAction("Index"); // show overview with Roles and Users again
        }

    }
}
