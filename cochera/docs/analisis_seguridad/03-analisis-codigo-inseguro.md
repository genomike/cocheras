# 03 - Análisis Detallado de Código Inseguro

## 3.1 Metodología de Análisis

Se realizó una revisión manual de código (Code Review) sobre **13 archivos fuente críticos** del sistema, enfocándose en los patrones de seguridad más comunes establecidos por:

- **OWASP Secure Coding Practices** (Checklist v2.0)
- **Microsoft Secure Development Lifecycle (SDL)**
- **CWE/SANS Top 25 Most Dangerous Software Weaknesses**

### Convenciones del documento

Cada hallazgo incluye:
- **Archivo y líneas afectadas** con fragmento de código exacto
- **CWE asociado** (Common Weakness Enumeration)
- **Explicación técnica** del riesgo
- **Escenario de explotación** concreto

---

## 3.2 Categoría: Autenticación y Control de Acceso

### 3.2.1 Suplantación de Identidad sin Restricciones

**Archivo:** `src/Cochera.Web/Services/UsuarioActualService.cs`

```csharp
public async Task CambiarUsuarioAsync(int usuarioId)
{
    using var scope = _serviceProvider.CreateScope();
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    _usuarioActual = await usuarioService.GetByIdAsync(usuarioId);
    
    try
    {
        if (_usuarioActual != null)
        {
            await _sessionStorage.SetAsync(StorageKey, _usuarioActual.Id);
        }
    }
    catch
    {
        // Ignorar errores de storage
    }
    
    OnUsuarioCambiado?.Invoke();
}
```

**CWE-306: Missing Authentication for Critical Function**

**Análisis:** El método `CambiarUsuarioAsync` acepta cualquier `usuarioId` como parámetro y establece la identidad del usuario actual sin:
1. Verificar credenciales (contraseña, token, biometría)
2. Validar que el solicitante tiene permiso para asumir esa identidad
3. Registrar el cambio de identidad como evento de seguridad

**Escenario de explotación:**
Un atacante simplemente manipula la solicitud Blazor para invocar `CambiarUsuarioAsync(1)` y obtiene acceso completo como administrador.

---

### 3.2.2 Modelo de Datos sin Campo de Contraseña

**Archivo:** `src/Cochera.Domain/Entities/Usuario.cs`

```csharp
public class Usuario : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsAdmin { get; set; }
    
    // Relaciones
    public ICollection<SesionEstacionamiento> Sesiones { get; set; } = new List<SesionEstacionamiento>();
}
```

**CWE-287: Improper Authentication**

**Análisis:** La entidad `Usuario` carece fundamentalmente de campos para autenticación:
- No existe `PasswordHash` ni `PasswordSalt`
- No hay `Email` para flujos de recuperación
- No hay `TwoFactorEnabled` ni `LockoutEnd`
- El campo `Codigo` (ej: "admin") funciona como identificador público y como supuesta "credencial"

**Impacto:** Es arquitecturalmente imposible implementar autenticación sin una migración de base de datos que agregue campos de credenciales.

---

### 3.2.3 SignalR Hub sin Protección de Grupos

**Archivo:** `src/Cochera.Web/Hubs/CocheraHub.cs`

```csharp
public class CocheraHub : Hub
{
    // Sin atributo [Authorize]
    
    public async Task UnirseComoAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        _logger.LogInformation("👤 Admin conectado al grupo 'admins': {ConnectionId}", 
            Context.ConnectionId);
    }

    public async Task UnirseComoUsuario(int usuarioId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario_{usuarioId}");
        _logger.LogInformation("👤 Usuario {UsuarioId} conectado al grupo: {ConnectionId}", 
            usuarioId, Context.ConnectionId);
    }
```

**CWE-862: Missing Authorization**

**Análisis:**
1. La clase `CocheraHub` **no tiene** el atributo `[Authorize]`
2. `UnirseComoAdmin()` no verifica si el ConnectionId corresponde a un usuario administrador
3. `UnirseComoUsuario(int)` acepta **cualquier** `usuarioId` — un atacante puede espiar a cualquier usuario
4. Los métodos `NuevoEvento` y `CambioEstado` envían a `Clients.All`, exponiendo datos a todos los conectados

