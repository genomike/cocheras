# 04 - Propuesta de Mejoras con Prácticas de Codificación Segura

## 4.1 Priorización de Mejoras

Las mejoras se organizan según el modelo **RICE** (Reach, Impact, Confidence, Effort) adaptado para seguridad:

| Prioridad | Criterio | Plazo recomendado |
|-----------|----------|-------------------|
| 🔴 **P0 - Urgente** | Vulnerabilidades críticas explotables remotamente | 1-2 semanas |
| 🟠 **P1 - Alta** | Vulnerabilidades altas que requieren cambios arquitectónicos | 2-4 semanas |
| 🟡 **P2 - Media** | Mejoras de hardening y buenas prácticas | 1-2 meses |
| 🟢 **P3 - Baja** | Optimizaciones de seguridad a largo plazo | 3-6 meses |

---

## 4.2 P0 – Implementar Autenticación y Autorización

### 4.2.1 Integrar ASP.NET Core Identity

**Problema resuelto:** V-001 (Sin autenticación), V-009 (Modelo sin contraseñas)

**Paso 1: Instalar paquetes NuGet**

```bash
dotnet add src/Cochera.Infrastructure/Cochera.Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add src/Cochera.Web/Cochera.Web.csproj package Microsoft.AspNetCore.Identity.UI --version 8.0.11
```

**Paso 2: Modificar entidad Usuario para heredar de IdentityUser**

```csharp
// Cochera.Domain/Entities/Usuario.cs - MEJORADO
using Microsoft.AspNetCore.Identity;

public class Usuario : IdentityUser<int>
{
    // IdentityUser ya incluye: PasswordHash, Email, PhoneNumber,
    // TwoFactorEnabled, LockoutEnd, AccessFailedCount, etc.
    
    public string Codigo { get; set; } = string.Empty;
    public bool EsAdmin { get; set; }
    
    // Campos de auditoría
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    
    // Relaciones
    public ICollection<SesionEstacionamiento> Sesiones { get; set; } = new List<SesionEstacionamiento>();
}
```

**Paso 3: Configurar DbContext con Identity**

```csharp
// Cochera.Infrastructure/Data/CocheraDbContext.cs - MEJORADO
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class CocheraDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    // DbSets existentes (excepto Usuarios, ya incluido por Identity)
    public DbSet<Cajon> Cajones => Set<Cajon>();
    public DbSet<SesionEstacionamiento> Sesiones => Set<SesionEstacionamiento>();
    // ... resto igual
}
```

**Paso 4: Configurar en Program.cs**

```csharp
// Program.cs - MEJORADO
builder.Services.AddIdentity<Usuario, IdentityRole<int>>(options =>
{
    // Política de contraseñas segura
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // Bloqueo de cuenta
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Verificación de email (opcional)
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<CocheraDbContext>()
.AddDefaultTokenProviders();

// Cookies de sesión segura
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/acceso-denegado";
});
```

---

### 4.2.2 Reemplazar UsuarioActualService con Autenticación Real

**Problema resuelto:** V-001 (Sin autenticación)

```csharp
// Cochera.Web/Services/UsuarioActualService.cs - MEJORADO
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

public class UsuarioActualService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserManager<Usuario> _userManager;
    private readonly ILogger<UsuarioActualService> _logger;

    public UsuarioActualService(
        AuthenticationStateProvider authStateProvider,
        UserManager<Usuario> userManager,
        ILogger<UsuarioActualService> logger)
    {
        _authStateProvider = authStateProvider;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Usuario?> GetUsuarioActualAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Solicitud sin autenticación");
            return null;
        }
        
        return await _userManager.GetUserAsync(user);
    }

    public async Task<bool> EsAdminAsync()
    {
        var usuario = await GetUsuarioActualAsync();
        return usuario?.EsAdmin ?? false;
    }
    
    // Ya no existe CambiarUsuarioAsync - la identidad viene del sistema de auth
}
```

---

### 4.2.3 Proteger SignalR Hub con Autorización

**Problema resuelto:** V-002 (SignalR sin autorización)

