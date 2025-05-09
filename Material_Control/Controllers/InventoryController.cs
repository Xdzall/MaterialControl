using Material_Control.Data;
using Material_Control.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            if (string.IsNullOrEmpty(mode)) mode = "FinishedGoods";

            Console.WriteLine($"Approve Requested. ID: '{id}' | Mode: '{mode}'");

            try 
            {
                var item = _context.PendingApproval.FirstOrDefault(x => x.IdentificationNo == id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                // Debug information
                string originalStatus = item.Status;
                Console.WriteLine($"Original Status: '{originalStatus}'");

                // Get user role from session
                var userRole = HttpContext.Session.GetString("Role");
                Console.WriteLine($"Role from session: '{userRole}'");

                // Check user roles either from session or claims
                bool isAdmin = userRole == "Admin" || User.IsInRole("Admin");
                bool isSuperAdmin = userRole == "Super Admin" || User.IsInRole("Super Admin");
        
                Console.WriteLine($"Is Admin: {isAdmin}, Is Super Admin: {isSuperAdmin}");
                Console.WriteLine($"Current item status: '{item.Status?.Trim()}'");

                // Process based on role and current status
                if (isAdmin && (item.Status?.Trim() == "Pending at Admin" || item.Status?.Trim() == "Pending"))
                {
                    // Update status
                    item.Status = "Pending at Super Admin";
                    Console.WriteLine("Item status changed to 'Pending at Super Admin'");
            
                    _context.Update(item);
                    int rowsAffected = _context.SaveChanges();
                    Console.WriteLine($"SaveChanges executed. Rows affected: {rowsAffected}");
            
                    return Json(new { 
                        success = true, 
                        message = "Item forwarded to Super Admin for final approval.", 
                        newStatus = item.Status,
                        dbUpdated = rowsAffected > 0
                    });
                }
                else if (isSuperAdmin && item.Status?.Trim() == "Pending at Super Admin")
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            item.Status = "Approved";

                            if (mode == "FinishedGoods")
                            {
                                _context.InventoryItems.Add(new InventoryItemModel
                                {
                                    IdentificationNo = item.IdentificationNo,
                                    ItemPart = item.ItemPart,
                                    CodePart = item.CodePart,
                                    Quantity = item.Quantity,
                                    StorageLocation = item.StorageLocation,
                                    Purpose = item.Purpose,
                                    PIC = item.PIC,
                                    CreatedAt = item.CreatedAt,
                                    Status = item.Status,
                                    RequestType = item.RequestType
                                });
                            }
                            else if (mode == "Parts")
                            {
                                _context.Parts.Add(new PartModel
                                {
                                    IdentificationNo = item.IdentificationNo,
                                    ItemPart = item.ItemPart,
                                    CodePart = item.CodePart,
                                    Quantity = item.Quantity,
                                    StorageLocation = item.StorageLocation,
                                    Purpose = item.Purpose,
                                    PIC = item.PIC,
                                    CreatedAt = item.CreatedAt,
                                    Status = item.Status,
                                    RequestType = item.RequestType
                                });
                            }
                            else if (mode == "Materials")
                            {
                                _context.Materials.Add(new MaterialModel
                                {
                                    IdentificationNo = item.IdentificationNo,
                                    ItemPart = item.ItemPart,
                                    CodePart = item.CodePart,
                                    Quantity = item.Quantity,
                                    StorageLocation = item.StorageLocation,
                                    Purpose = item.Purpose,
                                    PIC = item.PIC,
                                    CreatedAt = item.CreatedAt,
                                    Status = item.Status,
                                    RequestType = item.RequestType
                                });
                            }

                            _context.Update(item);
                            _context.SaveChanges();

                            _context.PendingApproval.Remove(item);
                            int rowsAffected = _context.SaveChanges();
                    
                            transaction.Commit();
                    
                            Console.WriteLine($"Transaction committed. Rows affected: {rowsAffected}");
                    
                            return Json(new { 
                                success = true, 
                                message = "Item approved successfully and moved to inventory.", 
                                newStatus = "Approved",
                                dbUpdated = rowsAffected > 0
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Transaction error: {ex.Message}");
                            return Json(new { success = false, message = $"Database error: {ex.Message}" });
                        }
                    }
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"This item cannot be approved at this stage. Current status: {item.Status}, Your role: {userRole}"
                    });
                }
            }
            catch (Exception ex)
            {
                _context.SaveChanges();
                Console.WriteLine($"Exception in Approve action: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }


        [HttpPost]
        public IActionResult Reject(string id, string mode)
        {
            var targetItem = _context.PendingApproval.FirstOrDefault(x => x.IdentificationNo == id);

            if (targetItem != null)
            {
                targetItem.Status = "Rejected";
                _context.SaveChanges();

                return Json(new { success = true, message = "Item rejected successfully.", newStatus = targetItem.Status });
            }

            return Json(new { success = false, message = "Item not found." });
        }

        public IActionResult PendingApproval(string mode = "FinishedGoods")
        {
            var items = _context.PendingApproval
                .Where(x => x.RequestType == mode && x.Status != "Rejected" && x.Status != "Approved")
                .ToList();

            ViewBag.Mode = mode;

            return View(items);
        }
    }
}