**Escenario de explotación (JavaScript):**
```javascript
// Desde cualquier navegador conectado a la aplicación
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/cocherahub")
    .build();

await connection.start();

// Escalación de privilegios: unirse como admin
await connection.invoke("UnirseComoAdmin");

// Espionaje: unirse como otro usuario
for (let i = 1; i <= 10; i++) {
    await connection.invoke("UnirseComoUsuario", i);
}

// Ahora recibe TODAS las notificaciones de TODOS los usuarios
connection.on("MiNuevaSesion", (sesion) => console.log("Sesión interceptada:", sesion));
connection.on("SolicitudCierreSesion", (sesion) => console.log("Pago interceptado:", sesion));
```

---

### 3.2.4 Verificación de Propiedad Insuficiente en Pagos

**Archivo:** `src/Cochera.Application/Services/SesionService.cs`

```csharp
public async Task<SesionEstacionamientoDto> ConfirmarPagoUsuarioAsync(
    ConfirmacionPagoDto confirmacion, CancellationToken cancellationToken = default)
{
    var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(
        confirmacion.SesionId, cancellationToken)
        ?? throw new InvalidOperationException("Sesión no encontrada");

    if (sesion.UsuarioId != confirmacion.UsuarioId)
        throw new InvalidOperationException("El usuario no coincide con la sesión");
    // ...
}
```

**CWE-639: Authorization Bypass Through User-Controlled Key**

**Análisis:** Aunque existe una verificación `sesion.UsuarioId != confirmacion.UsuarioId`, el `confirmacion.UsuarioId` es proporcionado por el **cliente** y no se valida contra un token de sesión del servidor. Un atacante puede enviar `confirmacion.UsuarioId` con el valor correcto para cualquier sesión.

**Problema raíz:** Sin autenticación del lado del servidor, la verificación `sesion.UsuarioId != confirmacion.UsuarioId` es fácilmente eludible.

---

### 3.2.5 Consultas sin filtro de autorización

**Archivo:** `src/Cochera.Application/Services/SesionService.cs`

```csharp
public async Task<SesionEstacionamientoDto?> GetByIdAsync(
    int id, CancellationToken cancellationToken = default)
{
    var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(id, cancellationToken);
    return sesion == null ? null : MapToDto(sesion);
    // No verifica: ¿el usuario actual puede ver esta sesión?
}

public async Task<IEnumerable<SesionEstacionamientoDto>> GetAllAsync(
    CancellationToken cancellationToken = default)
{
    var sesiones = await _unitOfWork.Sesiones.GetAllAsync(cancellationToken);
    return sesiones.Select(MapToDto);
    // Devuelve TODAS las sesiones sin importar quién pregunta
}
```

**CWE-200: Exposure of Sensitive Information**

**Análisis:** Los métodos `GetByIdAsync` y `GetAllAsync` no aceptan ni validan un parámetro de `usuarioActualId`. Cualquier usuario puede:
- Ver las sesiones de otros usuarios iterando IDs secuenciales
- Acceder al historial completo del sistema
- Ver montos de pagos de otros usuarios

---

## 3.3 Categoría: Gestión de Credenciales

### 3.3.1 Credenciales de Base de Datos en Texto Plano

**Archivo:** `src/Cochera.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  }
}
```

**CWE-798: Use of Hard-coded Credentials** + **CWE-312: Cleartext Storage of Sensitive Information**

**Análisis:**
1. Se usa la cuenta `postgres` (superusuario con máximos privilegios) en vez de un usuario dedicado con permisos restringidos
2. La contraseña `postgres` es la contraseña por defecto, incluida en diccionarios de ataque
3. El archivo `appsettings.json` se versiona en Git, exponiendo las credenciales en todo el historial del repositorio
4. No se usa `SslMode=Require` ni `Trust Server Certificate` correctamente

