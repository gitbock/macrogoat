using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MacroGoat.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        
        
        //All roles of this user
        public IEnumerable<string> Roles { get; set; }
    }
}