```csharp
// Cochera.Web/Hubs/CocheraHub.cs - MEJORADO
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[Authorize]
public class CocheraHub : Hub
{
    private readonly ILogger<CocheraHub> _logger;

    public CocheraHub(ILogger<CocheraHub> logger) => _logger = logger;

    [Authorize(Roles = "Admin")]
    public async Task UnirseComoAdmin()
    {
        // Verificación doble: el atributo Authorize valida, más verificación explícita
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = Context.User?.IsInRole("Admin") ?? false;
        
        if (!isAdmin)
        {
            _logger.LogWarning(
                "🚫 Intento no autorizado de unirse a admins. UserId: {UserId}, ConnectionId: {ConnId}",
                userId, Context.ConnectionId);
            throw new HubException("No autorizado para unirse al grupo de administradores");
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        _logger.LogInformation("✅ Admin {UserId} conectado a grupo 'admins'", userId);
    }

    [Authorize]
    public async Task UnirseComoUsuario()
    {
        // El usuario SOLO puede unirse a su propio grupo
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("Usuario no identificado");
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{userId}");
        _logger.LogInformation("✅ Usuario {UserId} conectado a su grupo", userId);
    }

    // Los métodos de notificación SOLO deben ser invocables por el Worker (service-to-service)
    [Authorize(Roles = "System")]
    public async Task NuevoEvento(EventoSensorDto evento)
    {
        await Clients.All.SendAsync("RecibirEvento", evento);
    }
}
```

---

## 4.3 P0 – Externalizar Credenciales

### 4.3.1 Usar .NET User Secrets para Desarrollo

**Problema resuelto:** V-004 (Credenciales hardcoded)

```bash
# Inicializar User Secrets en cada proyecto
cd src/Cochera.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=5432;Database=Cochera;Username=cochera_app;Password=<PASSWORD_SEGURA>;SslMode=Prefer"

cd ../Cochera.Worker
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=5432;Database=Cochera;Username=cochera_worker;Password=<PASSWORD_SEGURA>;SslMode=Prefer"
dotnet user-secrets set "Mqtt:Username" "cochera_worker"
dotnet user-secrets set "Mqtt:Password" "<MQTT_PASSWORD_SEGURA>"
```

### 4.3.2 Limpiar appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "localhost;cochera.local",
  "ConnectionStrings": {
    "DefaultConnection": "" 
  },
  "Mqtt": {
    "Server": "",
    "Port": 8883,
    "Topic": "cola_sensores",
    "ClientId": "CocheraWorker"
  }
}
```

### 4.3.3 Para Producción: Variables de Entorno o Azure Key Vault

```csharp
// Program.cs - Configuración segura para producción
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(prefix: "COCHERA_")
    .AddUserSecrets<Program>(optional: true); // Solo en desarrollo

// Validar que las credenciales estén configuradas
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "La cadena de conexión 'DefaultConnection' no está configurada. " +
        "Use User Secrets (desarrollo) o variables de entorno (producción).");
}
```

### 4.3.4 Eliminar Valores por Defecto Inseguros

```csharp
// MqttSettings.cs - MEJORADO (sin valores por defecto)
public class MqttSettings
{
    [Required]
    public string Server { get; set; } = string.Empty;
    
    [Range(1, 65535)]
    public int Port { get; set; } = 8883; // TLS por defecto
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string Topic { get; set; } = "cola_sensores";
    
    public string ClientId { get; set; } = "CocheraWorker";
    
    public bool UseTls { get; set; } = true; // TLS habilitado por defecto
}
```

---

## 4.4 P1 – Cifrar Comunicaciones

### 4.4.1 Habilitar MQTT sobre TLS (MQTTnet)

**Problema resuelto:** V-005 (MQTT sin TLS)

```csharp
// MqttConsumerService.cs - MEJORADO con TLS
var optionsBuilder = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, _settings.Port)
    .WithCredentials(_settings.Username, _settings.Password)
    .WithClientId(_settings.ClientId)
    .WithCleanSession(false)
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

