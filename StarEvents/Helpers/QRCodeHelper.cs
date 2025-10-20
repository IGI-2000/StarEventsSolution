using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QRCoder;
using StarEvents.Models.Domain;

namespace StarEvents.Helpers
{
    public static class QRCodeHelper
    {
        public static byte[] GenerateQRCode(string data)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var bitmap = qrCode.GetGraphic(20))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Png);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        public static string GenerateTicketData(Ticket ticket)
        {
            // Minimal formatted payload. Optionally encrypt/sign.
            return $"TICKET:{ticket.TicketNumber}|BOOKING:{ticket.BookingId}|DATE:{ticket.IssueDate:yyyyMMdd}";
        }
    }
}