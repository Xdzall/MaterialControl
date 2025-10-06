using Material_Control.Data;
using Material_Control.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        if (!string.IsNullOrEmpty(mode))
        {
            HttpContext.Session.SetString("CurrentMode", mode);
        }
        else
        {
            mode = HttpContext.Session.GetString("CurrentMode") ?? "Finished Goods";
        }
        ViewBag.Mode = mode;

        List<InventoryItemModel> inventoryItems = new();
        switch (mode)
        {
            case "Finished Goods":
                inventoryItems = _context.InventoryItems
                    .Where(x => x.Status == "Approved" || x.Status == "Rejected")
                    .ToList();
                break;
            case "Parts":
                inventoryItems = _context.Parts
                    .Where(p => p.Status == "Approved" || p.Status == "Rejected")
                    .Select(p => new InventoryItemModel
                    {
                        IdentificationNo = p.IdentificationNo,
                        ProjectName = p.ProjectName,
                        ItemPart = p.ItemPart,
                        CodePart = p.CodePart,
                        Quantity = p.Quantity,
                        StorageLocation = p.StorageLocation,
                        Purpose = p.Purpose,
                        CreatedAt = p.CreatedAt,
                        PIC = p.PIC,
                        Status = p.Status,
                        Borrower = p.Borrower,
                        RequestType = p.RequestType
                    }).ToList();
                break;
            case "Materials":
                inventoryItems = _context.Materials
                    .Where(m => m.Status == "Approved" || m.Status == "Rejected")
                    .Select(m => new InventoryItemModel
                    {
                        IdentificationNo = m.IdentificationNo,
                        ProjectName = m.ProjectName,
                        ItemPart = m.ItemPart,
                        CodePart = m.CodePart,
                        Quantity = m.Quantity,
                        StorageLocation = m.StorageLocation,
                        Purpose = m.Purpose,
                        CreatedAt = m.CreatedAt,
                        PIC = m.PIC,
                        Status = m.Status,
                        Borrower = m.Borrower,
                        RequestType = m.RequestType
                    }).ToList();
                break;
        }
        return View(inventoryItems);
    }

    [HttpGet]
    public IActionResult Create(string mode, string id)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            HttpContext.Session.SetString("CurrentMode", mode);
        }
        else
        {
            mode = HttpContext.Session.GetString("CurrentMode") ?? "Finished Goods";
        }
        ViewBag.Mode = mode;

        var now = DateTime.Now;
        var year = now.ToString("yy");
        var month = now.ToString("MM");
        string kodeTengah;
        int nextCount = 1; // Default

        if (mode == "Finished Goods")
        {
            kodeTengah = "FG";
            var lastItem = _context.InventoryItems
                .Where(i => i.CreatedAt.Year == now.Year && i.CreatedAt.Month == now.Month)
                .OrderByDescending(i => i.IdentificationNo)
                .FirstOrDefault();

            if (lastItem != null)
            {
                // 4 karakter terakhir (nomor urut) dan tambah 1
                string lastSequence = lastItem.IdentificationNo.Substring(lastItem.IdentificationNo.Length - 4);
                int.TryParse(lastSequence, out int lastCount);
                nextCount = lastCount + 1;
            }
        }
        else if (mode == "Parts")
        {
            kodeTengah = "P";
            var lastItem = _context.Parts
                .Where(p => p.CreatedAt.Year == now.Year && p.CreatedAt.Month == now.Month)
                .OrderByDescending(p => p.IdentificationNo)
                .FirstOrDefault();

            if (lastItem != null)
            {
                string lastSequence = lastItem.IdentificationNo.Substring(lastItem.IdentificationNo.Length - 4);
                int.TryParse(lastSequence, out int lastCount);
                nextCount = lastCount + 1;
            }
        }
        else
        {
            kodeTengah = "M";
            var lastItem = _context.Materials
                .Where(m => m.CreatedAt.Year == now.Year && m.CreatedAt.Month == now.Month)
                .OrderByDescending(m => m.IdentificationNo)
                .FirstOrDefault();

            if (lastItem != null)
            {
                string lastSequence = lastItem.IdentificationNo.Substring(lastItem.IdentificationNo.Length - 4);
                int.TryParse(lastSequence, out int lastCount);
                nextCount = lastCount + 1;
            }
        }

        var formattedCount = nextCount.ToString("D4");
        var generatedId = $"{year}{month}{kodeTengah}{formattedCount}";

        var username = HttpContext.Session.GetString("Username");
        if (!string.IsNullOrEmpty(username))
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                ViewBag.PICName = user.Name;
            }
        }

        var model = new PendingApproval { IdentificationNo = !string.IsNullOrEmpty(id) ? id : generatedId };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PendingApproval item, string mode)
    {
        // Cek apakah ini transaksi untuk item yang sudah ada (berdasarkan ID dari form)
        var existingItemInInventory = _context.InventoryItems.FirstOrDefault(i => i.IdentificationNo == item.IdentificationNo);
        var existingItemInParts = _context.Parts.FirstOrDefault(p => p.IdentificationNo == item.IdentificationNo);
        var existingItemInMaterials = _context.Materials.FirstOrDefault(m => m.IdentificationNo == item.IdentificationNo);

        // Jika ID dari form BUKAN ID yang baru digenerate DAN itemnya ada di database
        bool isUpdateOperation = existingItemInInventory != null || existingItemInParts != null || existingItemInMaterials != null;

        if (isUpdateOperation)
        {
            if (item.RequestType == "Borrowing")
            {
                if (string.IsNullOrEmpty(item.Borrower))
                {
                    ModelState.AddModelError("Borrower", "Borrower name is required.");
                }
                else
                {
                    if (mode == "Finished Goods" && existingItemInInventory != null)
                    {
                        existingItemInInventory.RequestType = item.RequestType;
                        existingItemInInventory.StorageLocation = item.StorageLocation;
                        existingItemInInventory.Purpose = item.Purpose;
                        existingItemInInventory.Borrower = item.Borrower;
                    }
                    else if (mode == "Parts" && existingItemInParts != null)
                    {
                        existingItemInParts.RequestType = item.RequestType;
                        existingItemInParts.StorageLocation = item.StorageLocation;
                        existingItemInParts.Purpose = item.Purpose;
                        existingItemInParts.Borrower = item.Borrower;
                    }
                    else if (mode == "Materials" && existingItemInMaterials != null)
                    {
                        existingItemInMaterials.RequestType = item.RequestType;
                        existingItemInMaterials.StorageLocation = item.StorageLocation;
                        existingItemInMaterials.Purpose = item.Purpose;
                        existingItemInMaterials.Borrower = item.Borrower;
                    }
                    _context.SaveChanges();
                    return RedirectToAction("Index", new { mode });
                }
            }
            else if (item.RequestType == "IN")
            {
                if (mode == "Finished Goods" && existingItemInInventory != null && existingItemInInventory.RequestType == "Borrowing")
                {
                    existingItemInInventory.RequestType = "IN";
                    existingItemInInventory.StorageLocation = item.StorageLocation;
                    existingItemInInventory.Purpose = item.Purpose;
                    existingItemInInventory.Borrower = null; // Hapus nama peminjam
                }
                else if (mode == "Parts" && existingItemInParts != null && existingItemInParts.RequestType == "Borrowing")
                {
                    existingItemInParts.RequestType = "IN";
                    existingItemInParts.StorageLocation = item.StorageLocation;
                    existingItemInParts.Purpose = item.Purpose;
                    existingItemInParts.Borrower = null;
                }
                else if (mode == "Materials" && existingItemInMaterials != null && existingItemInMaterials.RequestType == "Borrowing")
                {
                    existingItemInMaterials.RequestType = "IN";
                    existingItemInMaterials.StorageLocation = item.StorageLocation;
                    existingItemInMaterials.Purpose = item.Purpose;
                    existingItemInMaterials.Borrower = null;
                }
                _context.SaveChanges();
                return RedirectToAction("Index", new { mode });
            }
            else if (item.RequestType == "OUT" || item.RequestType == "SCRAP")
            {
                var newStatus = "Pending at Admin";

                if (mode == "Finished Goods" && existingItemInInventory != null)
                {
                    existingItemInInventory.RequestType = item.RequestType;
                    existingItemInInventory.StorageLocation = item.StorageLocation;
                    existingItemInInventory.Purpose = item.Purpose;
                    existingItemInInventory.Status = newStatus;
                    _context.PendingApproval.Add(new PendingApproval { IdentificationNo = existingItemInInventory.IdentificationNo, ProjectName = existingItemInInventory.ProjectName, ItemPart = existingItemInInventory.ItemPart, ModelName = existingItemInInventory.ModelName, SP_Number = existingItemInInventory.SP_Number, Quantity = existingItemInInventory.Quantity, StorageLocation = existingItemInInventory.StorageLocation, Purpose = existingItemInInventory.Purpose, CreatedAt = existingItemInInventory.CreatedAt, PIC = existingItemInInventory.PIC, Status = newStatus, RequestType = item.RequestType });
                }
                else if (mode == "Parts" && existingItemInParts != null)
                {
                    existingItemInParts.RequestType = item.RequestType;
                    existingItemInParts.StorageLocation = item.StorageLocation;
                    existingItemInParts.Purpose = item.Purpose;
                    existingItemInParts.Status = newStatus;
                    _context.PendingApprovalParts.Add(new PendingApprovalParts { IdentificationNo = existingItemInParts.IdentificationNo, ProjectName = existingItemInParts.ProjectName, ItemPart = existingItemInParts.ItemPart, CodePart = existingItemInParts.CodePart, Quantity = existingItemInParts.Quantity, StorageLocation = existingItemInParts.StorageLocation, Purpose = existingItemInParts.Purpose, CreatedAt = existingItemInParts.CreatedAt, PIC = existingItemInParts.PIC, Status = newStatus, RequestType = item.RequestType });
                }
                else if (mode == "Materials" && existingItemInMaterials != null)
                {
                    existingItemInMaterials.RequestType = item.RequestType;
                    existingItemInMaterials.StorageLocation = item.StorageLocation;
                    existingItemInMaterials.Purpose = item.Purpose;
                    existingItemInMaterials.Status = newStatus;
                    _context.PendingApprovalMaterials.Add(new PendingApprovalMaterials { IdentificationNo = existingItemInMaterials.IdentificationNo, ProjectName = existingItemInMaterials.ProjectName, ItemPart = existingItemInMaterials.ItemPart, CodePart = existingItemInMaterials.CodePart, Quantity = existingItemInMaterials.Quantity, StorageLocation = existingItemInMaterials.StorageLocation, Purpose = existingItemInMaterials.Purpose, CreatedAt = existingItemInMaterials.CreatedAt, PIC = existingItemInMaterials.PIC, Status = newStatus, RequestType = item.RequestType });
                }

                _context.SaveChanges();
                return RedirectToAction("Index", new { mode });
            }
        }
        else
        {
            ModelState.Clear();
            if (string.IsNullOrEmpty(item.ProjectName)) ModelState.AddModelError("ProjectName", "The Project Name field is required.");
            if (item.Quantity <= 0) ModelState.AddModelError("Quantity", "Quantity must be greater than 0.");
            if (string.IsNullOrEmpty(item.StorageLocation)) ModelState.AddModelError("StorageLocation", "The Storage Location field is required.");
            if (string.IsNullOrEmpty(item.Purpose)) ModelState.AddModelError("Purpose", "The Purpose field is required.");
            if (mode == "Finished Goods")
            {
                if (string.IsNullOrEmpty(item.ModelName))
                    ModelState.AddModelError("ModelName", "The Model Name field is required.");
                if (string.IsNullOrEmpty(item.SP_Number))
                    ModelState.AddModelError("SP_Number", "The SP Number field is required.");
            }
            else
            {
                if (string.IsNullOrEmpty(item.ItemPart))
                    ModelState.AddModelError("ItemPart", "The Item Part field is required.");
                if (string.IsNullOrEmpty(item.CodePart))
                    ModelState.AddModelError("CodePart", "The Code Part field is required.");
            }

            if (ModelState.IsValid)
            {
                var now = DateTime.Now;
                var year = now.ToString("yy");
                var month = now.ToString("MM");
                string kodeTengah;
                int countThisMonth;

                if (mode == "Finished Goods") { kodeTengah = "FG"; countThisMonth = _context.InventoryItems.Count(i => i.CreatedAt.Month == now.Month && i.CreatedAt.Year == now.Year); }
                else if (mode == "Parts") { kodeTengah = "P"; countThisMonth = _context.Parts.Count(p => p.CreatedAt.Month == now.Month && p.CreatedAt.Year == now.Year); }
                else { kodeTengah = "M"; countThisMonth = _context.Materials.Count(m => m.CreatedAt.Month == now.Month && m.CreatedAt.Year == now.Year); }

                var username = HttpContext.Session.GetString("Username");
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                var originalQuantityFromForm = item.Quantity;

                for (int i = 0; i < originalQuantityFromForm; i++)
                {
                    var currentCountInLoop = countThisMonth + i + 1;
                    var formattedCount = currentCountInLoop.ToString("D4");
                    var newId = $"{year}{month}{kodeTengah}{formattedCount}";

                    bool isDuplicate = _context.InventoryItems.Any(inv => inv.IdentificationNo == newId) || _context.Parts.Any(p => p.IdentificationNo == newId) || _context.Materials.Any(m => m.IdentificationNo == newId);
                    if (isDuplicate) { countThisMonth++; i--; continue; }

                    var status = (item.RequestType == "IN") ? "Approved" : "Pending at Admin";

                    if (mode == "Finished Goods")
                    {
                        var newItem = new InventoryItemModel { IdentificationNo = newId, ProjectName = item.ProjectName, ItemPart = item.ItemPart, ModelName = item.ModelName, SP_Number = item.SP_Number, Quantity = 1, StorageLocation = item.StorageLocation, Purpose = item.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType };
                        _context.InventoryItems.Add(newItem);
                        if (status == "Pending at Admin") { _context.PendingApproval.Add(new PendingApproval { IdentificationNo = newItem.IdentificationNo, ProjectName = newItem.ProjectName, ItemPart = newItem.ItemPart, ModelName = newItem.ModelName, SP_Number = newItem.SP_Number, Quantity = 1, StorageLocation = newItem.StorageLocation, Purpose = newItem.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType }); }
                    }
                    else if (mode == "Parts")
                    {
                        var newItem = new PartModel { IdentificationNo = newId, ProjectName = item.ProjectName, ItemPart = item.ItemPart, CodePart = item.CodePart, Quantity = 1, StorageLocation = item.StorageLocation, Purpose = item.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType };
                        _context.Parts.Add(newItem);
                        if (status == "Pending at Admin") { _context.PendingApprovalParts.Add(new PendingApprovalParts { IdentificationNo = newItem.IdentificationNo, ProjectName = newItem.ProjectName, ItemPart = newItem.ItemPart, CodePart = newItem.CodePart, Quantity = 1, StorageLocation = newItem.StorageLocation, Purpose = newItem.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType }); }
                    }
                    else if (mode == "Materials")
                    {
                        var newItem = new MaterialModel { IdentificationNo = newId, ProjectName = item.ProjectName, ItemPart = item.ItemPart, CodePart = item.CodePart, Quantity = 1, StorageLocation = item.StorageLocation, Purpose = item.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType };
                        _context.Materials.Add(newItem);
                        if (status == "Pending at Admin") { _context.PendingApprovalMaterials.Add(new PendingApprovalMaterials { IdentificationNo = newItem.IdentificationNo, ProjectName = newItem.ProjectName, ItemPart = newItem.ItemPart, CodePart = newItem.CodePart, Quantity = 1, StorageLocation = newItem.StorageLocation, Purpose = newItem.Purpose, CreatedAt = now, PIC = user?.Name, Status = status, RequestType = item.RequestType }); }
                    }
                }
                _context.SaveChanges();
                return RedirectToAction("Index", new { mode });
            }
        }

        ViewBag.Mode = mode;
        var currentUsername = HttpContext.Session.GetString("Username");
        if (!string.IsNullOrEmpty(currentUsername))
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == currentUsername);
            if (user != null)
            {
                ViewBag.PICName = user.Name;
            }
        }
        return View(item);
    }

    public IActionResult Privacy(string mode = "Finished Goods")
    {
        if (!string.IsNullOrEmpty(mode))
        {
            HttpContext.Session.SetString("CurrentMode", mode);
        }
        else
        {
            mode = HttpContext.Session.GetString("CurrentMode") ?? "Finished Goods";
        }
        ViewBag.Mode = mode;

        var userRole = HttpContext.Session.GetString("Role");
        List<PendingApproval> data = new();
        var statusFilter = userRole == "Admin" ? "Pending at Admin" :
                           userRole == "Super Admin" ? "Pending at Super Admin" : null;

        if (statusFilter != null || userRole == "Staff")
        {
            if (mode == "Finished Goods")
            {
                var query = _context.PendingApproval.AsQueryable();
                // Jika bukan Staff, filter status. Jika Staff, tampilkan semua.
                if (userRole != "Staff") query = query.Where(x => x.Status == statusFilter);
                data.AddRange(query);
            }
            else if (mode == "Parts")
            {
                var query = _context.PendingApprovalParts.AsQueryable();
                if (userRole != "Staff") query = query.Where(x => x.Status == statusFilter);
                data.AddRange(query.Select(p => new PendingApproval
                {
                    IdentificationNo = p.IdentificationNo,
                    ProjectName = p.ProjectName,
                    ItemPart = p.ItemPart,
                    CodePart = p.CodePart,
                    Quantity = p.Quantity,
                    StorageLocation = p.StorageLocation,
                    Purpose = p.Purpose,
                    CreatedAt = p.CreatedAt,
                    PIC = p.PIC,
                    Status = p.Status,
                    RequestType = p.RequestType
                }));
            }
            else if (mode == "Materials")
            {
                var query = _context.PendingApprovalMaterials.AsQueryable();
                if (userRole != "Staff") query = query.Where(x => x.Status == statusFilter);
                data.AddRange(query.Select(m => new PendingApproval
                {
                    IdentificationNo = m.IdentificationNo,
                    ProjectName = m.ProjectName,
                    ItemPart = m.ItemPart,
                    CodePart = m.CodePart,
                    Quantity = m.Quantity,
                    StorageLocation = m.StorageLocation,
                    Purpose = m.Purpose,
                    CreatedAt = m.CreatedAt,
                    PIC = m.PIC,
                    Status = m.Status,
                    RequestType = m.RequestType
                }));
            }
        }
        return View(data);
    }

    [HttpGet]
    public IActionResult GetItemById(string id)
    {
        string itemType = id.Contains("FG") ? "Finished Goods" :
                          id.Contains("P") ? "Parts" :
                          id.Contains("M") ? "Materials" :
                          "";

        object itemData = null;

        switch (itemType)
        {
            case "Finished Goods":
                var fgItem = _context.InventoryItems.FirstOrDefault(x => x.IdentificationNo == id && x.Status != "Rejected");
                if (fgItem != null)
                {
                    itemData = new
                    {
                        identificationNo = fgItem.IdentificationNo,
                        projectName = fgItem.ProjectName,
                        itemPart = "",
                        modelName = fgItem.ModelName,
                        spNumber = fgItem.SP_Number,
                        quantity = fgItem.Quantity,
                        storageLocation = fgItem.StorageLocation,
                        purpose = fgItem.Purpose,
                        pic = fgItem.PIC,
                        isActionable = (fgItem.Status == "Approved"), 
                        requestType = fgItem.RequestType,
                        borrower = fgItem.Borrower
                    };
                }
                break;

            case "Parts":
                var partItem = _context.Parts.FirstOrDefault(x => x.IdentificationNo == id && x.Status != "Rejected");
                if (partItem != null)
                {
                    itemData = new
                    {
                        identificationNo = partItem.IdentificationNo,
                        projectName = partItem.ProjectName,
                        itemPart = partItem.ItemPart,
                        codePart = partItem.CodePart,
                        quantity = partItem.Quantity,
                        storageLocation = partItem.StorageLocation,
                        purpose = partItem.Purpose,
                        pic = partItem.PIC,
                        isActionable = (partItem.Status == "Approved"),
                        requestType = partItem.RequestType,
                        borrower = partItem.Borrower
                    };
                }
                break;

            case "Materials":
                var matItem = _context.Materials.FirstOrDefault(x => x.IdentificationNo == id && x.Status != "Rejected");
                if (matItem != null)
                {
                    itemData = new
                    {
                        identificationNo = matItem.IdentificationNo,
                        projectName = matItem.ProjectName,
                        itemPart = matItem.ItemPart,
                        codePart = matItem.CodePart,
                        quantity = matItem.Quantity,
                        storageLocation = matItem.StorageLocation,
                        purpose = matItem.Purpose,
                        pic = matItem.PIC,
                        isActionable = (matItem.Status == "Approved"),
                        requestType = matItem.RequestType,
                        borrower = matItem.Borrower
                    };
                }
                break;
        }

        if (itemData == null)
        {
            return NotFound("Item not found or has been rejected.");
        }

        return Json(itemData);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string id, string mode)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Invalid ID");

        if (mode == "Finished Goods")
        {
            var item = _context.InventoryItems.Find(id);
            if (item != null) _context.InventoryItems.Remove(item);
        }
        else if (mode == "Parts")
        {
            var item = _context.Parts.Find(id);
            if (item != null) _context.Parts.Remove(item);
        }
        else if (mode == "Materials")
        {
            var item = _context.Materials.Find(id);
            if (item != null) _context.Materials.Remove(item);
        }

        _context.SaveChanges();
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}