if (_settings.UseTls)
{
    optionsBuilder.WithTlsOptions(tls =>
    {
        tls.WithCertificateValidationHandler(context =>
        {
            // En producción: validar certificado del broker
            // En desarrollo: se puede relajar temporalmente
            return true; // TODO: Implementar validación de certificado
        });
        tls.WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12 
                            | System.Security.Authentication.SslProtocols.Tls13);
    });
}

var options = optionsBuilder.Build();
```

**Configuración de RabbitMQ para MQTTS (puerto 8883):**

```ini
# rabbitmq.conf
mqtt.listeners.ssl.default = 8883
ssl_options.versions.1 = tlsv1.3
ssl_options.versions.2 = tlsv1.2
ssl_options.certfile = /etc/rabbitmq/certs/server.pem
ssl_options.keyfile = /etc/rabbitmq/certs/server.key
ssl_options.cacertfile = /etc/rabbitmq/certs/ca.pem
```

### 4.4.2 Habilitar HTTPS para toda la aplicación

**Problema resuelto:** V-005 (comunicaciones sin cifrar)

```csharp
// Program.cs - MEJORADO: Forzar HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Después de build
app.UseHsts();
app.UseHttpsRedirection();
```

```csharp
// SignalRNotificationService.cs - URL con HTTPS
_hubUrl = configuration["SignalR:HubUrl"] ?? "https://localhost:5000/cocherahub";
```

### 4.4.3 Conexión PostgreSQL con SSL

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=cochera_app;Password=<SEGURA>;SslMode=Require;Trust Server Certificate=false"
  }
}
```

---

## 4.5 P1 – Validación de Entrada

### 4.5.1 Validar Mensajes MQTT con Schema

**Problema resuelto:** V-007 (Inyección MQTT)

```csharp
// Cochera.Application/Validators/MensajeSensorValidator.cs - NUEVO
using System.ComponentModel.DataAnnotations;

public static class MensajeSensorValidator
{
    private static readonly HashSet<string> EventosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "SISTEMA_INICIADO", "MOVIMIENTO_ENTRADA", "MOVIMIENTO_ENTRADA_BLOQUEADO",
        "VEHICULO_SALIO", "CAJON_OCUPADO", "CAJON_LIBERADO",
        "PARPADEO_INICIADO", "PARPADEO_TIMEOUT", "COCHERA_LLENA"
    };

    private static readonly HashSet<string> EstadosCajonValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "LIBRE", "OCUPADO"
    };

    public static (bool IsValid, string Error) Validar(MensajeSensorMqtt? mensaje)
    {
        if (mensaje == null)
            return (false, "Mensaje nulo");

        // Validar tipo de evento
        if (string.IsNullOrWhiteSpace(mensaje.evento))
            return (false, "Evento vacío");
        
        if (!EventosValidos.Contains(mensaje.evento))
            return (false, $"Evento desconocido: {SanitizeForLog(mensaje.evento)}");

        // Validar estados de cajones
        if (!EstadosCajonValidos.Contains(mensaje.cajon1 ?? ""))
            return (false, $"Estado cajón 1 inválido: {SanitizeForLog(mensaje.cajon1)}");

        if (!EstadosCajonValidos.Contains(mensaje.cajon2 ?? ""))
            return (false, $"Estado cajón 2 inválido: {SanitizeForLog(mensaje.cajon2)}");

        // Validar rangos numéricos
        if (mensaje.libres < 0 || mensaje.libres > 2)
            return (false, $"Cantidad de libres fuera de rango: {mensaje.libres}");

        if (mensaje.ocupados < 0 || mensaje.ocupados > 2)
            return (false, $"Cantidad de ocupados fuera de rango: {mensaje.ocupados}");

        // Validar consistencia
        if (mensaje.libres + mensaje.ocupados != 2)
            return (false, "Inconsistencia: libres + ocupados != 2");

        if (mensaje.lleno != (mensaje.ocupados == 2))
            return (false, "Inconsistencia: flag 'lleno'");

        // Validar detalle (sanitización contra XSS)
        if (!string.IsNullOrEmpty(mensaje.detalle) && mensaje.detalle.Length > 200)
            return (false, "Detalle excede longitud máxima");

        return (true, string.Empty);
    }

    private static string SanitizeForLog(string? input)
    {
        if (input == null) return "(null)";
        // Remover caracteres de control y limitar longitud para prevenir log injection
        return new string(input.Where(c => !char.IsControl(c)).Take(50).ToArray());
    }
}
```

