using DinkToPdf;
using DinkToPdf.Contracts;
using Material_Control.Data;
using Material_Control.Services;
using Microsoft.AspNetCore.Mvc;
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
        var items = _context.InventoryItems.Where(x => x.IdentificationNo == id).ToList();
        if (items == null || !items.Any())
            return NotFound();

        // Render view menjadi HTML string
        var html = await _razorViewRenderer.RenderViewToStringAsync("Documents/DeliveryProof", items);

        // Membuat dokumen PDF dari HTML
        var pdfDoc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                DPI = 300
            },
            Objects = {
                new ObjectSettings() {
                    HtmlContent = html,
                    WebSettings = new WebSettings()
                    {
                        EnableIntelligentShrinking = true,
                        LoadImages = true,  
                        UserStyleSheet = "/path/to/local/styles.css"  
                    }
                }
            }
        };

        // Convert HTML ke PDF
        var pdfBytes = _pdfConverter.Convert(pdfDoc);

        // Menghasilkan file PDF
        return File(pdfBytes, "application/pdf", $"DeliveryProof_{id}.pdf");
    }
}
