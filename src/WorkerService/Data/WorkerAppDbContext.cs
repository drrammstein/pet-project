using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace WorkerService.Data
{
    public class WorkerAppDbContext : DbContext
    {
        public WorkerAppDbContext(DbContextOptions<WorkerAppDbContext> options)
            : base(options)
        {
        }
        public DbSet<AppData> AppData { get; set; }
    }
}
