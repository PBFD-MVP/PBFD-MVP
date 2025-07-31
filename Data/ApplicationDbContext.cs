using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VisitorLog_PBFD.Models;

namespace VisitorLog_PBFD.Data // Ensure this namespace matches your project's folder structure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Person> Persons { get; set; }
        public DbSet<NameType> NameTypes { get; set; }
        public DbSet<Report> Reports { get; set; }

        public DbSet<Location> Locations { get; set; }
        public DbSet<SchemaColumn> SchemaColumns { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SeedNameType(modelBuilder);
            modelBuilder.Entity<Location>().Property(l => l.Id).ValueGeneratedNever(); // Disable auto-generation for Id       

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasNoKey(); // Views don't have a primary key
            });
        }
        private void SeedNameType(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NameType>().HasData(
                new NameType { NameTypeId = 1, Name = "Continent" },
                new NameType { NameTypeId = 2, Name = "Country" },
                new NameType { NameTypeId = 3, Name = "State" },
                new NameType { NameTypeId = 4, Name = "County" },
                new NameType { NameTypeId = 5, Name = "City" },
                new NameType { NameTypeId = 6, Name = "District" },
                new NameType { NameTypeId = 7, Name = "Province" },
                new NameType { NameTypeId = 8, Name = "Station" },
                new NameType { NameTypeId = 9, Name = "Special Administrative Region" },
                new NameType { NameTypeId = 10, Name = "Separate Political Entity" },
                new NameType { NameTypeId = 11, Name = "Region" }
            );
        }
    }

}