**Escenario de impacto:** Si el repositorio es público (o un desarrollador lo comparte), un atacante puede:
- Conectarse a PostgreSQL si el puerto 5432 está expuesto
- Leer/modificar/eliminar toda la base de datos
- Ejecutar comandos del sistema operativo via `COPY` o extensiones

---

### 3.3.2 Credenciales MQTT con Valores por Defecto

**Archivo:** `src/Cochera.Infrastructure/Mqtt/MqttSettings.cs`

```csharp
public class MqttSettings
{
    public string Server { get; set; } = "192.168.100.16";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "esp32";
    public string Password { get; set; } = "123456";
    public string Topic { get; set; } = "cola_sensores";
    public string ClientId { get; set; } = "CocheraWorker";
}
```

**CWE-1188: Initialization with an Insecure Default**

**Análisis:** Los valores por defecto en la clase de configuración actúan como **fallback** si `appsettings.json` no define los valores. Esto significa que incluso si se externalizan las credenciales, si la sección de configuración falta, el sistema se conectará con credenciales inseguras sin advertencia.

---

### 3.3.3 Credenciales de Red WiFi en Firmware

**Archivo:** `sketch_jan16a.ino`

```cpp
const char* ssid = "AVRIL@2014";       
const char* password = "AVRIL@2014";   
const char* mqtt_server = "192.168.100.16"; 
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
```

**CWE-798: Use of Hard-coded Credentials**

**Análisis:**
1. Las credenciales WiFi de la red doméstica están en el código fuente
2. Estas credenciales se compilan en el binario del ESP32 y pueden extraerse con herramientas como `esptool.py`
3. La contraseña MQTT `123456` es la #1 en la lista de contraseñas más comunes
4. La dirección IP del servidor MQTT revela la topología de la red

**Extracción del firmware:**
```bash
# Extraer credenciales del firmware compilado
esptool.py --port COM3 read_flash 0 0x400000 flash_dump.bin
strings flash_dump.bin | grep -E "AVRIL|esp32|123456"
```

---

## 3.4 Categoría: Comunicaciones Inseguras

### 3.4.1 Conexión MQTT sin TLS

**Archivo:** `src/Cochera.Infrastructure/Mqtt/MqttConsumerService.cs`

```csharp
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(_settings.Server, _settings.Port)
    .WithCredentials(_settings.Username, _settings.Password)
    .WithClientId(_settings.ClientId)
    .WithCleanSession(false)
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
    .Build();
    // FALTA: .WithTlsOptions(...)
```

**CWE-319: Cleartext Transmission of Sensitive Information**

**Análisis:** La conexión MQTT usa puerto 1883 (no cifrado) en vez de 8883 (MQTT sobre TLS). Los datos transmitidos sin cifrar incluyen:
1. Credenciales MQTT en la fase de `CONNECT` (usuario y contraseña en texto plano)
2. Mensajes JSON con estados de cajones, eventos del sensor
3. Metadatos como Client ID y Topic

**Herramienta de captura (Wireshark):**
```
mqtt.topic == "cola_sensores"
```
Permite capturar todos los mensajes del sistema en la red local.

---

### 3.4.2 Conexión SignalR sin HTTPS

**Archivo:** `src/Cochera.Worker/SignalRNotificationService.cs`

```csharp
_hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5000/cocherahub";

_hubConnection = new HubConnectionBuilder()
    .WithUrl(_hubUrl)
    // FALTA: autenticación
    .WithAutomaticReconnect(new[] { 
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 
        TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30) })
    .Build();
```

**CWE-319: Cleartext Transmission of Sensitive Information**

**Análisis:**
1. La URL usa `http://` en vez de `https://`
2. No se envía ningún token de autenticación en la conexión
3. Los datos de sesiones, pagos y eventos viajan sin cifrar entre Worker y Web
4. Un man-in-the-middle puede interceptar y modificar los mensajes

---

### 3.4.3 Conexión a Base de Datos sin SSL

**Archivo:** `src/Cochera.Web/appsettings.json`

```json
"DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
```

**Análisis:** La cadena de conexión no incluye `SslMode=Require` ni `SslMode=VerifyFull`. Si la base de datos se mueve a un servidor remoto, la comunicación será en texto plano.

