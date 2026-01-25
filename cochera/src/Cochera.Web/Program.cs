using Cochera.Application.Interfaces;
using Cochera.Application.Services;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Cochera.Infrastructure.Repositories;
using Cochera.Web.Components;
using Cochera.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Radzen;

// Configurar Npgsql para aceptar DateTime sin timezone específico
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Radzen
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// SignalR
builder.Services.AddSignalR();

// Database
builder.Services.AddDbContext<CocheraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub
app.MapHub<CocheraHub>("/cocherahub");

// Asegurar que la base de datos existe
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CocheraDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
