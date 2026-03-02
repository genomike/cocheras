# 03 — Análisis de Código Inseguro

## 3.1 Resumen

Se identificaron **23 hallazgos de código inseguro** distribuidos en 13 archivos del sistema. Cada hallazgo fue clasificado con su CWE correspondiente, nivel de severidad y código vulnerable.

> **Actualización Marzo 2026:** Tras la implementación de ASP.NET Core Identity, **10 hallazgos fueron corregidos** y **13 permanecen pendientes**.

| Métrica | Antes | Después |
|---------|-------|---------|
| Hallazgos totales | 23 | 23 |
| ✅ Corregidos | 0 | 10 |
| ❌ Pendientes | 23 | 13 |
| Archivos afectados | 13 | 8 |

---

## 3.2 Hallazgos Corregidos ✅

### 3.2.1 Suplantación de Identidad sin Restricciones → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Services/UsuarioActualService.cs` |
| **CWE** | CWE-287: Improper Authentication |
| **Severidad** | 🔴 Crítica |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Código vulnerable (antes):**
```csharp
// ANTES: Cualquier usuario podía suplantar a otro
public async Task CambiarUsuarioAsync(int usuarioId)
{
    try {
        using var scope = _serviceProvider.CreateScope();
        var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
        _usuarioActual = await usuarioService.GetByIdAsync(usuarioId);
        // Sin validación de identidad — suplantación libre
    } catch { } // Excepción silenciada
}
```

**Código corregido (después):**
```csharp
// DESPUÉS: Usa AuthenticationStateProvider — no se puede suplantar
public async Task CambiarUsuarioAsync(int usuarioId)
{
    await RefrescarDesdeIdentidadAsync(); // Lee del cookie, no del parámetro
}

public async Task RefrescarDesdeIdentidadAsync()
{
    var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
    var userPrincipal = authState.User;

    if (userPrincipal?.Identity?.IsAuthenticated != true)
    {
        _usuarioActual = null;
        OnUsuarioCambiado?.Invoke();
        return;
    }

    var codigo = userPrincipal.Identity?.Name;
    using var scope = _serviceProvider.CreateScope();
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    _usuarioActual = await usuarioService.GetByCodigoAsync(codigo);
    OnUsuarioCambiado?.Invoke();
}
```

**Cambios clave:**
- `CambiarUsuarioAsync` ya no acepta IDs arbitrarios — delega a `RefrescarDesdeIdentidadAsync`
- La identidad proviene del `AuthenticationStateProvider` (cookie del servidor), no del cliente
- No hay bloques `catch {}` vacíos

---

### 3.2.2 Modelo de Usuario sin Campo de Contraseña → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Domain/Entities/Usuario.cs`, `Cochera.Infrastructure/Data/CocheraDbContext.cs` |
| **CWE** | CWE-257: Storing Passwords in a Recoverable Format |
| **Severidad** | 🔴 Crítica |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** La entidad `Usuario` no tenía campo de contraseña. Los datos seed no incluían credenciales.

**Después:** Se agregó `IdentityUser` como modelo paralelo con `PasswordHash` gestionado por `PasswordHasher<IdentityUser>`. El `CocheraDbContext` hereda de `IdentityDbContext<IdentityUser>` y hace seed de roles, usuarios Identity y asignaciones de rol.

```csharp
public class CocheraDbContext : IdentityDbContext<IdentityUser>
{
    // Seed con PasswordHasher
    var passwordHasher = new PasswordHasher<IdentityUser>();
    adminIdentityUser.PasswordHash = passwordHasher.HashPassword(adminIdentityUser, "Admin12345");
}
```

---

### 3.2.3 SignalR Hub Completamente Abierto → ✅ PARCIALMENTE CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Hubs/CocheraHub.cs` |
| **CWE** | CWE-862: Missing Authorization |
| **Severidad** | 🔴 Crítica → 🟡 Media |
| **Estado** | ⚠️ **PARCIALMENTE CORREGIDO** — Marzo 2026 |

**Antes:** Ningún método tenía `[Authorize]`. `UnirseComoAdmin()` y `UnirseComoUsuario()` no validaban identidad.

**Después:**
```csharp
[Authorize(Roles = "Admin")]
public async Task UnirseComoAdmin() { ... }

[Authorize]
public async Task UnirseComoUsuario(int usuarioId)
{
    var codigo = Context.User?.Identity?.Name;
    var usuario = await _usuarioService.GetByCodigoAsync(codigo);
    if (Context.User?.IsInRole("Admin") == true || usuario.Id == usuarioId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
        return;
    }
    throw new HubException("Acceso denegado");
}
```

**Pendiente:** Agregar `[Authorize]` a nivel de clase para proteger TODOS los métodos.

---

### 3.2.4 Catch Vacíos en UsuarioActualService → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Services/UsuarioActualService.cs` |
| **CWE** | CWE-390: Detection of Error Condition Without Action |
| **Severidad** | 🟡 Media |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** `catch { }` silenciaba todos los errores.
**Después:** Sin bloques try-catch vacíos. Si la identidad no es válida, `_usuarioActual` se establece en `null`.

