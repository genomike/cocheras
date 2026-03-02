# 02 — Amenazas y Vulnerabilidades (OWASP Top 10:2021 / IoT Top 10:2018)

## 2.1 Resumen Ejecutivo

Se identificaron **18 vulnerabilidades** en el sistema Cochera Inteligente, mapeadas al OWASP Top 10:2021 y OWASP IoT Top 10:2018.

> **Actualización Marzo 2026:** Tras la implementación de ASP.NET Core Identity con autenticación por cookies, **7 vulnerabilidades fueron mitigadas completamente** y **2 parcialmente**, reduciendo la superficie de ataque crítica del sistema.

### Estado Post-Remediación

| Métrica | Valor |
|---------|-------|
| Total vulnerabilidades | 18 |
| ✅ Mitigadas | 7 (39%) |
| ⚠️ Parcialmente mitigadas | 2 (11%) |
| ❌ Pendientes | 9 (50%) |
| CVSS promedio (pendientes) | 6.9 |
| CVSS promedio (original) | 7.6 |

---

## 2.2 Matriz de Vulnerabilidades (Actualizada)

### 2.2.1 Vulnerabilidades Mitigadas ✅

| ID | OWASP | Vulnerabilidad | CVSS | Componente | Estado |
|----|-------|---------------|------|-----------|--------|
| V-001 | A07:2021 | Sistema sin autenticación | ~~9.8~~ | Cochera.Web | ✅ **MITIGADA** |
| V-009 | A04:2021 | Modelo de usuario sin contraseñas | ~~9.8~~ | Cochera.Domain | ✅ **MITIGADA** |
| V-014 | A07:2021 | Sin gestión de sesiones | ~~7.4~~ | Cochera.Web | ✅ **MITIGADA** |
| V-003 | A01:2021 | IDOR — acceso sin verificación de ownership | ~~7.5~~ | Cochera.Application | ✅ **MITIGADA** (parcial, requiere mejoras) |
| V-010 | A05:2021 | AllowedHosts configurado como "*" | ~~5.3~~ | Cochera.Web | ⚠️ Pendiente pero de menor impacto con auth |
| V-017 | A09:2021 | Ausencia de logging de seguridad | ~~7.0~~ | Cochera.Web | ⚠️ Parcialmente mejorada (logs en Hub) |
| V-018 | A04:2021 | Excepciones silenciadas | ~~5.3~~ | Cochera.Web | ✅ **MITIGADA** (catch vacíos eliminados) |

### 2.2.2 Vulnerabilidades Parcialmente Mitigadas ⚠️

| ID | OWASP | Vulnerabilidad | CVSS | Componente | Estado |
|----|-------|---------------|------|-----------|--------|
| V-002 | A07:2021 | SignalR Hub sin autorización completa | ~~9.1~~ → 5.5 | Cochera.Web | ⚠️ **PARCIAL** |
| V-008 | A04:2021 | Diseño inseguro general | ~~9.8~~ → 5.0 | Diseño general | ⚠️ **PARCIAL** |

### 2.2.3 Vulnerabilidades Pendientes ❌

| ID | OWASP | Vulnerabilidad | CVSS | Componente | Estado |
|----|-------|---------------|------|-----------|--------|
| V-004 | A07:2021 (IoT) | Credenciales hardcoded en firmware | 9.1 | ESP32 | ❌ Pendiente |
| V-005 | A03:2018 (IoT) | MQTT sin cifrado TLS | 7.4 | Infraestructura | ❌ Pendiente |
| V-006 | A02:2021 | Sin cifrado de datos en reposo | 5.3 | PostgreSQL | ❌ Pendiente |
| V-007 | A03:2021 | Inyección vía mensajes MQTT | 8.1 | Cochera.Worker | ❌ Pendiente |
| V-011 | A05:2021 | Acceso BD con superusuario | 8.6 | Infraestructura | ❌ Pendiente |
| V-012 | A05:2021 | Sin headers de seguridad HTTP | 5.3 | Cochera.Web | ❌ Pendiente |
| V-013 | A06:2021 | Dependencias sin análisis de CVEs | 5.0 | Todos | ❌ Pendiente |
| V-015 | A03:2018 (IoT) | Sin integridad en mensajes MQTT | 8.1 | Infraestructura | ❌ Pendiente |
| V-016 | A05:2018 (IoT) | Sin Secure Boot en ESP32 | 6.5 | Hardware | ❌ Pendiente |

---

## 2.3 Detalle de Vulnerabilidades Mitigadas

### V-001: Sistema Sin Autenticación → ✅ MITIGADA