**Aplicación en el servicio:**

```csharp
// EventoSensorService.cs - MEJORADO con validación
public async Task<EventoSensorDto> ProcesarMensajeAsync(
    MensajeSensorMqtt mensaje, string jsonOriginal, 
    CancellationToken cancellationToken = default)
{
    // Validar antes de procesar
    var (isValid, error) = MensajeSensorValidator.Validar(mensaje);
    if (!isValid)
    {
        _logger.LogWarning("Mensaje MQTT rechazado: {Error}", error);
        throw new InvalidOperationException($"Mensaje MQTT inválido: {error}");
    }

    // Sanitizar campos de texto antes de almacenar
    var detalleSanitizado = System.Net.WebUtility.HtmlEncode(mensaje.detalle);
    
    var evento = new EventoSensor
    {
        TipoEvento = MapearTipoEvento(mensaje.evento),
        EventoOriginal = mensaje.evento,
        Detalle = detalleSanitizado,  // Sanitizado
        // ... resto
    };
    // ...
}
```

### 4.5.2 Validar Operaciones de Tarifa

```csharp
// TarifaService.cs - MEJORADO con validación
public async Task<TarifaDto> ActualizarTarifaAsync(
    ActualizarTarifaRequest request, CancellationToken cancellationToken = default)
{
    // Validaciones de negocio
    if (request.NuevoPrecioPorMinuto <= 0)
        throw new ArgumentException("El precio debe ser mayor que cero");
    
    if (request.NuevoPrecioPorMinuto > 1000)
        throw new ArgumentException("El precio excede el máximo permitido");

    if (string.IsNullOrWhiteSpace(request.Descripcion))
        request.Descripcion = "Tarifa actualizada";
    else if (request.Descripcion.Length > 200)
        throw new ArgumentException("La descripción excede el máximo de 200 caracteres");

    // ... lógica existente
}
```

---

## 4.6 P1 – Headers de Seguridad HTTP

### 4.6.1 Middleware de Security Headers

**Problema resuelto:** V-012 (Sin headers de seguridad)

```csharp
// Cochera.Web/Middleware/SecurityHeadersMiddleware.cs - NUEVO
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevenir XSS
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-XSS-Protection"] = "1; mode=block";
        
        // Prevenir clickjacking
        headers["X-Frame-Options"] = "DENY";
        
        // Content Security Policy (ajustar para Blazor + Radzen)
        headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // Blazor necesita inline
            "style-src 'self' 'unsafe-inline'; " +                   // Radzen necesita inline
            "img-src 'self' data:; " +
            "connect-src 'self' ws: wss:; " +                       // SignalR WebSockets
            "font-src 'self'; " +
            "frame-ancestors 'none';";

        // Política de Referrer
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permisos del navegador
        headers["Permissions-Policy"] = 
            "camera=(), microphone=(), geolocation=(), payment=()";

        // Cache control para páginas sensibles
        if (!context.Request.Path.StartsWithSegments("/wwwroot"))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            headers["Pragma"] = "no-cache";
        }

        await _next(context);
    }
}

// Extensión para registrar el middleware
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
```

**Registrar en Program.cs:**
```csharp
app.UseSecurityHeaders(); // Antes de UseStaticFiles
app.UseHsts();
app.UseHttpsRedirection();
```

---

## 4.7 P1 – Rate Limiting

### 4.7.1 Configurar Rate Limiting de ASP.NET 8

