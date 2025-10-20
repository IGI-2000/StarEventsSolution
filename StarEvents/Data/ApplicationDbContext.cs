using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using StarEvents.Models.Domain;

namespace StarEvents.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
            // Do not call Database.SetInitializer here when using migrations via Application_Start.
        }

        // DbSet (single declaration for each)
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<EventOrganizer> EventOrganizers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<StoredPaymentMethod> PaymentMethods { get; set; }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Discount> Discounts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // TPH for BaseUser
            modelBuilder.Entity<BaseUser>()
                .Map<Admin>(m => m.Requires("UserType").HasValue("Admin"))
                .Map<Customer>(m => m.Requires("UserType").HasValue("Customer"))
                .Map<EventOrganizer>(m => m.Requires("UserType").HasValue("Organizer"));

            // Event -> Organizer / Venue
            modelBuilder.Entity<Event>()
                .HasRequired(e => e.Organizer)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganizerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Event>()
                .HasRequired(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .WillCascadeOnDelete(false);

            // TicketType -> Event
            modelBuilder.Entity<TicketType>()
                .HasRequired(tt => tt.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(tt => tt.EventId)
                .WillCascadeOnDelete(true);

            // Booking relationships
            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Booking>()
                .HasRequired(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .WillCascadeOnDelete(false);

            // Payment -> Booking (Booking side has no Payment navigation to avoid circular FK)
            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BookingDetail>()
                .HasRequired(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<BookingDetail>()
                .HasRequired(bd => bd.TicketType)
                .WithMany(tt => tt.BookingDetails)
                .HasForeignKey(bd => bd.TicketTypeId)
                .WillCascadeOnDelete(false);

            // ---- EXPLICIT DECIMAL PRECISION MAPPINGS (fix provider manifest mapping issues) ----
            // TicketType.Price
            modelBuilder.Entity<TicketType>().Property(t => t.Price).HasPrecision(18, 2);

            // BookingDetail.UnitPrice, BookingDetail.Subtotal
            modelBuilder.Entity<BookingDetail>().Property(bd => bd.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<BookingDetail>().Property(bd => bd.Subtotal).HasPrecision(18, 2);

            // Booking totals
            modelBuilder.Entity<Booking>().Property(b => b.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Booking>().Property(b => b.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Booking>().Property(b => b.FinalAmount).HasPrecision(18, 2);

            // Payment amount
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}