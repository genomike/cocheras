# 06 - Gestión de Vulnerabilidades y Parcheo

## 6.1 Introducción

La gestión de vulnerabilidades es un proceso continuo que abarca la identificación, clasificación, priorización, remediación y verificación de debilidades de seguridad en el sistema. Para un sistema IoT como Cochera Inteligente, esto incluye tanto componentes de software (aplicación .NET, dependencias NuGet) como componentes de hardware/firmware (ESP32) y servicios de infraestructura (PostgreSQL, RabbitMQ).

---

## 6.2 Ciclo de Vida de la Gestión de Vulnerabilidades

```
┌─────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌─────────────┐
│ 1. Descubrir │───▶│ 2. Clasificar│───▶│ 3. Priorizar │───▶│ 4. Remediar  │───▶│ 5. Verificar│
└─────────────┘    └──────────────┘    └──────────────┘    └──────────────┘    └─────────────┘
       ▲                                                                              │
       └──────────────────────────────────────────────────────────────────────────────┘
                                    Proceso Continuo
```

### Fase 1: Descubrimiento
- Escaneos SAST/DAST automatizados (ver documento 05)
- Monitoreo de CVEs en dependencias (NVD, GitHub Advisories)
- Auditorías de código manuales
- Reportes de incidentes internos o externos
- Pruebas de penetración periódicas

### Fase 2: Clasificación
- Asignar identificador único (ej. COCH-VULN-001)
- Determinar tipo (CWE) y categoría OWASP
- Documentar vector de ataque y activo afectado
- Estimar impacto y probabilidad con CVSS v3.1

### Fase 3: Priorización
- Basada en severidad CVSS × exposición × impacto en el negocio
- Utilizar la matriz de riesgo de la sección 6.4

### Fase 4: Remediación
- Desarrollar e implementar el fix
- Code review obligatorio para cambios de seguridad
- Pruebas de regresión

### Fase 5: Verificación
- Re-ejecutar la prueba que detectó la vulnerabilidad
- Validar que no se introdujeron regresiones
- Cerrar el ticket de vulnerabilidad

---

## 6.3 Inventario de Activos y Componentes

### 6.3.1 Componentes de Software

| Componente | Versión actual | Tipo | Fuente de actualizaciones |
|------------|---------------|------|--------------------------|
| .NET Runtime | 8.0 LTS | Framework | https://dotnet.microsoft.com/en-us/download |
| ASP.NET Core | 8.0 | Framework | Microsoft Update |
| EF Core | 8.0.11 | ORM | NuGet |
| Npgsql | 8.0.11 | Driver BD | NuGet / GitHub Advisories |
| MQTTnet | 4.3.3.952 | Cliente MQTT | NuGet / GitHub Advisories |
| RabbitMQ.Client | 6.8.1 | Cliente AMQP | NuGet |
| Radzen.Blazor | 5.5.0 | UI components | NuGet |
| MediatR | 12.2.0 | mediador | NuGet |

### 6.3.2 Firmware

| Componente | Versión | Fuente |
|------------|---------|--------|
| Arduino ESP32 Core | 2.x | Arduino Board Manager |
| WiFi.h | SDK | Bundled with ESP32 Core |
| PubSubClient | 2.x | Arduino Library Manager |
| ArduinoJson | 7.x | Arduino Library Manager |

### 6.3.3 Infraestructura

| Componente | Versión | Fuente de CVE |
|------------|---------|--------------|
| PostgreSQL | 16.x | https://www.postgresql.org/support/security/ |
| RabbitMQ | 3.x / 4.x | https://www.rabbitmq.com/news.html |
| Erlang/OTP | 26.x | https://www.erlang.org/patches |

---

## 6.4 Matriz de Clasificación de Riesgo

### Severidad CVSS v3.1

| Rango CVSS | Severidad | Color |
|-----------|-----------|-------|
| 9.0 - 10.0 | Crítica | 🔴 |
| 7.0 - 8.9 | Alta | 🟠 |
| 4.0 - 6.9 | Media | 🟡 |
| 0.1 - 3.9 | Baja | 🟢 |