```csharp
// Program.cs - Rate Limiting
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // Política global: máximo 100 requests por minuto por IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // Política para login: máximo 5 intentos por 15 minutos
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync(
            "Demasiados intentos. Por favor espere antes de reintentar.", token);
    };
});

// En el pipeline
app.UseRateLimiter();
```

---

## 4.8 P2 – Logging de Seguridad y Auditoría

### 4.8.1 Implementar Logging Estructurado de Seguridad

**Problema resuelto:** V-017 (Logging insuficiente)

```csharp
// Cochera.Application/Services/AuditService.cs - NUEVO
public interface IAuditService
{
    Task RegistrarEventoAsync(string accion, string entidad, int? entidadId, 
        int? usuarioId, string? detalles = null, string? ipAddress = null);
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(ILogger<AuditService> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task RegistrarEventoAsync(
        string accion, string entidad, int? entidadId,
        int? usuarioId, string? detalles = null, string? ipAddress = null)
    {
        // Log estructurado con Serilog
        _logger.LogInformation(
            "AUDIT | Accion={Accion} | Entidad={Entidad} | EntidadId={EntidadId} | " +
            "UsuarioId={UsuarioId} | IP={IP} | Detalles={Detalles}",
            accion, entidad, entidadId, usuarioId, ipAddress, detalles);

        // Opcional: persistir en tabla de auditoría
        // await _unitOfWork.Auditorias.AddAsync(new AuditLog { ... });
    }
}
```

**Eventos a registrar:**

| Evento | Nivel | Ejemplo |
|--------|-------|---------|
| Login exitoso | Information | "Usuario 'admin' inició sesión desde 192.168.1.1" |
| Login fallido | Warning | "Intento fallido de login para 'admin' desde 192.168.1.100" |
| Cambio de tarifa | Information | "Tarifa actualizada de 8.00 a 10.00 por usuario ID 1" |
| Pago confirmado | Information | "Pago S/24.00 confirmado para sesión #15" |
| Acceso denegado | Warning | "Usuario ID 2 intentó acceder a sesión #5 (pertenece a usuario ID 3)" |
| SignalR: unión a grupo | Information | "ConnectionId X unido a grupo 'admins'" |
| MQTT: mensaje rechazado | Warning | "Mensaje MQTT rechazado: evento desconocido" |

### 4.8.2 Corregir Excepciones Silenciadas

```csharp
// UsuarioActualService.cs - MEJORADO con logging
public async Task InitializeAsync()
{
    if (_initialized) return;
    
    try
    {
        var result = await _sessionStorage.GetAsync<int>(StorageKey);
        if (result.Success && result.Value > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
            _usuarioActual = await usuarioService.GetByIdAsync(result.Value);
        }
    }
    catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
    {
        // Errores esperados de ProtectedSessionStorage durante prerendering
        _logger.LogDebug("Session storage no disponible (prerendering): {Message}", ex.Message);
    }
    catch (Exception ex)
    {
        // Errores inesperados: registrar para investigar
        _logger.LogWarning(ex, "Error inesperado al inicializar usuario desde session storage");
    }
    finally
    {
        _initialized = true;
    }
}
```

---

## 4.9 P2 – Seguridad de Base de Datos

### 4.9.1 Crear Usuario Dedicado de PostgreSQL

```sql
-- Crear usuario con permisos mínimos para la aplicación web
CREATE ROLE cochera_app WITH LOGIN PASSWORD '<PASSWORD_FUERTE_32_CHARS>';
GRANT CONNECT ON DATABASE "Cochera" TO cochera_app;
GRANT USAGE ON SCHEMA public TO cochera_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO cochera_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO cochera_app;
-- NO dar: DELETE, TRUNCATE, DROP, CREATE

-- Crear usuario separado para el worker (solo lectura + eventos)
CREATE ROLE cochera_worker WITH LOGIN PASSWORD '<OTRA_PASSWORD_FUERTE>';
GRANT CONNECT ON DATABASE "Cochera" TO cochera_worker;
GRANT USAGE ON SCHEMA public TO cochera_worker;
GRANT SELECT, INSERT ON eventos, estados_cochera, cajones TO cochera_worker;
GRANT UPDATE ON cajones, estados_cochera TO cochera_worker;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO cochera_worker;

-- Crear usuario para migraciones (solo en CI/CD)
CREATE ROLE cochera_migrations WITH LOGIN PASSWORD '<PASSWORD_MIGRACIONES>';
GRANT ALL PRIVILEGES ON DATABASE "Cochera" TO cochera_migrations;
```

