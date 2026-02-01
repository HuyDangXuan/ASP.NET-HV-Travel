using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using System.Collections.Generic;

namespace HVTravel.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
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
                        Category = "Adventure",
                        Destination = new Destination { City = "Ha Giang", Country = "Vietnam", Region = "North" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1596558450255-7c0b7be9d56a?auto=format&fit=crop&q=80&w=1000", 
                            "https://images.unsplash.com/photo-1625409559312-70e6332152d0?auto=format&fit=crop&q=80&w=1000" 
                        },
                        Price = new TourPrice { Adult = 3500000, Child = 2500000, Infant = 0, Currency = "VND" },
                        Duration = new TourDuration { Days = 3, Nights = 2, Text = "3 Days 2 Nights" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(12), DateTime.UtcNow.AddDays(20) },
                        GeneratedInclusions = new List<string> { "Motorbike rental", "Homestay accumulation", "Meals", "Guide" },
                        GeneratedExclusions = new List<string> { "Personal expenses", "Drinks" },
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
                        Category = "Culture",
                        Destination = new Destination { City = "Da Nang", Country = "Vietnam", Region = "Central" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1559592413-7cec430aaec3?auto=format&fit=crop&q=80&w=1000", 
                            "https://images.unsplash.com/photo-1565060169379-373bed711883?auto=format&fit=crop&q=80&w=1000" 
                        },
                        Price = new TourPrice { Adult = 4200000, Child = 3000000, Infant = 500000, Currency = "VND" },
                        Duration = new TourDuration { Days = 4, Nights = 3, Text = "4 Days 3 Nights" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(15) },
                        GeneratedInclusions = new List<string> { "Hotel 4*", "Ba Na Hills Tickets", "Meals", "Transfers" },
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
                        Category = "Luxury",
                        Destination = new Destination { City = "Quang Ninh", Country = "Vietnam", Region = "North" },
                        Images = new List<string> 
                        { 
                            "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&q=80&w=1000",
                            "https://images.unsplash.com/photo-1504457047772-27faf1c00561?auto=format&fit=crop&q=80&w=1000"
                        },
                        Price = new TourPrice { Adult = 3800000, Child = 2800000, Infant = 1000000, Currency = "VND" },
                        Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 Days 1 Night" },
                        StartDates = new List<DateTime> { DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(25) },
                        GeneratedInclusions = new List<string> { "Luxury Cabin", "All Meals", "Kayaking", "Entrance Fees" },
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
                            new Passenger { FullName = "Husband", Type = "Adult" }
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
                    new Notification { Title = "New Booking Payment", Message = "Booking #BK-9420-2024 has been fully paid.", Type = "Order", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                    new Notification { Title = "New Customer Registered", Message = "Customer Tran Thi Huong just signed up.", Type = "System", IsRead = true, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                    new Notification { Title = "Tour Review", Message = "Ha Giang Loop Adventure received a 5-star review.", Type = "Review", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-5) }
                };
                foreach (var n in notifications) await notificationRepository.AddAsync(n);
            }

            // 6. Seed Promotions
            if (!(await promotionRepository.GetAllAsync()).Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion { Code = "WELCOME2024", DiscountPercentage = 10, Description = "Welcome discount for new members", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(1), IsActive = true },
                    new Promotion { Code = "SUMMER_SALE", DiscountPercentage = 15, Description = "Summer vacation special offer", ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(3), IsActive = true }
                };
                foreach (var p in promotions) await promotionRepository.AddAsync(p);
            }
            
            // 7. Seed Reviews
            if (!(await reviewRepository.GetAllAsync()).Any())
            {
                var customers = await customerRepository.GetAllAsync();
                var tours = await tourRepository.GetAllAsync();
                
                if (customers.Any() && tours.Any())
                {
                    var reviews = new List<Review>
                    {
                        new Review { TourId = tours.FirstOrDefault().Id, CustomerId = customers.FirstOrDefault().Id, Rating = 5, Comment = "Amazing experience! The landscapes were breathtaking.", CreatedAt = DateTime.UtcNow.AddDays(-10), IsApproved = true },
                        new Review { TourId = tours.LastOrDefault().Id, CustomerId = customers.LastOrDefault().Id, Rating = 4, Comment = "Great tour but the food could be better.", CreatedAt = DateTime.UtcNow.AddDays(-5), IsApproved = true }
                    };
                    foreach (var r in reviews) await reviewRepository.AddAsync(r);
                }
            }
        }
    }
}
