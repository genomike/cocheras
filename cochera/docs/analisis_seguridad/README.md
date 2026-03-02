# Análisis de Seguridad — Cochera Inteligente

## Descripción

Este directorio contiene un **análisis exhaustivo de seguridad de software** del sistema Cochera Inteligente, realizado como parte del proyecto académico de la Maestría en Sistemas Embebidos.

El análisis se realizó sobre el código fuente completo del sistema, aplicando marcos de referencia reconocidos internacionalmente: **OWASP Top 10:2021**, **OWASP IoT Top 10:2018**, **CWE/SANS Top 25** y **CVSS v3.1**.

> **Actualización Marzo 2026:** Se implementó autenticación real con ASP.NET Core Identity, lo que mitigó las vulnerabilidades más críticas del sistema (V-001, V-002, V-008, V-009, V-014). Este documento refleja el estado de seguridad actualizado post-remediación.

---

## Documentos

| # | Documento | Descripción |
|---|-----------|------------|
| 01 | [Descripción del Sistema](01-descripcion-del-sistema.md) | Arquitectura, componentes, tecnologías, flujos de datos y superficie de ataque del sistema (actualizada) |
| 02 | [Amenazas y Vulnerabilidades (OWASP)](02-amenazas-y-vulnerabilidades-owasp.md) | 18 vulnerabilidades mapeadas al OWASP Top 10:2021 y OWASP IoT Top 10:2018. **7 mitigadas, 11 pendientes** |
| 03 | [Análisis de Código Inseguro](03-analisis-codigo-inseguro.md) | 23 hallazgos de código inseguro con fragmentos actualizados, CWE y escenarios. **10 corregidos, 13 pendientes** |
| 04 | [Propuesta de Mejoras](04-propuesta-mejoras.md) | 18 propuestas de mejora priorizadas P0-P3. **4 implementadas (P0), 14 pendientes** |
| 05 | [Pruebas de Seguridad (SAST/DAST)](05-pruebas-sast-dast.md) | Guía de herramientas de prueba con configuraciones actualizadas para el sistema con Identity |
| 06 | [Gestión de Vulnerabilidades y Parcheo](06-gestion-vulnerabilidades.md) | Ciclo de vida, SLAs, Dependabot/Snyk, plan de respuesta a incidentes. Registro actualizado |
| 07 | [Conclusiones y Reflexiones](07-conclusiones.md) | Resumen ejecutivo actualizado, evaluación OWASP SAMM mejorada, recomendaciones revisadas |

---

## Resumen de Hallazgos (Actualizado — Marzo 2026)

### Estado General Post-Remediación

| Métrica | Antes | Después | Cambio |
|---------|-------|---------|--------|
| Vulnerabilidades OWASP totales | 18 | 18 | — |
| ✅ Mitigadas | 0 | 7 | +7 |
| ⚠️ Parcialmente mitigadas | 0 | 2 | +2 |
| ❌ Pendientes | 18 | 9 | -9 |
| Hallazgos de código inseguro | 23 | 23 | — |
| ✅ Hallazgos corregidos | 0 | 10 | +10 |
| ❌ Hallazgos pendientes | 23 | 13 | -13 |
| Categorías OWASP con vulnerabilidades | 9/10 | 7/10 | -2 |

### Vulnerabilidades Críticas — Estado Actual

| CVSS | Vulnerabilidad | Componente | Estado |
|------|---------------|-----------|--------|
| ~~9.8~~ | ~~Sin autenticación~~ | ~~Cochera.Web~~ | ✅ **MITIGADA** — ASP.NET Core Identity |
| ~~9.8~~ | ~~Modelo sin contraseñas~~ | ~~Cochera.Domain~~ | ✅ **MITIGADA** — IdentityUser con PasswordHash |
| ~~9.8~~ | ~~Diseño sin seguridad~~ | ~~Diseño general~~ | ⚠️ **PARCIAL** — Auth/AuthZ implementados |
| ~~9.1~~ | ~~SignalR Hub sin autorización~~ | ~~Cochera.Web~~ | ⚠️ **PARCIAL** — Métodos protegidos, clase sin `[Authorize]` global |
| 8.6 | Credenciales hardcoded en firmware | ESP32 | ❌ Pendiente |
| 8.1 | MQTT sin cifrado TLS | Infraestructura | ❌ Pendiente |
| 7.5 | Credenciales de BD en código fuente | Cochera.Web | ❌ Pendiente |

### Distribución Actualizada por Severidad

| Severidad | Total | Mitigadas | Pendientes |
|-----------|-------|-----------|------------|
| 🔴 Crítico (5) | 5 | 3 ✅ + 2 ⚠️ | 0 puras |
| 🟠 Alto (8) | 8 | 2 ✅ | 6 |
| 🟡 Medio (5) | 5 | 0 | 5 |
| **Total** | **18** | **7** | **11** |

