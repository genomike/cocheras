# 03 — Análisis de Código Inseguro

## 3.1 Metodología

Se realizó revisión manual de código sobre 17 archivos críticos del sistema, identificando patrones de código inseguro según **CWE/SANS Top 25**, **OWASP Top 10:2021** y **OWASP IoT Top 10:2018**.

Cada hallazgo incluye: archivo, línea aproximada, fragmento de código, CWE asociado, vulnerabilidad relacionada, y escenario de explotación.

---

## 3.2 Resumen de Hallazgos

Se identificaron **16 hallazgos** de código inseguro:

| Severidad | Cantidad |
|-----------|----------|
| [CRITICA] Crítica | 2 |
| [ALTA] Alta | 6 |
| [MEDIA] Media | 7 |
| [BAJA] Baja | 1 |
| **Total** | **16** |

---

## 3.3 Hallazgos Detallados

---

### H-01 — Credenciales WiFi y MQTT Hardcoded en Firmware

| Campo | Valor |
|-------|-------|
| **Severidad** | [CRITICA] Crítica |
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **Vulnerabilidad** | V-001 |

```cpp
const char* ssid = "AVRIL@2014";
const char* password = "AVRIL@2014";
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
```

**Escenario de explotación:**
Un atacante con acceso físico al ESP32 ejecuta `esptool.py read_flash` para extraer el firmware, luego `strings` para obtener credenciales en texto plano. Con ellas, accede a la red WiFi y al broker MQTT.

---

### H-02 — Cadena de Conexión con Superusuario en Texto Plano

| Campo | Valor |
|-------|-------|
| **Severidad** | [CRITICA] Crítica |
| **Archivo** | `appsettings.json` (Web y Worker) |
| **CWE** | CWE-256: Plaintext Storage of a Password |
| **Vulnerabilidad** | V-002 |

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  }
}
```

**Escenario de explotación:**
Si un atacante obtiene acceso al sistema de archivos (vía directory traversal, backup expuesto, o repositorio público), obtiene acceso de superusuario a PostgreSQL. Puede ejecutar `DROP DATABASE`, `CREATE ROLE`, o incluso `COPY TO PROGRAM '/bin/bash -c ...'` para ejecución remota de comandos.

---

### H-03 — Deserialización MQTT sin Validación de Esquema

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `MqttConsumerService.cs` |
| **CWE** | CWE-20: Improper Input Validation |
| **Vulnerabilidad** | V-003 |

```csharp
var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

var mensaje = JsonSerializer.Deserialize<MensajeSensorMqtt>(payload, 
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (mensaje != null && OnMensajeRecibido != null)
{
    await OnMensajeRecibido.Invoke(mensaje, payload);
}
```

**Escenario de explotación:**
Atacante conectado al broker MQTT publica un JSON con valores fuera de rango:
```json
{"sensorId": 999, "distancia": -99999, "tipo": "entrada", "timestamp": "2099-01-01"}
```
El sistema procesa y almacena datos inconsistentes, corrompiendo el estado de la cochera.

---

### H-04 — Conexión MQTT sin TLS (Texto Plano)

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `MqttConsumerService.cs` |
| **CWE** | CWE-319: Cleartext Transmission of Sensitive Information |
| **Vulnerabilidad** | V-005 |

```csharp
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, _settings.Port)  // 1883 sin TLS
    .WithCredentials(_settings.Username, _settings.Password)
    .Build();
```

**Escenario de explotación:**
```bash
# Captura de tráfico MQTT con Wireshark
wireshark -i eth0 -f "tcp port 1883" -Y "mqtt"
# Filtro para ver credenciales: mqtt.username, mqtt.passwd
# Filtro para ver datos: mqtt.msg
```

---

### H-05 — ESP32 Conexión WiFi/MQTT sin Cifrado

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-319: Cleartext Transmission of Sensitive Information |
| **Vulnerabilidad** | V-005 |

```cpp
WiFi.begin(ssid, password);
client.setServer(mqtt_server, mqtt_port);  // Puerto 1883, sin TLS
client.connect("ESP32_Cochera", mqtt_user, mqtt_pass)
```

**Escenario de explotación:**
Un atacante en la misma red WiFi captura el tráfico MQTT del ESP32, obtiene los datos de sensores y credenciales MQTT.

---

### H-06 — Credenciales MQTT en appsettings.json (Worker)

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `appsettings.json` (Cochera.Worker) |
| **CWE** | CWE-256: Plaintext Storage of a Password |
| **Vulnerabilidad** | V-002 |

```json
{
  "Mqtt": {
    "Server": "192.168.100.16",
    "Port": 1883,
    "Username": "esp32",
    "Password": "123456",
    "Topic": "cola_sensores",
    "ClientId": "CocheraWorker"
  }
}
```

**Escenario de explotación:**
Las credenciales son las mismas del ESP32 (`esp32/123456`). Cualquier persona con acceso al código fuente puede conectarse al broker MQTT y publicar/suscribir mensajes.

---

### H-07 — IDOR en SesionService (sin Filtro por Usuario)

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `SesionService.cs` |
| **CWE** | CWE-639: Authorization Bypass Through User-Controlled Key |
| **Vulnerabilidad** | V-006 |

```csharp
public async Task<SesionEstacionamientoDto?> GetByIdAsync(int id, CancellationToken ct = default)
{
    var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(id, ct);
    return sesion == null ? null : MapToDto(sesion);
}

