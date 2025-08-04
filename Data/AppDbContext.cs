using Microsoft.EntityFrameworkCore;
using Backend_Project.Models;
using System.Collections.Generic;

namespace Backend_Project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet for User model
        public DbSet<User> Users { get; set; }
    }
}
