# 02 - Identificación de Amenazas y Vulnerabilidades (OWASP Top 10)

## 2.1 Marco de Referencia

El análisis se fundamenta en el **OWASP Top 10:2021**, el estándar de facto para la identificación de riesgos de seguridad en aplicaciones web, complementado con el **OWASP IoT Top 10:2018** para los componentes de hardware y comunicación embebida.

### Clasificación de Severidad

| Nivel | CVSS v3.1 | Descripción |
|-------|----------|-------------|
| 🔴 **Crítico** | 9.0 - 10.0 | Explotable remotamente, impacto total en confidencialidad, integridad o disponibilidad |
| 🟠 **Alto** | 7.0 - 8.9 | Explotable con poco esfuerzo, impacto significativo |
| 🟡 **Medio** | 4.0 - 6.9 | Requiere condiciones específicas, impacto moderado |
| 🟢 **Bajo** | 0.1 - 3.9 | Difícil de explotar, impacto limitado |

---

## 2.2 Análisis OWASP Top 10:2021

### A01:2021 – Broken Access Control (Control de Acceso Roto) 🔴 CRÍTICO

**Descripción OWASP:** Fallos en la aplicación de restricciones sobre lo que los usuarios autenticados pueden hacer.

#### Hallazgo A01-01: Ausencia Total de Autenticación

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.8) |
| **Ubicación** | `Cochera.Web/Services/UsuarioActualService.cs`, `MainLayout.razor` |
| **CWE** | CWE-306: Missing Authentication for Critical Function |

**Descripción:** El sistema no implementa ningún mecanismo de autenticación. Los usuarios se "identifican" seleccionándose de un menú desplegable (`UserSelector`) que lista todos los usuarios del sistema, incluyendo el administrador.

**Código vulnerable:**
```csharp
// UsuarioActualService.cs - Cualquiera puede "ser" cualquier usuario
public async Task CambiarUsuarioAsync(int usuarioId)
{
    using var scope = _serviceProvider.CreateScope();
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    _usuarioActual = await usuarioService.GetByIdAsync(usuarioId);
    // Sin verificación de credenciales, sin contraseña
    if (_usuarioActual != null)
    {
        await _sessionStorage.SetAsync(StorageKey, _usuarioActual.Id);
    }
}
```

**Impacto:** Cualquier persona que acceda a la URL `http://localhost:5000` puede asumir la identidad de cualquier usuario, incluyendo el administrador, y ejecutar todas las funciones del sistema sin restricción.

**Vector de ataque:**
1. Acceder a `http://localhost:5000`
2. Seleccionar "Administrador" en el dropdown
3. Acceso completo a dashboard, gestión de sesiones, tarifas, reportes

---

#### Hallazgo A01-02: SignalR Hub sin Autorización

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.1) |
| **Ubicación** | `Cochera.Web/Hubs/CocheraHub.cs` |
| **CWE** | CWE-862: Missing Authorization |

**Descripción:** El Hub de SignalR no tiene el atributo `[Authorize]` y los métodos para unirse a grupos no validan la identidad del solicitante.

**Código vulnerable:**
```csharp
// CocheraHub.cs - Sin atributo [Authorize]
public class CocheraHub : Hub
{
    // Cualquier conexión puede unirse al grupo de administradores
    public async Task UnirseComoAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    // Cualquier conexión puede unirse como cualquier usuario
    public async Task UnirseComoUsuario(int usuarioId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
    }
}
```

**Impacto:** Un atacante puede:
- Conectarse al Hub SignalR directamente (JavaScript/Postman)
- Unirse al grupo `admins` sin ser administrador
- Recibir todas las notificaciones de todos los usuarios
- Unirse al grupo `usuario_{id}` de cualquier usuario para espiar sus sesiones y pagos

**Prueba de concepto (JavaScript en consola del navegador):**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/cocherahub")
    .build();
