using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MacroGoat.Models
{
    public class CreateUserViewModel
    {

       
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [EmailAddress]
        [Display(Name = "E-Mail / Username")]
        public string Username { get; set; }

        [TempData]
        public string ProfilesPicturePath { get; set; }

        // string only
        [Display(Name = "Profile Picture")]
        public string ProfilePicture { get; set; }

        // image form for posting profile image 
        [Display(Name = "Profile Image")]
        public IFormFile ProfileImage { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }


        public CreateUserViewModel()
        {

        }

    }
}
