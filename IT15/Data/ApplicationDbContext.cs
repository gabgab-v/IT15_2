using IT15.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IT15.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DailyLog> DailyLogs { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PaySlip> PaySlips { get; set; }
        public DbSet<CompanyLedger> CompanyLedger { get; set; }
        public DbSet<AccountsReceivable> AccountsReceivables { get; set; }
        public DbSet<AccountsPayable> AccountsPayables { get; set; }
        public DbSet<SupplyRequest> SupplyRequests { get; set; }
        public DbSet<Supply> Supplies { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<ProductRequest> ProductRequests { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DeliveryService> DeliveryServices { get; set; }
        public DbSet<ResignationRequest> ResignationRequests { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Identity tables for PostgreSQL compatibility
            ConfigureIdentityTables(builder);

            // Your existing configurations
            builder.Entity<OvertimeRequest>()
                .HasOne(o => o.RequestingEmployee)
                .WithMany()
                .HasForeignKey(o => o.RequestingEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OvertimeRequest>()
                .HasOne(o => o.ApprovedBy)
                .WithMany()
                .HasForeignKey(o => o.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamptz");
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }



            // Additional PostgreSQL-specific configurations for your custom entities
            ConfigureCustomEntities(builder);
        }

        private void ConfigureIdentityTables(ModelBuilder builder)
        {
            // Configure AspNetUsers table
            builder.Entity<IdentityUser>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("text");
                entity.Property(e => e.UserName).HasColumnType("varchar(256)");
                entity.Property(e => e.NormalizedUserName).HasColumnType("varchar(256)");
                entity.Property(e => e.Email).HasColumnType("varchar(256)");
                entity.Property(e => e.NormalizedEmail).HasColumnType("varchar(256)");
                entity.Property(e => e.ConcurrencyStamp).HasColumnType("text");
                entity.Property(e => e.SecurityStamp).HasColumnType("text");
                entity.Property(e => e.PhoneNumber).HasColumnType("text");
                entity.Property(e => e.PasswordHash).HasColumnType("text");
                entity.Property(e => e.TwoFactorEnabled).HasColumnType("boolean");
                entity.Property(e => e.LockoutEnd).HasColumnType("timestamp with time zone");
                entity.Property(e => e.LockoutEnabled).HasColumnType("boolean");
                entity.Property(e => e.AccessFailedCount).HasColumnType("integer");
                entity.Property(e => e.EmailConfirmed).HasColumnType("boolean");
                entity.Property(e => e.PhoneNumberConfirmed).HasColumnType("boolean");
            });

            // Configure AspNetRoles table
            builder.Entity<IdentityRole>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("text");
                entity.Property(e => e.Name).HasColumnType("varchar(256)");
                entity.Property(e => e.NormalizedName).HasColumnType("varchar(256)");
                entity.Property(e => e.ConcurrencyStamp).HasColumnType("text");
            });

            // Configure AspNetUserClaims
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnType("text");
                entity.Property(e => e.ClaimType).HasColumnType("text");
                entity.Property(e => e.ClaimValue).HasColumnType("text");
            });

            // Configure AspNetUserLogins
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(e => e.LoginProvider).HasColumnType("varchar(128)");
                entity.Property(e => e.ProviderKey).HasColumnType("varchar(128)");
                entity.Property(e => e.ProviderDisplayName).HasColumnType("text");
                entity.Property(e => e.UserId).HasColumnType("text");
            });

            // Configure AspNetUserTokens
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasColumnType("text");
                entity.Property(e => e.LoginProvider).HasColumnType("varchar(128)");
                entity.Property(e => e.Name).HasColumnType("varchar(128)");
                entity.Property(e => e.Value).HasColumnType("text");
            });

            // Configure AspNetRoleClaims
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.RoleId).HasColumnType("text");
                entity.Property(e => e.ClaimType).HasColumnType("text");
                entity.Property(e => e.ClaimValue).HasColumnType("text");
            });

            // Configure AspNetUserRoles
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasColumnType("text");
                entity.Property(e => e.RoleId).HasColumnType("text");
            });
        }

        private void ConfigureCustomEntities(ModelBuilder builder)
        {
            // Configure string properties that might use nvarchar(max) to use text in PostgreSQL
            // Add configurations for your custom entities if they have specific requirements

            // Example: If any of your entities have large text fields
            builder.Entity<DailyLog>(entity =>
            {
                // If you have any string properties that should be unlimited length
                // entity.Property(e => e.Description).HasColumnType("text");
            });

            builder.Entity<LeaveRequest>(entity =>
            {
                // Configure any text fields
                // entity.Property(e => e.Reason).HasColumnType("text");
            });

            builder.Entity<PaySlip>(entity =>
            {
                // Configure decimal properties if needed
                // entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            });

            builder.Entity<CompanyLedger>(entity =>
            {
                entity.Property(e => e.EntryType).HasConversion<int>();
                entity.Property(e => e.Category).HasConversion<int>();
                entity.HasOne(e => e.AccountsReceivable)
                    .WithMany(r => r.LedgerEntries)
                    .HasForeignKey(e => e.AccountsReceivableId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AccountsPayable)
                    .WithMany(p => p.LedgerEntries)
                    .HasForeignKey(e => e.AccountsPayableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AccountsReceivable>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.RevenueCategory).HasConversion<int>();
            });

            builder.Entity<AccountsPayable>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.ExpenseCategory).HasConversion<int>();
            });

            // Configure DateTime properties to use timestamp with time zone if needed
            // This is important for PostgreSQL
            builder.Entity<AuditLog>(entity =>
            {
                // Allow unauthenticated events by permitting null UserId and set FK to null on delete
                entity.Property(e => e.UserId).IsRequired(false);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Add similar configurations for other entities as needed
        }
    }
}
