using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITicketService _ticketService;

        public EmailService(ApplicationDbContext context, ITicketService ticketService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        /// <summary>
        /// Generate ticket PDFs for the booking and send them to the booking customer via SMTP.
        /// SMTP settings are read from Web.config (system.net/mailSettings) or fallback to localhost.
        /// </summary>
        public async Task SendTicketEmailAsync(int bookingId)
        {
            var booking = _context.Bookings
                .Where(b => b.BookingId == bookingId)
                .Select(b => new
                {
                    b.BookingId,
                    b.BookingReference,
                    b.FinalAmount,
                    CustomerEmail = b.Customer.Email,
                    CustomerName = b.Customer.FirstName + " " + b.Customer.LastName
                })
                .FirstOrDefault();

            if (booking == null || string.IsNullOrWhiteSpace(booking.CustomerEmail))
                throw new InvalidOperationException("Booking or customer email not found.");

            // Get tickets
            var tickets = await _ticketService.GetTicketsByBookingIdAsync(bookingId);
            if (tickets == null || !tickets.Any())
            {
                // Nothing to send
                return;
            }

            // Generate PDFs (one per ticket) and build attachments
            var attachments = new System.Collections.Generic.List<Attachment>();
            try
            {
                foreach (var t in tickets)
                {
                    byte[] pdf = await _ticketService.GenerateTicketPdfAsync(t.TicketId);
                    var ms = new MemoryStream(pdf);
                    var attachment = new Attachment(ms, $"Ticket_{t.TicketNumber}.pdf", "application/pdf");
                    // Keep stream open until mail sent
                    attachments.Add(attachment);
                }

                // Build MailMessage
                var fromAddress = GetSmtpFromAddress();
                var mail = new MailMessage
                {
                    From = new MailAddress(fromAddress.Address, fromAddress.DisplayName),
                    Subject = $"Your StarEvents Tickets - Booking {booking.BookingReference}",
                    Body = $"Dear {booking.CustomerName},\n\nThank you for your booking. Attached are your e-tickets for booking reference {booking.BookingReference}.\n\nTotal paid: LKR {booking.FinalAmount:F2}\n\nPlease present the QR code at the venue entrance.\n\nRegards,\nStarEvents",
                    IsBodyHtml = false
                };
                mail.To.Add(booking.CustomerEmail);

                foreach (var att in attachments) mail.Attachments.Add(att);

                using (var smtp = CreateSmtpClient())
                {
                    await smtp.SendMailAsync(mail);
                }
            }
            finally
            {
                // dispose attachment streams
                foreach (var att in attachments)
                {
                    try { att.ContentStream?.Dispose(); } catch { }
                    try { att.Dispose(); } catch { }
                }
            }
        }

        private MailAddress GetSmtpFromAddress()
        {
            // Try to read from Web.config <system.net><mailSettings> if present (works in .NET Framework)
            try
            {
                var section = System.Configuration.ConfigurationManager.GetSection("system.net/mailSettings/smtp") as System.Net.Configuration.SmtpSection;
                if (section != null && !string.IsNullOrEmpty(section.From))
                    return new MailAddress(section.From, "StarEvents");
            }
            catch
            {
                // ignore and fallback
            }

            // fallback default
            return new MailAddress("noreply@starevents.lk", "StarEvents");
        }

        private SmtpClient CreateSmtpClient()
        {
            // Prefer mailSettings in Web.config
            try
            {
                var section = System.Configuration.ConfigurationManager.GetSection("system.net/mailSettings/smtp") as System.Net.Configuration.SmtpSection;
                if (section != null)
                {
                    var host = section.Network.Host;
                    var port = section.Network.Port;
                    var user = section.Network.UserName;
                    var pass = section.Network.Password;
                    var enableSsl = section.Network.EnableSsl;

                    var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl
                    };

                    if (!string.IsNullOrEmpty(user))
                    {
                        client.Credentials = new NetworkCredential(user, pass);
                    }
                    return client;
                }
            }
            catch
            {
                // ignore
            }

            // Fallback to localhost (no auth)
            return new SmtpClient("localhost");
        }
    }
}