### 4.9.2 Remover Migración Automática de Producción

```csharp
// Program.cs - MEJORADO
if (app.Environment.IsDevelopment())
{
    // Solo en desarrollo: migración automática
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CocheraDbContext>();
    context.Database.Migrate();
}
// En producción: usar dotnet ef database update en CI/CD
```

---

## 4.10 P2 – Seguridad del Firmware ESP32

### 4.10.1 Almacenar Credenciales en NVS Cifrado

```cpp
// sketch_jan16a.ino - MEJORADO
#include <WiFi.h>
#include <PubSubClient.h>
#include <Preferences.h>  // NVS (Non-Volatile Storage)

Preferences preferences;

// No más constantes hardcoded para credenciales
// Se leen del NVS cifrado
char ssid[33];
char wifi_password[65];
char mqtt_server[65];
int mqtt_port;
char mqtt_user[33];
char mqtt_pass[65];

void loadCredentials() {
    preferences.begin("cochera", true); // true = read-only
    
    String s = preferences.getString("ssid", "");
    String wp = preferences.getString("wifi_pass", "");
    String ms = preferences.getString("mqtt_srv", "");
    mqtt_port = preferences.getInt("mqtt_port", 8883);
    String mu = preferences.getString("mqtt_usr", "");
    String mp = preferences.getString("mqtt_pwd", "");
    
    preferences.end();
    
    if (s.isEmpty() || wp.isEmpty() || ms.isEmpty()) {
        Serial.println("ERROR: Credenciales no configuradas en NVS");
        Serial.println("Use el modo de configuración para establecer credenciales");
        // Entrar en modo AP para configuración
        startConfigMode();
        return;
    }
    
    s.toCharArray(ssid, sizeof(ssid));
    wp.toCharArray(wifi_password, sizeof(wifi_password));
    ms.toCharArray(mqtt_server, sizeof(mqtt_server));
    mu.toCharArray(mqtt_user, sizeof(mqtt_user));
    mp.toCharArray(mqtt_pass, sizeof(mqtt_pass));
}

// Modo AP para configuración inicial (una sola vez)
void startConfigMode() {
    WiFi.softAP("COCHERA_SETUP", "Setup12345!");
    // Servir página web de configuración en 192.168.4.1
    // El usuario ingresa las credenciales una vez
    // Se guardan en NVS y se reinicia
}
```

### 4.10.2 Agregar HMAC a Mensajes MQTT

