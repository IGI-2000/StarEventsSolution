// file: StarEvents\Services\Implementations\BookingService.cs
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Repositories.Interfaces;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventRepository _eventRepository;

        public BookingService(ApplicationDbContext context, IEventRepository eventRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        }

        // -----------------------------------------------------------
        // Retrieve Booking Form
        // -----------------------------------------------------------
        public async Task<BookingViewModel> GetBookingFormAsync(int eventId)
        {
            var evt = await _eventRepository.GetByIdAsync(eventId);
            if (evt == null) return null;

            return new BookingViewModel
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                EventDate = evt.EventDate,
                VenueName = evt.Venue?.VenueName,
                TicketSelections = evt.TicketTypes.Select(tt => new TicketSelectionViewModel
                {
                    TicketTypeId = tt.TicketTypeId,
                    TypeName = tt.TypeName,
                    Price = tt.Price,
                    AvailableQuantity = tt.AvailableQuantity,
                    Quantity = 0
                }).ToList()
            };
        }

        // -----------------------------------------------------------
        // Create Booking (Pending)
        // -----------------------------------------------------------
        public async Task<(bool Success, string ErrorMessage, int BookingId)> CreateBookingAsync(BookingViewModel model, int customerId)
        {
            if (model == null) return (false, "Invalid booking data", 0);
            if (!model.TicketSelections.Any(ts => ts.Quantity > 0)) return (false, "Select at least one ticket", 0);

            var evt = await _eventRepository.GetByIdAsync(model.EventId);
            if (evt == null) return (false, "Event not found", 0);

            // Validate availability
            foreach (var sel in model.TicketSelections.Where(s => s.Quantity > 0))
            {
                var tt = evt.TicketTypes.FirstOrDefault(t => t.TicketTypeId == sel.TicketTypeId);
                if (tt == null) return (false, $"Ticket type {sel.TypeName} not found", 0);
                if (sel.Quantity > tt.AvailableQuantity) return (false, $"Not enough tickets for {sel.TypeName}", 0);
            }

            var booking = new Booking
            {
                BookingReference = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}",
                BookingDate = DateTime.UtcNow,
                TotalAmount = 0m,
                DiscountAmount = 0m,
                FinalAmount = 0m,
                Status = BookingStatus.Pending,
                LoyaltyPointsEarned = 0,
                LoyaltyPointsUsed = 0,
                CustomerId = customerId,
                EventId = model.EventId
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Create booking details
            foreach (var sel in model.TicketSelections.Where(s => s.Quantity > 0))
            {
                var tt = await _context.TicketTypes.FindAsync(sel.TicketTypeId);
                if (tt == null) continue;

                var bd = new BookingDetail
                {
                    BookingId = booking.BookingId,
                    TicketTypeId = tt.TicketTypeId,
                    Quantity = sel.Quantity,
                    UnitPrice = tt.Price,
                    Subtotal = tt.Price * sel.Quantity
                };
                _context.BookingDetails.Add(bd);
            }

            await _context.SaveChangesAsync();

            // Recalculate totals
            var savedDetails = await _context.BookingDetails.Where(d => d.BookingId == booking.BookingId).ToListAsync();
            var total = savedDetails.Sum(d => d.Subtotal);
            decimal discount = booking.DiscountAmount;
            decimal final = total - discount;

            booking.TotalAmount = total;
            booking.DiscountAmount = discount;
            booking.FinalAmount = final;

            _context.Entry(booking).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return (true, null, booking.BookingId);
        }

        // -----------------------------------------------------------
        // Retrieve Booking Entity
        // -----------------------------------------------------------
        public async Task<Booking> GetBookingEntityAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Event.Venue)
                .Include(b => b.BookingDetails.Select(bd => bd.TicketType))
                .Include(b => b.Customer)
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        // -----------------------------------------------------------
        // Confirm Booking Payment
        // -----------------------------------------------------------
        public async Task<(bool Success, string ErrorMessage)> ConfirmBookingPaymentAsync(
            int bookingId,
            string transactionId,
            string cardLast4,
            string method,
            decimal amount)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    var booking = await _context.Bookings
                        .Include(b => b.BookingDetails)
                        .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                    if (booking == null) return (false, "Booking not found");

                    if (booking.Status == BookingStatus.Confirmed)
                        return (true, null);

                    // Validate availability
                    foreach (var bd in booking.BookingDetails)
                    {
                        var tt = await _context.TicketTypes.FindAsync(bd.TicketTypeId);
                        if (tt == null) return (false, "Ticket type not found");
                        if (bd.Quantity > tt.AvailableQuantity)
                            return (false, $"Not enough availability for {tt.TypeName}. Requested {bd.Quantity}, available {tt.AvailableQuantity}.");
                    }

                    // Decrement ticket availability
                    foreach (var bd in booking.BookingDetails)
                    {
                        var tt = await _context.TicketTypes.FindAsync(bd.TicketTypeId);
                        tt.AvailableQuantity -= bd.Quantity;
                        if (tt.AvailableQuantity < 0) tt.AvailableQuantity = 0;
                        _context.Entry(tt).State = EntityState.Modified;
                    }

                    // Check for existing payment
                    var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.BookingId == bookingId);
                    if (existingPayment != null)
                    {
                        booking.Status = BookingStatus.Confirmed;
                        _context.Entry(booking).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        tx.Commit();
                        return (true, null);
                    }

                    // Create Payment
                    var payment = new Payment
                    {
                        TransactionId = transactionId,
                        PaymentDate = DateTime.UtcNow,
                        Amount = amount,
                        Method = Enum.TryParse(method, out PaymentMethod pm) ? pm : PaymentMethod.OnlineBanking,
                        Status = PaymentStatus.Success,
                        PaymentGatewayResponse = "Simulated",
                        Last4 = cardLast4,
                        BookingId = booking.BookingId
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    booking.Status = BookingStatus.Confirmed;
                    _context.Entry(booking).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    tx.Commit();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return (false, ex.Message);
                }
            }
        }

        // -----------------------------------------------------------
        // Customer Bookings
        // -----------------------------------------------------------
        public async Task<List<Booking>> GetCustomerBookingsAsync(int customerId)
        {
            if (customerId <= 0) return new List<Booking>();

            return await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails.Select(bd => bd.TicketType))
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<BookingListItemViewModel>> GetCustomerBookingsForViewAsync(int customerId)
        {
            if (customerId <= 0) return new List<BookingListItemViewModel>();

            var list = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new
                {
                    b.BookingId,
                    b.BookingReference,
                    EventName = b.Event.EventName,
                    b.BookingDate,
                    b.FinalAmount,
                    b.Status,
                    TicketCount = b.BookingDetails.Sum(d => d.Quantity),
                    Tickets = b.Tickets.Select(t => new { t.TicketId, t.TicketNumber, t.SeatNumber })
                })
                .ToListAsync();

            return list.Select(x => new BookingListItemViewModel
            {
                BookingId = x.BookingId,
                BookingReference = x.BookingReference,
                EventName = x.EventName,
                BookingDate = x.BookingDate,
                FinalAmount = x.FinalAmount,
                Status = x.Status,
                TicketCount = x.TicketCount,
                Tickets = x.Tickets.Select(t => new TicketViewModel
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    SeatNumber = t.SeatNumber
                }).ToList()
            }).ToList();
        }

        public async Task<BookingDetailsViewModel> GetBookingDetailsForViewAsync(int bookingId)
        {
            var b = await _context.Bookings
                .AsNoTracking()
                .Include(x => x.Event)
                .Include(x => x.BookingDetails.Select(d => d.TicketType))
                .Include(x => x.Tickets)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);

            if (b == null) return null;

            var total = b.BookingDetails?.Sum(d => d.Subtotal) ?? b.TotalAmount;
            var discount = b.DiscountAmount;
            var final = total - discount;

            return new BookingDetailsViewModel
            {
                BookingId = b.BookingId,
                BookingReference = b.BookingReference,
                EventName = b.Event?.EventName,
                EventDate = b.Event?.EventDate,
                TotalAmount = total,
                DiscountAmount = discount,
                FinalAmount = final,
                Status = b.Status,
                Items = b.BookingDetails.Select(d => new BookingLineItemViewModel
                {
                    TicketTypeName = d.TicketType?.TypeName ?? string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList()
            };
        }
    }
}
