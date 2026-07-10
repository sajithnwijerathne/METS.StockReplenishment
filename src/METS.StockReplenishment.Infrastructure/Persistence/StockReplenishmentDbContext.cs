using Microsoft.EntityFrameworkCore;
public class StockReplenishmentDbContext : DbContext
{
    public StockReplenishmentDbContext(DbContextOptions<StockReplenishmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();

    public DbSet<RequestItem> RequestItems => Set<RequestItem>();

    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplenishmentRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LocationCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Priority)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.ValidationStatus)
                .IsRequired();

            entity.Property(x => x.RejectionReason)
                .HasMaxLength(500);

            entity.HasMany(x => x.Items)
                .WithOne(x => x.ReplenishmentRequest)
                .HasForeignKey(x => x.ReplenishmentRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ArticleNumber)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.RequestedQuantity)
                .HasPrecision(18, 2);

            entity.Property(x => x.FulfilledQuantity)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Address)
                .HasMaxLength(200);

            entity.Property(x => x.City)
                .HasMaxLength(100);

            entity.Property(x => x.State)
                .HasMaxLength(100);

            entity.Property(x => x.ZipCode)
                .HasMaxLength(20);

            entity.Property(x => x.Country)
                .HasMaxLength(100);
        });
    }
}