---

## Implementación Realizada (Marzo 2026)

### Mejoras P0 Implementadas

| ID | Mejora | Estado | Detalle |
|----|--------|--------|---------|
| M-01 | ASP.NET Core Identity | ✅ Implementada | `IdentityUser`, roles (Admin/User), `PasswordHasher`, cookies seguras, bloqueo por intentos fallidos |
| M-02 | `[Authorize]` en SignalR Hub | ✅ Implementada | Métodos protegidos con `[Authorize(Roles)]`, auto-join en `OnConnectedAsync`, validación de identidad |
| M-01b | Página de Login con form POST | ✅ Implementada | Login vía HTTP POST (`/auth/login`) fuera del circuito interactivo de Blazor |
| M-01c | Protección de rutas por roles | ✅ Implementada | `[Authorize(Roles = "Admin")]` en páginas admin, `[Authorize(Roles = "User")]` en páginas usuario |

### Arquitectura de Seguridad Implementada

```
┌──────────────────────────────────────────────────────────────┐
│                    FLUJO DE AUTENTICACIÓN                      │
│                                                                │
│   Navegador                    Servidor (.NET 8)              │
│   ┌────────┐   POST /auth/login   ┌─────────────────┐        │
│   │ Login  │ ────────────────────► │ SignInManager    │        │
│   │ (HTML) │                       │ .PasswordSignIn  │        │
│   └────────┘   Cookie: Cochera.Auth│ Async()          │        │
│       ▲        ◄─────────────────  └─────────────────┘        │
│       │                                                        │
│   ┌────────┐   Cookie en headers   ┌─────────────────┐        │
│   │ Blazor │ ────────────────────► │ AuthenticationState│      │
│   │ Server │                       │ Provider          │      │
│   └────────┘                       └─────────────────┘        │
│       │                                                        │
│   ┌────────┐   Cookie en upgrade   ┌─────────────────┐        │
│   │SignalR │ ────────────────────► │ Context.User     │        │
│   │ Hub    │                       │ .IsInRole()      │        │
│   └────────┘                       └─────────────────┘        │
└──────────────────────────────────────────────────────────────┘
```

---

## Marcos de Referencia Utilizados

- **OWASP Top 10:2021** — Clasificación de riesgos en aplicaciones web
- **OWASP IoT Top 10:2018** — Clasificación de riesgos en dispositivos IoT
- **CWE (Common Weakness Enumeration)** — Identificación de debilidades de software
- **CVSS v3.1 (Common Vulnerability Scoring System)** — Puntuación de severidad
- **OWASP SAMM (Software Assurance Maturity Model)** — Evaluación de madurez

---

## Archivos Analizados

Se realizó revisión de seguridad sobre los siguientes **17 archivos críticos** (13 originales + 4 nuevos):

| Proyecto | Archivo | Estado |
|----------|---------|--------|
| Cochera.Web | Program.cs | 🔄 Modificado (Identity, Auth middleware, endpoints login/logout) |
| Cochera.Web | CocheraHub.cs | 🔄 Modificado (`[Authorize]` en métodos, auto-join, validaciones) |
| Cochera.Web | UsuarioActualService.cs | 🔄 Modificado (usa `AuthenticationStateProvider`) |
| Cochera.Web | MainLayout.razor | 🔄 Modificado (`<AuthorizeView>`, botón logout) |
| Cochera.Web | Login.razor | 🆕 Nuevo (formulario HTML POST) |
| Cochera.Web | AccessDenied.razor | 🆕 Nuevo (página de acceso denegado) |
| Cochera.Web | RedirectToLogin.razor | 🆕 Nuevo (componente de redirección) |
| Cochera.Web | Routes.razor | 🔄 Modificado (`<AuthorizeRouteView>`) |
| Cochera.Web | App.razor | 🔄 Modificado (`<CascadingAuthenticationState>`) |
| Cochera.Infrastructure | CocheraDbContext.cs | 🔄 Modificado (`IdentityDbContext`, seed roles/users con PasswordHasher) |
| Cochera.Application | SesionService.cs, EventoSensorService.cs, UsuarioService.cs, TarifaService.cs | Sin cambios |
| Cochera.Infrastructure | MqttConsumerService.cs, MqttSettings.cs | Sin cambios |
| Cochera.Worker | MqttWorker.cs, SignalRNotificationService.cs | Sin cambios |
| Firmware | sketch_jan16a.ino | Sin cambios |

---

*Análisis original: Enero 2025*
*Actualización post-remediación: Marzo 2026*
