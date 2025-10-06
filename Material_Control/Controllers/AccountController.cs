using Material_Control.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Nama", user.Name);
            HttpContext.Session.SetString("Role", user.Role);

            // Langsung arahkan ke mode default "Finished Goods"
            if (user.Role == "Admin" || user.Role == "Super Admin")//|| user.Role == "Staff"
            {
                return RedirectToAction("Privacy", "Home", new { mode = "Finished Goods" });
            }
            else
            {
                return RedirectToAction("Index", "Home", new { mode = "Finished Goods" });
            }
        }

        ViewBag.ErrorMessage = "Invalid username or password.";
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }
}