---

## 3.5 Categoría: Validación de Entrada y Datos

### 3.5.1 Deserialización de MQTT sin Validación de Esquema

**Archivo:** `src/Cochera.Infrastructure/Mqtt/MqttConsumerService.cs`

```csharp
_mqttClient.ApplicationMessageReceivedAsync += async e =>
{
    try
    {
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        _logger.LogInformation("Mensaje recibido en topic {Topic}: {Payload}", 
            e.ApplicationMessage.Topic, payload);

        var mensaje = JsonSerializer.Deserialize<MensajeSensorMqtt>(payload, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (mensaje != null && OnMensajeRecibido != null)
        {
            await OnMensajeRecibido.Invoke(mensaje, payload);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al procesar mensaje MQTT");
        e.IsHandled = false;
    }
};
```

**CWE-20: Improper Input Validation** + **CWE-502: Deserialization of Untrusted Data**

**Análisis:**
1. `JsonSerializer.Deserialize<MensajeSensorMqtt>` acepta cualquier JSON sin validar que tenga los campos esperados
2. Un JSON parcialmente válido creará un objeto `MensajeSensorMqtt` con valores nulos o por defecto
3. No se validan rangos de valores (ej: `libres` podría ser -1 o 999)
4. El payload se registra en el log completo (`{Payload}`), potencialmente incluyendo datos maliciosos

**JSON malicioso que pasaría la deserialización:**
```json
{
    "evento": "CAJON_OCUPADO",
    "detalle": "<img src=x onerror=alert('XSS')>",
    "timestamp": "AAAA",
    "cajon1": "VALOR_INVALIDO",
    "cajon2": "OCUPADO",
    "libres": -999,
    "ocupados": 999,
    "lleno": true
}
```

---

### 3.5.2 Almacenamiento de JSON sin Sanitización

**Archivo:** `src/Cochera.Application/Services/EventoSensorService.cs`

```csharp
var evento = new EventoSensor
{
    TipoEvento = tipoEvento,
    EventoOriginal = mensaje.evento,
    Detalle = mensaje.detalle,           // Sin sanitización
    TimestampESP32 = mensaje.timestamp,
    EstadoCajon1 = mensaje.cajon1,
    EstadoCajon2 = mensaje.cajon2,
    CajonesLibres = mensaje.libres,
    CajonesOcupados = mensaje.ocupados,
    CocheraLlena = mensaje.lleno,
    JsonOriginal = jsonOriginal           // JSON crudo almacenado
};

await _unitOfWork.Eventos.AddAsync(evento, cancellationToken);
```

**CWE-79: Cross-site Scripting (XSS) - Stored**

**Análisis:**
1. El campo `Detalle` se almacena directamente del mensaje MQTT sin sanitización
2. Si la interfaz Blazor renderiza este campo con `@((MarkupString)evento.Detalle)`, ejecutará código JavaScript
3. El campo `JsonOriginal` almacena el JSON completo sin filtrar
4. Aunque Blazor por defecto escapa HTML, componentes como Radzen podrían renderizar HTML crudo

---

### 3.5.3 Valor por Defecto en Mapeo de Eventos

**Archivo:** `src/Cochera.Application/Services/EventoSensorService.cs`

```csharp
private static TipoEvento MapearTipoEvento(string evento)
{
    return evento.ToUpperInvariant() switch
    {
        "SISTEMA_INICIADO" => TipoEvento.SistemaIniciado,
        "MOVIMIENTO_ENTRADA" => TipoEvento.MovimientoEntrada,
        // ... otros mapeos
        _ => TipoEvento.SistemaIniciado  // Valor por defecto silencioso
    };
}
```

**CWE-394: Unexpected Status Code or Return Value**

**Análisis:** Cualquier tipo de evento desconocido se mapea silenciosamente a `SistemaIniciado`. Esto significa que un atacante puede enviar eventos con tipos arbitrarios y el sistema los procesará como si fueran eventos legítimos de sistema, potencialmente corrompiendo datos y estadísticas.

---

### 3.5.4 Falta de Validación en Tarifa

