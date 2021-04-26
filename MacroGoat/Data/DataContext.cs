using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroGoat.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MacroGoat.Data
{
    public class DataContext : IdentityDbContext<GUser>
    {

        public DataContext(DbContextOptions<DataContext> dbOptions) : base(dbOptions)
        {

        }



    }
}
