using System;

namespace StarEvents.Services.Interfaces
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCode(string data);
        string CreateTicketData(int ticketId, string ticketNumber);
        bool ValidateQRCode(string qrData);
    }
}