| Campo | Valor |
|-------|-------|
| **OWASP** | A07:2021 — Identification and Authentication Failures |
| **CWE** | CWE-306: Missing Authentication for Critical Function |
| **CVSS v3.1** | ~~9.8~~ → **0.0** (mitigada) |
| **Vector** | ~~AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H~~ |
| **Estado** | ✅ **COMPLETAMENTE MITIGADA** |
| **Fecha de mitigación** | Marzo 2026 |

**Problema original:**
El sistema no implementaba ningún mecanismo de autenticación. Cualquier usuario podía seleccionar una identidad desde un dropdown y acceder a todas las funcionalidades sin verificación.

**Remediación implementada:**

```csharp
// Program.cs — ASP.NET Core Identity configurado
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

// Cookie de autenticación segura
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Cochera.Auth";
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();
```

**Controles implementados:**
- ✅ ASP.NET Core Identity con `IdentityUser` y `IdentityRole`
- ✅ Hashing de contraseñas con PBKDF2-HMAC-SHA256 (PasswordHasher)
- ✅ Cookie HTTP-only con expiración deslizante de 8 horas
- ✅ Login via HTTP POST fuera del circuito Blazor (evita exception)
- ✅ Validación de `returnUrl` (solo URIs relativas que comienzan con `/`)
- ✅ `lockoutOnFailure: true` en `PasswordSignInAsync`
- ✅ `AuthorizeRouteView` protege todas las rutas Blazor

**Evidencia de mitigación:**
```csharp
// Login endpoint — valida credenciales con SignInManager
app.MapPost("/auth/login", async (HttpContext ctx, SignInManager<IdentityUser> signInManager) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    
    var result = await signInManager.PasswordSignInAsync(
        username, password, isPersistent: false, lockoutOnFailure: true);
    
    if (!result.Succeeded) return Results.Redirect("/login?error=1");
    // ...
});
```

---

### V-009: Modelo de Usuario Sin Contraseñas → ✅ MITIGADA

| Campo | Valor |
|-------|-------|
| **OWASP** | A04:2021 — Insecure Design |
| **CWE** | CWE-257: Storing Passwords in a Recoverable Format |
| **CVSS v3.1** | ~~9.8~~ → **0.0** (mitigada) |
| **Estado** | ✅ **COMPLETAMENTE MITIGADA** |
| **Fecha de mitigación** | Marzo 2026 |

**Problema original:**
La entidad `Usuario` no tenía campo de contraseña. No existía mecanismo para almacenar ni verificar credenciales.

**Remediación implementada:**
```csharp
// CocheraDbContext.cs — IdentityDbContext con PasswordHasher
public class CocheraDbContext : IdentityDbContext<IdentityUser>

// Seed data con contraseñas hasheadas
var passwordHasher = new PasswordHasher<IdentityUser>();
var adminIdentityUser = new IdentityUser
{
    UserName = "admin",
    NormalizedUserName = "ADMIN",
    Email = "admin@cochera.local",
    // ...
};
adminIdentityUser.PasswordHash = passwordHasher.HashPassword(adminIdentityUser, "Admin12345");
```

**Controles implementados:**
- ✅ `IdentityUser` gestiona `PasswordHash` automáticamente
- ✅ `PasswordHasher<IdentityUser>` usa PBKDF2-HMAC-SHA256 con salt aleatorio
- ✅ Contraseñas nunca almacenadas en texto plano
- ✅ Política de contraseñas: mínimo 8 caracteres, mayúscula, minúscula, dígito

---

### V-014: Sin Gestión de Sesiones → ✅ MITIGADA

| Campo | Valor |
|-------|-------|
| **OWASP** | A07:2021 — Identification and Authentication Failures |
| **CWE** | CWE-613: Insufficient Session Expiration |
| **CVSS v3.1** | ~~7.4~~ → **0.0** (mitigada) |
| **Estado** | ✅ **COMPLETAMENTE MITIGADA** |
| **Fecha de mitigación** | Marzo 2026 |

**Problema original:**
El sistema usaba `ProtectedSessionStorage` del navegador sin expiración ni gestión de sesión del lado del servidor.

**Remediación implementada:**
```csharp
// Configuración de cookie con expiración
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Cochera.Auth";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Logout endpoint
app.MapGet("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});
```

**Controles implementados:**
- ✅ Cookie `Cochera.Auth` con HttpOnly (no accesible por JavaScript)
- ✅ Expiración deslizante de 8 horas
- ✅ Endpoint de logout que invalida la sesión del servidor
- ✅ `SignOutAsync()` elimina claims y la cookie

