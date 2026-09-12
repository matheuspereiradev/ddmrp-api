using Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Infra.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Center> Center { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Forecast> Forecast { get; set; }
        public DbSet<History> History { get; set; }
        public DbSet<Partner> Partner { get; set; }
        public DbSet<Tag> Tag { get; set; }
        public DbSet<Reason> Reason { get; set; }
        public DbSet<AllocationGroup> AllocationGroup { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
