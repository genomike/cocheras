# 04 — Propuesta de Mejoras de Seguridad

## 4.1 Resumen

Se definieron **18 mejoras de seguridad** priorizadas en 4 niveles (P0 a P3). Las mejoras P0 abordan las vulnerabilidades más críticas del sistema.

> **Actualización Marzo 2026:** Se implementaron **4 mejoras P0** relacionadas con autenticación, autorización y gestión de sesiones. **14 mejoras permanecen pendientes**.

### Estado Global

| Prioridad | Total | Implementadas | Pendientes |
|-----------|-------|--------------|------------|
| P0 — Crítica | 4 | 4 ✅ | 0 |
| P1 — Alta | 6 | 0 | 6 |
| P2 — Media | 5 | 0 | 5 |
| P3 — Baja | 3 | 0 | 3 |
| **Total** | **18** | **4** | **14** |

---

## 4.2 Mejoras P0 — Prioridad Crítica (Implementadas ✅)

### M-01: Implementar Autenticación Real con ASP.NET Core Identity ✅

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-001, V-009, V-014, V-018 |
| **Esfuerzo** | ~16 horas |
| **Estado** | ✅ **IMPLEMENTADA** — Marzo 2026 |

**Implementación realizada:**

1. **ASP.NET Core Identity** con `IdentityUser` e `IdentityRole`
2. **Policy de contraseñas:** RequireDigit, RequireLowercase, RequireUppercase, RequiredLength=8
3. **CocheraDbContext** heredando de `IdentityDbContext<IdentityUser>`
4. **Seed data** con `PasswordHasher<IdentityUser>` (PBKDF2-HMAC-SHA256)
5. **Roles:** Admin y User con asignación en seed data
6. **Cookie `Cochera.Auth`:** HttpOnly, SlidingExpiration, 8h expiry

**Código clave implementado:**
```csharp
// Program.cs
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<CocheraDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.Name = "Cochera.Auth";
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
```

---

### M-02: Proteger SignalR Hub con [Authorize] ✅

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-002 |
| **Estado** | ✅ **IMPLEMENTADA** (parcial — métodos protegidos, clase no) |

**Implementación realizada:**
```csharp
[Authorize(Roles = "Admin")]
public async Task UnirseComoAdmin() { ... }

[Authorize]
public async Task UnirseComoUsuario(int usuarioId)
{
    // Valida que el usuario autenticado sea el dueño
    var codigo = Context.User?.Identity?.Name;
    var usuario = await _usuarioService.GetByCodigoAsync(codigo);
    if (Context.User?.IsInRole("Admin") == true || usuario.Id == usuarioId) { ... }
    else throw new HubException("Acceso denegado");
}
```

**⚠️ Pendiente:** Agregar `[Authorize]` a nivel de clase.

---

### M-03: Página de Login con Form POST ✅

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-001, V-008 |
| **Estado** | ✅ **IMPLEMENTADA** |

Se creó `Login.razor` con un formulario HTML puro (`<form method="post" action="/auth/login">`) que envía credenciales fuera del circuito interactivo de Blazor, evitando la excepción de cookies.

```razor
<form method="post" action="/auth/login">
    <input name="username" class="form-control" required />
    <input name="password" type="password" class="form-control" required />
    <input type="hidden" name="returnUrl" value="@returnUrl" />
    <button type="submit" class="btn btn-primary">Ingresar</button>
</form>
```

---

### M-04: Protección de Rutas por Roles ✅

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-001, V-008 |
| **Estado** | ✅ **IMPLEMENTADA** |

```razor
<!-- Routes.razor con AuthorizeRouteView -->
<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
    <NotAuthorized><RedirectToLogin /></NotAuthorized>
</AuthorizeRouteView>

<!-- App.razor con CascadingAuthenticationState -->
<CascadingAuthenticationState>
    <Routes @rendermode="InteractiveServer" />
</CascadingAuthenticationState>
```

---

## 4.3 Mejoras P1 — Prioridad Alta (Pendientes)

### M-05: Cifrado TLS para MQTT ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-005, V-015 |
| **Esfuerzo estimado** | ~8 horas |
| **Estado** | ❌ **PENDIENTE** |

**Propuesta:**
```csharp
// MqttConsumerService.cs — Agregar TLS
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, 8883)  // Puerto TLS
    .WithTlsOptions(o => {
        o.UseTls = true;
        o.CertificateValidationHandler = ctx => true; // o validar CA
    })
    .WithCredentials(_settings.Username, _settings.Password)
    .Build();
```

---

### M-06: Gestión Segura de Credenciales ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-004, V-011 |
| **Esfuerzo estimado** | ~4 horas |
| **Estado** | ❌ **PENDIENTE** |

**Propuesta:**
- Usar `dotnet user-secrets` en desarrollo
- Variables de entorno o Azure Key Vault en producción
- Crear usuario PostgreSQL dedicado con permisos mínimos (no `postgres`)

