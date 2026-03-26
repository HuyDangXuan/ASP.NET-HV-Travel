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
            await EnsureBookingIndexesAsync(serviceProvider);

            var userRepository = serviceProvider.GetRequiredService<IRepository<User>>();
            var tourRepository = serviceProvider.GetRequiredService<IRepository<Tour>>();
            var customerRepository = serviceProvider.GetRequiredService<IRepository<Customer>>();
            var bookingRepository = serviceProvider.GetRequiredService<IRepository<Booking>>();
            var notificationRepository = serviceProvider.GetRequiredService<IRepository<Notification>>();
            var promotionRepository = serviceProvider.GetRequiredService<IRepository<Promotion>>();
            var reviewRepository = serviceProvider.GetRequiredService<IRepository<Review>>();


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
                        Name = "Hà Giang Loop Adventure",
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
                        GeneratedInclusions = new List<string> { "Thuê xe máy", "Homestay", "Bữa ăn", "Hướng dẫn viên" },
                        GeneratedExclusions = new List<string> { "Chi tiêu cá nhân", "Đồ uống" },
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
                        GeneratedInclusions = new List<string> { "Khách sạn 4*", "Vé Bà Nà Hills", "Bữa ăn", "Xe đưa đón" },
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
                        GeneratedInclusions = new List<string> { "Cabin cao cấp", "Toàn bộ bữa ăn", "Chèo kayak", "Vé tham quan" },
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
                            new Passenger { FullName = "Chồng", Type = "Adult" }
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
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Thanh toán đơn đặt mới", Message = "Đơn #BK-9420-2024 đã được thanh toán đầy đủ.", Type = "Order", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Khách hàng mới đăng ký", Message = "Khách hàng Trần Thị Hương vừa đăng ký tài khoản.", Type = "System", IsRead = true, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Đánh giá tour", Message = "Hà Giang Loop Adventure vừa nhận được đánh giá 5 sao.", Type = "Review", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-5) }
                };
                foreach (var n in notifications) await notificationRepository.AddAsync(n);
            }

            // 6. Seed Promotions
            if (!(await promotionRepository.GetAllAsync()).Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "WELCOME2024", DiscountPercentage = 10, Description = "Ưu đãi chào mừng dành cho thành viên mới", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(1), IsActive = true },
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "SUMMER_SALE", DiscountPercentage = 15, Description = "Ưu đãi đặc biệt cho mùa du lịch hè", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(3), IsActive = true }
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
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.FirstOrDefault()?.Id, CustomerId = customers.FirstOrDefault()?.Id, Rating = 5, Comment = "Trải nghiệm tuyệt vời! Cảnh quan thật sự ngoạn mục.", CreatedAt = DateTime.UtcNow.AddDays(-10), IsApproved = true },
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.LastOrDefault()?.Id, CustomerId = customers.LastOrDefault()?.Id, Rating = 4, Comment = "Tour rất ổn nhưng phần ăn có thể cải thiện thêm.", CreatedAt = DateTime.UtcNow.AddDays(-5), IsApproved = true }
                    };
                    foreach (var r in reviews) await reviewRepository.AddAsync(r);
                }
            }
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
