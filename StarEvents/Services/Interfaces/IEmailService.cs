using System;
using System.Threading.Tasks;

namespace StarEvents.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendTicketEmailAsync(int bookingId);
    }
}