```cpp
// sketch_jan16a.ino - MEJORADO con firma de mensajes
#include <mbedtls/md.h>

const char* hmac_key = ""; // Se carga del NVS

void computeHMAC(const char* data, char* output, size_t outLen) {
    byte hmacResult[32];
    mbedtls_md_context_t ctx;
    mbedtls_md_init(&ctx);
    mbedtls_md_setup(&ctx, mbedtls_md_info_from_type(MBEDTLS_MD_SHA256), 1);
    mbedtls_md_hmac_starts(&ctx, (const unsigned char*)hmac_key, strlen(hmac_key));
    mbedtls_md_hmac_update(&ctx, (const unsigned char*)data, strlen(data));
    mbedtls_md_hmac_finish(&ctx, hmacResult);
    mbedtls_md_free(&ctx);
    
    // Convertir a hex
    for (int i = 0; i < 32 && (i * 2 + 1) < outLen; i++) {
        sprintf(output + (i * 2), "%02x", hmacResult[i]);
    }
}

void publicarMensaje(const char* evento, const char* detalle) {
    if (!client.connected()) return;
    
    // Agregar nonce (counter monotónico) para prevenir replay
    static unsigned long messageCounter = 0;
    messageCounter++;
    
    String ts = obtenerTimestamp();
    int libres = (cajon1_ocupado ? 0 : 1) + (cajon2_ocupado ? 0 : 1);
    int ocupados = 2 - libres;
    
    // Construir payload sin firma
    char payload[384];
    snprintf(payload, sizeof(payload),
        "{\"evento\":\"%s\",\"detalle\":\"%s\",\"timestamp\":\"%s\","
        "\"cajon1\":\"%s\",\"cajon2\":\"%s\",\"libres\":%d,\"ocupados\":%d,"
        "\"lleno\":%s,\"nonce\":%lu}",
        evento, detalle, ts.c_str(),
        cajon1_ocupado ? "OCUPADO" : "LIBRE",
        cajon2_ocupado ? "OCUPADO" : "LIBRE",
        libres, ocupados,
        (cajon1_ocupado && cajon2_ocupado) ? "true" : "false",
        messageCounter);
    
    // Calcular HMAC del payload
    char hmac[65];
    computeHMAC(payload, hmac, sizeof(hmac));
    
    // Construir mensaje final con firma
    snprintf(jsonBuffer, sizeof(jsonBuffer),
        "{\"payload\":%s,\"hmac\":\"%s\"}", payload, hmac);
    
    client.publish(topic_sensores, jsonBuffer);
}
```

---

## 4.11 P2 – CORS Policy

```csharp
// Program.cs - Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CocheraPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:5000", "https://cochera.local")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// En el pipeline (antes de UseAuthorization)
app.UseCors("CocheraPolicy");
```

---

## 4.12 Resumen de Mejoras Propuestas

| ID | Mejora | Prioridad | Vulnerabilidades que resuelve | Esfuerzo |
|----|--------|-----------|-------------------------------|----------|
| M-01 | Integrar ASP.NET Identity | 🔴 P0 | V-001, V-009 | Alto |
| M-02 | Proteger SignalR con [Authorize] | 🔴 P0 | V-002 | Medio |
| M-03 | Externalizar credenciales (User Secrets) | 🔴 P0 | V-004 | Bajo |
| M-04 | Eliminar defaults en MqttSettings | 🔴 P0 | V-004 | Bajo |
| M-05 | Habilitar MQTT sobre TLS | 🟠 P1 | V-005 | Medio |
| M-06 | Habilitar HTTPS completo | 🟠 P1 | V-005 | Medio |
| M-07 | Validar mensajes MQTT | 🟠 P1 | V-007 | Medio |
| M-08 | Headers de seguridad HTTP | 🟠 P1 | V-012 | Bajo |
| M-09 | Rate Limiting | 🟠 P1 | V-017 (DoS) | Bajo |
| M-10 | Logging de auditoría | 🟡 P2 | V-017, V-018 | Medio |
| M-11 | Corregir catch vacíos | 🟡 P2 | V-018 | Bajo |
| M-12 | Usuario PostgreSQL dedicado | 🟡 P2 | V-011 | Bajo |
| M-13 | SSL en conexión PostgreSQL | 🟡 P2 | V-005 | Bajo |
| M-14 | NVS cifrado en ESP32 | 🟡 P2 | V-004 (IoT) | Alto |
| M-15 | HMAC en mensajes MQTT | 🟡 P2 | V-015 | Alto |
| M-16 | CORS Policy | 🟡 P2 | V-010 | Bajo |
| M-17 | Migración controlada | 🟡 P2 | V-008 | Bajo |
| M-18 | Filtros de autorización en servicios | 🟠 P1 | V-003 | Medio |

### Estimación de Esfuerzo Total

| Prioridad | Mejoras | Semanas-Persona** |
|-----------|---------|-------------------|
| 🔴 P0 | 4 | 3-4 semanas |
| 🟠 P1 | 5 | 3-4 semanas |
| 🟡 P2 | 9 | 4-6 semanas |
| **Total** | **18** | **10-14 semanas** |

** Estimación para un desarrollador con experiencia en seguridad de .NET
