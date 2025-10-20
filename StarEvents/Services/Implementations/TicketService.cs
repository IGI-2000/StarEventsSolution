using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using QRCoder;
using System.Data.Entity;
using iTextSharp.text;
using iTextSharp.text.pdf;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _db;
        public TicketService(ApplicationDbContext db) { _db = db ?? throw new ArgumentNullException(nameof(db)); }

        // Returns PNG byte[] for given payload (used by QR generation)
        private byte[] CreateQrPngBytes(string payload)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            using (var qr = new QRCode(qrData))
            using (var bitmap = qr.GetGraphic(20))
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // Produce data URI (existing callers might use)
        private string CreateQrDataUri(string payload)
        {
            var png = CreateQrPngBytes(payload);
            var base64 = Convert.ToBase64String(png);
            return $"data:image/png;base64,{base64}";
        }

        // Interface implementation: get tickets by booking
        public async Task<List<Ticket>> GetTicketsForBookingAsync(int bookingId)
        {
            return await _db.Tickets
                .AsNoTracking()
                .Include(t => t.TicketType)
                .Where(t => t.BookingId == bookingId)
                .OrderBy(t => t.TicketId)
                .ToListAsync();
        }

        // Alias - keep compatibility
        public Task<List<Ticket>> GetTicketsByBookingIdAsync(int bookingId)
        {
            return GetTicketsForBookingAsync(bookingId);
        }

        public async Task<Ticket> GetTicketByIdAsync(int ticketId)
        {
            return await _db.Tickets
                .Include(t => t.TicketType)
                .Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);
        }

        // Return PNG bytes for QR (ticket image)
        public async Task<byte[]> GenerateTicketQrAsync(int ticketId, string ticketNumber)
        {
            // If ticketNumber not provided, try load from DB
            if (string.IsNullOrWhiteSpace(ticketNumber))
            {
                var t = await _db.Tickets.FindAsync(ticketId);
                ticketNumber = t?.TicketNumber ?? $"TICKET-{ticketId}";
            }

            var payload = $"ticket:{ticketNumber};id:{ticketId};issued:{DateTime.UtcNow:o}";
            return CreateQrPngBytes(payload);
        }

        // Generate a simple PDF (A6) for the ticket and return PDF bytes
        // Requires iTextSharp (Install-Package iTextSharp -Version 5.5.13.2)
        public async Task<byte[]> GenerateTicketPdfAsync(int ticketId)
        {
            var ticket = await _db.Tickets
                .Include(t => t.TicketType)
                .Include(t => t.Booking)
                .Include("Booking.Event")
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null) throw new InvalidOperationException("Ticket not found");

            var qrBytes = await GenerateTicketQrAsync(ticketId, ticket.TicketNumber);

            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A6);
                var writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                doc.Add(new Paragraph("Event Ticket", titleFont));
                doc.Add(new Paragraph($"Ticket: {ticket.TicketNumber}", textFont));
                if (ticket.Booking != null)
                {
                    doc.Add(new Paragraph($"Booking: {ticket.Booking.BookingReference}", textFont));
                    doc.Add(new Paragraph($"Booking Date: {ticket.Booking.BookingDate.ToLocalTime():g}", textFont));
                }
                if (ticket.TicketType != null)
                {
                    doc.Add(new Paragraph($"Type: {ticket.TicketType.TypeName}", textFont));
                    doc.Add(new Paragraph($"Price: {ticket.TicketType.Price:C}", textFont));
                }
                if (!string.IsNullOrEmpty(ticket.SeatNumber))
                {
                    doc.Add(new Paragraph($"Seat: {ticket.SeatNumber}", textFont));
                }
                doc.Add(new Paragraph($"Issued: {ticket.IssueDate.ToLocalTime():g}", textFont));
                doc.Add(Chunk.NEWLINE);

                // add QR image
                var qrImg = iTextSharp.text.Image.GetInstance(qrBytes);
                qrImg.Alignment = Element.ALIGN_CENTER;
                qrImg.ScaleToFit(200f, 200f);
                doc.Add(qrImg);

                doc.Close();
                writer.Close();
                return ms.ToArray();
            }
        }

        // Create tickets for a booking (alias of GenerateTicketsForBookingAsync)
        public Task GenerateTicketsAsync(int bookingId)
        {
            return GenerateTicketsForBookingAsync(bookingId);
        }

        // Generate sequential ticket numbers per booking and create Ticket entities with QR data-uri.
        public async Task GenerateTicketsForBookingAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .Include(b => b.BookingDetails)
                .Include("BookingDetails.TicketType")
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) throw new InvalidOperationException("Booking not found");

            // Normalize booking reference (remove spaces)
            var bookingRefSafe = string.IsNullOrWhiteSpace(booking.BookingReference) ? "BK" : booking.BookingReference.Trim();

            // Determine current number of tickets already created for this booking to continue sequence
            var existingCount = await _db.Tickets.CountAsync(t => t.BookingId == bookingId);
            int createdSoFar = 0;

            foreach (var bd in booking.BookingDetails)
            {
                for (int i = 0; i < bd.Quantity; i++)
                {
                    createdSoFar++;
                    var seqNumber = existingCount + createdSoFar;
                    // sequence formatted as 4 digits: 0001, 0002, ...
                    var seqSuffix = seqNumber.ToString("D4");
                    var ticketNumber = $"{bookingRefSafe}-{seqSuffix}";

                    // payload includes ticketNumber and more info for QR
                    var payload = $"ticket:{ticketNumber};booking:{booking.BookingReference};event:{booking.Event?.EventName};type:{bd.TicketType?.TypeName};issued:{DateTime.UtcNow:o}";
                    var pngBytes = CreateQrPngBytes(payload);
                    var dataUri = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";

                    var ticket = new Ticket
                    {
                        TicketNumber = ticketNumber,
                        BookingId = bookingId,
                        TicketTypeId = bd.TicketTypeId,
                        IssueDate = DateTime.UtcNow,
                        QrCodeBase64 = dataUri,
                        QrCode = dataUri,
                        SeatNumber = null
                    };

                    _db.Tickets.Add(ticket);

                    // decrement available quantity on TicketType (best-effort)
                    var tt = await _db.TicketTypes.FindAsync(bd.TicketTypeId);
                    if (tt != null)
                    {
                        tt.AvailableQuantity = Math.Max(0, tt.AvailableQuantity - 1);
                        _db.Entry(tt).State = EntityState.Modified;
                    }
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}