### SLAs de Remediación

| Severidad | SLA máximo | Acciones inmediatas |
|-----------|-----------|-------------------|
| **Crítica** | 72 horas | Notificar equipo, evaluar workaround temporal, parche urgente |
| **Alta** | 7 días | Incluir en sprint actual, comunicar stakeholders |
| **Media** | 30 días | Planificar para próximo sprint |
| **Baja** | 90 días | Incluir en backlog priorizado |

### Matriz de Priorización (Impacto × Probabilidad)

|  | Impacto Bajo | Impacto Medio | Impacto Alto | Impacto Crítico |
|--|-------------|---------------|-------------|----------------|
| **Probabilidad Alta** | Media | Alta | Crítica | Crítica |
| **Probabilidad Media** | Baja | Media | Alta | Crítica |
| **Probabilidad Baja** | Info | Baja | Media | Alta |

---

## 6.5 Registro de Vulnerabilidades Conocidas

### Ejemplo de registro para Cochera Inteligente

| ID | Título | CVSS | Estado | SLA | Fecha det. | Fecha res. |
|----|--------|------|--------|-----|-----------|-----------|
| COCH-001 | Sin autenticación en web | 9.8 | Pendiente | 72h | 2025-01-16 | - |
| COCH-002 | Sin autorización en SignalR Hub | 9.1 | Pendiente | 72h | 2025-01-16 | - |
| COCH-003 | Credenciales hardcoded en firmware | 8.6 | Pendiente | 72h | 2025-01-16 | - |
| COCH-004 | MQTT sin TLS | 8.1 | Pendiente | 7 días | 2025-01-16 | - |
| COCH-005 | String de conexión con password en config | 7.5 | Pendiente | 7 días | 2025-01-16 | - |
| COCH-006 | IDOR en sesiones de estacionamiento | 7.2 | Pendiente | 7 días | 2025-01-16 | - |
| COCH-007 | Catch vacíos en UsuarioActualService | 5.3 | Pendiente | 30 días | 2025-01-16 | - |
| COCH-008 | No hay rate limiting | 6.5 | Pendiente | 30 días | 2025-01-16 | - |
| COCH-009 | AllowedHosts configurado como "*" | 5.0 | Pendiente | 30 días | 2025-01-16 | - |

---

## 6.6 Gestión de Dependencias y Parcheo

### 6.6.1 Política de Actualización de Dependencias

| Tipo de actualización | Estrategia | Frecuencia |
|----------------------|-----------|-----------|
| **Parches de seguridad (CVE)** | Actualización inmediata según SLA | Reactivo |
| **Patch versions** (x.x.PATCH) | Actualización automática | Semanal |
| **Minor versions** (x.MINOR.x) | Revisión + testing | Mensual |
| **Major versions** (MAJOR.x.x) | Evaluación de breaking changes + migration plan | Trimestral |

### 6.6.2 Configuración de Dependabot (GitHub)

```yaml
# .github/dependabot.yml
version: 2
updates:
  # NuGet packages
  - package-ecosystem: "nuget"
    directory: "/cochera/src"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "06:00"
      timezone: "America/Argentina/Buenos_Aires"
    open-pull-requests-limit: 10
    labels:
      - "dependencies"
      - "security"
    reviewers:
      - "equipo-seguridad"
    groups:
      microsoft:
        patterns:
          - "Microsoft.*"
          - "System.*"
      entityframework:
        patterns:
          - "Microsoft.EntityFrameworkCore*"
          - "Npgsql*"
    # Ignorar actualizaciones mayores automáticamente
    ignore:
      - dependency-name: "Radzen.Blazor"
        update-types: ["version-update:semver-major"]

  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

### 6.6.3 Proceso de Actualización de Dependencias

```
1. Dependabot/Snyk detecta actualización disponible
        │
        ▼
2. Se crea PR automático con changelog
        │
        ▼