await connection.start();
await connection.invoke("UnirseComoAdmin"); // Ahora recibe todo
await connection.invoke("UnirseComoUsuario", 2); // Espiar usuario 2
```

---

#### Hallazgo A01-03: IDOR (Insecure Direct Object Reference)

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 7.5) |
| **Ubicación** | `Cochera.Application/Services/SesionService.cs` |
| **CWE** | CWE-639: Authorization Bypass Through User-Controlled Key |

**Descripción:** Los servicios permiten acceder a recursos de otros usuarios solo conociendo el ID numérico. Las operaciones de consulta no verifican que el usuario solicitante sea el propietario del recurso.

**Código vulnerable:**
```csharp
// SesionService.cs - No valida que el usuario actual sea el propietario
public async Task<SesionEstacionamientoDto?> GetByIdAsync(int id, ...)
{
    var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(id, cancellationToken);
    return sesion == null ? null : MapToDto(sesion);
    // No verifica: ¿el usuario actual tiene permiso de ver esta sesión?
}
```

**Impacto:** Un usuario puede consultar sesiones, pagos e historial de otros usuarios iterando IDs secuenciales.

---

### A02:2021 – Cryptographic Failures (Fallos Criptográficos) 🔴 CRÍTICO

**Descripción OWASP:** Fallos relacionados con criptografía que exponen datos sensibles.

#### Hallazgo A02-01: Credenciales en Texto Plano en Configuración

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.1) |
| **Ubicación** | `appsettings.json` (Web y Worker), `MqttSettings.cs`, `sketch_jan16a.ino` |
| **CWE** | CWE-312: Cleartext Storage of Sensitive Information |

**Código vulnerable en appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  },
  "Mqtt": {
    "Username": "esp32",
    "Password": "123456"
  }
}
```

**Código vulnerable en MqttSettings.cs (valores por defecto hardcoded):**
```csharp
public class MqttSettings
{
    public string Server { get; set; } = "192.168.100.16";
    public string Username { get; set; } = "esp32";
    public string Password { get; set; } = "123456"; // Hardcoded!
}
```

**Código vulnerable en sketch_jan16a.ino:**
```cpp
const char* ssid = "AVRIL@2014";
const char* password = "Abr11@2014";
const char* mqtt_server = "192.168.100.16";
const char* mqtt_user = "esp32";
const char* mqtt_password = "123456";
```

**Impacto:**
- Credenciales de base de datos (`postgres/postgres`) comprometidas si el repositorio es público
- Credenciales WiFi de la red doméstica expuestas en el código fuente
- Credenciales MQTT permiten a cualquiera publicar mensajes falsos al broker
- Compromiso de cadena completa: repositorio → credenciales → acceso a todos los servicios

---

#### Hallazgo A02-02: Comunicaciones sin Cifrado (MQTT y SignalR)

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 7.4) |
| **Ubicación** | Comunicación MQTT (puerto 1883), SignalR (HTTP puerto 5000) |
| **CWE** | CWE-319: Cleartext Transmission of Sensitive Information |

**Descripción:**
- **MQTT:** El ESP32 se comunica con RabbitMQ por el puerto 1883 (MQTT sin TLS). Todos los mensajes JSON viajan en texto plano por la red WiFi.
- **SignalR:** El Worker se conecta al Hub via `http://localhost:5000/cocherahub` (sin HTTPS). Los datos de sesiones, pagos y eventos se transmiten sin cifrar.
- **PostgreSQL:** La cadena de conexión no especifica `SslMode=Require`.

**Impacto:** Un atacante en la misma red puede interceptar (sniffing):
- Eventos del sensor con estados de la cochera
- Datos de sesiones y montos de pago
- Credenciales MQTT en la negociación de conexión

---

#### Hallazgo A02-03: Sin Cifrado de Datos Sensibles en Base de Datos

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 5.3) |
| **Ubicación** | `Cochera.Domain/Entities/Usuario.cs`, `Pago.cs` |
| **CWE** | CWE-311: Missing Encryption of Sensitive Data |

**Descripción:** Los códigos de usuario y referencias de pago se almacenan en texto plano en la base de datos. No existe cifrado en reposo (at-rest encryption) para datos sensibles.

```csharp
public class Usuario : BaseEntity
{
    public string Codigo { get; set; } = string.Empty; // Texto plano
}
```

