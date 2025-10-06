using Microsoft.EntityFrameworkCore;
using Material_Control.Data;
using QuestPDF;
using QuestPDF.Infrastructure;
using Material_Control.Services;
using DinkToPdf;
using DinkToPdf.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

QuestPDF.Settings.License = LicenseType.Community;

;

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Super Admin"));
});

var dllPath = Path.Combine(Directory.GetCurrentDirectory(), "NativeLibrary", "libwkhtmltox.dll");
if (!File.Exists(dllPath))
{
    throw new FileNotFoundException("libwkhtmltox.dll not found in the root directory.", dllPath);
}
var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(dllPath);

builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
