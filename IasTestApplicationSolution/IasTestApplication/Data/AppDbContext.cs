using Microsoft.EntityFrameworkCore;

namespace IasTestApplication.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Offer>(e =>
        {
            e.HasKey(o => o.Code);
            e.Property(o => o.Code).HasMaxLength(20);
            e.Property(o => o.Name).HasMaxLength(200);
            e.Property(o => o.NormalizedCode).HasMaxLength(20);
            e.Property(o => o.NormalizedName).HasMaxLength(200);

            // Red de seguridad: aunque el código falle, la BD nunca permite cupos negativos.
            e.ToTable(t => t.HasCheckConstraint("CK_Offers_AvailableSlots", "AvailableSlots >= 0"));
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.OfferCode).HasMaxLength(20);
            e.Property(s => s.IdempotencyKey).HasMaxLength(100);
            e.HasIndex(s => s.IdempotencyKey).IsUnique();
            e.HasIndex(s => s.OfferCode);
            e.HasOne<Offer>().WithMany().HasForeignKey(s => s.OfferCode);
        });
    }
}