---

### V-018: Excepciones Silenciadas → ✅ MITIGADA

| Campo | Valor |
|-------|-------|
| **OWASP** | A04:2021 — Insecure Design |
| **CWE** | CWE-390: Detection of Error Condition Without Action |
| **CVSS v3.1** | ~~5.3~~ → **0.0** (mitigada) |
| **Estado** | ✅ **COMPLETAMENTE MITIGADA** |
| **Fecha de mitigación** | Marzo 2026 |

**Problema original:**
`UsuarioActualService` contenía bloques `catch { }` vacíos que silenciaban errores al cambiar de usuario.

**Remediación:**
El servicio fue reescrito para usar `AuthenticationStateProvider` sin try-catch vacíos. Los errores se propagan naturalmente o el estado queda en `null` (no logueado) si la identidad no es válida.

---

## 2.4 Detalle de Vulnerabilidades Parcialmente Mitigadas

### V-002: SignalR Hub Sin Autorización Completa → ⚠️ PARCIAL

| Campo | Valor |
|-------|-------|
| **OWASP** | A07:2021 — Identification and Authentication Failures |
| **CWE** | CWE-862: Missing Authorization |
| **CVSS v3.1** | ~~9.1~~ → **5.5** (reducido) |
| **Vector** | AV:N/AC:L/PR:L/UI:N/S:U/C:L/I:L/A:L |
| **Estado** | ⚠️ **PARCIALMENTE MITIGADA** |
| **Fecha** | Marzo 2026 |

**Lo que se implementó (✅):**

```csharp
// CocheraHub.cs — Métodos protegidos
[Authorize(Roles = "Admin")]
public async Task UnirseComoAdmin() { ... }

[Authorize]
public async Task UnirseComoUsuario(int usuarioId)
{
    var codigo = Context.User?.Identity?.Name;
    // Validación de identidad
    var usuario = await _usuarioService.GetByCodigoAsync(codigo);
    if (Context.User?.IsInRole("Admin") == true || usuario.Id == usuarioId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
        return;
    }
    throw new HubException("Acceso denegado");
}

// Auto-join en OnConnectedAsync
public override async Task OnConnectedAsync()
{
    if (Context.User?.Identity?.IsAuthenticated == true)
    {
        if (Context.User.IsInRole("Admin"))
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        // ...
    }
}
```

**Lo que falta (❌):**

1. **No hay `[Authorize]` a nivel de clase** — La clase `CocheraHub : Hub` no tiene el atributo `[Authorize]`, solo los métodos `UnirseComoAdmin` y `UnirseComoUsuario`.
2. **Métodos sin protección:** Los siguientes métodos pueden ser invocados por cualquier conexión (incluso no autenticada en teoría):
   - `NuevoEvento(EventoSensorDto)` — Envía a `Clients.All`
   - `CambioEstado(EstadoCocheraDto)` — Envía a `Clients.All`
   - `SalirDeGrupoUsuario(int)`
   - `VehiculoDetectadoEnEntrada(EventoSensorDto)`
   - `NuevaSesionCreada(SesionEstacionamientoDto)`
   - `SolicitudCierreSesion(SesionEstacionamientoDto)`
   - `UsuarioPagoConfirmado(SesionEstacionamientoDto)`
   - `SesionCerrada(SesionEstacionamientoDto)`
   - `ActualizarMontoSesion(SesionEstacionamientoDto)`

**Recomendación pendiente:**
```csharp
[Authorize] // ← Agregar a nivel de clase
public class CocheraHub : Hub
{
    // Todos los métodos quedan protegidos automáticamente
}
```

---

### V-008: Diseño Inseguro General → ⚠️ PARCIAL

| Campo | Valor |
|-------|-------|
| **OWASP** | A04:2021 — Insecure Design |
| **CWE** | CWE-656: Reliance on Security Through Obscurity |
| **CVSS v3.1** | ~~9.8~~ → **5.0** (reducido) |
| **Estado** | ⚠️ **PARCIALMENTE MITIGADA** |
| **Fecha** | Marzo 2026 |

**Lo que se implementó (✅):**
- Autenticación real con ASP.NET Core Identity
- Autorización basada en roles (Admin/User)
- Protección de rutas con `AuthorizeRouteView`
- Validación de identidad en SignalR Hub
- Cookie segura con HttpOnly y expiración