public async Task<IEnumerable<SesionEstacionamientoDto>> GetAllAsync(CancellationToken ct = default)
{
    var sesiones = await _unitOfWork.Sesiones.GetAllAsync(ct);
    return sesiones.Select(MapToDto);
}
```

**Escenario de explotación:**
Usuario autenticado como `usuario_1` (Id=2) llama a `GetByIdAsync(99)` y ve la sesión de `usuario_3`, incluyendo detalles de pago y tarifa.

---

### H-08 — AllowedHosts Configurado como Wildcard

| Campo | Valor |
|-------|-------|
| **Severidad** | [ALTA] Alta |
| **Archivo** | `appsettings.json` (Cochera.Web) |
| **CWE** | CWE-183: Permissive List of Allowed Inputs |
| **Vulnerabilidad** | V-010 |

```json
{
  "AllowedHosts": "*"
}
```

**Escenario de explotación:**
Permite ataques de host header injection donde un atacante envía un header `Host: evil.com` y la aplicación lo acepta, potencialmente generando URLs maliciosas en respuestas.

---

### H-09 — Hub SignalR sin `[Authorize]` a Nivel de Clase

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `CocheraHub.cs` |
| **CWE** | CWE-862: Missing Authorization |
| **Vulnerabilidad** | V-009 |

```csharp
// [RIESGO]️ Sin [Authorize] a nivel de clase
public class CocheraHub : Hub
{
    // Métodos públicos sin protección:
    public async Task NuevoEvento(EventoSensorDto evento)
    {
        await Clients.All.SendAsync("RecibirEvento", evento);
    }

    public async Task CambioEstado(EstadoCocheraDto estado)
    {
        await Clients.All.SendAsync("RecibirEstado", estado);
        await Clients.All.SendAsync("ActualizarCajones", estado);
    }
}
```

**Escenario de explotación:**
```javascript
// Sin cookie de autenticación
const conn = new signalR.HubConnectionBuilder()
    .withUrl("/cocherahub").build();
await conn.start();
await conn.invoke("CambioEstado", { cajon1Ocupado: true, cajon2Ocupado: true });
// Todos los clientes reciben estado falso
```

---

### H-10 — LockoutEnabled = false en Usuarios Seed

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `CocheraDbContext.cs` |
| **CWE** | CWE-307: Improper Restriction of Excessive Authentication Attempts |
| **Vulnerabilidad** | V-012 |

```csharp
new IdentityUser
{
    Id = "1",
    UserName = "admin",
    NormalizedUserName = "ADMIN",
    LockoutEnabled = false,  // [RIESGO]️ anti-fuerza bruta deshabilitado
    SecurityStamp = Guid.NewGuid().ToString()
}
```

**Escenario de explotación:**
```bash
# Fuerza bruta sin bloqueo
for i in $(seq 1 10000); do
  curl -s -X POST https://localhost/auth/login \
    -d "username=admin&password=pass$i"
done
# Sin rate limiting + sin lockout = intentos ilimitados
```

---

### H-11 — DisableAntiforgery en Endpoint de Login

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `Program.cs` |
| **CWE** | CWE-352: Cross-Site Request Forgery |
| **Vulnerabilidad** | V-013 |

```csharp
app.MapPost("/auth/login", async (...) =>
{
    var result = await signInManager.PasswordSignInAsync(username, password, 
        isPersistent: false, lockoutOnFailure: true);
    // ...
}).DisableAntiforgery();
```

**Escenario de explotación:**
```html
<!-- Sitio malicioso: fuerza al usuario a loguearse como "admin" -->
<form action="https://cochera.example/auth/login" method="POST" style="display:none">
  <input name="username" value="attacker_account">
  <input name="password" value="attacker_password">
  <input name="returnUrl" value="/">
</form>
<script>document.forms[0].submit();</script>
```

---

### H-12 — Reconexión MQTT con Delay Fijo

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `MqttConsumerService.cs` |
| **CWE** | CWE-400: Uncontrolled Resource Consumption |
| **Vulnerabilidad** | V-014 |

```csharp
_mqttClient.DisconnectedAsync += async e =>
{
    _logger.LogWarning("Desconectado del broker MQTT. Intentando reconectar...");
    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);  // Fijo
    
    try
    {
        await _mqttClient.ConnectAsync(options, cancellationToken);
    }
    catch { /* retry loop */ }
};
```

**Mejora recomendada:** Backoff exponencial con jitter (1s, 2s, 4s, 8s... máx 60s + random).

---

### H-13 — Buffer JSON sin Sanitización en ESP32

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `sketch_jan16a.ino` |
| **CWE** | CWE-120: Buffer Copy without Checking Size of Input |
| **Vulnerabilidad** | V-003 |

```cpp
char jsonBuffer[512];  // Buffer fijo de 512 bytes

