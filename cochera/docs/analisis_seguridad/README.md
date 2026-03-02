# Análisis de Seguridad — Cochera Inteligente

## Descripción

Este directorio contiene un **análisis exhaustivo de seguridad de software** del sistema Cochera Inteligente, realizado como parte del proyecto académico de la Maestría en Sistemas Embebidos.

El análisis se realizó sobre el código fuente completo del sistema, aplicando marcos de referencia reconocidos internacionalmente: **OWASP Top 10:2021**, **OWASP IoT Top 10:2018**, **CWE/SANS Top 25** y **CVSS v3.1**.

---

## Documentos

| # | Documento | Descripción |
|---|-----------|------------|
| 01 | [Descripción del Sistema](01-descripcion-del-sistema.md) | Arquitectura, componentes, tecnologías, flujos de datos y superficie de ataque del sistema |
| 02 | [Amenazas y Vulnerabilidades (OWASP)](02-amenazas-y-vulnerabilidades-owasp.md) | Identificación de 18 vulnerabilidades mapeadas al OWASP Top 10:2021 y OWASP IoT Top 10:2018, con scoring CVSS v3.1 |
| 03 | [Análisis de Código Inseguro](03-analisis-codigo-inseguro.md) | Análisis detallado de 23 hallazgos de código inseguro con fragmentos exactos, referencias CWE y escenarios de explotación |
| 04 | [Propuesta de Mejoras](04-propuesta-mejoras.md) | 18 propuestas de mejora con código de implementación concreto, priorizadas P0-P3, con estimación de esfuerzo total de 10-14 semanas |
| 05 | [Pruebas de Seguridad (SAST/DAST)](05-pruebas-sast-dast.md) | Guía de herramientas de prueba: SecurityCodeScan, SonarQube, Semgrep, OWASP ZAP, Burp Suite, mosquitto-clients, nmap; con configuraciones y pipeline CI/CD |
| 06 | [Gestión de Vulnerabilidades y Parcheo](06-gestion-vulnerabilidades.md) | Ciclo de vida de gestión de vulnerabilidades, SLAs, configuración de Dependabot/Snyk, plan de respuesta a incidentes y gestión de secretos |
| 07 | [Conclusiones y Reflexiones](07-conclusiones.md) | Resumen ejecutivo, evaluación según OWASP SAMM, reflexiones sobre el proceso y recomendaciones finales |

---

## Resumen de Hallazgos

- **18 vulnerabilidades** mapeadas a OWASP Top 10:2021
- **23 hallazgos** de código inseguro con referencias CWE
- **5 Críticas** | **8 Altas** | **5 Medias**
- **9 de 10** categorías OWASP Top 10:2021 presentan vulnerabilidades
- **11 de 13** archivos analizados contienen hallazgos de seguridad

### Vulnerabilidades más críticas

| CVSS | Vulnerabilidad | Componente |
|------|---------------|-----------|
| 9.8 | Sin autenticación | Cochera.Web |
| 9.1 | Sin autorización en SignalR Hub | Cochera.Web |
| 8.6 | Credenciales hardcoded en firmware | ESP32 |
| 8.1 | MQTT sin cifrado TLS | Infraestructura |
| 7.5 | Credenciales de BD en código fuente | Cochera.Web |

---

## Marcos de Referencia Utilizados

- **OWASP Top 10:2021** — Clasificación de riesgos en aplicaciones web
- **OWASP IoT Top 10:2018** — Clasificación de riesgos en dispositivos IoT
- **CWE (Common Weakness Enumeration)** — Identificación de debilidades de software
- **CVSS v3.1 (Common Vulnerability Scoring System)** — Puntuación de severidad
- **OWASP SAMM (Software Assurance Maturity Model)** — Evaluación de madurez

---

## Archivos Analizados

Se realizó revisión de seguridad sobre los siguientes 13 archivos críticos:

| Proyecto | Archivo |
|----------|---------|
| Cochera.Web | Program.cs, MainLayout.razor, UsuarioActualService.cs, CocheraHub.cs |
| Cochera.Application | SesionService.cs, EventoSensorService.cs, UsuarioService.cs, TarifaService.cs |
| Cochera.Infrastructure | MqttConsumerService.cs, MqttSettings.cs, CocheraDbContext.cs |
| Cochera.Worker | MqttWorker.cs, SignalRNotificationService.cs |
| Firmware | sketch_jan16a.ino |

---

*Fecha del análisis: Enero 2025*