3. Pipeline CI ejecuta:
   ├── Build
   ├── Unit tests
   ├── SAST scan
   └── SCA scan
        │
        ▼
4. ¿Pasan todas las pruebas?
   ├── SÍ → Code review → Merge
   └── NO → Revisar breaking changes → Fix manual
        │
        ▼
5. Deploy a staging → Pruebas de regresión
        │
        ▼
6. Deploy a producción
```

### 6.6.4 Actualizaciones del Firmware ESP32

El firmware IoT presenta desafíos adicionales:

| Desafío | Solución recomendada |
|---------|---------------------|
| No hay OTA actualmente | Implementar actualización OTA vía WiFi |
| Biblioteca PubSubClient sin mantenimiento activo | Evaluar migración a AsyncMqttClient o esp-mqtt nativo |
| No hay versionado del firmware | Implementar esquema semver en código |
| No hay rollback | Implementar partición OTA dual (factory + ota_0 + ota_1) |

**Configuración OTA recomendada:**
```cpp
#include <ArduinoOTA.h>

void setupOTA() {
    ArduinoOTA.setHostname("cochera-esp32");
    ArduinoOTA.setPassword("ota_secure_password");
    ArduinoOTA.setPort(3232);
    
    ArduinoOTA.onStart([]() {
        Serial.println("Inicio de actualización OTA");
    });
    
    ArduinoOTA.onEnd([]() {
        Serial.println("Actualización completada");
    });
    
    ArduinoOTA.onError([](ota_error_t error) {
        Serial.printf("Error OTA [%u]\n", error);
    });
    
    ArduinoOTA.begin();
}
```

---

## 6.7 Monitoreo Continuo de Seguridad

### 6.7.1 Fuentes de Inteligencia de Amenazas

| Fuente | URL | Relevancia |
|--------|-----|-----------|
| NVD (NIST) | https://nvd.nist.gov/ | CVEs de todas las dependencias |
| GitHub Security Advisories | https://github.com/advisories | Paquetes NuGet y npm |
| Microsoft Security Response Center | https://msrc.microsoft.com/ | .NET, ASP.NET Core |
| RabbitMQ CVEs | https://www.cvedetails.com/vendor/12639/Pivotal-Software.html | RabbitMQ |
| PostgreSQL Security | https://www.postgresql.org/support/security/ | Base de datos |
| Espressif Security Advisories | https://www.espressif.com/en/news/Espressif_Security_Advisories | ESP32 |

### 6.7.2 Alertas y Notificaciones

Se recomienda configurar alertas automáticas:

```yaml
# Configuración de GitHub Security Alerts
# Settings → Code security and analysis →
# - Dependabot alerts: ✅ Enabled
# - Dependabot security updates: ✅ Enabled
# - Code scanning: ✅ Enabled (con CodeQL)
# - Secret scanning: ✅ Enabled
```

### 6.7.3 Dashboard de Seguridad

Métricas a seguir en un dashboard centralizado:

| Métrica | Meta | Frecuencia de revisión |
|---------|------|----------------------|
| Vulnerabilidades abiertas por severidad | Críticas: 0, Altas: < 3 | Diaria |
| Edad media de vulnerabilidades abiertas | < 15 días | Semanal |
| % de dependencias actualizadas | > 90% | Semanal |
| Cobertura de escaneo SAST | 100% de repos | Mensual |
| Tiempo medio de remediación | Dentro del SLA | Mensual |
| Número de incidentes de seguridad | 0 | Mensual |
| Score de SonarQube Security | A | Cada build |

---

## 6.8 Plan de Respuesta a Incidentes

### 6.8.1 Clasificación de Incidentes

| Nivel | Descripción | Ejemplo |
|-------|-------------|---------|
| SEV-1 | Acceso no autorizado a datos o control del sistema | Explotación de falta de autenticación |
| SEV-2 | Explotación parcial o denegación de servicio | Inyección MQTT masiva |
| SEV-3 | Intento de ataque detectado sin éxito | Escaneo de puertos |
| SEV-4 | Anomalía menor | Pico inusual de consultas |

### 6.8.2 Procedimiento de Respuesta

```
DETECTAR → CONTENER → ERRADICAR → RECUPERAR → LECCIONES APRENDIDAS
```

**Fase 1: Detección** (0-15 min)
- Identificar el incidente (alerta automática o reporte manual)
- Clasificar severidad
- Notificar al equipo responsable

**Fase 2: Contención** (15-60 min)
- Aislar el sistema afectado si es necesario
- Ej: Detener MqttWorker si hay inyección MQTT
- Ej: Restringir acceso de red al broker
- Preservar evidencia (logs, capturas de tráfico)

**Fase 3: Erradicación** (1-24h según severidad)
- Identificar la causa raíz
- Desarrollar y aplicar fix
- Verificar que el vector de ataque está cerrado

**Fase 4: Recuperación** (post-fix)
- Restaurar servicios a operación normal
- Monitorear de cerca por 24-72 horas
- Verificar integridad de datos

**Fase 5: Lecciones Aprendidas** (1 semana post-incidente)
- Documentar timeline del incidente
- Identificar mejoras en detección/respuesta
- Actualizar runbooks y procedimientos
- Fortalecer las pruebas de seguridad

---

## 6.9 Gestión de Secretos y Credenciales

### 6.9.1 Estado Actual (Problemático)

| Secreto | Ubicación actual | Riesgo |
|---------|-----------------|--------|
| PostgreSQL password ("postgres") | appsettings.json (en repo) | Crítico |
| MQTT credentials ("esp32"/"123456") | MqttSettings.cs, appsettings.json | Crítico |
| WiFi password ("Abr11@2014") | sketch_jan16a.ino (en repo) | Alto |
| RabbitMQ credentials | appsettings.json (en repo) | Alto |

### 6.9.2 Solución: Gestión Centralizada de Secretos

**Opción A: User Secrets de .NET (para desarrollo)**
```bash
# Inicializar
cd src/Cochera.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=Cochera;Username=cochera_app;Password=<SECURE_PASSWORD>"
dotnet user-secrets set "Mqtt:Password" "<SECURE_MQTT_PASSWORD>"
```

**Opción B: Azure Key Vault (para producción)**
```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://cochera-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