---

### A03:2021 – Injection (Inyección) 🟡 MEDIO

**Descripción OWASP:** Datos proporcionados por el usuario que no son validados, filtrados o sanitizados.

#### Hallazgo A03-01: Inyección de Mensajes MQTT

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 8.1) |
| **Ubicación** | `MqttConsumerService.cs`, `EventoSensorService.cs` |
| **CWE** | CWE-20: Improper Input Validation |

**Descripción:** El sistema deserializa mensajes MQTT del ESP32 sin validar su autenticidad ni sanitizar el contenido. Cualquier cliente MQTT que conozca las credenciales (`esp32`/`123456`) puede publicar mensajes arbitrarios en el topic `cola_sensores`.

**Código vulnerable:**
```csharp
// MqttConsumerService.cs - Deserialización sin validación
var mensaje = JsonSerializer.Deserialize<MensajeSensorMqtt>(payload, 
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (mensaje != null && OnMensajeRecibido != null)
{
    await OnMensajeRecibido.Invoke(mensaje, payload);
}
```

```csharp
// EventoSensorService.cs - Se confía en los datos del mensaje sin validar
var evento = new EventoSensor
{
    Detalle = mensaje.detalle,          // Sin sanitización
    JsonOriginal = jsonOriginal,         // Se almacena el JSON crudo
    EstadoCajon1 = mensaje.cajon1,      // Sin validación de valores esperados
    // ...
};
```

**Impacto:** Un atacante puede:
- Publicar eventos falsos que cambien el estado de los cajones
- Inyectar datos maliciosos en el campo `detalle` que se renderizará en la UI
- Manipular contadores de cajones libres/ocupados
- Causar denegación de servicio inundando el topic con mensajes

**Prueba de concepto (usando mosquitto_pub):**
```bash
mosquitto_pub -h 192.168.100.16 -p 1883 -u esp32 -P 123456 \
  -t cola_sensores \
  -m '{"evento":"CAJON_OCUPADO","detalle":"<script>alert(1)</script>","timestamp":"2026-01-01","cajon1":"OCUPADO","cajon2":"OCUPADO","libres":0,"ocupados":2,"lleno":true}'
```

---

#### Hallazgo A03-02: Protección Parcial contra SQL Injection

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟢 Bajo (CVSS: 2.0) |
| **Ubicación** | Repositorios via Entity Framework Core |
| **CWE** | CWE-89: SQL Injection |

**Descripción:** El uso de Entity Framework Core con consultas LINQ parametrizadas proporciona una **protección inherente** contra inyección SQL. Sin embargo, la ausencia de validación de entrada en la capa de aplicación deja la puerta abierta si en el futuro se introdujeran consultas SQL raw.

**Mitigación existente:** EF Core usa consultas parametrizadas automáticamente.

**Riesgo residual:** Si se agregan consultas `FromSqlRaw()` o `ExecuteSqlRaw()` sin parametrizar en el futuro.

---

### A04:2021 – Insecure Design (Diseño Inseguro) 🔴 CRÍTICO

**Descripción OWASP:** Defectos de diseño a nivel de arquitectura que no pueden resolverse con una implementación perfecta.

#### Hallazgo A04-01: Arquitectura sin Security by Design

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.8) |
| **Ubicación** | Diseño general del sistema |
| **CWE** | CWE-657: Violation of Secure Design Principles |

**Descripción:** El sistema fue diseñado sin incorporar seguridad desde el diseño:

1. **Sin modelo de amenazas (Threat Modeling):** No se realizó un análisis STRIDE o similar durante el diseño
2. **Sin principio de menor privilegio:** Todos los servicios se ejecutan con las mismas credenciales de base de datos (`postgres` superusuario)
3. **Sin defensa en profundidad:** Una sola capa de defensa (EF Core) protege contra inyección SQL
4. **Sin separación de obligaciones:** El mismo proceso web maneja la UI, la API SignalR y el acceso a datos
5. **Confianza implícita en la red:** Se asume que la red local es segura

