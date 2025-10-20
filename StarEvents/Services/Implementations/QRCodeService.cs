using System;
using QRCoder;
using System.Drawing;
using System.IO;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class QRCodeService : IQRCodeService
    {
        public byte[] GenerateQRCode(string data)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            using (var bmp = qrCode.GetGraphic(20))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public string CreateTicketData(int ticketId, string ticketNumber)
        {
            var payload = new { TicketId = ticketId, TicketNumber = ticketNumber, Issued = DateTime.UtcNow };
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(payload)));
        }

        public bool ValidateQRCode(string qrData)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(qrData));
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                return obj != null;
            }
            catch { return false; }
        }
    }
}