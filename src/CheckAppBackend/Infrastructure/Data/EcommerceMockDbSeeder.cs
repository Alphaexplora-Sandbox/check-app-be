using CheckAppBackend.Domain.Entities;
using CheckAppBackend.Domain.Enums;
using CheckAppBackend.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CheckAppBackend.Infrastructure.Data;

public static class EcommerceMockDbSeeder
{
    public static async Task SeedAsync(EcommerceMockDbContext dbContext, IPasswordHasher passwordHasher)
    {
        // Ensure database created if relational or in-memory
        await dbContext.Database.EnsureCreatedAsync();

        var cardId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        if (!await dbContext.PaymentMethods.AnyAsync(p => p.Id == cardId))
        {
            var demoCard = new PaymentMethod
            {
                Id = cardId,
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                CardHolderName = "Customer Demo",
                CardNumberMasked = " " " " "  " " " "  " " " " 4242",
                ExpiryMonth = "12",
                ExpiryYear = "2028",
                Brand = "Visa",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            };
            try
            {
                await dbContext.PaymentMethods.AddAsync(demoCard);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Concurrently seeded
            }
        }


        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!await dbContext.Users.AnyAsync(u => u.Id == adminId))
        {
            var adminUser = new User
            {
                Id = adminId,
                Email = "admin@e-commerce-mock.com",
                PasswordHash = passwordHasher.HashPassword("AdminPass123!"),
                FullName = "Store Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var customerUser = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "customer@e-commerce-mock.com",
                PasswordHash = passwordHasher.HashPassword("CustomerPass123!"),
                FullName = "Customer Demo",
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await dbContext.Users.AddRangeAsync(adminUser, customerUser);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Concurrently seeded by another test instance
            }
        }

        var audioCatId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        if (!await dbContext.Categories.AnyAsync(c => c.Id == audioCatId))
        {
            var audioCat = new Category
            {
                Id = audioCatId,
                Name = "Audio & Acoustics",
                Slug = "audio",
                Description = "High-fidelity audiophile gear and studio-grade monitoring solutions.",
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            };

            var workspaceCat = new Category
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Minimalist Workspace",
                Slug = "workspace",
                Description = "Precision-engineered desktop peripherals and ergonomic tools.",
                ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            };

            var wearablesCat = new Category
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Name = "Smart Wearables",
                Slug = "wearables",
                Description = "Advanced biometric tracking and intelligent everyday micro-wearables.",
                ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&q=80",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await dbContext.Categories.AddRangeAsync(audioCat, workspaceCat, wearablesCat);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Concurrently seeded
            }

            var firstProdId = Guid.Parse("33333333-3333-3333-3333-333333333301");
            if (!await dbContext.Products.AnyAsync(p => p.Id == firstProdId))
            {
                var products = new List<Product>
                {
                    new()
                    {
                        Id = firstProdId,
                        CategoryId = audioCat.Id,
                        Name = "Pro Wireless ANC Headphones",
                        Slug = "pro-wireless-anc-headphones",
                        Description = "Active noise cancelling with 40mm beryllium drivers, 36-hour battery, and custom acoustic tuning for unmatched spatial presence.",
                        Price = 299.00m,
                        StockQuantity = 45,
                        ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = true,
                        Rating = 4.9m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
                        CategoryId = audioCat.Id,
                        Name = "Studio Reference Desktop Monitors",
                        Slug = "studio-reference-desktop-monitors",
                        Description = "Bi-amplified precision reference nearfield monitors with Kevlar cones and flat frequency response for discerning creators.",
                        Price = 449.00m,
                        StockQuantity = 12,
                        ImageUrl = "https://images.unsplash.com/photo-1545454675-3531b543be5d?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = true,
                        Rating = 4.8m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333303"),
                        CategoryId = workspaceCat.Id,
                        Name = "Machined Aluminum Mechanical Keyboard",
                        Slug = "machined-aluminum-mechanical-keyboard",
                        Description = "CNC anodized chassis, hot-swappable tactile switches, gasket mount acoustic dampening, and custom QMK/VIA firmware.",
                        Price = 189.00m,
                        StockQuantity = 30,
                        ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = true,
                        Rating = 4.9m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333304"),
                        CategoryId = workspaceCat.Id,
                        Name = "Solid Walnut Desk Riser & Organizer",
                        Slug = "solid-walnut-desk-riser-organizer",
                        Description = "Handcrafted Appalachian walnut monitor riser with integrated cable routing trays and micro-beveled edges.",
                        Price = 129.00m,
                        StockQuantity = 20,
                        ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = false,
                        Rating = 4.7m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333305"),
                        CategoryId = workspaceCat.Id,
                        Name = "Ergonomic Wireless Precision Mouse",
                        Slug = "ergonomic-wireless-precision-mouse",
                        Description = "Ergonomic sculpted thumb rest with dual magnetic scroll wheels, darkfield sensor tracking on glass, and multi-device pairing.",
                        Price = 99.00m,
                        StockQuantity = 50,
                        ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = false,
                        Rating = 4.6m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333306"),
                        CategoryId = workspaceCat.Id,
                        Name = "Minimalist Ceramic Induction Charger",
                        Slug = "minimalist-ceramic-induction-charger",
                        Description = "Matte finish ceramic dual fast-charging pad supporting Qi2 MagSafe alignment up to 15W per terminal.",
                        Price = 59.00m,
                        StockQuantity = 85,
                        ImageUrl = "https://images.unsplash.com/photo-1586816879360-004f5b0c51e3?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = false,
                        Rating = 4.5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333307"),
                        CategoryId = wearablesCat.Id,
                        Name = "Titanium Horizon Smart Ring",
                        Slug = "titanium-horizon-smart-ring",
                        Description = "Aerospace grade titanium ring tracking HRV, sleep stages, skin temperature variance, and recovery readiness score.",
                        Price = 249.00m,
                        StockQuantity = 15,
                        ImageUrl = "https://images.unsplash.com/photo-1605100804763-247f67b3557e?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = true,
                        Rating = 4.8m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333308"),
                        CategoryId = wearablesCat.Id,
                        Name = "Active Sport Sensor Watch Ultra",
                        Slug = "active-sport-sensor-watch-ultra",
                        Description = "Sapphire crystal display, dual-frequency GPS, 100m water resistance, and 60-hour endurance battery life in extreme conditions.",
                        Price = 399.00m,
                        StockQuantity = 8,
                        ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&q=80",
                        IsFeatured = false,
                        Rating = 4.7m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                try
                {
                    await dbContext.Products.AddRangeAsync(products);
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Concurrently seeded
                }
            }
        }
    }
}