#### Hallazgo A04-02: Modelo de Usuario sin Contraseñas

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.8) |
| **Ubicación** | `Cochera.Domain/Entities/Usuario.cs` |
| **CWE** | CWE-287: Improper Authentication |

**Descripción:** La entidad `Usuario` no tiene campo de contraseña (`Password`, `PasswordHash`). Los usuarios se identifican por un código simple (`admin`, `usuario_1`), sin ningún secreto asociado.

```csharp
public class Usuario : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty; // Único "identificador"
    public bool EsAdmin { get; set; }                   // Rol hardcoded
}
```

---

### A05:2021 – Security Misconfiguration (Configuración de Seguridad Incorrecta) 🟠 ALTO

#### Hallazgo A05-01: AllowedHosts Wildcard

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 5.3) |
| **Ubicación** | `Cochera.Web/appsettings.json` |
| **CWE** | CWE-16: Configuration |

```json
"AllowedHosts": "*"
```

**Impacto:** Permite que la aplicación acepte solicitudes desde cualquier hostname, facilitando ataques de host header injection.

#### Hallazgo A05-02: Credenciales por Defecto de PostgreSQL

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 8.6) |
| **Ubicación** | `appsettings.json` |
| **CWE** | CWE-798: Use of Hard-coded Credentials |

**Descripción:** Se usa el superusuario `postgres` con contraseña `postgres` para la aplicación. Este es el usuario con máximos privilegios en PostgreSQL.

#### Hallazgo A05-03: RabbitMQ con Credenciales por Defecto

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 8.6) |
| **Ubicación** | RabbitMQ Management (puerto 15672) |
| **CWE** | CWE-798: Use of Hard-coded Credentials |

**Descripción:** El panel de administración de RabbitMQ (`guest/guest`) permanece accesible. Las credenciales del usuario MQTT (`esp32/123456`) son triviales.

#### Hallazgo A05-04: Sin Headers de Seguridad HTTP

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 5.3) |
| **Ubicación** | `Cochera.Web/Program.cs` |
| **CWE** | CWE-693: Protection Mechanism Failure |

**Descripción:** La aplicación no configura headers de seguridad HTTP esenciales:

| Header Faltante | Riesgo |
|----------------|--------|
| `Content-Security-Policy` | XSS, inyección de contenido |
| `X-Content-Type-Options` | MIME type sniffing |
| `X-Frame-Options` | Clickjacking |
| `Strict-Transport-Security` | Downgrade a HTTP |
| `X-XSS-Protection` | XSS reflejado |
| `Referrer-Policy` | Fuga de información en Referer |
| `Permissions-Policy` | Acceso a APIs del navegador |

---

### A06:2021 – Vulnerable and Outdated Components 🟡 MEDIO

#### Hallazgo A06-01: Dependencias con Posibles Vulnerabilidades

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 5.0) |
| **CWE** | CWE-1104: Use of Unmaintained Third Party Components |

**Dependencias identificadas:**

| Paquete | Versión Instalada | Última Estable (Mar 2026) | Riesgo |
|---------|-------------------|---------------------------|--------|
| `Microsoft.EntityFrameworkCore` | 8.0.11 | 9.0.x+ | Parches de seguridad pendientes |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | 9.0.x+ | Parches de seguridad pendientes |
| `MQTTnet` | 4.3.3.952 | 5.x+ | Sin soporte activo |
| `RabbitMQ.Client` | 6.8.1 | 7.x+ | API obsoleta |
| `Radzen.Blazor` | 5.5.0 | 6.x+ | Posibles fixes de seguridad |
| `MediatR` | 12.2.0 | 12.x+ | Bajo riesgo |

**Nota:** Se requiere ejecutar `dotnet list package --vulnerable` para verificar vulnerabilidades conocidas (CVEs) en las versiones exactas.

---

### A07:2021 – Identification and Authentication Failures 🔴 CRÍTICO

#### Hallazgo A07-01: Sin Mecanismo de Autenticación

(Cubierto en A01-01, aquí se refuerza desde la perspectiva de autenticación)

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🔴 Crítico (CVSS: 9.8) |
| **CWE** | CWE-306: Missing Authentication for Critical Function |

