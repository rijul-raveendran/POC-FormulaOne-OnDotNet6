using FormulaOne.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormulaOne.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public DbSet<Team> Teams { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        { 
        }

    }
}
