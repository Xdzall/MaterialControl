using Material_Control.Data;
using Microsoft.AspNetCore.Mvc;

namespace Material_Control.Controllers
{
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Approve(string id, string mode)
        {
            var userRole = HttpContext.Session.GetString("Role");
            string currentStatus = GetItemStatus(id, mode);

            if (userRole == "Admin" && currentStatus == "Pending at Admin")
            {
                UpdatePendingStatus(id, "Pending at Super Admin", mode);
                return Json(new { success = true, message = "Forwarded to Super Admin.", newStatus = "Pending at Super Admin" });
            }
            else if (userRole == "Super Admin" && currentStatus == "Pending at Super Admin")
            {
                UpdateItemStatusInMainTable(id, "Approved", mode);
                RemoveFromPending(id, mode);
                return Json(new { success = true, message = "Item approved.", newStatus = "Approved" });
            }

            return Json(new { success = false, message = "Authorization failed or item in wrong state." });
        }

        [HttpPost]
        public IActionResult Reject(string id, string mode)
        {
            var userRole = HttpContext.Session.GetString("Role");
            string currentStatus = GetItemStatus(id, mode);

            if (string.IsNullOrEmpty(currentStatus))
            {
                return Json(new { success = false, message = "Item not found in pending list." });
            }

            bool canReject = (userRole == "Admin" && currentStatus == "Pending at Admin") ||
                             (userRole == "Super Admin" && currentStatus == "Pending at Super Admin");

            if (canReject)
            {
                UpdateItemStatusInMainTable(id, "Rejected", mode);
                RemoveFromPending(id, mode);
                return Json(new { success = true, message = "Item rejected.", newStatus = "Rejected" });
            }

            return Json(new { success = false, message = "You are not authorized to reject this item." });
        }

        private string GetItemStatus(string id, string mode)
        {
            return mode switch
            {
                "Finished Goods" => _context.PendingApproval.Find(id)?.Status,
                "Parts" => _context.PendingApprovalParts.Find(id)?.Status,
                "Materials" => _context.PendingApprovalMaterials.Find(id)?.Status,
                _ => null
            };
        }

        private void UpdatePendingStatus(string id, string newStatus, string mode)
        {
            switch (mode)
            {
                case "Finished Goods":
                    _context.PendingApproval.Find(id).Status = newStatus;
                    break;
                case "Parts":
                    _context.PendingApprovalParts.Find(id).Status = newStatus;
                    break;
                case "Materials":
                    _context.PendingApprovalMaterials.Find(id).Status = newStatus;
                    break;
            }
            _context.SaveChanges();
        }

        private void RemoveFromPending(string id, string mode)
        {
            switch (mode)
            {
                case "Finished Goods":
                    var fgItem = _context.PendingApproval.Find(id);
                    if (fgItem != null) _context.PendingApproval.Remove(fgItem);
                    break;
                case "Parts":
                    var partItem = _context.PendingApprovalParts.Find(id);
                    if (partItem != null) _context.PendingApprovalParts.Remove(partItem);
                    break;
                case "Materials":
                    var matItem = _context.PendingApprovalMaterials.Find(id);
                    if (matItem != null) _context.PendingApprovalMaterials.Remove(matItem);
                    break;
            }
            _context.SaveChanges();
        }

        private void UpdateItemStatusInMainTable(string id, string status, string mode)
        {
            if (mode == "Finished Goods")
            {
                var item = _context.InventoryItems.Find(id);
                if (item != null) item.Status = status;
            }
            else if (mode == "Parts")
            {
                var item = _context.Parts.Find(id);
                if (item != null) item.Status = status;
            }
            else if (mode == "Materials")
            {
                var item = _context.Materials.Find(id);
                if (item != null) item.Status = status;
            }
            _context.SaveChanges();
        }
    }
}