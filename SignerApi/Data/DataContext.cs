using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SignerApi.Models;

namespace SignerApi.Data
{
    public class DataContext : DbContext
    {

        public DataContext(DbContextOptions<DataContext> dbOptions) : base(dbOptions)
        {

        }

        public DbSet<ApiActivity> ApiActivity { get; set; }

    }
}