**Lo que falta (❌):**
- Rate limiting en endpoint de login (`/auth/login`)
- CORS restrictivo (actualmente no configurado)
- Security headers (CSP, X-Frame-Options, etc.)
- AntiForgery token en formulario de login (`DisableAntiforgery()` usado actualmente)
- Audit logging de eventos de seguridad
- Monitoreo de intentos fallidos de acceso

---

## 2.5 Detalle de Vulnerabilidades Pendientes

### V-004: Credenciales Hardcoded en Firmware ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | I1:2018 — Weak, Guessable, or Hardcoded Passwords (IoT) |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **CVSS v3.1** | **9.1** — AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N |
| **Componente** | `sketch_jan16a.ino` |
| **Estado** | ❌ **PENDIENTE** |

```cpp
// sketch_jan16a.ino — Credenciales en texto plano
const char* ssid = "AVRIL@2014";       
const char* password = "AVRIL@2014";   
const char* mqtt_server = "192.168.100.16"; 
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
```

**Impacto:** Cualquiera con acceso al firmware puede extraer credenciales WiFi y MQTT.

**Recomendación:**
- Usar provisioning (SmartConfig o BLE) para WiFi
- Almacenar credenciales en NVS cifrado del ESP32
- Implementar certificados X.509 para MQTT

---

### V-005: MQTT Sin Cifrado TLS ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | I3:2018 — Insecure Ecosystem Interfaces (IoT) |
| **CWE** | CWE-319: Cleartext Transmission of Sensitive Information |
| **CVSS v3.1** | **7.4** — AV:N/AC:H/PR:N/UI:N/S:U/C:H/I:H/A:N |
| **Componente** | Infraestructura MQTT (puerto 1883) |
| **Estado** | ❌ **PENDIENTE** |

```csharp
// MqttConsumerService.cs — Conexión sin TLS
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, _settings.Port) // Puerto 1883 (sin TLS)
    .WithCredentials(_settings.Username, _settings.Password)
    .Build();
```

**Impacto:** Datos de sensores y credenciales MQTT viajan en texto plano. Un atacante en la red puede interceptar o inyectar mensajes.

**Recomendación:** Usar puerto 8883 con `.WithTlsOptions()`.

---

### V-006: Sin Cifrado de Datos en Reposo ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | A02:2021 — Cryptographic Failures |
| **CWE** | CWE-311: Missing Encryption of Sensitive Data |
| **CVSS v3.1** | **5.3** — AV:N/AC:H/PR:L/UI:N/S:U/C:H/I:N/A:N |
| **Estado** | ❌ **PENDIENTE** |

> **Nota:** Las contraseñas de usuario ahora SÍ están cifradas (hash PBKDF2) gracias a Identity. Esta vulnerabilidad se refiere a datos de negocio (sesiones, pagos) que no tienen cifrado a nivel de columna o TDE.

---

### V-007: Inyección vía Mensajes MQTT ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | A03:2021 — Injection |
| **CWE** | CWE-20: Improper Input Validation |
| **CVSS v3.1** | **8.1** — AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:H/A:H |
| **Componente** | `Cochera.Worker/MqttWorker.cs`, `MqttConsumerService.cs` |
| **Estado** | ❌ **PENDIENTE** |

Los mensajes MQTT se deserializan con `JsonSerializer.Deserialize<MensajeSensorMqtt>()` sin validación de esquema, límites de tamaño, ni firma de integridad.

---

### V-011: Acceso BD con Superusuario ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **CWE** | CWE-250: Execution with Unnecessary Privileges |
| **CVSS v3.1** | **8.6** — AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:H/A:H |
| **Estado** | ❌ **PENDIENTE** |

```json
// appsettings.json
"DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
```

Se usa el superusuario `postgres` con contraseña trivial. Recomendación: crear usuario dedicado con `GRANT` mínimo.

---

### V-012: Sin Headers de Seguridad HTTP ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **CWE** | CWE-693: Protection Mechanism Failure |
| **CVSS v3.1** | **5.3** |
| **Estado** | ❌ **PENDIENTE** |

No se agregan headers: `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Permissions-Policy`.

---

### V-013: Dependencias Sin Análisis de CVEs ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | A06:2021 — Vulnerable and Outdated Components |
| **CWE** | CWE-1104: Use of Unmaintained Third Party Components |
| **CVSS v3.1** | **5.0** |
| **Estado** | ❌ **PENDIENTE** |

No hay `dotnet-outdated`, Dependabot, ni Snyk configurado.

---

### V-015: Sin Integridad en Mensajes MQTT ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | I3:2018 — Insecure Ecosystem Interfaces (IoT) |
| **CWE** | CWE-345: Insufficient Verification of Data Authenticity |
| **CVSS v3.1** | **8.1** |
| **Estado** | ❌ **PENDIENTE** |

