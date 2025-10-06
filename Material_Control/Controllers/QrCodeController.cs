using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class QrCodeController : Controller
{
    [HttpGet]
    // PERBAIKAN: Mengubah nama parameter agar lebih generik
    public IActionResult Generate(string id, string projectName, string itemPartOrModel, string codeOrSp, string quantity, string location, string pic)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("ID is required.");
        }

        try
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            string qrContent = $"https://localhost:7032/Home/Create?id={id}";
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(8);

            // Mengirim semua parameter ke fungsi AddTextToImage
            Bitmap qrCodeWithText = AddTextToImage(qrCodeImage, id, projectName, itemPartOrModel, codeOrSp, quantity, location, pic);

            using (var stream = new MemoryStream())
            {
                qrCodeWithText.Save(stream, ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating QR Code: {ex.Message}.");
        }
    }

    private Bitmap AddTextToImage(Bitmap qrImage, string id, string projectName, string itemPartOrModel, string codeOrSp, string quantity, string location, string pic)
    {
        int newWidth = qrImage.Width + 400;
        int newHeight = qrImage.Height + 20;
        Bitmap newBitmap = new Bitmap(newWidth, newHeight);

        using (Graphics g = Graphics.FromImage(newBitmap))
        {
            g.Clear(Color.White);
            g.DrawImage(qrImage, new Point(10, 30));

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            StringFormat format = new StringFormat() { Alignment = StringAlignment.Near };
            Font font = new Font("Arial", 16, FontStyle.Regular);
            SolidBrush textBrush = new SolidBrush(Color.Black);

            float textX = qrImage.Width + 40;
            float yPos = 58;

            // --- PERBAIKAN: Logika Tampilan Teks ---
            string p1, p2;
            // Menentukan label berdasarkan apakah ID berisi "FG"
            if (id.Contains("FG"))
            {
                p1 = "Model Name:";
                p2 = "SP Number:";
            }
            else
            {
                p1 = "Item Part:";
                p2 = "Code Part:";
            }

            g.DrawString($"ID: {id}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"Project: {projectName}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"{p1} {itemPartOrModel}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"{p2} {codeOrSp}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"Quantity: {quantity}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"Location: {location}", font, textBrush, textX, yPos, format);
            yPos += 45;
            g.DrawString($"PIC: {pic}", font, textBrush, textX, yPos, format);

            g.Flush();
        }

        return newBitmap;
    }
}