**Archivo:** `src/Cochera.Application/Services/TarifaService.cs`

```csharp
public async Task<TarifaDto> ActualizarTarifaAsync(
    ActualizarTarifaRequest request, CancellationToken cancellationToken = default)
{
    // No valida que request.NuevoPrecioPorMinuto > 0
    // No valida límites máximos
    // No requiere autorización de admin
    
    var nuevaTarifa = new Tarifa
    {
        PrecioPorMinuto = request.NuevoPrecioPorMinuto,
        // ...
    };
}
```

**CWE-20: Improper Input Validation**

**Análisis:**
1. No se valida que el precio sea positivo (podría ser 0 o negativo)
2. No se establece un límite máximo razonable
3. Cualquier usuario (no solo admin) puede invocar este método
4. No hay registro de auditoría del cambio de tarifa

---

## 3.6 Categoría: Manejo de Errores y Logging

### 3.6.1 Excepciones Silenciadas Sistemáticamente

**Archivo:** `src/Cochera.Web/Services/UsuarioActualService.cs`

```csharp
public async Task InitializeAsync()
{
    // ...
    try
    {
        var result = await _sessionStorage.GetAsync<int>(StorageKey);
        // ...
    }
    catch
    {
        // Ignorar errores de storage (puede fallar en prerendering)
    }
    // ...
}

public async Task CambiarUsuarioAsync(int usuarioId)
{
    // ...
    try
    {
        if (_usuarioActual != null)
            await _sessionStorage.SetAsync(StorageKey, _usuarioActual.Id);
    }
    catch
    {
        // Ignorar errores de storage
    }
    // ...
}

public async Task CerrarSesionAsync()
{
    // ...
    try
    {
        await _sessionStorage.DeleteAsync(StorageKey);
    }
    catch
    {
        // Ignorar errores de storage
    }
    // ...
}
```

**CWE-390: Detection of Error Condition Without Action**

**Análisis:** Se identifican **3 bloques catch vacíos** en un solo archivo. Estos bloques:
1. Ocultan errores de seguridad potenciales (manipulación de storage)
2. Impiden la detección de estados inconsistentes
3. Hacen imposible diagnosticar problemas en producción
4. Un atacante que manipule `ProtectedSessionStorage` no dejaría rastro

---

### 3.6.2 Information Disclosure en Logs

**Archivo:** `src/Cochera.Web/Hubs/CocheraHub.cs`

```csharp
public override async Task OnConnectedAsync()
{
    _logger.LogInformation("🔌 Cliente conectado: {ConnectionId}", Context.ConnectionId);
    await Clients.Caller.SendAsync("Connected");
    await base.OnConnectedAsync();
}

public override async Task OnDisconnectedAsync(Exception? exception)
{
    _logger.LogInformation("🔌 Cliente desconectado: {ConnectionId} - Error: {Error}", 
        Context.ConnectionId, exception?.Message ?? "Ninguno");
}
```

**CWE-532: Insertion of Sensitive Information into Log File**

**Análisis:**
1. Se registran ConnectionIds que podrían usarse para ataques de session hijacking
2. Los mensajes de excepción en `OnDisconnectedAsync` pueden contener stack traces con información interna
3. Los logs no están protegidos contra acceso no autorizado

---

### 3.6.3 Logging de Payload Completo del MQTT

**Archivo:** `src/Cochera.Infrastructure/Mqtt/MqttConsumerService.cs`

```csharp
var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
_logger.LogInformation("Mensaje recibido en topic {Topic}: {Payload}", 
    e.ApplicationMessage.Topic, payload);
```

**CWE-117: Improper Output Neutralization for Logs**

**Análisis:** El payload MQTT completo se registra sin sanitización. Un atacante puede inyectar caracteres de control o secuencias ANSI en el log:
```json
{"evento":"SISTEMA_INICIADO\nERROR: Database corruption detected\n2025-01-20 CRITICAL:","detalle":"..."}
```
Esto crea **log injection**, potencialmente confundiendo a los operadores.

---

## 3.7 Categoría: Configuración de Seguridad