**Opción C: HashiCorp Vault (self-hosted)**
```bash
# Almacenar secretos
vault kv put secret/cochera/database \
  connection_string="Host=...;Password=..."

vault kv put secret/cochera/mqtt \
  username="cochera_worker" \
  password="<GENERATED>"
```

### 6.9.3 Rotación de Credenciales

| Secreto | Frecuencia de rotación | Automatizable |
|---------|----------------------|---------------|
| PostgreSQL password | 90 días | Sí (con Vault) |
| MQTT credentials | 90 días | Sí |
| WiFi password | 180 días | No (requiere reflash) |
| API keys (futuras) | 60 días | Sí |

---

## 6.10 Checklist de Verificación Periódica

### Mensual
- [ ] Revisar alertas de Dependabot/Snyk
- [ ] Actualizar dependencias NuGet (patch versions)
- [ ] Ejecutar escaneo SAST completo con SonarQube
- [ ] Revisar logs de acceso a RabbitMQ Management
- [ ] Verificar estado de certificados TLS (cuando se implementen)

### Trimestral
- [ ] Ejecutar prueba DAST completa con OWASP ZAP
- [ ] Revisar y actualizar reglas de Semgrep
- [ ] Auditar usuarios y permisos de PostgreSQL
- [ ] Auditar topics y permisos MQTT
- [ ] Actualizar inventario de componentes (sección 6.3)
- [ ] Revisar y actualizar la matriz de riesgos

### Semestral
- [ ] Prueba de penetración manual
- [ ] Actualización de major versions (.NET, PostgreSQL, RabbitMQ)
- [ ] Revisión de firmware ESP32 y bibliotecas
- [ ] Simulacro de respuesta a incidentes
- [ ] Revisión del plan de gestión de vulnerabilidades
