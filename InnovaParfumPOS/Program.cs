using InnovaParfumPOS.Frontend.Components;
using InnovaParfumPOS.Backend.Models;
using InnovaParfumPOS.Backend.Services;
using InnovaParfumPOS.Frontend.Services;
using Microsoft.EntityFrameworkCore;
using InnovaParfumPOS.Backend.Interceptors;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// Registramos la Factoría de Contexto para evitar errores de concurrencia en Blazor Server
builder.Services.AddDbContextFactory<InnovaParfumDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(new SqlSettingInterceptor()));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<UserSession>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConfiguracionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<ILabelService, LabelService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<DailyReportPdfService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHostedService<DailyReportScheduler>();

// Configurar Kestrel para producción
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/favicon.ico", async (ConfiguracionService config, IWebHostEnvironment env) => {
    var logo = await config.GetSettingAsync("Empresa_Logo");
    var path = string.IsNullOrEmpty(logo) ? "default_favicon.png" : logo;
    var physicalPath = System.IO.Path.Combine(env.WebRootPath, path.TrimStart('/'));
    if (System.IO.File.Exists(physicalPath)) {
        return Results.File(physicalPath, "image/png");
    }
    return Results.File(System.IO.Path.Combine(env.WebRootPath, "default_favicon.png"), "image/png");
});

app.MapGet("/favicon.png", async (ConfiguracionService config, IWebHostEnvironment env) => {
    var logo = await config.GetSettingAsync("Empresa_Logo");
    var path = string.IsNullOrEmpty(logo) ? "default_favicon.png" : logo;
    var physicalPath = System.IO.Path.Combine(env.WebRootPath, path.TrimStart('/'));
    if (System.IO.File.Exists(physicalPath)) {
        return Results.File(physicalPath, "image/png");
    }
    return Results.File(System.IO.Path.Combine(env.WebRootPath, "default_favicon.png"), "image/png");
});
app.Run();

