using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MacroGoat.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly UserManager<GUser> _usrmgr;
        private readonly RoleManager<IdentityRole> _rolemgr;
        private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(UserManager<GUser> usrmgr, RoleManager<IdentityRole> rolemgr, ILogger<UserRolesController> logger)
        {
            _rolemgr = rolemgr;
            _usrmgr = usrmgr;
            _logger = logger;
        }




        // MANAGING USERS


        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UserManager()
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
        public IActionResult CreateUser()
        {
            return View();
        }


        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel newUser)
        {
            if (ModelState.IsValid)
            {
                var user = new GUser { UserName = newUser.Username, Email = newUser.Username, FirstName = newUser.FirstName, LastName = newUser.LastName };
                var result = await _usrmgr.CreateAsync(user, newUser.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    return RedirectToAction("UserManager"); // show overview with Roles and Users again

                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                
            }
            else
            {
                foreach (var modelState in ViewData.ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.ErrorMessage);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View();

        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (userId == null)
            {
                ViewBag.ErrorMessage = $"No User ID to delete to manage";
                return View("NotFound");
            }

            GUser user = await _usrmgr.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User {userId} not found for managing its roles";
                return View("NotFound");
            }

            await _usrmgr.DeleteAsync(user);



            return RedirectToAction("UserManager"); // show overview with Roles and Users again
        }



        // MANAGING ROLES

        [Authorize(Roles = "SuperAdmin")]
        public IActionResult RoleManager()
        {
            var roles = _rolemgr.Roles.ToList();
            return View(roles);
        }


        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        // shows all Roles by user
        public async Task<IActionResult> EditUserRoles(string userId)
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
        public async Task<IActionResult> EditUserRoles(List<ManageUserRolesViewModel> managedRoles, string userId)
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

            
            return RedirectToAction("UserManager"); // show overview with Roles and Users again
        }



        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (roleName != null)
            {
                await _rolemgr.CreateAsync(new IdentityRole(roleName.Trim()));
            }
            return RedirectToAction("RoleManager");
        }


        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> DeleteRole(List<string> roleId)
        {
            if (roleId != null)
            {
                foreach (var role in roleId)
                {
                    var deleteRole = await _rolemgr.FindByIdAsync(role);
                    if (deleteRole != null)
                    {
                        await _rolemgr.DeleteAsync(deleteRole);
                    }
                }


            }
            return RedirectToAction("RoleManager");
        }

    }
}
