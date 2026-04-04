using System;
using System.Linq;
using MongoDB.Bson;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using MongoDB.Driver;

namespace HVTravel.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await EnsureCommerceIndexesAsync(serviceProvider);
            await EnsureBookingIndexesAsync(serviceProvider);

            var userRepository = serviceProvider.GetRequiredService<IRepository<User>>();
            var tourRepository = serviceProvider.GetRequiredService<IRepository<Tour>>();
            var customerRepository = serviceProvider.GetRequiredService<IRepository<Customer>>();
            var bookingRepository = serviceProvider.GetRequiredService<IRepository<Booking>>();
            var notificationRepository = serviceProvider.GetRequiredService<IRepository<Notification>>();
            var promotionRepository = serviceProvider.GetRequiredService<IRepository<Promotion>>();
            var reviewRepository = serviceProvider.GetRequiredService<IRepository<Review>>();
            var articleRepository = serviceProvider.GetRequiredService<IRepository<TravelArticle>>();


            // 1. Seed Users
            if (!(await userRepository.GetAllAsync()).Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        Email = "admin@hvtravel.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = "Admin",
                        FullName = "Super Admin",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?u=admin",
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Email = "staff@hvtravel.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
                        Role = "Staff",
                        FullName = "Support Staff",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?u=staff",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                foreach (var u in users) await userRepository.AddAsync(u);
            }

            // 2. Seed Tours
            if (!(await tourRepository.GetAllAsync()).Any())
            {
                var tours = new List<Tour>
                {
                    new Tour
                    {
                        Name = "H√† Giang Loop Adventure",
                        Code = "TOUR-001",
                        Description = "<p>Explore the majestic landscapes of Ha Giang with our 3-day loop tour. Experience the Ma Pi Leng Pass, Nho Que River, and authentic Hmong culture.</p>",
                        ShortDescription = "3 Days of breathtaking mountain views and cultural immersion.",
                        Destination = new Destination { City = "Ha Giang", Country = "Vietnam", Region = "North" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1596558450255-7c0b7be9d56a?auto=format&fit=crop&q=80&w=1000", 
                            "https://images.unsplash.com/photo-1625409559312-70e6332152d0?auto=format&fit=crop&q=80&w=1000" 
                        },
                        Price = new TourPrice { Adult = 3500000, Child = 2500000, Infant = 0, Currency = "VND" },
                        Duration = new TourDuration { Days = 3, Nights = 2, Text = "3 Days 2 Nights" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(12), DateTime.UtcNow.AddDays(20) },
                        GeneratedInclusions = new List<string> { "Thu√™ xe m√°y", "Homestay", "B·ªØa ƒÉn", "H∆∞·ªõng d·∫´n vi√™n" },
                        GeneratedExclusions = new List<string> { "Chi ti√™u c√° nh√¢n", "ƒê·ªì u·ªëng" },
                        Schedule = new List<ScheduleItem> 
                        {
                            new ScheduleItem { Day = 1, Title = "Ha Giang - Quan Ba - Yen Minh", Description = "Start your journey...", Activities = new List<string> { "Ride to Quan Ba", "Visit Twin Mountains" } },
                            new ScheduleItem { Day = 2, Title = "Yen Minh - Dong Van - Ma Pi Leng", Description = "The highlight of the trip...", Activities = new List<string> { "Conquer Ma Pi Leng Pass", "Boat trip on Nho Que River" } },
                            new ScheduleItem { Day = 3, Title = "Meo Vac - Ha Giang", Description = "Return journey...", Activities = new List<string> { "Visit Hmong King Palace", "Return to Ha Giang" } }
                        },
                        MaxParticipants = 12,
                        CurrentParticipants = 4,
                        Rating = 4.8,
                        ReviewCount = 45,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Tour
                    {
                        Name = "Da Nang & Hoi An Discovery",
                        Code = "TOUR-002",
                        Description = "<p>Discover the charm of Hoi An Ancient Town and the modern vibes of Da Nang. Visit Ba Na Hills, Golden Bridge, and My Khe Beach.</p>",
                        ShortDescription = "Relaxing 4-day trip to Central Vietnam's gems.",
                        Destination = new Destination { City = "Da Nang", Country = "Vietnam", Region = "Central" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1559592413-7cec430aaec3?auto=format&fit=crop&q=80&w=1000", 
                            "https://images.unsplash.com/photo-1565060169379-373bed711883?auto=format&fit=crop&q=80&w=1000" 
                        },
                        Price = new TourPrice { Adult = 4200000, Child = 3000000, Infant = 500000, Currency = "VND" },
                        Duration = new TourDuration { Days = 4, Nights = 3, Text = "4 Days 3 Nights" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(15) },
                        GeneratedInclusions = new List<string> { "Kh√°ch s·∫°n 4*", "V√© B√† N√† Hills", "B·ªØa ƒÉn", "Xe ƒë∆∞a ƒë√≥n" },
                        Status = "Active",
                        MaxParticipants = 20,
                        CurrentParticipants = 15,
                        Rating = 4.9,
                        ReviewCount = 120,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Tour
                    {
                        Name = "Ha Long Bay Luxury Cruise",
                        Code = "TOUR-003",
                        Description = "<p>Experience the UNESCO World Heritage site on a 5-star cruise. Kayaking, cooking classes, and tai chi on the deck.</p>",
                        ShortDescription = "2 Days 1 Night on a luxury cruise.",
                        Destination = new Destination { City = "Quang Ninh", Country = "Vietnam", Region = "North" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&q=80&w=1000",
                            "https://images.unsplash.com/photo-1504457047772-27faf1c00561?auto=format&fit=crop&q=80&w=1000"
                        },
                        Price = new TourPrice { Adult = 3800000, Child = 2800000, Infant = 1000000, Currency = "VND" },
                        Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 Days 1 Night" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(25) },
                        GeneratedInclusions = new List<string> { "Cabin cao c·∫•p", "To√†n b·ªô b·ªØa ƒÉn", "Ch√®o kayak", "V√© tham quan" },
                        Status = "Active",
                        MaxParticipants = 30,
                        CurrentParticipants = 10,
                        Rating = 4.7,
                        ReviewCount = 85,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                foreach (var t in tours) await tourRepository.AddAsync(t);
            }

            // 3. Seed Customers
            if (!(await customerRepository.GetAllAsync()).Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUS-8821",
                        FullName = "Nguyen Van A",
                        Email = "nguyenvana@gmail.com",
                        PhoneNumber = "0912345678",
                        AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAEcuip7QN29LzlUaukSzw1TgG5p1Hfw6IbJOAHs9ICHZdgZ11ps3EHw4rDwOEVF0KeDTKdVsNP_CHUumeNu5fOXHgMW8rGlF_RCmQ68GdSJbZ_lIj_eCfo3b-bS_i7XXlPKC9kjOPVpm29GdgdE72XCIp831JkI7WcUHJeGabqQDj_xt8ETun5DNhZnuckVYou77stQoonRvynMFY4jWts7W_6CVPaNV4os7MD4VQKnWIOPX8aGGITE3lP_zED8suQ2std2e3vQQ",
                        Address = new Address { Street = "123 Le Loi", City = "Hanoi", Country = "Vietnam" },
                        Segment = "VIP",
                        Status = "Active",
                        Stats = new CustomerStats { TotalSpending = 150000000, TotalOrders = 12, LoyaltyPoints = 5000 },
                        CreatedAt = DateTime.UtcNow.AddMonths(-6)
                    },
                    new Customer
                    {
                        CustomerCode = "CUS-9102",
                        FullName = "Tran Thi Huong",
                        Email = "huong.tran@company.vn",
                        PhoneNumber = "0988111222",
                        AvatarUrl = "https://i.pravatar.cc/150?u=huong",
                        Address = new Address { Street = "456 Nguyen Hue", City = "Ho Chi Minh", Country = "Vietnam" },
                        Segment = "New",
                        Status = "Active",
                        Stats = new CustomerStats { TotalSpending = 4500000, TotalOrders = 1, LoyaltyPoints = 100 },
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new Customer
                    {
                        CustomerCode = "CUS-7734",
                        FullName = "Le Minh K",
                        Email = "leminh.k@outlook.com",
                        PhoneNumber = "0905999888",
                        AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCi3LjdsB91Fyp3EwZBZJNY8_I4mQLHvSgyDB_tVo4W8BL2qL5-zM6Huo0S9LUbMYD35-m9--rc-WlmnMKEqWoj1aJStVNHN4ZindCMvLGtg66RuVynF4W0LTtTc4Zpm7fJ6VyLJ34cnokrKxYlHdJakFLmDqQmWqGxBUQU5jSsqW97uIo3XxcvLmp3QKGismPNotgpr5JQAmUtZmnY_j71xazZ34uxuSi-E66Zi5Vk8RXZWM46YUK1jbPhfVFgVDD3woOlfl61Cg",
                        Address = new Address { Street = "789 Tran Phu", City = "Da Nang", Country = "Vietnam" },
                        Segment = "Standard",
                        Status = "Active",
                        Stats = new CustomerStats { TotalSpending = 12200000, TotalOrders = 3, LoyaltyPoints = 300 },
                        CreatedAt = DateTime.UtcNow.AddMonths(-3)
                    }
                };
                foreach (var c in customers) await customerRepository.AddAsync(c);
            }

            // 4. Seed Bookings
            if (!(await bookingRepository.GetAllAsync()).Any())
            {
                var customers = await customerRepository.GetAllAsync();
                var tours = await tourRepository.GetAllAsync();
                var storedCustomers = customers.ToList();
                var storedTours = tours.ToList();

                if (storedCustomers.Any() && storedTours.Any())
                {
                    var bookings = new List<Booking>();
                    
                    // Create some bookings
                    var customer1 = storedCustomers[0]; // VIP
                    var tour1 = storedTours[0]; // Ha Giang Loop

                    bookings.Add(new Booking
                    {
                        BookingCode = "BK-9420-2024",
                        CustomerId = customer1.Id,
                        TourId = tour1.Id,
                        TourSnapshot = new TourSnapshot { Code = tour1.Code, Name = tour1.Name, StartDate = tour1.StartDates.FirstOrDefault(), Duration = tour1.Duration.Text },
                        BookingDate = DateTime.UtcNow.AddDays(-2),
                        TotalAmount = 3500000,
                        Status = "Paid",
                        PaymentStatus = "Full",
                        ParticipantsCount = 1,
                        Passengers = new List<Passenger> { new Passenger { FullName = customer1.FullName, Type = "Adult" } },
                        ContactInfo = new ContactInfo { Name = customer1.FullName, Email = customer1.Email, Phone = customer1.PhoneNumber },
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });

                    var customer2 = storedCustomers[1]; // New
                    var tour2 = storedTours[1]; // Da Nang

                    bookings.Add(new Booking
                    {
                        BookingCode = "BK-9421-2024",
                        CustomerId = customer2.Id,
                        TourId = tour2.Id,
                        TourSnapshot = new TourSnapshot { Code = tour2.Code, Name = tour2.Name, StartDate = tour2.StartDates.FirstOrDefault(), Duration = tour2.Duration.Text },
                        BookingDate = DateTime.UtcNow.AddHours(-1),
                        TotalAmount = 7200000,
                        Status = "Pending",
                        PaymentStatus = "Unpaid",
                        ParticipantsCount = 2,
                        Passengers = new List<Passenger> 
                        { 
                            new Passenger { FullName = customer2.FullName, Type = "Adult" },
                            new Passenger { FullName = "Ch·ªìng", Type = "Adult" }
                        },
                        ContactInfo = new ContactInfo { Name = customer2.FullName, Email = customer2.Email, Phone = customer2.PhoneNumber },
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    });

                    foreach (var b in bookings) await bookingRepository.AddAsync(b);
                }
            }

            // 5. Seed Notifications
            if (!(await notificationRepository.GetAllAsync()).Any())
            {
                var notifications = new List<Notification>
                {
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Thanh to√°n ƒë∆°n ƒë·∫∑t m·ªõi", Message = "ƒê∆°n #BK-9420-2024 ƒë√£ ƒë∆∞·ª£c thanh to√°n ƒë·∫ßy ƒë·ªß.", Type = "Order", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Kh√°ch h√†ng m·ªõi ƒëƒÉng k√Ω", Message = "Kh√°ch h√†ng Tr·∫ßn Th·ªã H∆∞∆°ng v·ª´a ƒëƒÉng k√Ω t√†i kho·∫£n.", Type = "System", IsRead = true, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "ƒê√°nh gi√° tour", Message = "H√† Giang Loop Adventure v·ª´a nh·∫≠n ƒë∆∞·ª£c ƒë√°nh gi√° 5 sao.", Type = "Review", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-5) }
                };
                foreach (var n in notifications) await notificationRepository.AddAsync(n);
            }

            // 6. Seed Promotions
            if (!(await promotionRepository.GetAllAsync()).Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "WELCOME2024", DiscountPercentage = 10, Description = "∆Øu ƒë√£i ch√†o m·ª´ng d√†nh cho th√†nh vi√™n m·ªõi", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(1), IsActive = true },
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "SUMMER_SALE", DiscountPercentage = 15, Description = "∆Øu ƒë√£i ƒë·∫∑c bi·ªát cho m√πa du l·ªãch h√®", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(3), IsActive = true }
                };
                foreach (var p in promotions) await promotionRepository.AddAsync(p);
            }
            
            // 7. Seed Reviews
            if (!(await reviewRepository.GetAllAsync()).Any())
            {
                // Cleanup legacy indexes only on first seed
                try
                {
                    var context = serviceProvider.GetRequiredService<MongoContext>();
                    var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                    var reviewCollectionName = configuration.GetValue<string>("HVTravelDatabase:ReviewsCollectionName") ?? "Reviews";
                    var reviewCollection = context.GetCollection<Review>(reviewCollectionName);
                    await reviewCollection.Indexes.DropAllAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not drop indexes: {ex.Message}");
                }

                var customers = await customerRepository.GetAllAsync();
                var tours = await tourRepository.GetAllAsync();
                
                if (customers.Any() && tours.Any())
                {
                    var reviews = new List<Review>
                    {
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.FirstOrDefault()?.Id, CustomerId = customers.FirstOrDefault()?.Id, Rating = 5, Comment = "Tr?i nghi?m tuy?t v?i! C?nh quan th?t s? ngo?n m?c.", CreatedAt = DateTime.UtcNow.AddDays(-10), IsApproved = true, ModerationStatus = "Approved", DisplayName = customers.FirstOrDefault()?.FullName ?? "Kh·ch h‡ng" },
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.LastOrDefault()?.Id, CustomerId = customers.LastOrDefault()?.Id, Rating = 4, Comment = "Tour r?t ?n nhung ph?n an cÛ th? c?i thi?n thÍm.", CreatedAt = DateTime.UtcNow.AddDays(-5), IsApproved = true, ModerationStatus = "Approved", DisplayName = customers.LastOrDefault()?.FullName ?? "Kh·ch h‡ng" }
                    };
                    foreach (var r in reviews) await reviewRepository.AddAsync(r);
                }
            }

            // 8. Seed Travel Articles
            if (!(await articleRepository.GetAllAsync()).Any())
            {
                var articles = new List<TravelArticle>
                {
                    new TravelArticle
                    {
                        Slug = "visa-nhat-ban-checklist",
                        Title = "Checklist visa Nh?t B?n cho kh·ch di t? t˙c ho?c theo tour",
                        Summary = "T?ng h?p h? so, timeline v‡ c·c l?i thu?ng g?p khi chu?n b? visa Nh?t B?n.",
                        Body = "<p>Chu?n b? visa Nh?t B?n nÍn b?t d?u t? l?ch trÏnh, ch?ng minh t‡i chÌnh v‡ t?p ch?ng t? nh‚n th‚n. N?u di theo tour, b?n v?n nÍn ch?t l?ch ngh? v‡ ki?m tra hi?u l?c h? chi?u tru?c.</p><p>HV Travel cÛ th? h? tr? r‡ so·t checklist v‡ timeline n?p h? so theo m˘a cao di?m.</p>",
                        Category = "Visa Tips",
                        Destination = "Nh?t B?n",
                        HeroImageUrl = "https://images.unsplash.com/photo-1542051841857-5f90071e7989?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "visa", "nh?t b?n", "checklist" },
                        Featured = true,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new TravelArticle
                    {
                        Slug = "hanh-trinh-san-deal-cuoi-nam",
                        Title = "C·ch san deal cu?i nam m‡ khÙng b? v? ng‚n s·ch",
                        Summary = "G?i ˝ ch?n th·ng kh?i h‡nh, k?t h?p voucher v‡ d?c d˙ng tÌn hi?u flash sale.",
                        Body = "<p>Deal t?t khÙng ch? n?m ? gi· r? m‡ cÚn ? t?ng chi phÌ sau khi c?ng h‡nh l˝, di chuy?n v‡ ph? thu m˘a cao di?m. H„y uu tiÍn nh?ng h‡nh trÏnh cÛ khuy?n m„i rı r‡ng, l?ch kh?i h‡nh g?n v‡ s? ch? cÚn Ìt.</p><p>Trang promotion center c?a HV Travel du?c thi?t k? d? l‡m d˙ng vi?c dÛ.</p>",
                        Category = "Seasonal Campaign",
                        Destination = "Ch‚u ¡",
                        HeroImageUrl = "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "deal", "flash sale", "ng‚n s·ch" },
                        Featured = false,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new TravelArticle
                    {
                        Slug = "lich-trinh-gia-dinh-ngan-ngay",
                        Title = "Thi?t k? l?ch trÏnh gia dÏnh ng?n ng‡y m‡ v?n nhi?u tr?i nghi?m",
                        Summary = "C‚n d?i nh?p di chuy?n, d? tu?i tr? nh? v‡ c·c di?m d?ng cÛ gi· tr? th?t cho c? nh‡.",
                        Body = "<p>Gia dÏnh di ng?n ng‡y nÍn tr·nh l?ch trÏnh d?i kh·ch s?n liÍn t?c. Thay v‡o dÛ, h„y ch?n tuy?n bay d?, m?t di?m chÌnh d? s‚u v‡ m?t s? ho?t d?ng cÛ th? thay d?i theo th?i ti?t.</p>",
                        Category = "Destination Guide",
                        Destination = "Vi?t Nam",
                        HeroImageUrl = "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "gia dÏnh", "ng?n ng‡y", "itinerary" },
                        Featured = false,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                foreach (var article in articles) await articleRepository.AddAsync(article);
            }
        }
        private static async Task EnsureCommerceIndexesAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var context = serviceProvider.GetRequiredService<MongoContext>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                var tourCollection = context.GetCollection<Tour>(configuration.GetValue<string>("HVTravelDatabase:TourCollectionName") ?? "Tours");
                await CreateIndexesAsync(tourCollection, new[]
                {
                    new CreateIndexModel<Tour>(Builders<Tour>.IndexKeys.Ascending(t => t.Slug), new CreateIndexOptions { Name = "tour_slug" }),
                    new CreateIndexModel<Tour>(Builders<Tour>.IndexKeys.Ascending(t => t.Status).Ascending("destination.region").Ascending("destination.city"), new CreateIndexOptions { Name = "tour_status_destination" }),
                    new CreateIndexModel<Tour>(Builders<Tour>.IndexKeys.Ascending("departures.startDate"), new CreateIndexOptions { Name = "tour_departures_startDate" }),
                    new CreateIndexModel<Tour>(Builders<Tour>.IndexKeys.Ascending(t => t.Code), new CreateIndexOptions { Name = "tour_code" })
                });

                var bookingCollection = context.GetCollection<Booking>(configuration.GetValue<string>("HVTravelDatabase:BookingCollectionName") ?? "Bookings");
                await CreateIndexesAsync(bookingCollection, new[]
                {
                    new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending(b => b.TourId).Ascending(b => b.DepartureId), new CreateIndexOptions { Name = "booking_tour_departure" }),
                    new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending(b => b.Status).Descending(b => b.BookingDate), new CreateIndexOptions { Name = "booking_status_date" }),
                    new CreateIndexModel<Booking>(Builders<Booking>.IndexKeys.Ascending("contactInfo.email"), new CreateIndexOptions { Name = "booking_contact_email" })
                });

                var promotionCollection = context.GetCollection<Promotion>(configuration.GetValue<string>("HVTravelDatabase:PromotionCollectionName") ?? "Promotions");
                await CreateIndexesAsync(promotionCollection, new[]
                {
                    new CreateIndexModel<Promotion>(Builders<Promotion>.IndexKeys.Ascending(p => p.Code), new CreateIndexOptions { Name = "promotion_code" }),
                    new CreateIndexModel<Promotion>(Builders<Promotion>.IndexKeys.Ascending(p => p.IsActive).Ascending(p => p.ValidTo), new CreateIndexOptions { Name = "promotion_active_validTo" })
                });

                var articleCollection = context.GetCollection<TravelArticle>(configuration.GetValue<string>("HVTravelDatabase:TravelArticleCollectionName") ?? "TravelArticles");
                await CreateIndexesAsync(articleCollection, new[]
                {
                    new CreateIndexModel<TravelArticle>(Builders<TravelArticle>.IndexKeys.Ascending(article => article.Slug), new CreateIndexOptions { Name = "article_slug" }),
                    new CreateIndexModel<TravelArticle>(Builders<TravelArticle>.IndexKeys.Ascending(article => article.IsPublished).Descending(article => article.PublishedAt), new CreateIndexOptions { Name = "article_publish_window" }),
                    new CreateIndexModel<TravelArticle>(Builders<TravelArticle>.IndexKeys.Ascending("tags"), new CreateIndexOptions { Name = "article_tags" })
                });

                var chatCollection = context.GetCollection<ChatConversation>(configuration.GetValue<string>("HVTravelDatabase:ChatConversationCollectionName") ?? "ChatConversations");
                await CreateIndexesAsync(chatCollection, new[]
                {
                    new CreateIndexModel<ChatConversation>(Builders<ChatConversation>.IndexKeys.Ascending(conversation => conversation.ConversationCode), new CreateIndexOptions { Name = "chat_conversationCode" }),
                    new CreateIndexModel<ChatConversation>(Builders<ChatConversation>.IndexKeys.Ascending(conversation => conversation.Status).Descending(conversation => conversation.LastMessageAt), new CreateIndexOptions { Name = "chat_status_lastMessage" }),
                    new CreateIndexModel<ChatConversation>(Builders<ChatConversation>.IndexKeys.Ascending(conversation => conversation.CustomerId).Ascending(conversation => conversation.AssignedStaffUserId), new CreateIndexOptions { Name = "chat_customer_assignee" })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not ensure commerce indexes: {ex.Message}");
            }
        }

        private static async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, IEnumerable<CreateIndexModel<T>> indexes)
        {
            await collection.Indexes.CreateManyAsync(indexes);
        }
        private static async Task EnsureBookingIndexesAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var context = serviceProvider.GetRequiredService<MongoContext>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var bookingCollectionName = configuration.GetValue<string>("HVTravelDatabase:BookingCollectionName") ?? "Bookings";
                var bookingCollection = context.GetCollection<BsonDocument>(bookingCollectionName);

                var existingIndexes = await bookingCollection.Indexes.ListAsync();
                var indexDocs = await existingIndexes.ToListAsync();
                var hasBookingCodeIndex = false;

                foreach (var indexDoc in indexDocs)
                {
                    var indexName = indexDoc.GetValue("name", "").AsString;
                    if (indexName == "booking_code_1")
                    {
                        await bookingCollection.Indexes.DropOneAsync(indexName);
                        continue;
                    }

                    if (indexName == "bookingCode_unique")
                    {
                        hasBookingCodeIndex = true;
                    }
                }

                if (!hasBookingCodeIndex)
                {
                    var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("bookingCode");
                    var indexOptions = new CreateIndexOptions<BsonDocument>
                    {
                        Name = "bookingCode_unique",
                        Unique = true,
                        PartialFilterExpression = new BsonDocument
                        {
                            { "bookingCode", new BsonDocument { { "$exists", true }, { "$type", "string" }, { "$gt", "" } } }
                        }
                    };

                    await bookingCollection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not ensure booking indexes: {ex.Message}");
            }
        }
    }
}





