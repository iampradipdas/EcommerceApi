using System;
using System.Collections.Generic;
using System.Linq;
using EcommerceApi.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EcommerceApi.Dal
{
    public static class DbSeeder
    {
        public static void Seed(EcomDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during EnsureCreated: {ex.Message}");
            }

            // If the core table "users" is missing, the DB schema is incomplete or corrupted (e.g. EnsureCreated skipped because of a flyway table).
            // Wipe the schema and recreate it cleanly.
            if (!TableExists(context, "users"))
            {
                Console.WriteLine("Core table 'users' does not exist. Re-initializing database schema...");
                try
                {
                    context.Database.ExecuteSqlRaw("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
                    
                    // Directly force EF Core to create the tables in the clean public schema
                    var databaseCreator = context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
                    databaseCreator?.CreateTables();
                    
                    Console.WriteLine("Database schema initialized successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to re-initialize database schema: {ex.Message}");
                    throw;
                }
            }

            // 1. Seed Users if they don't exist by email
            bool hasChanges = false;
            
            if (!context.Users.Any(u => u.Email == "admin@ecommerce.com"))
            {
                var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");
                var admin = new User
                {
                    Email = "admin@ecommerce.com",
                    PasswordHash = adminPasswordHash,
                    FirstName = "System",
                    LastName = "Admin",
                    PhoneNumber = "9876543210",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(admin);
                hasChanges = true;
            }

            if (!context.Users.Any(u => u.Email == "customer@ecommerce.com"))
            {
                var customerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");
                var customer = new User
                {
                    Email = "customer@ecommerce.com",
                    PasswordHash = customerPasswordHash,
                    FirstName = "John",
                    LastName = "Doe",
                    PhoneNumber = "9988776655",
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(customer);
                hasChanges = true;
            }

            if (hasChanges)
            {
                context.SaveChanges();
            }

            // Get Categories (assuming V1__initial table.sql seeded them)
            var categories = context.Categories.ToList();
            if (categories.Count == 0)
            {
                // Fallback: Seed categories if database is completely empty
                var electronics = new Category { Name = "Electronics", Slug = "electronics", IsActive = true };
                var clothing = new Category { Name = "Clothing", Slug = "clothing", IsActive = true };
                var books = new Category { Name = "Books", Slug = "books", IsActive = true };

                context.Categories.AddRange(electronics, clothing, books);
                context.SaveChanges();

                var mobiles = new Category { Name = "Mobiles", Slug = "mobiles", ParentCategoryId = electronics.CategoryId, IsActive = true };
                var laptops = new Category { Name = "Laptops", Slug = "laptops", ParentCategoryId = electronics.CategoryId, IsActive = true };
                var men = new Category { Name = "Men", Slug = "men", ParentCategoryId = clothing.CategoryId, IsActive = true };
                var women = new Category { Name = "Women", Slug = "women", ParentCategoryId = clothing.CategoryId, IsActive = true };

                context.Categories.AddRange(mobiles, laptops, men, women);
                context.SaveChanges();

                categories = context.Categories.ToList();
            }

            // 2. Seed Products if empty
            if (!context.Products.Any())
            {
                var mobilesCat = categories.FirstOrDefault(c => c.Slug == "mobiles") ?? categories.First();
                var laptopsCat = categories.FirstOrDefault(c => c.Slug == "laptops") ?? categories.First();
                var menCat = categories.FirstOrDefault(c => c.Slug == "men") ?? categories.First();
                var womenCat = categories.FirstOrDefault(c => c.Slug == "women") ?? categories.First();
                var booksCat = categories.FirstOrDefault(c => c.Slug == "books") ?? categories.First();

                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "iPhone 15 Pro Max",
                        Slug = "iphone-15-pro-max",
                        Description = "Experience the ultimate iPhone with a strong and light titanium design, a powerful new A17 Pro chip, and a customizable Action button.",
                        Price = 149900.00m,
                        DiscountPrice = 139900.00m,
                        Stock = 15,
                        ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=600&auto=format&fit=crop",
                        CategoryId = mobilesCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-10)
                    },
                    new Product
                    {
                        Name = "Samsung Galaxy S24 Ultra",
                        Slug = "samsung-galaxy-s24-ultra",
                        Description = "Welcome to the era of mobile AI. With Galaxy S24 Ultra in your hands, you can unleash whole new levels of creativity, productivity and possibility.",
                        Price = 129900.00m,
                        DiscountPrice = null,
                        Stock = 12,
                        ImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?q=80&w=600&auto=format&fit=crop",
                        CategoryId = mobilesCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-8)
                    },
                    new Product
                    {
                        Name = "MacBook Pro 14 M3 Pro",
                        Slug = "macbook-pro-14-m3-pro",
                        Description = "The 14-inch MacBook Pro blasts forward with M3 Pro, an incredibly advanced chip that brings serious speed and capability for demanding workflows.",
                        Price = 199900.00m,
                        DiscountPrice = 189900.00m,
                        Stock = 8,
                        ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=600&auto=format&fit=crop",
                        CategoryId = laptopsCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    },
                    new Product
                    {
                        Name = "Dell XPS 15",
                        Slug = "dell-xps-15",
                        Description = "Stunning 15.6-inch laptop with a 16:10 4-sided InfinityEdge display. Featuring 13th Gen Intel Core processors and GeForce RTX graphics.",
                        Price = 175000.00m,
                        DiscountPrice = 165000.00m,
                        Stock = 5,
                        ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?q=80&w=600&auto=format&fit=crop",
                        CategoryId = laptopsCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-5)
                    },
                    new Product
                    {
                        Name = "Men's Premium Cotton Hoodie",
                        Slug = "mens-premium-cotton-hoodie",
                        Description = "A classic regular-fit pullover hoodie crafted from heavy fleece cotton. Soft-brushed interior with front kangaroo pocket.",
                        Price = 2499.00m,
                        DiscountPrice = 1799.00m,
                        Stock = 50,
                        ImageUrl = "https://images.unsplash.com/photo-1556821840-3a63f95609a7?q=80&w=600&auto=format&fit=crop",
                        CategoryId = menCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-2)
                    },
                    new Product
                    {
                        Name = "Women's Floral Summer Dress",
                        Slug = "womens-floral-summer-dress",
                        Description = "Lightweight, breathable summer dress featuring a beautiful floral print, wrap V-neck style, and adjustable tie waist.",
                        Price = 3499.00m,
                        DiscountPrice = null,
                        Stock = 30,
                        ImageUrl = "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?q=80&w=600&auto=format&fit=crop",
                        CategoryId = womenCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-1)
                    },
                    new Product
                    {
                        Name = "Atomic Habits by James Clear",
                        Slug = "atomic-habits-james-clear",
                        Description = "No matter your goals, Atomic Habits offers a proven framework for improving—every day. James Clear, one of the world's leading experts on habit formation, reveals practical strategies.",
                        Price = 799.00m,
                        DiscountPrice = 549.00m,
                        Stock = 100,
                        ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?q=80&w=600&auto=format&fit=crop",
                        CategoryId = booksCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new Product
                    {
                        Name = "The Psychology of Money",
                        Slug = "the-psychology-of-money",
                        Description = "Timeless lessons on wealth, greed, and happiness. Doing well with money isn't necessarily about what you know. It's about how you behave.",
                        Price = 599.00m,
                        DiscountPrice = 399.00m,
                        Stock = 85,
                        ImageUrl = "https://images.unsplash.com/photo-1592492159418-09f3133a9683?q=80&w=600&auto=format&fit=crop",
                        CategoryId = booksCat.CategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }

            // 3. Seed Reviews if empty
            if (!context.Reviews.Any())
            {
                var customer = context.Users.FirstOrDefault(u => u.Email == "customer@ecommerce.com");
                var admin = context.Users.FirstOrDefault(u => u.Email == "admin@ecommerce.com");
                var products = context.Products.ToList();

                if (customer != null && admin != null && products.Count > 0)
                {
                    var reviews = new List<Review>();

                    // Reviews for Product 1 (iPhone)
                    var iphone = products.FirstOrDefault(p => p.Slug == "iphone-15-pro-max");
                    if (iphone != null)
                    {
                        reviews.Add(new Review
                        {
                            UserId = customer.UserId,
                            ProductId = iphone.ProductId,
                            Rating = 5,
                            Comment = "Absolutely amazing phone! The titanium build makes it feel super light in the hand. The camera zoom is incredible.",
                            CreatedAt = DateTime.Now.AddDays(-5)
                        });

                        reviews.Add(new Review
                        {
                            UserId = admin.UserId,
                            ProductId = iphone.ProductId,
                            Rating = 4,
                            Comment = "Excellent performance and build quality. Only downside is the battery life is similar to last year's model.",
                            CreatedAt = DateTime.Now.AddDays(-3)
                        });
                    }

                    // Reviews for Product 2 (Galaxy S24)
                    var galaxy = products.FirstOrDefault(p => p.Slug == "samsung-galaxy-s24-ultra");
                    if (galaxy != null)
                    {
                        reviews.Add(new Review
                        {
                            UserId = customer.UserId,
                            ProductId = galaxy.ProductId,
                            Rating = 5,
                            Comment = "The display is incredibly bright and flat! Love the anti-reflective coating. AI features like circle to search are very handy.",
                            CreatedAt = DateTime.Now.AddDays(-4)
                        });
                    }

                    // Reviews for Product 5 (Hoodie)
                    var hoodie = products.FirstOrDefault(p => p.Slug == "mens-premium-cotton-hoodie");
                    if (hoodie != null)
                    {
                        reviews.Add(new Review
                        {
                            UserId = customer.UserId,
                            ProductId = hoodie.ProductId,
                            Rating = 4,
                            Comment = "Very comfortable and fits well. The material is heavy and warm. Shrunk a tiny bit after the first wash, but still fits.",
                            CreatedAt = DateTime.Now.AddDays(-1)
                        });
                    }

                    // Reviews for Product 7 (Atomic Habits)
                    var book = products.FirstOrDefault(p => p.Slug == "atomic-habits-james-clear");
                    if (book != null)
                    {
                        reviews.Add(new Review
                        {
                            UserId = customer.UserId,
                            ProductId = book.ProductId,
                            Rating = 5,
                            Comment = "A life-changing book. James Clear explains the psychology of habits in a very simple and actionable way. Highly recommended!",
                            CreatedAt = DateTime.Now.AddDays(-12)
                        });
                    }

                    context.Reviews.AddRange(reviews);
                    context.SaveChanges();
                }
            }
        }

        private static bool TableExists(EcomDbContext context, string tableName)
        {
            try
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName);";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                context.Database.OpenConnection();
                var exists = (bool)command.ExecuteScalar()!;
                return exists;
            }
            catch
            {
                return false;
            }
            finally
            {
                context.Database.CloseConnection();
            }
        }
    }
}