**Funciones críticas sin autenticación:**
- Iniciar sesiones de estacionamiento (cobro monetario)
- Cerrar sesiones (afecta facturación)
- Modificar tarifas (impacto financiero directo)
- Acceso al dashboard (información sensible)
- Confirmar pagos (afecta registros financieros)

#### Hallazgo A07-02: Sin Gestión de Sesiones Segura

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 7.4) |
| **Ubicación** | `UsuarioActualService.cs` |
| **CWE** | CWE-384: Session Fixation |

**Descripción:** La "sesión" del usuario se almacena en `ProtectedSessionStorage` del navegador, que solo contiene el `Id` del usuario. No hay:
- Token de sesión con expiración
- Rotación de sesión al cambiar de usuario
- Invalidación de sesión del lado del servidor
- Timeout por inactividad

---

### A08:2021 – Software and Data Integrity Failures 🟠 ALTO

#### Hallazgo A08-01: Sin Validación de Integridad de Mensajes MQTT

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 8.1) |
| **Ubicación** | Comunicación ESP32 → RabbitMQ → Worker |
| **CWE** | CWE-345: Insufficient Verification of Data Authenticity |

**Descripción:** Los mensajes MQTT del ESP32 no llevan firma digital ni HMAC. El backend no puede distinguir entre un mensaje legítimo del ESP32 y un mensaje falsificado por un atacante.

**Consecuencia:** Un atacante en la red puede inyectar eventos falsos que alteren el estado de los cajones, generen sesiones fantasma o manipulen el dashboard.

#### Hallazgo A08-02: Sin Verificación de Integridad del Firmware

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 6.5) |
| **Ubicación** | `sketch_jan16a.ino` |
| **CWE** | CWE-494: Download of Code Without Integrity Check |

**Descripción:** El firmware del ESP32 se carga via USB sin verificación criptográfica. No existe mecanismo de Secure Boot ni OTA (Over-The-Air) seguro. Un atacante con acceso físico puede reemplazar el firmware.

---

### A09:2021 – Security Logging and Monitoring Failures 🟠 ALTO

#### Hallazgo A09-01: Logging Insuficiente para Seguridad

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟠 Alto (CVSS: 7.0) |
| **Ubicación** | Todo el sistema |
| **CWE** | CWE-778: Insufficient Logging |

**Descripción:** El sistema tiene logging operacional (eventos del sensor, conexiones), pero no tiene logging de seguridad:

| Evento de seguridad | ¿Se registra? |
|---------------------|---------------|
| Cambio de usuario (suplantación) | ❌ No |
| Intento de acceso a recurso ajeno | ❌ No |
| Conexión SignalR no autorizada | ❌ No |
| Mensaje MQTT malformado o sospechoso | ❌ No (solo error genérico) |
| Modificación de tarifas | ❌ No |
| Creación/cierre de sesiones | ✅ Parcial (como log operacional) |
| Pagos procesados | ❌ No |
| Errores de autenticación | ❌ No aplica (no hay autenticación) |

#### Hallazgo A09-02: Excepciones Silenciadas

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟡 Medio (CVSS: 5.3) |
| **Ubicación** | `UsuarioActualService.cs` |
| **CWE** | CWE-390: Detection of Error Condition Without Action |

**Código vulnerable:**
```csharp
catch
{
    // Ignorar errores de storage (puede fallar en prerendering)
}
```

**Impacto:** Las excepciones silenciadas ocultan posibles ataques o fallos de seguridad. Un atacante que manipule el storage no dejaría rastro.

---

### A10:2021 – Server-Side Request Forgery (SSRF) 🟢 BAJO

| Aspecto | Detalle |
|---------|---------|
| **Severidad** | 🟢 Bajo (CVSS: 2.0) |
| **Análisis** | El sistema no tiene funcionalidades que permitan al usuario especificar URLs o recursos remotos para que el servidor los recupere. El riesgo de SSRF es mínimo en la arquitectura actual. |

---

## 2.3 Análisis OWASP IoT Top 10:2018

### I1: Weak, Guessable, or Hardcoded Passwords 🔴 CRÍTICO