```bash
# Ejemplo: User Secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Username=cochera_app;..."
```

---

### M-07: Validación y Firma de Mensajes MQTT ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-007, V-015 |
| **Esfuerzo estimado** | ~12 horas |
| **Estado** | ❌ **PENDIENTE** |

**Propuesta:**
```csharp
// Validar esquema con FluentValidation o System.ComponentModel.DataAnnotations
public class MensajeSensorValidator : AbstractValidator<MensajeSensorMqtt>
{
    public MensajeSensorValidator()
    {
        RuleFor(x => x.Evento).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Detalle).MaximumLength(200);
        RuleFor(x => x.Libres).InclusiveBetween(0, 10);
    }
}

// Agregar HMAC-SHA256 en ESP32 y verificar en Worker
```

---

### M-08: Agregar [Authorize] a nivel de clase en Hub ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-002 |
| **Esfuerzo estimado** | ~30 minutos |
| **Estado** | ❌ **PENDIENTE** |

```csharp
[Authorize]  // ← Una línea que protege todo el Hub
public class CocheraHub : Hub { ... }
```

---

### M-09: Rate Limiting en Login ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-008 (parcial) |
| **Esfuerzo estimado** | ~2 horas |
| **Estado** | ❌ **PENDIENTE** |

```csharp
// Program.cs — .NET 8 built-in rate limiting
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("login", opt => {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.MapPost("/auth/login", ...).RequireRateLimiting("login");
```

---

### M-10: Habilitar LockoutEnabled en Usuarios Seed ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-008 |
| **Esfuerzo estimado** | ~15 minutos |
| **Estado** | ❌ **PENDIENTE** |

```csharp
var adminIdentityUser = new IdentityUser {
    LockoutEnabled = true  // ← Cambiar de false a true
};
```

---

## 4.4 Mejoras P2 — Prioridad Media (Pendientes)

### M-11: Headers de Seguridad HTTP ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-012 |
| **Esfuerzo estimado** | ~2 horas |
| **Estado** | ❌ **PENDIENTE** |

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=()";
    await next();
});
```

---

### M-12: Ownership Validation en Services ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-003 |
| **Esfuerzo estimado** | ~8 horas |
| **Estado** | ❌ **PENDIENTE** |

---

### M-13: Provisioning WiFi/MQTT para ESP32 ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-004 |
| **Esfuerzo estimado** | ~16 horas |
| **Estado** | ❌ **PENDIENTE** |

---

### M-14: Dependabot / Snyk para CVEs ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-013 |
| **Esfuerzo estimado** | ~2 horas |
| **Estado** | ❌ **PENDIENTE** |

---

### M-15: Configurar AllowedHosts ❌

| Campo | Valor |
|-------|-------|
| **Vulnerabilidades abordadas** | V-010 |
| **Esfuerzo estimado** | ~15 minutos |
| **Estado** | ❌ **PENDIENTE** |

```json
"AllowedHosts": "cochera.example.com;localhost"
```

---

## 4.5 Mejoras P3 — Prioridad Baja (Pendientes)

### M-16: Cifrado de Datos en Reposo ❌

| **Vulnerabilidades abordadas** | V-006 |
| **Estado** | ❌ **PENDIENTE** |

### M-17: Secure Boot ESP32 ❌

| **Vulnerabilidades abordadas** | V-016 |
| **Estado** | ❌ **PENDIENTE** |

### M-18: Audit Logging de Seguridad ❌

| **Vulnerabilidades abordadas** | V-017 |
| **Estado** | ❌ **PENDIENTE** |

---

## 4.6 Roadmap de Implementación

```
Fase 1 (COMPLETADA ✅):
├── M-01 ASP.NET Core Identity          ✅
├── M-02 [Authorize] en Hub             ✅ (parcial)
├── M-03 Login con form POST            ✅
└── M-04 Protección de rutas            ✅

Fase 2 (SIGUIENTE — Alta prioridad):
├── M-08 [Authorize] clase Hub          (~30 min)
├── M-09 Rate limiting login            (~2h)
├── M-10 LockoutEnabled = true          (~15 min)
└── M-06 Gestión segura credenciales    (~4h)

Fase 3 (Infraestructura):
├── M-05 MQTT TLS                       (~8h)
├── M-07 Validación mensajes MQTT       (~12h)
├── M-11 Security headers               (~2h)
└── M-15 AllowedHosts                   (~15 min)

Fase 4 (Mejora continua):
├── M-12 Ownership validation           (~8h)
├── M-14 Dependabot/Snyk                (~2h)
├── M-18 Audit logging                  (~8h)
├── M-13 Provisioning ESP32             (~16h)
├── M-16 Cifrado en reposo              (~8h)
└── M-17 Secure Boot                    (~12h)
```

---

*Anterior: [03 — Análisis de Código Inseguro](03-analisis-codigo-inseguro.md)*
*Siguiente: [05 — Pruebas de Seguridad (SAST/DAST)](05-pruebas-sast-dast.md)*
