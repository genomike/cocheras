# 01 — Descripción del Sistema

## 1.1 Resumen Ejecutivo

**Cochera Inteligente** es un sistema de gestión de estacionamiento basado en IoT que integra sensores ultrasónicos (ESP32), un broker de mensajería MQTT (RabbitMQ), y una aplicación web en tiempo real construida con ASP.NET Core 8.0 y Blazor Server.

> **Actualización Marzo 2026:** El sistema ahora cuenta con **autenticación real** basada en ASP.NET Core Identity con cookies HTTP-only, roles (Admin/User), hashing de contraseñas con PBKDF2, y autorización en rutas y hub SignalR.

---

## 1.2 Arquitectura del Sistema

### 1.2.1 Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        SISTEMA COCHERA INTELIGENTE                          │
│                                                                             │
│  ┌──────────┐    MQTT (1883)    ┌─────────────┐    EF Core    ┌──────────┐ │
│  │  ESP32   │ ───────────────►  │  RabbitMQ   │               │PostgreSQL│ │
│  │(Sensores)│   Plaintext       │  (Broker)   │               │          │ │
│  └──────────┘                   └──────┬──────┘               └─────┬────┘ │
│                                        │ MQTT                       │      │
│                                  ┌─────▼──────┐              ┌─────▼────┐  │
│                                  │  Cochera    │──── Repos ──►│ Cochera  │  │
│                                  │  Worker     │              │ Infra    │  │
│                                  └─────┬──────┘              └──────────┘  │
│                                        │ HTTP (SignalR)                     │
│                                  ┌─────▼──────┐                            │
│  ┌──────────┐   WSS/HTTPS       │  Cochera    │                            │
│  │Navegador │ ◄───────────────► │  Web        │                            │
│  │ (Blazor) │   Cookie Auth     │ (.NET 8)    │                            │
│  └──────────┘                   └─────────────┘                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2.2 Arquitectura Clean Architecture

```
┌────────────────────────────────────────────────┐
│              Cochera.Web (Presentación)          │
│  - Blazor Server (InteractiveServer)            │
│  - ASP.NET Core Identity (Auth)          🔒 NEW │
│  - SignalR Hub con [Authorize]           🔒 NEW │
│  - Login.razor (form HTML POST)          🔒 NEW │
│  - AuthorizeRouteView                    🔒 NEW │
├────────────────────────────────────────────────┤
│              Cochera.Worker (Background)         │
│  - MqttWorker (BackgroundService)               │
│  - SignalRNotificationService                   │
├────────────────────────────────────────────────┤
│            Cochera.Application (Servicios)       │
│  - DTOs, Interfaces, Services                   │
│  - Sin validación de ownership ⚠️               │
├────────────────────────────────────────────────┤
│             Cochera.Domain (Entidades)           │
│  - Entities, Enums, Repository Interfaces       │
├────────────────────────────────────────────────┤
│          Cochera.Infrastructure (Datos)          │
│  - CocheraDbContext (IdentityDbContext)   🔒 NEW │
│  - PasswordHasher seed data              🔒 NEW │
│  - Roles y UserRoles seed                🔒 NEW │
│  - Repositories, MQTT, RabbitMQ                 │
└────────────────────────────────────────────────┘
```

---

## 1.3 Stack Tecnológico

| Componente | Tecnología | Versión | Notas de Seguridad |
|-----------|-----------|---------|---------------------|
| Framework | ASP.NET Core | 8.0 | LTS, soporte hasta Nov 2026 |
| Frontend | Blazor Server | 8.0 | InteractiveServer render mode |
| **Autenticación** | **ASP.NET Core Identity** | **8.0** | 🔒 **Implementado** — IdentityUser, IdentityRole, PasswordHasher (PBKDF2) |
| **Cookies** | **Cookie Authentication** | **8.0** | 🔒 **Implementado** — HttpOnly, 8h expiry, SlidingExpiration |
| Componentes UI | Radzen Blazor | 5.x | Componentes de terceros |
| ORM | Entity Framework Core | 8.0 | Npgsql provider |
| Base de datos | PostgreSQL | 16.x | Acceso con superusuario ⚠️ |
| Mensajería | RabbitMQ (MQTT Plugin) | — | Sin TLS ⚠️ |
| MQTT Client | MQTTnet | 4.3.3 | Sin cifrado ⚠️ |
| Tiempo real | SignalR | 8.0 | WebSocket con [Authorize] parcial |
| Firmware | Arduino/ESP32 | — | Credenciales hardcoded ⚠️ |