### 3.7.1 Hosts Permitidos sin Restricción

**Archivo:** `src/Cochera.Web/appsettings.json`

```json
{
  "AllowedHosts": "*"
}
```

**CWE-16: Configuration**

**Análisis:** Permite que la aplicación responda a solicitudes con cualquier header `Host`, habilitando ataques de **Host Header Injection** que pueden usarse para:
- Envenenamiento de caché (Cache Poisoning)
- Redirección a dominios maliciosos
- Bypass de controles basados en hostname

---

### 3.7.2 Sin Middleware de Seguridad HTTP

**Archivo:** `src/Cochera.Web/Program.cs`

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// AUSENTE: app.UseAuthentication();
// AUSENTE: app.UseAuthorization();
// AUSENTE: Security Headers Middleware
// AUSENTE: CORS Policy
// AUSENTE: Rate Limiting
```

**CWE-693: Protection Mechanism Failure**

**Análisis:** El pipeline HTTP carece de:

| Middleware | Estado | Riesgo |
|-----------|--------|--------|
| `UseAuthentication()` | ❌ Ausente | Sin autenticación |
| `UseAuthorization()` | ❌ Ausente | Sin autorización |
| `UseCors()` | ❌ Ausente | Ataques CORS |
| `UseRateLimiter()` | ❌ Ausente | DoS, fuerza bruta |
| Security Headers | ❌ Ausente | XSS, clickjacking |
| `UseResponseCaching()` | ❌ Ausente | Datos sensibles en caché |

---

### 3.7.3 Migración Automática en Startup

**Archivo:** `src/Cochera.Web/Program.cs`

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CocheraDbContext>();
    context.Database.Migrate();
}
```

**CWE-1188: Initialization with an Insecure Default**

**Análisis:**
1. Las migraciones se ejecutan automáticamente al iniciar la aplicación
2. Esto requiere que la aplicación tenga permisos DDL en la base de datos
3. Un error en una migración puede dejar la BD en estado inconsistente
4. En producción, las migraciones deben ejecutarse como parte de un pipeline CI/CD controlado

---

## 3.8 Categoría: Seguridad IoT/Firmware

### 3.8.1 Sin Protección de Memoria Flash

**Archivo:** `sketch_jan16a.ino`

```cpp
const char* ssid = "AVRIL@2014";
const char* password = "AVRIL@2014";
const char* mqtt_server = "192.168.100.16";
const char* mqtt_user = "esp32";
const char* mqtt_pass = "123456";
```

**CWE-921: Storage of Sensitive Data in a Mechanism without Access Control**

**Análisis del ESP32:**
1. Las credenciales se compilan como strings constantes en la sección `.rodata` del binario
2. Sin **Secure Boot** habilitado, el firmware puede ser extraído y analizado
3. Sin **Flash Encryption**, las credenciales son legibles directamente desde la flash
4. La función `setup_wifi()` no implementa timeout ni manejo de errores de autenticación

### 3.8.2 Sin Validación de Integridad en Comunicación

**Archivo:** `sketch_jan16a.ino`

```cpp
void publicarMensaje(const char* evento, const char* detalle) {
    if (!client.connected()) return;
    
    snprintf(jsonBuffer, sizeof(jsonBuffer),
        "{\"evento\":\"%s\",\"detalle\":\"%s\",...}",
        evento, detalle, ...);
    
    client.publish(topic_sensores, jsonBuffer);
    // Sin firma HMAC
    // Sin nonce para prevenir replay attacks
    // Sin cifrado del payload
}
```

**CWE-345: Insufficient Verification of Data Authenticity**

**Análisis:**
1. Los mensajes no llevan firma digital (HMAC-SHA256) que permita al backend verificar su autenticidad
2. No hay **nonce** ni **timestamp verificado** contra replay attacks
3. Un atacante puede capturar un mensaje legítimo y reenviarlo indefinidamente
4. La función `snprintf` previene buffer overflow pero no protege contra inyección en campos de texto

---

## 3.9 Categoría: Datos Semilla Inseguros

### 3.9.1 Códigos de Usuario Predecibles