// Construcción manual de JSON con snprintf
snprintf(jsonBuffer, sizeof(jsonBuffer), 
    "{\"tipo\":\"%s\",\"distancia\":%d, ...}", tipo, distancia);
client.publish(topic_sensores, jsonBuffer);
```

**Nota:** Aunque `snprintf` previene overflow, el buffer fijo de 512 bytes podría truncar mensajes si se agregan más campos en el futuro. No hay validación de que el JSON resultante sea completo.

---

### H-14 — Credenciales de Prueba Visibles en Página de Login

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `Login.razor` |
| **CWE** | CWE-200: Exposure of Sensitive Information |
| **Vulnerabilidad** | V-012 |

**Descripción:**
La página de login muestra las credenciales de prueba (admin/Admin12345, etc.) directamente en la interfaz. Aunque es útil en desarrollo, representa un riesgo en producción.

**Impacto:**
- Cualquier persona que acceda a la URL de login puede ver las credenciales
- Si se despliega en producción sin remover, compromete todas las cuentas

---

### H-15 — Endpoint de Login sin Rate Limiting

| Campo | Valor |
|-------|-------|
| **Severidad** | [MEDIA] Media |
| **Archivo** | `Program.cs` |
| **CWE** | CWE-307: Improper Restriction of Excessive Authentication Attempts |
| **Vulnerabilidad** | V-012 |

```csharp
app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<IdentityUser> signInManager) =>
{
    // [RIESGO]️ Sin middleware de rate limiting (UseRateLimiter)
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    // ...
});
```

**Escenario de explotación:**
Sin rate limiting, un atacante puede enviar miles de solicitudes por segundo al endpoint de login. Combinado con `LockoutEnabled = false` (H-10), tiene intentos ilimitados para fuerza bruta.

---

### H-16 — Logging Genérico sin Estructura de Auditoría

| Campo | Valor |
|-------|-------|
| **Severidad** | [BAJA] Baja |
| **Archivo** | `appsettings.json`, `Program.cs` |
| **CWE** | CWE-778: Insufficient Logging |
| **Vulnerabilidad** | V-007 |

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Lo que falta:**
- No hay Serilog, Seq, o ELK para logging estructurado persistente
- Los logs solo van a consola (se pierden al reiniciar)
- No hay categoría de logs de seguridad separada
- No hay alertas ante patrones de ataque

---

## 3.4 Matriz de Hallazgos por Archivo

| Archivo | Hallazgos | Severidad Máx. |
|---------|-----------|---------------|
| `sketch_jan16a.ino` | H-01, H-05, H-13 | [CRITICA] Crítica |
| `appsettings.json` (Web) | H-02, H-08, H-16 | [CRITICA] Crítica |
| `appsettings.json` (Worker) | H-06 | [ALTA] Alta |
| `MqttConsumerService.cs` | H-03, H-04, H-12 | [ALTA] Alta |
| `SesionService.cs` | H-07 | [ALTA] Alta |
| `CocheraHub.cs` | H-09 | [MEDIA] Media |
| `CocheraDbContext.cs` | H-10 | [MEDIA] Media |
| `Program.cs` | H-11, H-15 | [MEDIA] Media |
| `Login.razor` | H-14 | [MEDIA] Media |

---

## 3.5 Distribución por CWE

| CWE | Descripción | Hallazgos | Cantidad |
|-----|------------|-----------|----------|
| CWE-798 | Hard-coded Credentials | H-01 | 1 |
| CWE-256 | Plaintext Storage of Password | H-02, H-06 | 2 |
| CWE-20 | Improper Input Validation | H-03 | 1 |
| CWE-319 | Cleartext Transmission | H-04, H-05 | 2 |
| CWE-639 | Authorization Bypass (IDOR) | H-07 | 1 |
| CWE-183 | Permissive Allowed Inputs | H-08 | 1 |
| CWE-862 | Missing Authorization | H-09 | 1 |
| CWE-307 | Excessive Auth Attempts | H-10, H-15 | 2 |
| CWE-352 | Cross-Site Request Forgery | H-11 | 1 |
| CWE-400 | Uncontrolled Resource Consumption | H-12 | 1 |
| CWE-120 | Buffer Copy Without Size Check | H-13 | 1 |
| CWE-200 | Information Exposure | H-14 | 1 |
| CWE-778 | Insufficient Logging | H-16 | 1 |

---


