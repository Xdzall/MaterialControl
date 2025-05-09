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

    // GET: Login Page
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: Handle Login Attempt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        // Retrieve the user from the database
        var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            // Store user details in the session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Nama", user.Name);
            HttpContext.Session.SetString("Role", user.Role); // Storing the role in the session

            // Check user role and redirect accordingly
            if (user.Role == "Admin")
            {
                return RedirectToAction("Privacy", "Home"); // Redirect to Privacy page for Admin
            }
            else if (user.Role == "Super Admin")
            {
                return RedirectToAction("Privacy", "Home"); // Redirect to Privacy page for Super Admin
            }
            else
            {
                return RedirectToAction("Index", "Home"); // Redirect to a different page for other roles
            }
        }

        // If the login is unsuccessful
        ViewBag.ErrorMessage = "Invalid username or password.";
        return View();
    }

    // Logout action
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // Clear the session
        return RedirectToAction("Login", "Account"); // Redirect to the login page
    }
}
