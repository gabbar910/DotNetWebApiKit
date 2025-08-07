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
        }
    }
}
