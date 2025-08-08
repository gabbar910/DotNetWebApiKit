namespace DotNetApiStarterKit.Data
{
    using DotNetApiStarterKit.Models;
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserCredential> Users { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserCredential entity
            modelBuilder.Entity<UserCredential>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);
                
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(500);
                
                entity.Property(e => e.Pincode)
                    .IsRequired()
                    .HasMaxLength(10);
                
                entity.Property(e => e.State)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                
                entity.Property(e => e.OrderDate)
                    .IsRequired();
                
                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);

                // Configure relationship with Customer
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure relationship with OrderItems
                entity.HasMany(e => e.OrderItems)
                    .WithOne(e => e.Order)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add index for better query performance
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.OrderDate);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.ItemId);
                
                entity.Property(e => e.Price)
                    .IsRequired()
                    .HasPrecision(18, 2);
                
                entity.Property(e => e.TotalPrice)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.PartId)
                    .IsRequired();

                // Add indexes for better query performance
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.PartId);
            });
        }
    }
}
