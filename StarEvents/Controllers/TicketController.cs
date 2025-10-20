using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        // GET: Ticket
        public ActionResult Index()
        {
            return View();
        }

        // GET /Ticket/Download/5
        public async Task<ActionResult> Download(int id)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(id);
            if (ticket == null) return HttpNotFound();

            // only confirmed bookings allowed to download
            if (ticket.Booking == null || ticket.Booking.Status != global::StarEvents.Models.Domain.BookingStatus.Confirmed)
                return new HttpStatusCodeResult(403, "Ticket not available for download until booking is confirmed.");

            var pdf = await _ticketService.GenerateTicketPdfAsync(id);
            if (pdf == null) return HttpNotFound();
            return File(pdf, "application/pdf", $"Ticket_{ticket.TicketNumber}.pdf");
        }
    }
}