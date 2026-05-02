using ERP_System.Core;
using ERP_System.Web;
using ERP_System.Web.appMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// 1. REJESTRACJA SERWISÓW (Zgodnie z Twoimi plikami)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<CategoryService>(); // Poprawna nazwa serwisu
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ContractorService>();
builder.Services.AddScoped<InvoiceService>();

// KONFIGURACJA SESJI - Niezbędna do działania ContractorsEndpoints
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// QuestPDF License
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// Konfiguracja Bazy Danych
var connectionStringLocal = builder.Configuration.GetConnectionString("HbmDatabase");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionStringLocal, b =>
        b.MigrationsAssembly("ERP_System.Core")));

var app = builder.Build();

// 2. INICJALIZACJA BAZY I DANYCH DOMYŚLNYCH
app.UpdateDatabase();

using (var scope = app.Services.CreateScope())
{
    var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
    categoryService.addDefaultCategories();
}

// 3. KONFIGURACJA POTOKU (Middleware)
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// UseSession musi być przed mapowaniem endpointów!
app.UseSession(); 

// 4. MAPOWANIE ENDPOINTÓW
// MapAllEndpoints zajmuje się klasami implementującymi IEndpoint (Login, Dashboard itp.)
app.MapAllEndpoints(); 

// MapContractorEndpoints to Twoja dedykowana metoda rozszerzająca (static)
app.MapContractorEndpoints(); 

app.Run();