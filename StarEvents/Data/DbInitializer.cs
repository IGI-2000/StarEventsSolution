using System;
using System.Data.Entity.Migrations;
using System.Linq;
using BCrypt.Net;
using StarEvents.Models.Domain;

namespace StarEvents.Data
{
    public static class DbInitializer
    {
        public static void Initialize()
        {
            using (var context = new ApplicationDbContext())
            {
                context.Database.Initialize(false);

                // Admin
                if (!context.Admins.Any(a => a.Email == "admin@starevents.lk"))
                {
                    var admin = new Admin
                    {
                        Email = "admin@starevents.lk",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        FirstName = "System",
                        LastName = "Admin",
                        PhoneNumber = "0770000001",
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true,
                        Role = UserRole.Admin,
                        AdminLevel = "SuperAdmin"
                    };
                    context.Admins.Add(admin);
                }

                // Organizer
                if (!context.EventOrganizers.Any(o => o.Email == "organizer@starevents.lk"))
                {
                    var organizer = new EventOrganizer
                    {
                        Email = "organizer@starevents.lk",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Organizer@123"),
                        FirstName = "Org",
                        LastName = "One",
                        CompanyName = "Elite Events",
                        PhoneNumber = "0770000002",
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true,
                        Role = UserRole.Organizer,
                        IsVerified = true
                    };
                    context.EventOrganizers.Add(organizer);
                    context.SaveChanges(); // need id for organizer below
                }

                // Customer
                if (!context.Customers.Any(c => c.Email == "customer@starevents.lk"))
                {
                    var customer = new Customer
                    {
                        Email = "customer@starevents.lk",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                        FirstName = "Jane",
                        LastName = "Doe",
                        PhoneNumber = "0770000003",
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true,
                        Role = UserRole.Customer,
                        LoyaltyPoints = 100
                    };
                    context.Customers.Add(customer);
                }

                context.SaveChanges();

                // Seed a demo Venue + Event + TicketTypes if not present
                if (!context.Venues.Any(v => v.VenueName.Contains("BMICH Demo")))
                {
                    var venue = new Venue
                    {
                        VenueName = "BMICH Demo Venue",
                        Address = "Demo Address",
                        City = "Colombo",
                        Province = "Western",
                        Capacity = 2000,
                        ContactNumber = "0110000000",
                        Facilities = "Demo Facilities"
                    };
                    context.Venues.Add(venue);
                    context.SaveChanges();
                }

                var organizerEntity = context.EventOrganizers.FirstOrDefault(e => e.Email == "organizer@starevents.lk");
                var demoVenue = context.Venues.FirstOrDefault(v => v.VenueName.Contains("BMICH Demo"));

                if (organizerEntity != null && demoVenue != null && !context.Events.Any(ev => ev.EventName.Contains("Demo Concert")))
                {
                    var demoEvent = new Event
                    {
                        EventName = "Demo Concert",
                        Description = "Demo seeded concert for validation and testing.",
                        EventDate = DateTime.UtcNow.AddDays(30),
                        EventEndDate = DateTime.UtcNow.AddDays(30).AddHours(3),
                        Category = "Concert",
                        ImageUrl = "/images/events/music-festival.jpg",
                        TotalSeats = 500,
                        AvailableSeats = 500,
                        IsActive = true,
                        // Mark demo event as published so it shows up on home/events
                        IsPublished = true,
                        PublishedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow,
                        OrganizerId = organizerEntity.Id,
                        VenueId = demoVenue.VenueId
                    };
                    context.Events.Add(demoEvent);
                    context.SaveChanges();

                    context.TicketTypes.AddOrUpdate(tt => new { tt.EventId, tt.TypeName },
                        new TicketType { EventId = demoEvent.EventId, TypeName = "VIP", Price = 15000m, AvailableQuantity = 50, Description = "VIP seating" },
                        new TicketType { EventId = demoEvent.EventId, TypeName = "Regular", Price = 5000m, AvailableQuantity = 300, Description = "Regular seating" },
                        new TicketType { EventId = demoEvent.EventId, TypeName = "Economy", Price = 2500m, AvailableQuantity = 150, Description = "Economy" });
                    context.SaveChanges();
                }
            }
        }
    }
}