**Archivo:** `src/Cochera.Infrastructure/Data/CocheraDbContext.cs`

```csharp
modelBuilder.Entity<Usuario>().HasData(
    new Usuario { Id = 1, Nombre = "Administrador", Codigo = "admin",
                  EsAdmin = true, FechaCreacion = seedDate },
    new Usuario { Id = 2, Nombre = "Usuario 1", Codigo = "usuario_1",
                  EsAdmin = false, FechaCreacion = seedDate },
    new Usuario { Id = 3, Nombre = "Usuario 2", Codigo = "usuario_2",
                  EsAdmin = false, FechaCreacion = seedDate },
    new Usuario { Id = 4, Nombre = "Usuario 3", Codigo = "usuario_3",
                  EsAdmin = false, FechaCreacion = seedDate }
);
```

**CWE-798: Use of Hard-coded Credentials**

**Análisis:**
1. Los códigos son trivialmente predecibles: "admin", "usuario_1", "usuario_2", etc.
2. El ID del administrador es 1 (primer número a probar en una enumeración)
3. Los IDs son secuenciales, facilitando IDOR
4. No se indica que estos datos son solo para desarrollo

---

## 3.10 Resumen de Hallazgos de Código

| # | Hallazgo | Archivo Principal | CWE | Severidad |
|---|----------|-------------------|-----|-----------|
| 1 | Suplantación sin restricciones | UsuarioActualService.cs | 306 | 🔴 Crítico |
| 2 | Modelo sin contraseña | Usuario.cs | 287 | 🔴 Crítico |
| 3 | SignalR Hub sin autorización | CocheraHub.cs | 862 | 🔴 Crítico |
| 4 | IDOR en verificación de pago | SesionService.cs | 639 | 🟠 Alto |
| 5 | Consultas sin filtro de acceso | SesionService.cs | 200 | 🟠 Alto |
| 6 | Credenciales BD en texto plano | appsettings.json | 798 | 🔴 Crítico |
| 7 | Credenciales MQTT hardcoded | MqttSettings.cs | 1188 | 🟠 Alto |
| 8 | Credenciales WiFi en firmware | sketch_jan16a.ino | 798 | 🟠 Alto |
| 9 | MQTT sin TLS | MqttConsumerService.cs | 319 | 🟠 Alto |
| 10 | SignalR sin HTTPS | SignalRNotificationService.cs | 319 | 🟠 Alto |
| 11 | JSON MQTT sin validación | MqttConsumerService.cs | 502 | 🟠 Alto |
| 12 | Datos sin sanitización | EventoSensorService.cs | 79 | 🟡 Medio |
| 13 | Mapeo silencioso a default | EventoSensorService.cs | 394 | 🟡 Medio |
| 14 | Tarifa sin validación | TarifaService.cs | 20 | 🟡 Medio |
| 15 | 3 catch vacíos | UsuarioActualService.cs | 390 | 🟡 Medio |
| 16 | Information disclosure en logs | CocheraHub.cs | 532 | 🟢 Bajo |
| 17 | Log injection | MqttConsumerService.cs | 117 | 🟡 Medio |
| 18 | AllowedHosts wildcard | appsettings.json | 16 | 🟡 Medio |
| 19 | Sin middleware de seguridad | Program.cs | 693 | 🟠 Alto |
| 20 | Migración automática | Program.cs | 1188 | 🟡 Medio |
| 21 | Flash sin protección | sketch_jan16a.ino | 921 | 🟡 Medio |
| 22 | Sin HMAC en MQTT | sketch_jan16a.ino | 345 | 🟠 Alto |
| 23 | Datos semilla predecibles | CocheraDbContext.cs | 798 | 🟡 Medio |

### Estadísticas

- **Total de hallazgos:** 23
- **Archivos afectados:** 11 de 13 analizados (84.6%)
- **Distribución por severidad:**
  - 🔴 Crítico: 4 (17.4%)
  - 🟠 Alto: 10 (43.5%)
  - 🟡 Medio: 8 (34.8%)
  - 🟢 Bajo: 1 (4.3%)