---

### 3.2.5 Sin Middleware de Autenticación/Autorización → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Program.cs` |
| **CWE** | CWE-306: Missing Authentication for Critical Function |
| **Severidad** | 🔴 Crítica |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** `Program.cs` no contenía `UseAuthentication()` ni `UseAuthorization()`.

**Después:**
```csharp
app.UseAuthentication();   // ✅ Agregado
app.UseAuthorization();    // ✅ Agregado
```

---

### 3.2.6 Rutas sin Protección de Autorización → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Components/Routes.razor` |
| **CWE** | CWE-862: Missing Authorization |
| **Severidad** | 🔴 Crítica |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** `<RouteView>` sin autorización.
**Después:**
```razor
<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeRouteView>
```

---

### 3.2.7 Sin CascadingAuthenticationState → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Components/App.razor` |
| **CWE** | CWE-862: Missing Authorization |
| **Severidad** | 🟠 Alta |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Después:**
```razor
<CascadingAuthenticationState>
    <Routes @rendermode="InteractiveServer" />
</CascadingAuthenticationState>
```

---

### 3.2.8 OnConnectedAsync sin Verificación → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Hubs/CocheraHub.cs` |
| **CWE** | CWE-862: Missing Authorization |
| **Severidad** | 🟠 Alta |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** `OnConnectedAsync` no verificaba la identidad del usuario.
**Después:** Auto-join a grupos basado en la identidad autenticada.

---

### 3.2.9 Datos Seed sin Contraseñas Hasheadas → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Infrastructure/Data/CocheraDbContext.cs` |
| **CWE** | CWE-259: Use of Hard-coded Password |
| **Severidad** | 🟠 Alta |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

Los datos seed ahora usan `PasswordHasher<IdentityUser>` para generar hashes PBKDF2.

---

### 3.2.10 ProtectedSessionStorage para Identidad → ✅ CORREGIDO

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Services/UsuarioActualService.cs` |
| **CWE** | CWE-784: Reliance on Cookies without Validation |
| **Severidad** | 🟡 Media |
| **Estado** | ✅ **CORREGIDO** — Marzo 2026 |

**Antes:** Se usaba `ProtectedSessionStorage` del navegador, manipulable por el usuario.
**Después:** Se usa `AuthenticationStateProvider` que lee la cookie del servidor.

---

## 3.3 Hallazgos Pendientes ❌

### 3.3.1 IDOR en Servicios de Aplicación

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Application/Services/SesionService.cs` y otros |
| **CWE** | CWE-639: Authorization Bypass Through User-Controlled Key |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

Los servicios de dominio no verifican que el usuario autenticado sea el dueño del recurso:

```csharp
// SesionService.cs — Cualquier usuario autenticado puede acceder a cualquier sesión
public async Task<SesionEstacionamientoDto?> GetByIdAsync(int id)
{
    var sesion = await _unitOfWork.Sesiones.GetByIdAsync(id);
    return sesion != null ? MapToDto(sesion) : null;
    // ❌ No verifica que sesion.UsuarioId == usuario autenticado
}
```

**Recomendación:** Inyectar la identidad del usuario actual en la capa de servicios y filtrar por ownership.

---

### 3.3.2 Credenciales de BD en Texto Plano

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/appsettings.json` |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

```json
"DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
```

**Recomendación:** Usar `User Secrets` en desarrollo y `Azure Key Vault` o variables de entorno en producción.

---

### 3.3.3 MQTT Credenciales en Texto Plano (Firmware)

| Campo | Valor |
|-------|-------|
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **Severidad** | 🔴 Crítica |
| **Estado** | ❌ **PENDIENTE** |

```cpp
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
const char* ssid = "AVRIL@2014";
const char* password = "AVRIL@2014";
```

---

### 3.3.4 Sin Validación de Mensajes MQTT

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Infrastructure/Mqtt/MqttConsumerService.cs` |
| **CWE** | CWE-20: Improper Input Validation |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

```csharp
// Deserialización sin validación de esquema ni límite de tamaño
var mensaje = JsonSerializer.Deserialize<MensajeSensorMqtt>(payload, 
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
```

---

### 3.3.5 Cliente MQTT sin TLS

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Infrastructure/Mqtt/MqttConsumerService.cs` |
| **CWE** | CWE-319: Cleartext Transmission of Sensitive Information |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

```csharp
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, _settings.Port) // ❌ Sin .WithTlsOptions()
    .WithCredentials(_settings.Username, _settings.Password) // Credenciales en texto plano
    .Build();
```

---

### 3.3.6 WiFi Credentials Hardcoded (ESP32)

| Campo | Valor |
|-------|-------|
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

---

### 3.3.7 AllowedHosts = "*"

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/appsettings.json` |
| **CWE** | CWE-942: Overly Permissive Cross-domain Whitelist |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** |

```json
"AllowedHosts": "*"
```

---

