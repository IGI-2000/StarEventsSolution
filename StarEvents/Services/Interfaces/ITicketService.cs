using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.Domain;

namespace StarEvents.Services.Interfaces
{
    public interface ITicketService
    {
        Task<List<Ticket>> GetTicketsForBookingAsync(int bookingId);
        Task<List<Ticket>> GetTicketsByBookingIdAsync(int bookingId); // added alias for existing callers

        Task<Ticket> GetTicketByIdAsync(int ticketId);
        Task<byte[]> GenerateTicketQrAsync(int ticketId, string ticketNumber);
        Task<byte[]> GenerateTicketPdfAsync(int ticketId);
        Task GenerateTicketsAsync(int bookingId);
    }
}