---

## 1.4 Flujos de Datos Críticos

### 1.4.1 Flujo de Autenticación (🔒 NUEVO)

```
┌────────────┐     GET /login      ┌──────────────┐
│  Navegador │ ──────────────────► │  Login.razor  │
│            │ ◄────────────────── │  (HTML form)  │
│            │     HTML estático    └──────────────┘
│            │
│            │    POST /auth/login   ┌──────────────────────────┐
│            │ ────────────────────► │ Minimal API Endpoint     │
│            │    Form data          │ SignInManager             │
│            │    (username/password) │   .PasswordSignInAsync() │
│            │                       └──────────┬───────────────┘
│            │                                  │
│            │    Set-Cookie: Cochera.Auth=...   │ (success)
│            │    302 Redirect /               ◄┘
│            │ ◄──────────────────────────────── 
│            │                                    (failure)
│            │    302 Redirect /login?error=1   ◄┘
│            │ ◄──────────────────────────────── 
└────────────┘

Nota: El login usa HTTP POST fuera del circuito interactivo de Blazor
para evitar la excepción de cookies en componentes InteractiveServer.
```

### 1.4.2 Flujo de Datos IoT → Web

```
ESP32 (sensores) 
    → MQTT plaintext (puerto 1883) 
        → RabbitMQ 
            → Cochera.Worker (MqttWorker) 
                → HTTP POST a SignalR Hub 
                    → [Sin clase-level Authorize] ⚠️
                        → Clients.All.SendAsync("RecibirEvento") 
                            → Blazor UI actualiza en tiempo real
```

### 1.4.3 Flujo de Sesión de Estacionamiento

```
Admin detecta vehículo → Crea sesión (cajonId, usuarioId)
    → SignalR notifica a Admin (grupo "admins") 
    → SignalR notifica a Usuario (grupo "usuario_{id}")
        → [Authorize] valida identidad ✅

Admin solicita cierre → Usuario recibe notificación
    → Usuario confirma pago → Admin recibe confirmación
        → Admin cierra sesión → Ambos notificados
```

---

## 1.5 Usuarios y Roles del Sistema

### 1.5.1 Modelo de Usuarios (Actualizado)

El sistema ahora gestiona dos modelos de usuario paralelos:

| Modelo | Propósito | Autenticación |
|--------|----------|---------------|
| `IdentityUser` | Autenticación (ASP.NET Core Identity) | PasswordHash (PBKDF2-HMAC-SHA256) |
| `Usuario` (dominio) | Lógica de negocio (sesiones, pagos) | Vinculado por `Codigo` |

### 1.5.2 Usuarios Seed

| IdentityUser (UserName) | Rol | Usuario Dominio (Codigo) | Contraseña |
|-------------------------|-----|--------------------------|------------|
| `admin` | Admin | `admin` (Id=1) | `Admin12345` |
| `usuario_1` | User | `usuario_1` (Id=2) | `Usuario12345` |
| `usuario_2` | User | `usuario_2` (Id=3) | `Usuario12345` |
| `usuario_3` | User | `usuario_3` (Id=4) | `Usuario12345` |

> **⚠️ Observación de seguridad:** Las contraseñas se muestran en la página de login como "usuarios de prueba". `LockoutEnabled = false` en los usuarios seed desactiva la protección contra fuerza bruta.

### 1.5.3 Roles y Permisos

| Rol | Páginas Accesibles | Hub SignalR |
|-----|-------------------|-------------|
| **Admin** | Dashboard, Gestión Entrada/Salida, Sesiones, Historial, Eventos, Tarifas, Cajones, Reportes | `UnirseComoAdmin()` [Authorize(Roles="Admin")], auto-join grupo "admins" |
| **User** | Mi Estacionamiento, Mis Pagos, Mi Historial | `UnirseComoUsuario(id)` [Authorize] con validación de identidad |
| **Anónimo** | Login, AccessDenied | Ninguno |

---

## 1.6 Superficie de Ataque

### 1.6.1 Puntos de Entrada (Actualizado)

