using Microsoft.EntityFrameworkCore;
using InternConnect_Backend.Models;
using System.Collections.Generic;

namespace InternConnect_Backend.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet for User model
        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Activity> Activities { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Profile>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);

            



            modelBuilder.Entity<Education>()
                .HasOne(e => e.Profile)
                .WithMany(p => p.Educations)
                .HasForeignKey(e => e.ProfileId);

            modelBuilder.Entity<Skill>()
                .HasOne(s => s.Profile)
                .WithMany(p => p.Skills)
                .HasForeignKey(s => s.ProfileId);

            modelBuilder.Entity<Project>()
                .HasOne(pj => pj.Profile)
                .WithMany(p => p.Projects)
                .HasForeignKey(pj => pj.ProfileId);

            modelBuilder.Entity<Achievement>()
                .HasOne(a => a.Profile)
                .WithMany(p => p.Achievements)
                .HasForeignKey(a => a.ProfileId);


            modelBuilder.Entity<Opportunity>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);

            modelBuilder.Entity<Activity>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);
        }
    }
}

