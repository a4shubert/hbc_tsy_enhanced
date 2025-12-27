using Microsoft.EntityFrameworkCore;

namespace HbcRest.Data;

public class HbcContext : DbContext
{
    public DbSet<NycOpenData311CustomerSatisfactionSurvey> CustomerSatisfactionSurveys => Set<NycOpenData311CustomerSatisfactionSurvey>();
    public DbSet<NycOpenData311CallCenterInquiry> CallCenterInquiries => Set<NycOpenData311CallCenterInquiry>();
    public DbSet<NycOpenData311ServiceRequests> ServiceRequests => Set<NycOpenData311ServiceRequests>();

    public HbcContext(DbContextOptions<HbcContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NycOpenData311CustomerSatisfactionSurvey>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();

        modelBuilder.Entity<NycOpenData311CallCenterInquiry>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();

        modelBuilder.Entity<NycOpenData311ServiceRequests>()
            .HasIndex(c => c.HbcUniqueKey)
            .IsUnique();
    }
}
