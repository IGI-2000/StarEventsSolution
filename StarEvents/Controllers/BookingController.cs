using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        private readonly ITicketService _ticketService;
        private readonly ApplicationDbContext _dbContext;
        private const int MaxTicketsPerBooking = 10;

        public BookingController(
            IBookingService bookingService,
            IPaymentService paymentService,
            ITicketService ticketService,
            ApplicationDbContext dbContext)
        {
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // ------------------- BOOKING PROCESS -------------------
        [HttpGet]
        public async Task<ActionResult> BookTicket(int eventId)
        {
            var model = await _bookingService.GetBookingFormAsync(eventId);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Index", "Event");
            }

            ViewBag.MaxTicketsPerBooking = MaxTicketsPerBooking;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmBooking(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MaxTicketsPerBooking = MaxTicketsPerBooking;
                return View("BookTicket", model);
            }

            if (!model.TicketSelections.Any(ts => ts.Quantity > 0))
            {
                ModelState.AddModelError("", "Please select at least one ticket.");
                ViewBag.MaxTicketsPerBooking = MaxTicketsPerBooking;
                return View("BookTicket", model);
            }

            var totalSelected = model.TicketSelections.Sum(s => s.Quantity);
            if (totalSelected > MaxTicketsPerBooking)
            {
                ModelState.AddModelError("", $"You cannot book more than {MaxTicketsPerBooking} tickets at once.");
                ViewBag.MaxTicketsPerBooking = MaxTicketsPerBooking;
                return View("BookTicket", model);
            }

            var customerId = GetCurrentUserId();
            if (customerId == 0)
            {
                TempData["ErrorMessage"] = "Invalid user.";
                return RedirectToAction("Login", "Account");
            }

            var result = await _bookingService.CreateBookingAsync(model, customerId);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Booking failed");
                ViewBag.MaxTicketsPerBooking = MaxTicketsPerBooking;
                return View("BookTicket", model);
            }

            return RedirectToAction("ProcessPayment", new { bookingId = result.BookingId });
        }

        // ------------------- PAYMENT PROCESS -------------------
        [HttpGet]
        public async Task<ActionResult> ProcessPayment(int bookingId)
        {
            var booking = await _bookingService.GetBookingEntityAsync(bookingId);
            if (booking == null || booking.CustomerId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Booking not found or access denied.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] = "This booking cannot be paid because it’s not pending.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            var model = new PaymentViewModel
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                Amount = booking.FinalAmount,
                CustomerEmail = booking.Customer?.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessPayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var booking = await _bookingService.GetBookingEntityAsync(model.BookingId);
            if (booking == null || booking.CustomerId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Invalid booking.";
                return RedirectToAction("MyBookings");
            }

            var paymentResult = await _paymentService.ProcessPaymentAsync(model.BookingId, model);
            if (!paymentResult.Success)
            {
                ModelState.AddModelError("", paymentResult.ErrorMessage ?? "Payment failed.");
                return View(model);
            }

            var last4 = model.CardNumber?.Length >= 4 ? model.CardNumber.Substring(model.CardNumber.Length - 4) : null;
            var confirm = await _bookingService.ConfirmBookingPaymentAsync(
                model.BookingId,
                paymentResult.TransactionId,
                last4,
                model.Method ?? "CreditCard",
                model.Amount
            );

            if (!confirm.Success)
            {
                ModelState.AddModelError("", confirm.ErrorMessage ?? "Failed to confirm booking");
                return View(model);
            }

            await _ticketService.GenerateTicketsAsync(model.BookingId);
            // after tickets created, ensure QR is populated (convert bytes -> data URI)
            var tickets = await _ticketService.GetTicketsForBookingAsync(model.BookingId);
            foreach (var t in tickets)
            {
                if (string.IsNullOrEmpty(t.QrCode))
                {
                    var qrBytes = await _ticketService.GenerateTicketQrAsync(t.TicketId, t.TicketNumber);
                    if (qrBytes != null && qrBytes.Length > 0)
                    {
                        var dataUri = "data:image/png;base64," + Convert.ToBase64String(qrBytes);
                        t.QrCode = dataUri;
                        t.QrCodeBase64 = dataUri; // keep both properties in sync if present
                        _dbContext.Entry(t).State = EntityState.Modified;
                    }
                }
            }
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment successful! Tickets generated successfully.";
            return RedirectToAction("BookingConfirmation", new { id = model.BookingId });
        }

        // ------------------- RETRY / CANCEL PAYMENT -------------------
        [HttpGet]
        public ActionResult RetryPayment(int bookingId)
        {
            return RedirectToAction("ProcessPayment", new { bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelPayment(int bookingId)
        {
            var booking = await _bookingService.GetBookingEntityAsync(bookingId);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending bookings can be cancelled.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            TempData["SuccessMessage"] = "Payment cancelled. You can retry later.";
            return RedirectToAction("MyBookings");
        }

        // ------------------- BOOKING CONFIRMATION -------------------
        [HttpGet]
        [Authorize] // require authentication; allow admin detection inside method
        public async Task<ActionResult> BookingConfirmation(int id)
        {
            var booking = await _bookingService.GetBookingEntityAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            // Allow: customer owner OR admin (by role OR admin record in DB)
            var currentUserId = GetCurrentUserId();
            var email = User?.Identity?.Name;
            var isRoleAdmin = User?.IsInRole("Admin") ?? false;
            var isDbAdmin = false;
            try
            {
                if (!string.IsNullOrEmpty(email))
                    isDbAdmin = _dbContext.Admins.Any(a => a.Email == email);
            }
            catch
            {
                // ignore DB errors here to avoid leaking exceptions
            }

            if (!isRoleAdmin && !isDbAdmin && booking.CustomerId != currentUserId)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        // ------------------- CUSTOMER BOOKINGS -------------------
        [HttpGet]
        public async Task<ActionResult> MyBookings()
        {
            var customerId = GetCurrentUserId();
            if (customerId == 0)
            {
                TempData["ErrorMessage"] = "Invalid user.";
                return RedirectToAction("Index", "Home");
            }

            var bookings = await _bookingService.GetCustomerBookingsForViewAsync(customerId);
            return View(bookings);
        }

        [HttpGet]
        public async Task<ActionResult> BookingDetails(int id)
        {
            var bookingVm = await _bookingService.GetBookingDetailsForViewAsync(id);
            if (bookingVm == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            var ownerId = (await _bookingService.GetBookingEntityAsync(id))?.CustomerId ?? 0;
            if (ownerId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("MyBookings");
            }

            return View(bookingVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelBooking(int id)
        {
            var booking = await _bookingService.GetBookingEntityAsync(id);
            if (booking == null || booking.CustomerId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Booking not found or access denied.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Booking already cancelled.";
                return RedirectToAction("MyBookings");
            }

            foreach (var bd in booking.BookingDetails.ToList())
            {
                var tt = await _dbContext.TicketTypes.FindAsync(bd.TicketTypeId);
                if (tt != null)
                {
                    tt.AvailableQuantity += bd.Quantity;
                    _dbContext.Entry(tt).State = EntityState.Modified;
                }
            }

            booking.Status = BookingStatus.Cancelled;
            _dbContext.Entry(booking).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking cancelled and seats released.";
            return RedirectToAction("MyBookings");
        }

        // ------------------- DOWNLOAD TICKET -------------------
        [HttpGet]
        public async Task<ActionResult> DownloadTicket(int ticketId)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
            if (ticket == null || ticket.Booking?.CustomerId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Ticket not found or access denied.";
                return RedirectToAction("MyBookings");
            }

            var pdf = await _ticketService.GenerateTicketPdfAsync(ticketId);
            return File(pdf, "application/pdf", $"Ticket_{ticket.TicketNumber}.pdf");
        }

        // ------------------- HELPERS -------------------
        private int GetCurrentUserId()
        {
            try
            {
                var ci = User?.Identity as ClaimsIdentity;
                if (ci != null)
                {
                    var idClaim = ci.FindFirst(ClaimTypes.NameIdentifier);
                    if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
                        return parsed;
                }

                var email = User?.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    var customer = _dbContext.Customers.FirstOrDefault(c => c.Email == email);
                    if (customer != null)
                        return customer.Id;
                }
            }
            catch
            {
                // ignore
            }
            return 0;
        }
    }
}