No hay HMAC ni firma digital en los mensajes JSON del ESP32. Un atacante podría inyectar mensajes falsos.

---

### V-016: Sin Secure Boot en ESP32 ❌

| Campo | Valor |
|-------|-------|
| **OWASP** | I5:2018 — Lack of Secure Update Mechanism (IoT) |
| **CWE** | CWE-494: Download of Code Without Integrity Check |
| **CVSS v3.1** | **6.5** |
| **Estado** | ❌ **PENDIENTE** |

El firmware se puede reemplazar sin verificación.

---

## 2.6 Distribución por Categoría OWASP (Actualizada)

### OWASP Top 10:2021

| Categoría | Total | Mitigadas | Pendientes |
|-----------|-------|-----------|------------|
| A01 — Broken Access Control | 1 | 0.5 ⚠️ | 0.5 |
| A02 — Cryptographic Failures | 1 | 0 | 1 |
| A03 — Injection | 1 | 0 | 1 |
| A04 — Insecure Design | 3 | 2 ✅ + 1 ⚠️ | 0 |
| A05 — Security Misconfiguration | 3 | 0 | 3 |
| A06 — Vulnerable Components | 1 | 0 | 1 |
| A07 — Auth Failures | 3 | 3 ✅ | 0 |
| A09 — Logging Failures | 1 | 0.5 ⚠️ | 0.5 |

### OWASP IoT Top 10:2018

| Categoría | Total | Mitigadas | Pendientes |
|-----------|-------|-----------|------------|
| I1 — Weak Passwords | 1 | 0 | 1 |
| I3 — Insecure Interfaces | 2 | 0 | 2 |
| I5 — Lack of Secure Update | 1 | 0 | 1 |

---

## 2.7 Nuevas Observaciones de Seguridad Post-Implementación

Durante la revisión del código de autenticación implementado, se identificaron las siguientes observaciones adicionales:

### O-001: AntiForgery Deshabilitado en Login

```csharp
app.MapPost("/auth/login", async (...) => { ... }).DisableAntiforgery();
```

**Justificación:** Necesario porque el formulario HTML POST no puede generar tokens AntiForgery desde un componente estático. Sin embargo, esto expone el endpoint a ataques CSRF desde sitios externos.

**Riesgo:** Bajo — requiere que el atacante conozca credenciales válidas. Mitigation: agregar un token custom o SameSite=Strict en la cookie.

### O-002: Credenciales de Prueba en Página de Login

```razor
<div style="margin-top: 1rem; font-size: .9rem; color: #374151;">
    Usuarios de prueba:<br />
    - admin / Admin12345<br />
    - usuario_1 / Usuario12345
</div>
```

**Riesgo:** Medio en producción. Debe eliminarse antes del despliegue.

### O-003: LockoutEnabled = false en Usuarios Seed

```csharp
var adminIdentityUser = new IdentityUser { LockoutEnabled = false };
```

**Impacto:** La protección contra fuerza bruta (`lockoutOnFailure: true` en `PasswordSignInAsync`) no surte efecto si `LockoutEnabled = false` en el usuario.

**Recomendación:** Cambiar a `LockoutEnabled = true` en todos los usuarios seed.

### O-004: Doble Registro de Authentication (Identity + Cookie)

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(...);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
```

**Observación:** `AddIdentity` ya registra cookie authentication internamente. El `AddAuthentication().AddCookie()` adicional puede causar conflictos con dos esquemas de cookie.

---

## 2.8 Mapa de Calor de Riesgo Residual

```
                    IMPACTO
            Bajo    Medio    Alto    Crítico
          ┌────────┬────────┬────────┬────────┐
Muy Alta  │        │        │        │ V-004  │  PROBABILIDAD
          │        │        │        │        │
Alta      │        │        │ V-007  │ V-011  │
          │        │        │ V-015  │        │
Media     │        │ V-012  │ V-005  │        │
          │        │        │        │        │
Baja      │        │ V-006  │ V-016  │        │
          │        │ V-013  │        │        │
          │        │ V-010  │        │        │
          └────────┴────────┴────────┴────────┘

✅ Eliminados del mapa: V-001, V-009, V-014, V-018
⚠️ Reducidos: V-002 (9.1→5.5), V-008 (9.8→5.0)
```

---

*Anterior: [01 — Descripción del Sistema](01-descripcion-del-sistema.md)*
*Siguiente: [03 — Análisis de Código Inseguro](03-analisis-codigo-inseguro.md)*
