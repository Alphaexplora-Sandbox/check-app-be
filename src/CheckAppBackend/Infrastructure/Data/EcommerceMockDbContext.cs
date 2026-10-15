using CheckAppBackend.Domain.Entities;
using CheckAppBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CheckAppBackend.Infrastructure.Data;

public class EcommerceMockDbContext : DbContext
{
    public EcommerceMockDbContext(DbContextOptions<EcommerceMockDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Rating).HasPrecision(2, 1);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsFeatured);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
