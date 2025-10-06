using DinkToPdf;
using DinkToPdf.Contracts;
using Material_Control.Data;
using Material_Control.Models;
using Material_Control.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DocumentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IRazorViewToStringRenderer _razorViewRenderer;
    private readonly IConverter _pdfConverter;

    public DocumentsController(AppDbContext context, IRazorViewToStringRenderer razorViewRenderer, IConverter pdfConverter)
    {
        _context = context;
        _razorViewRenderer = razorViewRenderer;
        _pdfConverter = pdfConverter;
    }

    [HttpGet]
    public async Task<IActionResult> DeliveryProof(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Identification No is required.");
        }

        var itemsForPdf = new List<InventoryItemModel>();
        string mode = "";

        // Menentukan mode berdasarkan ID
        if (id.Contains("FG"))
        {
            mode = "Finished Goods";
            var item = _context.InventoryItems.FirstOrDefault(x => x.IdentificationNo == id);
            if (item != null) itemsForPdf.Add(item);
        }
        else if (id.Contains("P"))
        {
            mode = "Parts";
            var item = _context.Parts.FirstOrDefault(x => x.IdentificationNo == id);
            if (item != null)
            {
                // Memetakan PartModel ke InventoryItemModel untuk digunakan di view
                itemsForPdf.Add(new InventoryItemModel
                {
                    ItemPart = item.ItemPart,
                    CodePart = item.CodePart,
                    Quantity = item.Quantity,
                    RequestType = item.RequestType,
                    Purpose = item.Purpose
                });
            }
        }
        else if (id.Contains("M"))
        {
            mode = "Materials";
            var item = _context.Materials.FirstOrDefault(x => x.IdentificationNo == id);
            if (item != null)
            {
                // Memetakan MaterialModel ke InventoryItemModel untuk digunakan di view
                itemsForPdf.Add(new InventoryItemModel
                {
                    ItemPart = item.ItemPart,
                    CodePart = item.CodePart,
                    Quantity = item.Quantity,
                    RequestType = item.RequestType,
                    Purpose = item.Purpose
                });
            }
        }

        if (!itemsForPdf.Any())
        {
            return NotFound("Item not found.");
        }

        // Mengirim mode ke view melalui ViewBag
        ViewBag.Mode = mode;

        // Render view menjadi HTML string
        var html = await _razorViewRenderer.RenderViewToStringAsync("Documents/DeliveryProof", itemsForPdf);

        var pdfDoc = new HtmlToPdfDocument()
        {
            GlobalSettings = { PaperSize = PaperKind.A4, Orientation = Orientation.Portrait, DPI = 300 },
            Objects = {
                new ObjectSettings() {
                    HtmlContent = html,
                    WebSettings = new WebSettings() { EnableIntelligentShrinking = true, LoadImages = true }
                }
            }
        };

        var pdfBytes = _pdfConverter.Convert(pdfDoc);
        return File(pdfBytes, "application/pdf", $"DeliveryProof_{id}.pdf");
    }
}