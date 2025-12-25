using Microsoft.EntityFrameworkCore;

namespace HbcRest.Data;

public class HbcContext : DbContext
{
    public DbSet<CustomerSatisfactionSurvey> CustomerSatisfactionSurveys => Set<CustomerSatisfactionSurvey>();
    public DbSet<CallCenterInquiry> CallCenterInquiries => Set<CallCenterInquiry>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public HbcContext(DbContextOptions<HbcContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerSatisfactionSurvey>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();

        modelBuilder.Entity<CallCenterInquiry>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();
    }
}
