# Análisis de Seguridad — Cochera Inteligente

## Descripción

Este directorio contiene un **análisis exhaustivo de seguridad de software** del sistema Cochera Inteligente, realizado como parte del proyecto académico de la Maestría en Sistemas Embebidos.

El análisis se realizó sobre el código fuente completo del sistema en su estado actual (con autenticación ASP.NET Core Identity implementada), aplicando marcos de referencia reconocidos internacionalmente: **OWASP Top 10:2021**, **OWASP IoT Top 10:2018**, **CWE/SANS Top 25** y **CVSS v3.1**.

---

## Documentos

| # | Documento | Descripción |
|---|-----------|------------|
| 01 | [Descripción del Sistema](01-descripcion-del-sistema.md) | Arquitectura, componentes, tecnologías, flujos de datos y superficie de ataque |
| 02 | [Amenazas y Vulnerabilidades (OWASP)](02-amenazas-y-vulnerabilidades-owasp.md) | 14 vulnerabilidades identificadas mapeadas al OWASP Top 10:2021 y OWASP IoT Top 10:2018 |
| 03 | [Análisis de Código Inseguro](03-analisis-codigo-inseguro.md) | 16 hallazgos de código inseguro con fragmentos, CWE y escenarios de explotación |
| 04 | [Propuesta de Mejoras](04-propuesta-mejoras.md) | 16 propuestas de mejora priorizadas P0-P3 con estimaciones de esfuerzo |
| 05 | [Pruebas de Seguridad (SAST/DAST)](05-pruebas-sast-dast.md) | Guía de herramientas de prueba con configuraciones para el sistema |
| 06 | [Gestión de Vulnerabilidades y Parcheo](06-gestion-vulnerabilidades.md) | Ciclo de vida, SLAs, plan de respuesta a incidentes |
| 07 | [Conclusiones y Reflexiones](07-conclusiones.md) | Resumen ejecutivo, evaluación OWASP SAMM, recomendaciones |

---

## Resumen de Hallazgos

### Vulnerabilidades Identificadas

Se identificaron **14 vulnerabilidades** en el sistema, distribuidas por severidad:

| Severidad | Cantidad | CVSS |
|-----------|----------|------|
| [CRITICA] Crítica (9.0+) | 1 | 9.1 |
| [ALTA] Alta (7.0–8.9) | 5 | 7.0–8.6 |
| [MEDIA] Media (4.0–6.9) | 7 | 4.3–6.5 |
| [BAJA] Baja (< 4.0) | 1 | 3.5 |
| **Total** | **14** | **Promedio: 6.2** |

### Vulnerabilidades por Severidad

| ID | Vulnerabilidad | CVSS | Componente |
|----|---------------|------|-----------|
| V-001 | Credenciales hardcoded en firmware ESP32 | 9.1 | ESP32 |
| V-002 | Acceso a BD con superusuario PostgreSQL | 8.6 | Infraestructura |
| V-003 | Inyección vía mensajes MQTT sin validación | 8.1 | Cochera.Worker |
| V-004 | Sin integridad en mensajes MQTT (sin HMAC) | 8.1 | Infraestructura |
| V-005 | MQTT sin cifrado TLS (texto plano) | 7.4 | Infraestructura |
| V-006 | IDOR — sin verificación de ownership en servicios | 7.5 | Cochera.Application |
| V-007 | Sin logging/auditoría de seguridad | 7.0 | Cochera.Web |
| V-008 | Sin Secure Boot en ESP32 | 6.5 | Hardware |
| V-009 | SignalR Hub sin `[Authorize]` a nivel de clase | 5.5 | Cochera.Web |
| V-010 | Sin headers de seguridad HTTP | 5.3 | Cochera.Web |
| V-011 | Sin análisis de CVEs en dependencias | 5.0 | Todos |
| V-012 | LockoutEnabled deshabilitado + sin rate limiting | 5.0 | Cochera.Web |
| V-013 | AntiForgery deshabilitado en endpoint de login | 4.3 | Cochera.Web |
| V-014 | MQTT reconexión sin backoff exponencial | 3.5 | Cochera.Infrastructure |

### Hallazgos de Código Inseguro

Se identificaron **16 hallazgos** de código inseguro en 9 archivos del sistema.

| Severidad | Cantidad |
|-----------|----------|
| [CRITICA] Crítica | 2 |
| [ALTA] Alta | 6 |
| [MEDIA] Media | 7 |
| [BAJA] Baja | 1 |

### Distribución por Categoría OWASP Top 10:2021

| Categoría | Vulnerabilidades |
|-----------|-----------------|
| A01 — Broken Access Control | V-006, V-009 |
| A02 — Cryptographic Failures | V-005 |
| A03 — Injection | V-003 |
| A04 — Insecure Design | V-012, V-013 |
| A05 — Security Misconfiguration | V-002, V-010 |
| A06 — Vulnerable and Outdated Components | V-011 |
| A09 — Security Logging and Monitoring Failures | V-007 |

### Distribución por OWASP IoT Top 10:2018

| Categoría | Vulnerabilidades |
|-----------|-----------------|
| I1 — Weak/Hardcoded Passwords | V-001 |
| I3 — Insecure Ecosystem Interfaces | V-004, V-005 |
| I5 — Lack of Secure Update Mechanism | V-008 |

---

## Seguridad Actual del Sistema

El sistema cuenta actualmente con los siguientes controles de seguridad implementados:

- **Autenticación**: ASP.NET Core Identity con `IdentityUser`, `IdentityRole`, y `PasswordHasher` (PBKDF2-HMAC-SHA256)
- **Cookie de sesión**: `Cochera.Auth` con HttpOnly, SlidingExpiration (8 horas)
- **Autorización por roles**: `Admin` y `User` con `AuthorizeRouteView` en todas las rutas
- **SignalR parcial**: `[Authorize(Roles = "Admin")]` en `UnirseComoAdmin()`, `[Authorize]` con validación de identidad en `UnirseComoUsuario()`
- **Hashing de contraseñas**: PBKDF2-HMAC-SHA256 automático vía Identity
- **Login fuera de circuito Blazor**: HTTP POST a `/auth/login` para evitar conflictos de cookies en InteractiveServer

---

## Marcos de Referencia Utilizados

- **OWASP Top 10:2021** — Clasificación de riesgos en aplicaciones web
- **OWASP IoT Top 10:2018** — Clasificación de riesgos en dispositivos IoT
- **CWE (Common Weakness Enumeration)** — Identificación de debilidades de software
- **CVSS v3.1 (Common Vulnerability Scoring System)** — Puntuación de severidad
- **OWASP SAMM (Software Assurance Maturity Model)** — Evaluación de madurez

---

## Archivos Analizados

Se realizó revisión de seguridad sobre **17 archivos críticos**:

| Proyecto | Archivo |
|----------|---------|
| Cochera.Web | Program.cs, CocheraHub.cs, UsuarioActualService.cs, Login.razor, MainLayout.razor, Routes.razor, App.razor, appsettings.json |
| Cochera.Infrastructure | CocheraDbContext.cs, MqttConsumerService.cs |
| Cochera.Application | SesionService.cs, EventoSensorService.cs, UsuarioService.cs |
| Cochera.Worker | MqttWorker.cs, SignalRNotificationService.cs |
| Firmware | sketch_jan16a.ino |

---

*Marzo 2026*