**Hallazgo:** Contraseña WiFi (`Abr11@2014`) y MQTT (`123456`) hardcoded en el firmware. La contraseña MQTT es trivialmente adivinable.

### I2: Insecure Network Services 🟠 ALTO

**Hallazgo:** MQTT sin TLS en puerto 1883. Credenciales MQTT viajan en texto plano en la conexión inicial.

### I3: Insecure Ecosystem Interfaces 🔴 CRÍTICO

**Hallazgo:** La interfaz web no tiene autenticación. El panel de RabbitMQ Management tiene credenciales por defecto.

### I4: Lack of Secure Update Mechanism 🟡 MEDIO

**Hallazgo:** No hay mecanismo de actualización OTA para el ESP32. Las actualizaciones requieren acceso físico al dispositivo via USB.

### I5: Use of Insecure or Outdated Components 🟡 MEDIO

**Hallazgo:** La librería PubSubClient del ESP32 no soporta TLS nativo. MQTTnet 4.3.3 tiene versiones más nuevas disponibles.

### I7: Insecure Data Transfer and Storage 🟠 ALTO

**Hallazgo:** Datos de sensores transmitidos sin cifrado. Credenciales WiFi almacenadas en la memoria flash del ESP32 sin protección.

### I9: Insecure Default Settings 🟠 ALTO

**Hallazgo:** Valores por defecto inseguros en `MqttSettings.cs`:
```csharp
public string Username { get; set; } = "esp32";
public string Password { get; set; } = "123456";
```

---

## 2.4 Matriz Resumen de Vulnerabilidades

| ID | Vulnerabilidad | OWASP | Severidad | CVSS | CWE |
|----|---------------|-------|-----------|------|-----|
| V-001 | Sin autenticación | A01, A07 | 🔴 Crítico | 9.8 | CWE-306 |
| V-002 | SignalR Hub sin autorización | A01 | 🔴 Crítico | 9.1 | CWE-862 |
| V-003 | IDOR en servicios | A01 | 🟠 Alto | 7.5 | CWE-639 |
| V-004 | Credenciales hardcoded | A02 | 🔴 Crítico | 9.1 | CWE-312 |
| V-005 | MQTT sin TLS | A02 | 🟠 Alto | 7.4 | CWE-319 |
| V-006 | Sin cifrado en BD | A02 | 🟡 Medio | 5.3 | CWE-311 |
| V-007 | Inyección MQTT | A03 | 🟠 Alto | 8.1 | CWE-20 |
| V-008 | Diseño sin seguridad | A04 | 🔴 Crítico | 9.8 | CWE-657 |
| V-009 | Modelo sin contraseñas | A04 | 🔴 Crítico | 9.8 | CWE-287 |
| V-010 | AllowedHosts wildcard | A05 | 🟡 Medio | 5.3 | CWE-16 |
| V-011 | Superusuario PostgreSQL | A05 | 🟠 Alto | 8.6 | CWE-798 |
| V-012 | Sin headers seguridad | A05 | 🟡 Medio | 5.3 | CWE-693 |
| V-013 | Dependencias desactualizadas | A06 | 🟡 Medio | 5.0 | CWE-1104 |
| V-014 | Sin gestión de sesiones | A07 | 🟠 Alto | 7.4 | CWE-384 |
| V-015 | Sin integridad MQTT | A08 | 🟠 Alto | 8.1 | CWE-345 |
| V-016 | Sin Secure Boot ESP32 | A08 | 🟡 Medio | 6.5 | CWE-494 |
| V-017 | Logging insuficiente | A09 | 🟠 Alto | 7.0 | CWE-778 |
| V-018 | Excepciones silenciadas | A09 | 🟡 Medio | 5.3 | CWE-390 |

### Distribución por Severidad

| Severidad | Cantidad | Porcentaje |
|-----------|----------|------------|
| 🔴 Crítico | 5 | 27.8% |
| 🟠 Alto | 8 | 44.4% |
| 🟡 Medio | 5 | 27.8% |
| 🟢 Bajo | 0 | 0% |
| **Total** | **18** | **100%** |
