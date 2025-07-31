using VisitorLog_PBFD.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VisitorLog_PBFD.Models;
using System.Text;
using VisitorLog_PBFD.Services;

namespace ContinentsApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register ApplicationDbContext with the DI container
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add controllers with views
            builder.Services.AddControllersWithViews();
            // Configure HttpClient with custom handler (e.g., for bypassing SSL in development)
            builder.Services.AddHttpClient("MyClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7151/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Bypass SSL (only for development)
                };
            });

            builder.Services.AddScoped<ILocationSaveService, LocationSaveService>();
            builder.Services.AddScoped<ILocationResetService, LocationResetService>();
            builder.Services.AddScoped<ILocationReportService, LocationReportService>();

            var app = builder.Build();

            if (builder.Configuration["CreateDatabase"] == "True")
            {
                CreateDatabase(app);
            }

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Persons}/{action=Index}/{id?}");

            app.Run();
        }

        private static void CreateDatabase(WebApplication app)
        {
            // Use the service provider to create a scope and obtain the context
            using (var scope = app.Services.CreateScope())
            {
                var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "location.json");

                if (File.Exists(jsonPath))
                {
                    var jsonData = File.ReadAllText(jsonPath);
                    var locations = JsonSerializer.Deserialize<List<Location>>(jsonData);

                    using (var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                    {
                        // Clear existing data (optional)
                        context.Locations.RemoveRange(context.Locations);

                        if (locations != null)
                        {
                            // Add new data from JSON
                            context.Locations.AddRange(locations);
                            CreateTables(context, locations);
                        }

                        // Save changes
                        context.SaveChanges();
                    }
                }
            }
        }
        private static void CreateTables(ApplicationDbContext _context, List<Location> locations)
        {
            foreach (var location in locations)
            {
                if(location.Level>5)
                    continue;

                var parentChildren = locations.Where(x => x.ParentId == location.Id).ToList();

                // Generate the CREATE TABLE SQL dynamically
                var createTableSql = new StringBuilder();
                createTableSql.AppendLine($"CREATE TABLE [{location.Name}] (");
                createTableSql.AppendLine($"  PersonId INT,");

                foreach (var child in parentChildren)
                {
                    createTableSql.AppendLine($"  [{child.Name}] {child.Type.ToUpper()},");
                }
                createTableSql.AppendLine($"  IsDeleted Bit Null,");

                // Use regular expression to replace all special characters with underscores for the constraint name
                var sanitizedLocationName = System.Text.RegularExpressions.Regex.Replace(location.Name, @"[^a-zA-Z0-9_]", "_");
                var constraintName = $"FK_{sanitizedLocationName}_Persons";
                createTableSql.AppendLine($"  CONSTRAINT PK_{sanitizedLocationName} PRIMARY KEY (PersonId),");
                createTableSql.AppendLine($"  CONSTRAINT {constraintName} FOREIGN KEY (PersonId) REFERENCES Persons(PersonId)");
                createTableSql.AppendLine(");");

                // Execute the SQL to create the table
                _context.Database.ExecuteSqlRaw(createTableSql.ToString());
            }
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
