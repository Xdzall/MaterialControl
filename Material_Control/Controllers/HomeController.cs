using Material_Control.Data;
using Material_Control.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;


public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(string mode = "Finished Goods")
    {
        List<InventoryItemModel> inventoryItems = new();

        ViewBag.Mode = mode;

        switch (mode)
        {
            case "Finished Goods":
                inventoryItems = _context.InventoryItems
                    .Where(x => x.Status == "Approved")
                    .ToList();
                break;

            case "Parts":
                inventoryItems = _context.Parts
                    .Where(p => p.Status == "Approved")
                    .Select(p => new InventoryItemModel
                    {
                        IdentificationNo = p.IdentificationNo,
                        ItemPart = p.ItemPart,
                        CodePart = p.CodePart,
                        Quantity = p.Quantity,
                        StorageLocation = p.StorageLocation,
                        Purpose = p.Purpose,
                        CreatedAt = p.CreatedAt,
                        PIC = p.PIC,
                        Status = p.Status,
                        RequestType = p.RequestType
                    }).ToList();
                break;

            case "Materials":
                inventoryItems = _context.Materials
                    .Where(m => m.Status == "Approved")
                    .Select(m => new InventoryItemModel
                    {
                        IdentificationNo = m.IdentificationNo,
                        ItemPart = m.ItemPart,
                        CodePart = m.CodePart,
                        Quantity = m.Quantity,
                        StorageLocation = m.StorageLocation,
                        Purpose = m.Purpose,
                        CreatedAt = m.CreatedAt,
                        PIC = m.PIC,
                        Status = m.Status,
                        RequestType = m.RequestType
                    }).ToList();
                break;

            default:
                TempData["Error"] = $"Mode '{mode}' tidak dikenali.";
                break;
        }

        return View(inventoryItems);
    }

    public IActionResult Login()
    {
        var username = HttpContext.Session.GetString("Username");

        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.Username = username;
        return View();
    }

    public IActionResult Select()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Create(string mode = "Finished Goods")
    {
        var now = DateTime.Now;
        var year = now.ToString("yy");
        var month = now.ToString("MM");

        string kodeTengah;
        int countThisMonth;

        if (mode == "Finished Goods")
        {
            kodeTengah = "FG";
            countThisMonth = _context.InventoryItems
                .Where(i => i.CreatedAt.Month == now.Month && i.CreatedAt.Year == now.Year)
                .Count() + 1;
        }
        else if (mode == "Parts")
        {
            kodeTengah = "P";
            countThisMonth = _context.Parts
                .Where(p => p.CreatedAt.Month == now.Month && p.CreatedAt.Year == now.Year)
                .Count() + 1;
        }
        else
        {
            kodeTengah = "M";
            countThisMonth = _context.Materials
                .Where(m => m.CreatedAt.Month == now.Month && m.CreatedAt.Year == now.Year)
                .Count() + 1;
        }

        var formattedCount = countThisMonth.ToString("D4");
        var generatedId = $"{year}{month}{kodeTengah}{formattedCount}";

        ViewBag.Mode = mode;

        var username = HttpContext.Session.GetString("Username");
        if (!string.IsNullOrEmpty(username))
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                ViewBag.PICName = user.Name;
            }
        }

        var model = new PendingApproval
        {
            IdentificationNo = generatedId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PendingApproval item, string mode)
    {
        if (ModelState.IsValid)
        {
            item.CreatedAt = DateTime.Now;
            item.Status = "Pending at Admin";

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    item.PIC = user.Name;
                }
            }

            _context.PendingApproval.Add(item); 

            if (mode == "Finished Goods")
            {
                var inventoryItem = new InventoryItemModel
                {
                    IdentificationNo = item.IdentificationNo,
                    ItemPart = item.ItemPart,
                    CodePart = item.CodePart,
                    Quantity = item.Quantity,
                    StorageLocation = item.StorageLocation,
                    Purpose = item.Purpose,
                    CreatedAt = item.CreatedAt,
                    PIC = item.PIC,
                    Status = item.Status,
                    RequestType = item.RequestType
                };
                _context.InventoryItems.Add(inventoryItem);
            }
            else if (mode == "Parts")
            {
                var part = new PartModel
                {
                    IdentificationNo = item.IdentificationNo,
                    ItemPart = item.ItemPart,
                    CodePart = item.CodePart,
                    Quantity = item.Quantity,
                    StorageLocation = item.StorageLocation,
                    Purpose = item.Purpose,
                    CreatedAt = item.CreatedAt,
                    PIC = item.PIC,
                    Status = item.Status,
                    RequestType = item.RequestType
                };
                _context.Parts.Add(part);
            }
            else if (mode == "Materials")
            {
                var material = new MaterialModel
                {
                    IdentificationNo = item.IdentificationNo,
                    ItemPart = item.ItemPart,
                    CodePart = item.CodePart,
                    Quantity = item.Quantity,
                    StorageLocation = item.StorageLocation,
                    Purpose = item.Purpose,
                    CreatedAt = item.CreatedAt,
                    PIC = item.PIC,
                    Status = item.Status,
                    RequestType = item.RequestType
                };
                _context.Materials.Add(material);
            }

            _context.SaveChanges();

            return RedirectToAction("Create", new { mode });
        }

        return View(item);
    }


    public IActionResult Success(string id)
    {
        ViewBag.IdentificationNo = id;
        return View();
    }


    public IActionResult Privacy(string mode = "Finished Goods")
    {
        List<PendingApproval> data = new();

        if (mode == "Finished Goods")
        {
            data = _context.InventoryItems
                .Where(x => x.Status.Contains("Pending"))
                .Select(x => new PendingApproval
                {
                    IdentificationNo = x.IdentificationNo,
                    ItemPart = x.ItemPart,
                    CodePart = x.CodePart,
                    Quantity = x.Quantity,
                    StorageLocation = x.StorageLocation,
                    Purpose = x.Purpose,
                    CreatedAt = x.CreatedAt,
                    PIC = x.PIC,
                    Status = x.Status,
                    RequestType = x.RequestType
                }).ToList();
        }
        else if (mode == "Parts")
        {
            data = _context.Parts
                .Where(x => x.Status.Contains("Pending"))
                .Select(x => new PendingApproval
                {
                    IdentificationNo = x.IdentificationNo,
                    ItemPart = x.ItemPart,
                    CodePart = x.CodePart,
                    Quantity = x.Quantity,
                    StorageLocation = x.StorageLocation,
                    Purpose = x.Purpose,
                    CreatedAt = x.CreatedAt,
                    PIC = x.PIC,
                    Status = x.Status,
                    RequestType = x.RequestType
                }).ToList();
        }
        else if (mode == "Materials")
        {
            data = _context.Materials
                .Where(x => x.Status.Contains("Pending"))
                .Select(x => new PendingApproval
                {
                    IdentificationNo = x.IdentificationNo,
                    ItemPart = x.ItemPart,
                    CodePart = x.CodePart,
                    Quantity = x.Quantity,
                    StorageLocation = x.StorageLocation,
                    Purpose = x.Purpose,
                    CreatedAt = x.CreatedAt,
                    PIC = x.PIC,
                    Status = x.Status,
                    RequestType = x.RequestType
                }).ToList();
        }

        ViewBag.Mode = mode;
        return View(data);
    }

    [HttpGet]
    public IActionResult GetItemById(string id)
    {
        var item = _context.InventoryItems.FirstOrDefault(x => x.IdentificationNo == id);

        if (item == null)
            return NotFound();

        return Json(new
        {
            identificationNo = item.IdentificationNo,
            itemPart = item.ItemPart,
            codePart = item.CodePart,
            quantity = item.Quantity,
            storageLocation = item.StorageLocation,
            purpose = item.Purpose,
            pic = item.PIC
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Invalid ID");

        var item = _context.PendingApproval.FirstOrDefault(x => x.IdentificationNo == id);
        if (item == null)
            return NotFound("Item not found");

        _context.PendingApproval.Remove(item);
        _context.SaveChanges();

        return Ok();
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}