### 3.3.8 Superusuario PostgreSQL

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/appsettings.json` |
| **CWE** | CWE-250: Execution with Unnecessary Privileges |
| **Severidad** | 🟠 Alta |
| **Estado** | ❌ **PENDIENTE** |

---

### 3.3.9 Sin Headers de Seguridad HTTP

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Program.cs` |
| **CWE** | CWE-693: Protection Mechanism Failure |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** |

Falta middleware para CSP, X-Frame-Options, X-Content-Type-Options, HSTS.

---

### 3.3.10 MQTT Reconexión sin Backoff Exponencial

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Infrastructure/Mqtt/MqttConsumerService.cs` |
| **CWE** | CWE-400: Uncontrolled Resource Consumption |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** |

```csharp
// Reconexión con delay fijo de 5s — puede saturar el broker
await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
```

---

### 3.3.11 Sin Rate Limiting en Login

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Program.cs` |
| **CWE** | CWE-307: Improper Restriction of Excessive Authentication Attempts |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** |

El endpoint `POST /auth/login` no tiene rate limiting. Combinado con `LockoutEnabled = false` en los usuarios seed, es vulnerable a ataques de fuerza bruta.

---

### 3.3.12 DisableAntiforgery en Login

| Campo | Valor |
|-------|-------|
| **Archivo** | `Cochera.Web/Program.cs` |
| **CWE** | CWE-352: Cross-Site Request Forgery |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** (complejidad técnica con Blazor Server) |

```csharp
app.MapPost("/auth/login", async (...) => { ... }).DisableAntiforgery();
```

---

### 3.3.13 JSON Buffer sin Sanitización (ESP32)

| Campo | Valor |
|-------|-------|
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-120: Buffer Copy without Checking Size of Input |
| **Severidad** | 🟡 Media |
| **Estado** | ❌ **PENDIENTE** |

```cpp
char jsonBuffer[512];
snprintf(jsonBuffer, sizeof(jsonBuffer),
    "{\"evento\":\"%s\",\"detalle\":\"%s\",...}", evento, detalle);
// Si evento o detalle exceden el buffer, truncamiento silencioso
```

---

## 3.4 Tabla Resumen de Hallazgos

| # | Hallazgo | CWE | Severidad | Estado |
|---|----------|-----|-----------|--------|
| 1 | Suplantación de identidad sin restricciones | CWE-287 | 🔴 Crítica | ✅ Corregido |
| 2 | Modelo sin campo de contraseña | CWE-257 | 🔴 Crítica | ✅ Corregido |
| 3 | SignalR Hub abierto | CWE-862 | 🔴→🟡 | ⚠️ Parcial |
| 4 | Catch vacíos | CWE-390 | 🟡 Media | ✅ Corregido |
| 5 | Sin middleware auth | CWE-306 | 🔴 Crítica | ✅ Corregido |
| 6 | Rutas sin autorización | CWE-862 | 🔴 Crítica | ✅ Corregido |
| 7 | Sin CascadingAuthState | CWE-862 | 🟠 Alta | ✅ Corregido |
| 8 | OnConnectedAsync sin verificación | CWE-862 | 🟠 Alta | ✅ Corregido |
| 9 | Seed sin password hash | CWE-259 | 🟠 Alta | ✅ Corregido |
| 10 | ProtectedSessionStorage para identidad | CWE-784 | 🟡 Media | ✅ Corregido |
| 11 | IDOR en servicios | CWE-639 | 🟠 Alta | ❌ Pendiente |
| 12 | Credenciales BD en texto plano | CWE-798 | 🟠 Alta | ❌ Pendiente |
| 13 | Credenciales MQTT firmware | CWE-798 | 🔴 Crítica | ❌ Pendiente |
| 14 | Sin validación MQTT | CWE-20 | 🟠 Alta | ❌ Pendiente |
| 15 | MQTT sin TLS | CWE-319 | 🟠 Alta | ❌ Pendiente |
| 16 | WiFi hardcoded ESP32 | CWE-798 | 🟠 Alta | ❌ Pendiente |
| 17 | AllowedHosts = "*" | CWE-942 | 🟡 Media | ❌ Pendiente |
| 18 | Superusuario PostgreSQL | CWE-250 | 🟠 Alta | ❌ Pendiente |
| 19 | Sin headers seguridad | CWE-693 | 🟡 Media | ❌ Pendiente |
| 20 | MQTT reconexión sin backoff | CWE-400 | 🟡 Media | ❌ Pendiente |
| 21 | Sin rate limiting login | CWE-307 | 🟡 Media | ❌ Pendiente |
| 22 | DisableAntiforgery login | CWE-352 | 🟡 Media | ❌ Pendiente |
| 23 | JSON buffer sin sanitización | CWE-120 | 🟡 Media | ❌ Pendiente |

---

*Anterior: [02 — Amenazas y Vulnerabilidades](02-amenazas-y-vulnerabilidades-owasp.md)*
*Siguiente: [04 — Propuesta de Mejoras](04-propuesta-mejoras.md)*