| # | Punto de Entrada | Protocolo | Autenticación | Estado |
|---|-----------------|-----------|---------------|--------|
| 1 | `POST /auth/login` | HTTPS | Público (AntiForgery deshabilitado) | 🔒 Nuevo |
| 2 | `GET /logout` | HTTPS | Autenticado | 🔒 Nuevo |
| 3 | `/cocherahub` (SignalR) | WSS | Parcial — métodos protegidos, clase no | ⚠️ Mejorado |
| 4 | Páginas Blazor (`/admin/*`, `/usuario/*`) | HTTPS | `[Authorize(Roles)]` vía `AuthorizeRouteView` | 🔒 Nuevo |
| 5 | `/login` | HTTPS | Público | 🔒 Nuevo |
| 6 | MQTT broker (puerto 1883) | TCP plano | Basic auth (user/pass) | ❌ Sin cifrar |
| 7 | PostgreSQL (puerto 5432) | TCP | Superusuario `postgres` | ❌ Inseguro |
| 8 | WiFi ESP32 | WPA2 | Credenciales hardcoded | ❌ Inseguro |

### 1.6.2 Datos Sensibles Identificados

| Dato | Ubicación | Protección Actual |
|------|-----------|-------------------|
| Contraseñas de usuarios | PostgreSQL (tabla `AspNetUsers`) | ✅ Hash PBKDF2 (PasswordHasher) |
| Cookie de sesión | Navegador → Servidor | ✅ HttpOnly, SlidingExpiration 8h |
| Credenciales BD | `appsettings.json` | ❌ Texto plano (`postgres/postgres`) |
| Credenciales MQTT broker | `appsettings.json` (Worker) | ❌ Texto plano |
| Credenciales WiFi/MQTT | `sketch_jan16a.ino` | ❌ Hardcoded (`esp32/123456`) |
| Datos de sensores | Tránsito MQTT → DB | ❌ Sin cifrado en tránsito |
| Seed passwords | `CocheraDbContext.cs` | ⚠️ En código fuente pero hasheadas en BD |

---

## 1.7 Alcance del Análisis de Seguridad

### 1.7.1 Archivos Analizados (17 archivos)

**Cochera.Web (10 archivos):**
- `Program.cs` — Startup, Identity, Auth middleware, endpoints login/logout
- `Hubs/CocheraHub.cs` — SignalR hub con [Authorize] parcial
- `Services/UsuarioActualService.cs` — Servicio de usuario actual (usa AuthenticationStateProvider)
- `Components/Pages/Login.razor` — Página de login (form HTML POST)
- `Components/Pages/AccessDenied.razor` — Página de acceso denegado
- `Components/RedirectToLogin.razor` — Componente de redirección
- `Components/Routes.razor` — AuthorizeRouteView
- `Components/App.razor` — CascadingAuthenticationState
- `Components/Layout/MainLayout.razor` — Layout con AuthorizeView
- `appsettings.json` — Configuración con credenciales

**Cochera.Infrastructure (2 archivos):**
- `Data/CocheraDbContext.cs` — IdentityDbContext con seed de roles/usuarios
- `Mqtt/MqttConsumerService.cs` — Cliente MQTT sin TLS

**Cochera.Application (3 archivos):**
- `Services/SesionService.cs` — Gestión de sesiones (sin filtro por usuario)
- `Services/EventoSensorService.cs` — Eventos de sensores
- `Services/UsuarioService.cs` — Gestión de usuarios

**Cochera.Worker (1 archivo):**
- `MqttWorker.cs` — Background service para MQTT

**Firmware (1 archivo):**
- `sketch_jan16a.ino` — Firmware ESP32 con credenciales hardcoded

### 1.7.2 Marcos de Referencia

| Marco | Aplicación |
|-------|-----------|
| OWASP Top 10:2021 | Clasificación de vulnerabilidades web |
| OWASP IoT Top 10:2018 | Clasificación de vulnerabilidades IoT |
| CWE/SANS Top 25 | Identificación de debilidades de código |
| CVSS v3.1 | Puntuación de severidad |
| OWASP SAMM | Evaluación de madurez de seguridad |

---

*Siguiente: [02 — Amenazas y Vulnerabilidades (OWASP)](02-amenazas-y-vulnerabilidades-owasp.md)*
