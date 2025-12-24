using Microsoft.EntityFrameworkCore;

namespace HbcRest.Data;

public class HbcContext : DbContext
{
    public DbSet<CustomerSatisfactionSurvey> CustomerSatisfactionSurveys => Set<CustomerSatisfactionSurvey>();

    public HbcContext(DbContextOptions<HbcContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerSatisfactionSurvey>()
            .HasIndex(c => c.UniqueKey)
            .IsUnique();
    }
}
