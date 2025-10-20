using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.Domain;

namespace StarEvents.Helpers
{
    public static class EmailHelper
    {
        public static async Task SendTicketEmailAsync(Customer customer, Booking booking, List<Ticket> tickets)
        {
            // For development: write to debug output or log.
            await Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Sending ticket email to {customer.Email} for booking {booking.BookingReference}");
                // Real implementation: SMTP or external provider
            });
        }

        public static string GenerateTicketEmailBody(Booking booking, List<Ticket> tickets)
        {
            return $"Booking Reference: {booking.BookingReference}\nEvent: {booking.Event?.EventName}\nTickets: {tickets.Count}\nThank you for booking with StarEvents.";
        }
    }
}