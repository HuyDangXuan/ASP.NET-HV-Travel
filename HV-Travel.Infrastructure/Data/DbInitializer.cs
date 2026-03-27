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
                        Name = "HÃ  Giang Loop Adventure",
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
                        GeneratedInclusions = new List<string> { "ThuÃª xe mÃ¡y", "Homestay", "Bá»¯a Äƒn", "HÆ°á»›ng dáº«n viÃªn" },
                        GeneratedExclusions = new List<string> { "Chi tiÃªu cÃ¡ nhÃ¢n", "Äá»“ uá»‘ng" },
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
                        GeneratedInclusions = new List<string> { "KhÃ¡ch sáº¡n 4*", "VÃ© BÃ  NÃ  Hills", "Bá»¯a Äƒn", "Xe Ä‘Æ°a Ä‘Ã³n" },
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
                        GeneratedInclusions = new List<string> { "Cabin cao cáº¥p", "ToÃ n bá»™ bá»¯a Äƒn", "ChÃ¨o kayak", "VÃ© tham quan" },
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
                            new Passenger { FullName = "Chá»“ng", Type = "Adult" }
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
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "Thanh toÃ¡n Ä‘Æ¡n Ä‘áº·t má»›i", Message = "ÄÆ¡n #BK-9420-2024 Ä‘Ã£ Ä‘Æ°á»£c thanh toÃ¡n Ä‘áº§y Ä‘á»§.", Type = "Order", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "KhÃ¡ch hÃ ng má»›i Ä‘Äƒng kÃ½", Message = "KhÃ¡ch hÃ ng Tráº§n Thá»‹ HÆ°Æ¡ng vá»«a Ä‘Äƒng kÃ½ tÃ i khoáº£n.", Type = "System", IsRead = true, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Notification { Id = ObjectId.GenerateNewId().ToString(), Title = "ÄÃ¡nh giÃ¡ tour", Message = "HÃ  Giang Loop Adventure vá»«a nháº­n Ä‘Æ°á»£c Ä‘Ã¡nh giÃ¡ 5 sao.", Type = "Review", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-5) }
                };
                foreach (var n in notifications) await notificationRepository.AddAsync(n);
            }

            // 6. Seed Promotions
            if (!(await promotionRepository.GetAllAsync()).Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "WELCOME2024", DiscountPercentage = 10, Description = "Æ¯u Ä‘Ã£i chÃ o má»«ng dÃ nh cho thÃ nh viÃªn má»›i", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(1), IsActive = true },
                    new Promotion { Id = ObjectId.GenerateNewId().ToString(), Code = "SUMMER_SALE", DiscountPercentage = 15, Description = "Æ¯u Ä‘Ã£i Ä‘áº·c biá»‡t cho mÃ¹a du lá»‹ch hÃ¨", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(3), IsActive = true }
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
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.FirstOrDefault()?.Id, CustomerId = customers.FirstOrDefault()?.Id, Rating = 5, Comment = "Trải nghiệm tuyệt vời! Cảnh quan thật sự ngoạn mục.", CreatedAt = DateTime.UtcNow.AddDays(-10), IsApproved = true, ModerationStatus = "Approved", DisplayName = customers.FirstOrDefault()?.FullName ?? "Khách hàng" },
                        new Review { Id = ObjectId.GenerateNewId().ToString(), TourId = tours.LastOrDefault()?.Id, CustomerId = customers.LastOrDefault()?.Id, Rating = 4, Comment = "Tour rất ổn nhưng phần ăn có thể cải thiện thêm.", CreatedAt = DateTime.UtcNow.AddDays(-5), IsApproved = true, ModerationStatus = "Approved", DisplayName = customers.LastOrDefault()?.FullName ?? "Khách hàng" }
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
                        Title = "Checklist visa Nhật Bản cho khách đi tự túc hoặc theo tour",
                        Summary = "Tổng hợp hồ sơ, timeline và các lỗi thường gặp khi chuẩn bị visa Nhật Bản.",
                        Body = "<p>Chuẩn bị visa Nhật Bản nên bắt đầu từ lịch trình, chứng minh tài chính và tệp chứng từ nhân thân. Nếu đi theo tour, bạn vẫn nên chốt lịch nghỉ và kiểm tra hiệu lực hộ chiếu trước.</p><p>HV Travel có thể hỗ trợ rà soát checklist và timeline nộp hồ sơ theo mùa cao điểm.</p>",
                        Category = "Visa Tips",
                        Destination = "Nhật Bản",
                        HeroImageUrl = "https://images.unsplash.com/photo-1542051841857-5f90071e7989?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "visa", "nhật bản", "checklist" },
                        Featured = true,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new TravelArticle
                    {
                        Slug = "hanh-trinh-san-deal-cuoi-nam",
                        Title = "Cách săn deal cuối năm mà không bị vỡ ngân sách",
                        Summary = "Gợi ý chọn tháng khởi hành, kết hợp voucher và đọc đúng tín hiệu flash sale.",
                        Body = "<p>Deal tốt không chỉ nằm ở giá rẻ mà còn ở tổng chi phí sau khi cộng hành lý, di chuyển và phụ thu mùa cao điểm. Hãy ưu tiên những hành trình có khuyến mãi rõ ràng, lịch khởi hành gần và số chỗ còn ít.</p><p>Trang promotion center của HV Travel được thiết kế để làm đúng việc đó.</p>",
                        Category = "Seasonal Campaign",
                        Destination = "Châu Á",
                        HeroImageUrl = "https://images.unsplash.com/photo-1488646953014-85cb44e25828?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "deal", "flash sale", "ngân sách" },
                        Featured = false,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new TravelArticle
                    {
                        Slug = "lich-trinh-gia-dinh-ngan-ngay",
                        Title = "Thiết kế lịch trình gia đình ngắn ngày mà vẫn nhiều trải nghiệm",
                        Summary = "Cân đối nhịp di chuyển, độ tuổi trẻ nhỏ và các điểm dừng có giá trị thật cho cả nhà.",
                        Body = "<p>Gia đình đi ngắn ngày nên tránh lịch trình đổi khách sạn liên tục. Thay vào đó, hãy chọn tuyến bay dễ, một điểm chính đủ sâu và một số hoạt động có thể thay đổi theo thời tiết.</p>",
                        Category = "Destination Guide",
                        Destination = "Việt Nam",
                        HeroImageUrl = "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&q=80&w=1200",
                        Tags = new List<string> { "gia đình", "ngắn ngày", "itinerary" },
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




