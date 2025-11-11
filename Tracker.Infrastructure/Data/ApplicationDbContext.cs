using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tracker.Infrastructure.Models;

namespace Tracker.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        private readonly ILogger<ApplicationDbContext>? _logger;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext>? logger = null)
            : base(options)
        {
            _logger = logger;
            
            // Log database initialization
            _logger?.LogInformation("Initializing database context");
            _logger?.LogInformation("Database provider: {Provider}", Database.ProviderName);
            _logger?.LogInformation("Database connection string: {ConnectionString}", 
                Database.GetDbConnection()?.ConnectionString);
                
            // Apply pending migrations on startup
            try
            {
                if (Database.IsRelational())
                {
                    _logger?.LogInformation("Applying pending migrations...");
                    var pendingMigrations = Database.GetPendingMigrations().ToList();
                    if (pendingMigrations.Any())
                    {
                        _logger?.LogInformation("Found {Count} pending migrations", pendingMigrations.Count);
                        Database.Migrate();
                        _logger?.LogInformation("Database migrations applied successfully");
                    }
                    else
                    {
                        _logger?.LogInformation("No pending migrations");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying database migrations");
                throw;
            }
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Individual> Individuals { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<IncidentIndividual> IncidentIndividuals { get; set; }
        public DbSet<IncidentTimeline> IncidentTimelines { get; set; }
        public DbSet<IncidentAttachment> IncidentAttachments { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<EnrollmentCode> EnrollmentCodes { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _logger?.LogInformation("Configuring database model...");
            
            // Configure SQLite specific settings
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                _logger?.LogInformation("Configuring SQLite-specific model settings");
                // SQLite doesn't support decimal type natively, so we need to convert it to string
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));
                    
                    foreach (var property in properties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion<double>();
                    }
                }
            }
            
            // Call base implementation
            _logger?.LogInformation("Configuring base identity model...");
            base.OnModelCreating(modelBuilder);
            _logger?.LogInformation("Base identity model configured");

            // Configure User-Organization many-to-many relationship (Users can belong to multiple organizations)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Organizations)
                .WithMany(o => o.Members)
                .UsingEntity<Dictionary<string, object>>(
                    "UserOrganizations",
                    j => j.HasOne<Organization>().WithMany().HasForeignKey("OrganizationId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("UserId", "OrganizationId"));
            
            // Configure one-to-many relationship between Organization and User (primary organization)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete to avoid circular reference

            // Configure Incident-Individual many-to-many relationship with SQLite-compatible delete behaviors
            modelBuilder.Entity<IncidentIndividual>()
                .HasKey(ii => new { ii.IncidentId, ii.IndividualId });

            modelBuilder.Entity<IncidentIndividual>()
                .HasOne(ii => ii.Incident)
                .WithMany(i => i.InvolvedIndividuals)
                .HasForeignKey(ii => ii.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IncidentIndividual>()
                .HasOne(ii => ii.Individual)
                .WithMany(i => i.IncidentInvolvements)
                .HasForeignKey(ii => ii.IndividualId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure LogEntry entity
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(255);
                entity.Property(e => e.UserId).HasMaxLength(450);
                
                // Add index on timestamp for better query performance
                entity.HasIndex(e => e.Timestamp);
                
                // Add index on level for filtering
                entity.HasIndex(e => e.Level);
                
                // Add index on user ID for filtering by user
                entity.HasIndex(e => e.UserId);
            });

            // Configure enums and constraints
            modelBuilder.Entity<Incident>()
                .Property(i => i.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Incident>()
                .Property(i => i.Severity)
                .HasConversion<string>();

            modelBuilder.Entity<Individual>()
                .Property(i => i.Status)
                .HasConversion<string>();

            // Configure cascading deletes
            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Individuals)
                .WithOne(i => i.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Contacts)
                .WithOne(c => c.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            // Set delete behavior to NoAction to prevent circular reference
            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Incidents)
                .WithOne(i => i.Organization)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.EnrollmentCodes)
                .WithOne(e => e.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Timeline)
                .WithOne(t => t.Incident)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Attachments)
                .WithOne(a => a.Incident)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Name)
                .IsUnique();

            modelBuilder.Entity<EnrollmentCode>()
                .HasIndex(ec => ec.Code)
                .IsUnique();
        }
    }
}
