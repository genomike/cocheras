using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Application.Services;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Cochera.Infrastructure.Mqtt;
using Cochera.Infrastructure.Repositories;
using Cochera.Worker;
using Microsoft.EntityFrameworkCore;

// Configurar Npgsql para aceptar DateTime sin timezone específico
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = Host.CreateApplicationBuilder(args);

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

// MQTT
builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IMqttConsumerService, MqttConsumerService>();

// SignalR Notification Service (se conectará al Hub del proyecto Web)
builder.Services.AddSingleton<ISignalRNotificationService, SignalRNotificationService>();

// Worker
builder.Services.AddHostedService<MqttWorker>();

var host = builder.Build();

// Asegurar que la base de datos existe y aplicar migraciones
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CocheraDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
