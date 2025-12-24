using Microsoft.EntityFrameworkCore;

namespace HbcRest.Data;

public class HbcContext : DbContext
{
    public DbSet<CustomerSatisfactionSurvey> CustomerSatisfactionSurveys => Set<CustomerSatisfactionSurvey>();

    public HbcContext(DbContextOptions<HbcContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // No composite uniqueness enforced at the EF level; rely on Id PK only.
    }
}
