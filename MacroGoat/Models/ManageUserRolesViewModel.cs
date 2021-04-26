using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MacroGoat.Models
{
    /// <summary>
    /// for View which allows adding / removing roles to users
    /// 
    /// </summary>
    public class ManageUserRolesViewModel
    {
        public string Role { get; set; }
        public string RoleId { get; set; }

        public bool Selected { get; set; }
    }
}
