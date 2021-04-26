using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using MacroGoat.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace MacroGoat.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<GUser> _userManager;
        private readonly SignInManager<GUser> _signInManager;
        private readonly IConfiguration _conf;
        private readonly IWebHostEnvironment _webHostEnv;

        public IndexModel(
            UserManager<GUser> userManager,
            SignInManager<GUser> signInManager,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnv)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _conf = configuration;
            _webHostEnv = webHostEnv;
            
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string ProfilesPicturePath { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "First Name")]
            public string FirstName { get; set; }
            [Display(Name = "Last Name")]
            public string LastName { get; set; }
            
            // string only
            [Display(Name = "Profile Picture")]
            public string ProfilePicture { get; set; }

            // image form for posting profile image 
            [Display(Name = "Profile Image")]
            public IFormFile ProfileImage { get; set; }

            [Display(Name = "Delete Profile Picture")]
            public bool DeleteProfilePic { get; set; }
        }

        


        private async Task LoadAsync(GUser user)
        {
            //provide Path in Model to use in html directly
            ProfilesPicturePath = _conf.GetSection("FileConfig")["ProfilePicturePath"];
            
            
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var firstName = user.FirstName;
            var lastName = user.LastName;
            var profilePicture = user.ProfilePicture; // name string only


            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FirstName = firstName,
                LastName = lastName,
                ProfilePicture = profilePicture // name string only
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }


            var firstName = user.FirstName;
            var lastName = user.LastName;
            var profilePicture = user.ProfilePicture;
            if (Input.FirstName != firstName)
            {
                user.FirstName = Input.FirstName;
                await _userManager.UpdateAsync(user);
            }
            if (Input.LastName != lastName)
            {
                user.LastName = Input.LastName;
                await _userManager.UpdateAsync(user);
            }

            // if delete checkbox was set, delete profile picture
            if (Input.DeleteProfilePic && user.ProfilePicture != null)
            {
                string systemFolder = GHelper.getProfilePicturesSystemDir(_webHostEnv, _conf);
                System.IO.File.Delete(Path.Combine(systemFolder, user.ProfilePicture));
                user.ProfilePicture = null;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                // store image if one was posted
                if (Input.ProfileImage != null)
                {
                    await StoreProfilePicureAsync(Input, user);
                    await _userManager.UpdateAsync(user);
                }
            }
            

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }


        /// <summary>
        /// Stores profile picture given in model after post on filesystem giving a unique name
        /// creates mapping reference in DB 
        /// </summary>
        /// <param name="model">model which was posted including the file as iFormfile</param>
        /// <param name="user">User variable for saving in DB backend reference</param>
        /// <returns></returns>
        private async Task<string> StoreProfilePicureAsync(InputModel model, GUser user)
        {
            // unique, random file name so it cannot be enumerated!
            string uniqueFileName = null;

            string systemFolder = GHelper.getProfilePicturesSystemDir(_webHostEnv, _conf);

            // delete old file of user
            if (user.ProfilePicture != null)
            {
                System.IO.File.Delete(Path.Combine(systemFolder, user.ProfilePicture));
            }
                
            uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
            string filePath = Path.Combine(systemFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                model.ProfileImage.CopyTo(fileStream);
            }
            // save unique filename to DB
            user.ProfilePicture = uniqueFileName;
            await _userManager.UpdateAsync(user);
            return uniqueFileName;
        }
    }

    
}
