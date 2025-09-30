using IT15.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

            // This configuration tells Entity Framework not to use cascade delete
            // for the OvertimeRequest's relationship to the user, which prevents the error.
            builder.Entity<OvertimeRequest>()
                .HasOne(o => o.RequestingEmployee)
                .WithMany()
                .HasForeignKey(o => o.RequestingEmployeeId)
                .OnDelete(DeleteBehavior.Restrict); // This is the key change

            builder.Entity<OvertimeRequest>()
                .HasOne(o => o.ApprovedBy)
                .WithMany()
                .HasForeignKey(o => o.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
