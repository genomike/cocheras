using Cochera.Application.Interfaces;
using Cochera.Application.Services;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Cochera.Infrastructure.Repositories;
using Cochera.Web.Components;
using Cochera.Web.Hubs;
using Cochera.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;

// Configurar Npgsql para aceptar DateTime sin timezone específico
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Radzen
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// SignalR
builder.Services.AddSignalR();

// Database - Usando Factory para Blazor Server concurrency
builder.Services.AddDbContextFactory<CocheraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<CocheraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<CocheraDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Cochera.Auth";
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICajonService, CajonService>();
builder.Services.AddScoped<ISesionService, SesionService>();
builder.Services.AddScoped<IEventoSensorService, EventoSensorService>();
builder.Services.AddScoped<IEstadoCocheraService, EstadoCocheraService>();
builder.Services.AddScoped<ITarifaService, TarifaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Servicio de notificaciones de sesiones (SignalR)
builder.Services.AddScoped<ISesionNotificationService, SesionNotificationService>();

// Usuario Actual Service (Cascading/Singleton para Blazor Server)
builder.Services.AddScoped<UsuarioActualService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub
app.MapHub<CocheraHub>("/cocherahub");

app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<IdentityUser> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=1");
    }

    var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        return Results.Redirect("/login?error=1");
    }

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) || !returnUrl.StartsWith('/'))
    {
        return Results.Redirect("/");
    }

    return Results.Redirect(returnUrl);
}).DisableAntiforgery();

app.MapGet("/logout", async (SignInManager<IdentityUser> signInManager, HttpContext httpContext) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

// Asegurar que la base de datos existe
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CocheraDbContext>>();
    await using var db = await contextFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.Run();
