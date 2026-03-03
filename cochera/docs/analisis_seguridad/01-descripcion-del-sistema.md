# 01 — Descripción del Sistema

## 1.1 Resumen Ejecutivo

**Cochera Inteligente** es un sistema de gestión de estacionamiento basado en IoT que integra sensores ultrasónicos (ESP32), un broker de mensajería MQTT (RabbitMQ), y una aplicación web en tiempo real construida con ASP.NET Core 8.0 y Blazor Server.

El sistema cuenta con autenticación basada en ASP.NET Core Identity con cookies HTTP-only, roles (Admin/User), hashing de contraseñas con PBKDF2, y autorización en rutas y hub SignalR.

---

## 1.2 Arquitectura del Sistema

### 1.2.1 Diagrama de Componentes

```plantuml
@startuml
title Sistema Cochera Inteligente - Diagrama de Componentes
left to right direction

rectangle "ESP32\n(Sensores)" as ESP32
rectangle "RabbitMQ\n(Broker)" as MQ
rectangle "Cochera.Worker" as Worker
rectangle "Cochera.Web\n(.NET 8)" as Web
rectangle "Cochera.Infrastructure" as Infra
database "PostgreSQL" as DB
rectangle "Navegador\n(Blazor)" as Browser

ESP32 --> MQ : MQTT (1883)\nTexto plano
MQ --> Worker : MQTT
Worker --> Infra : Repositorios
Infra --> DB : EF Core
Worker --> Web : HTTP (SignalR)
Browser <--> Web : HTTP/WSS\nCookie Auth
@enduml
```

### 1.2.2 Arquitectura Clean Architecture

```plantuml
@startuml
title Cochera - Arquitectura Clean Architecture
top to bottom direction

rectangle "Cochera.Web\n(Presentación)\n- Blazor Server\n- ASP.NET Core Identity\n- SignalR Hub\n- Login.razor\n- AuthorizeRouteView" as Web
rectangle "Cochera.Worker\n(Background)\n- MqttWorker\n- SignalRNotificationService" as Worker
rectangle "Cochera.Application\n(Servicios)\n- DTOs, Interfaces, Services\n- Sin validación de ownership [RIESGO]" as App
rectangle "Cochera.Domain\n(Entidades)\n- Entities, Enums, Repository Interfaces" as Domain
rectangle "Cochera.Infrastructure\n(Datos)\n- CocheraDbContext\n- PasswordHasher seed\n- Repositories, MQTT, RabbitMQ" as Infra

Web --> App
Worker --> App
App --> Domain
App --> Infra
Infra --> Domain
@enduml
```

---

## 1.3 Stack Tecnológico

| Componente | Tecnología | Versión | Notas de Seguridad |
|-----------|-----------|---------|---------------------|
| Framework | ASP.NET Core | 8.0 | LTS, soporte hasta Nov 2026 |
| Frontend | Blazor Server | 8.0 | InteractiveServer render mode |
| Autenticación | ASP.NET Core Identity | 8.0 | IdentityUser, IdentityRole, PasswordHasher (PBKDF2) |
| Cookies | Cookie Authentication | 8.0 | HttpOnly, 8h expiry, SlidingExpiration |
| Componentes UI | Radzen Blazor | 5.x | Componentes de terceros |
| ORM | Entity Framework Core | 8.0 | Npgsql provider |
| Base de datos | PostgreSQL | 16.x | Acceso con superusuario [RIESGO]️ |
| Mensajería | RabbitMQ (MQTT Plugin) | — | Sin TLS [RIESGO]️ |
| MQTT Client | MQTTnet | 4.3.3 | Sin cifrado [RIESGO]️ |
| Tiempo real | SignalR | 8.0 | WebSocket con [Authorize] parcial [RIESGO]️ |
| Firmware | Arduino/ESP32 | — | Credenciales hardcoded [RIESGO]️ |

---

## 1.4 Flujos de Datos Críticos

### 1.4.1 Flujo de Autenticación

```plantuml
@startuml
title Flujo de Autenticación
actor Navegador
participant "Login.razor\n(HTML form)" as Login
participant "Minimal API\n/auth/login" as Endpoint
participant "SignInManager" as SignIn

Navegador -> Login : GET /login
Login --> Navegador : HTML estático
Navegador -> Endpoint : POST /auth/login\nusername/password/returnUrl
Endpoint -> SignIn : PasswordSignInAsync(...)

alt Credenciales válidas
    SignIn --> Endpoint : Succeeded
    Endpoint --> Navegador : Set-Cookie + 302 Redirect
else Credenciales inválidas
    SignIn --> Endpoint : Failed
    Endpoint --> Navegador : 302 /login?error=1
end

note right
El login usa HTTP POST fuera del circuito
interactivo de Blazor para evitar conflicto
de cookies en InteractiveServer.
end note
@enduml
```

### 1.4.2 Flujo de Datos IoT → Web

```plantuml
@startuml
title Flujo de Datos IoT a Web
actor ESP32
participant "RabbitMQ\n(MQTT 1883)" as MQ
participant "Cochera.Worker\n(MqttWorker)" as Worker
participant "SignalR Hub" as Hub
participant "Blazor UI" as UI

ESP32 -> MQ : Publica evento (JSON)
note right of MQ
Transporte en texto plano
[RIESGO] Sin TLS
end note

MQ -> Worker : Mensaje MQTT
Worker -> Hub : HTTP POST / notificación
note right of Hub
[RIESGO] Hub sin [Authorize]
a nivel de clase
end note
Hub -> UI : Clients.All.SendAsync(...)
@enduml
```

### 1.4.3 Flujo de Sesión de Estacionamiento

```plantuml
@startuml
title Flujo de Sesión de Estacionamiento
start
:Admin detecta vehículo;
:Crea sesión (cajonId, usuarioId);
:SignalR notifica a grupo admins;
:SignalR notifica a grupo usuario_{id};
:Admin solicita cierre de sesión;
:Usuario recibe notificación;
:Usuario confirma pago;
:Admin recibe confirmación;
:Admin cierra sesión;
:SignalR notifica cierre a admin y usuario;
stop
@enduml
```

---

## 1.5 Usuarios y Roles del Sistema

### 1.5.1 Modelo de Usuarios

El sistema gestiona dos modelos de usuario paralelos:

| Modelo | Propósito | Autenticación |
|--------|----------|---------------|
| `IdentityUser` | Autenticación (ASP.NET Core Identity) | PasswordHash (PBKDF2-HMAC-SHA256) |
| `Usuario` (dominio) | Lógica de negocio (sesiones, pagos) | Vinculado por `Codigo`/`UserName` |

### 1.5.2 Usuarios Seed

| IdentityUser (UserName) | Rol | Usuario Dominio (Codigo) | Contraseña |
|-------------------------|-----|--------------------------|------------|
| `admin` | Admin | `admin` (Id=1) | `Admin12345` |
| `usuario_1` | User | `usuario_1` (Id=2) | `Usuario12345` |
| `usuario_2` | User | `usuario_2` (Id=3) | `Usuario12345` |
| `usuario_3` | User | `usuario_3` (Id=4) | `Usuario12345` |

### 1.5.3 Roles y Permisos

| Rol | Páginas Accesibles | Hub SignalR |
|-----|-------------------|-------------|
| **Admin** | Dashboard, Gestión Entrada/Salida, Sesiones, Historial, Eventos, Tarifas, Cajones, Reportes | `UnirseComoAdmin()` [Authorize(Roles="Admin")] |
| **User** | Mi Estacionamiento, Mis Pagos, Mi Historial | `UnirseComoUsuario(id)` [Authorize] con validación |
| **Anónimo** | Login, AccessDenied | Ninguno |

---

## 1.6 Superficie de Ataque

### 1.6.1 Puntos de Entrada

| # | Punto de Entrada | Protocolo | Autenticación | Observación |
|---|-----------------|-----------|---------------|-------------|
| 1 | `POST /auth/login` | HTTP | Público | AntiForgery deshabilitado, sin rate limiting [RIESGO]️ |
| 2 | `GET /logout` | HTTP | Autenticado | — |
| 3 | `/cocherahub` (SignalR) | WSS | Parcial | Métodos clave protegidos, clase sin `[Authorize]` [RIESGO]️ |
| 4 | Páginas Blazor (`/admin/*`, `/usuario/*`) | HTTP | `[Authorize(Roles)]` vía `AuthorizeRouteView` | — |
| 5 | `/login` | HTTP | Público | Muestra credenciales de prueba [RIESGO]️ |
| 6 | MQTT broker (puerto 1883) | TCP plano | Basic auth (user/pass) | Sin cifrado TLS [RIESGO]️ |
| 7 | PostgreSQL (puerto 5432) | TCP | Superusuario `postgres` | Permisos excesivos [RIESGO]️ |
| 8 | WiFi ESP32 | WPA2 | Credenciales hardcoded | Extraíbles del firmware [RIESGO]️ |

### 1.6.2 Datos Sensibles Identificados

| Dato | Ubicación | Protección Actual |
|------|-----------|-------------------|
| Contraseñas de usuarios | PostgreSQL (tabla `AspNetUsers`) | [OK] Hash PBKDF2 (PasswordHasher) |
| Cookie de sesión | Navegador → Servidor | [OK] HttpOnly, SlidingExpiration 8h |
| Credenciales BD | `appsettings.json` | [FALLA] Texto plano (`postgres/postgres`) |
| Credenciales MQTT broker | `appsettings.json` (Worker) | [FALLA] Texto plano |
| Credenciales WiFi/MQTT | `sketch_jan16a.ino` | [FALLA] Hardcoded (`esp32/123456`) |
| Datos de sensores | Tránsito MQTT → DB | [FALLA] Sin cifrado en tránsito |
| Seed passwords | `CocheraDbContext.cs` | [RIESGO]️ En código fuente pero hasheadas en BD |

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


