# Guía de Instalación y Configuración - Cochera Inteligente

## Requisitos Previos

| Software | Versión | Descarga |
|----------|---------|----------|
| Git | 2.40+ | https://git-scm.com/downloads |
| .NET 8 SDK | 8.0.x | https://dotnet.microsoft.com/download/dotnet/8.0 |
| PostgreSQL | 16.x | https://www.postgresql.org/download/ |
| RabbitMQ | 3.13+ | https://www.rabbitmq.com/download.html |
| Erlang/OTP | 26+ | https://www.erlang.org/downloads (requerido por RabbitMQ) |
| Arduino IDE | 2.x | https://www.arduino.cc/en/software |
| Visual Studio / VS Code | 2022+ / Latest | Para desarrollo .NET |

---

## Paso 1: Instalar Git

### Windows

1. Descargar el instalador desde https://git-scm.com/downloads/win
2. Ejecutar el instalador con las opciones por defecto
3. Verificar la instalación:
   ```powershell
   git --version
   ```
   Debe mostrar algo como: `git version 2.43.0.windows.1`

---

## Paso 2: Clonar el Repositorio

```powershell
# Navegar a la carpeta de proyectos
cd C:\Proyectos

# Clonar el repositorio (ajustar la URL según tu repositorio)
git clone <URL_DEL_REPOSITORIO> cochera-inteligente

# Entrar al directorio
cd cochera-inteligente
```

---

## Paso 3: Instalar .NET 8 SDK

### Windows

1. Descargar el SDK desde https://dotnet.microsoft.com/download/dotnet/8.0
   - Seleccionar **SDK** (no Runtime) para la plataforma Windows x64
2. Ejecutar el instalador
3. Verificar la instalación:
   ```powershell
   dotnet --version
   ```
   Debe mostrar: `8.0.xxx`

4. Verificar que el SDK 8.0 está disponible:
   ```powershell
   dotnet --list-sdks
   ```

---

## Paso 4: Instalar PostgreSQL 16

### Windows

1. Descargar desde https://www.postgresql.org/download/windows/
2. Ejecutar el instalador de **EDB** (EnterpriseDB)
3. Durante la instalación:
   - **Puerto:** `5432` (por defecto)
   - **Contraseña de superusuario (postgres):** `postgres`
   - **Locale:** Default
   - **Stack Builder:** Opcional (puedes omitirlo)

4. Verificar la instalación:
   ```powershell
   # Agregar PostgreSQL al PATH si no está
   # Generalmente en: C:\Program Files\PostgreSQL\16\bin
   
   psql --version
   ```

5. **Crear la base de datos:**
   ```powershell
   # Conectar como superusuario
   psql -U postgres -h localhost -p 5432
   ```
   ```sql
   -- En la consola de PostgreSQL:
   CREATE DATABASE "Cochera";
   
   -- Verificar que se creó
   \l
   
   -- Salir
   \q
   ```

   > **Nota:** La base de datos se llama `Cochera` (con C mayúscula). Las migraciones de Entity Framework crearán automáticamente las tablas y datos semilla al ejecutar la aplicación.

### Alternativa: pgAdmin 4

Si prefieres una interfaz gráfica, pgAdmin 4 se instala junto con PostgreSQL:
1. Abrir pgAdmin 4
2. Conectar al servidor local (localhost:5432, usuario: postgres)
3. Click derecho en "Databases" → Create → Database
4. Nombre: `Cochera`
5. Click en "Save"

---

## Paso 5: Instalar y Configurar RabbitMQ

### 5.1 Instalar Erlang (prerrequisito)

1. Descargar desde https://www.erlang.org/downloads
2. Instalar con opciones por defecto
3. Verificar:
   ```powershell
   erl -version
   ```

### 5.2 Instalar RabbitMQ

1. Descargar desde https://www.rabbitmq.com/install-windows.html
2. Ejecutar el instalador
3. RabbitMQ se instalará como **servicio de Windows** y se iniciará automáticamente

4. Verificar que el servicio está corriendo:
   ```powershell
   # En PowerShell como Administrador
   Get-Service RabbitMQ
   ```
   Estado debe ser: `Running`

### 5.3 Habilitar el Plugin de Management (Panel Web)

```powershell
# En PowerShell como Administrador
# Navegar al directorio de RabbitMQ sbin
cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.x\sbin"

# Habilitar el plugin de gestión web
.\rabbitmq-plugins.bat enable rabbitmq_management
```

Acceder al panel web: http://localhost:15672  
Credenciales por defecto: `guest` / `guest`

### 5.4 Habilitar el Plugin MQTT

**Este paso es CRÍTICO.** El ESP32 se comunica vía MQTT, y RabbitMQ actúa como broker.

```powershell
# En PowerShell como Administrador
cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.x\sbin"

# Habilitar el plugin MQTT
.\rabbitmq-plugins.bat enable rabbitmq_mqtt

# Verificar plugins activos
.\rabbitmq-plugins.bat list -e
```

Debe aparecer en la lista:
- `rabbitmq_mqtt` (puerto 1883)
- `rabbitmq_management` (puerto 15672)

### 5.5 Crear Usuario para ESP32

```powershell
cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.x\sbin"

# Crear usuario
.\rabbitmqctl.bat add_user esp32 123456

# Asignar permisos completos al vhost por defecto
.\rabbitmqctl.bat set_permissions -p / esp32 ".*" ".*" ".*"

# Asignar tag de administrador (opcional, para acceder al management UI)
.\rabbitmqctl.bat set_user_tags esp32 management
```

### 5.6 Verificar la Configuración de Colas

Cuando el sistema esté funcionando, la cola MQTT se creará automáticamente. Para verificar manualmente:

1. Acceder a http://localhost:15672 (guest/guest)
2. Ir a la pestaña **Queues**
3. Debe existir la cola asociada al topic `cola_sensores`
   - En MQTT sobre RabbitMQ, los topics se mapean al exchange `amq.topic`
   - La cola se crea al suscribirse el consumidor .NET

### 5.7 Puertos Utilizados por RabbitMQ

| Puerto | Protocolo | Uso |
|--------|-----------|-----|
| 5672 | AMQP | Comunicación .NET ↔ RabbitMQ |
| 1883 | MQTT | Comunicación ESP32 → RabbitMQ |
| 15672 | HTTP | Panel de administración web |

---

## Paso 6: Configurar la Aplicación .NET

### 6.1 Configurar Cadena de Conexión

Editar `cochera/src/Cochera.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  },
  "MqttSettings": {
    "Server": "192.168.100.16",
    "Port": 1883,
    "Username": "esp32",
    "Password": "123456",
    "Topic": "cola_sensores",
    "ClientId": "cochera-web-client"
  }
}
```

Editar también `cochera/src/Cochera.Worker/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  },
  "MqttSettings": {
    "Server": "192.168.100.16",
    "Port": 1883,
    "Username": "esp32",
    "Password": "123456",
    "Topic": "cola_sensores",
    "ClientId": "cochera-worker-client"
  },
  "SignalR": {
    "HubUrl": "http://localhost:5000/cocherahub"
  }
}
```

> **IMPORTANTE:** Ajustar la IP de `MqttSettings.Server` a la IP de tu máquina donde corre RabbitMQ. Si todo está en la misma máquina, usar `localhost` o `127.0.0.1`.

### 6.2 Restaurar Paquetes NuGet

```powershell
cd cochera/src

# Restaurar todos los paquetes de la solución
dotnet restore Cochera.sln
```

### 6.3 Compilar la Solución

```powershell
dotnet build Cochera.sln
```

Debe compilar sin errores. Si hay errores de paquetes, ejecutar `dotnet restore` nuevamente.

---

## Paso 7: Ejecutar las Migraciones de Base de Datos

Las migraciones se ejecutan **automáticamente** al iniciar `Cochera.Web`. Sin embargo, si quieres ejecutarlas manualmente:

```powershell
cd cochera/src

# Instalar la herramienta de EF Core (si no la tienes)
dotnet tool install --global dotnet-ef

# Ejecutar las migraciones
dotnet ef database update --project Cochera.Infrastructure --startup-project Cochera.Web
```

Esto creará:
- Todas las tablas del modelo
- **Datos semilla:**
  - 4 usuarios (1 admin + 3 regulares)
  - 2 cajones (Libre)
  - 1 tarifa ($8.00/min, activa)
  - 1 estado de cochera (vacía)

---

## Paso 8: Ejecutar la Aplicación Web (Cochera.Web)

```powershell
cd cochera/src/Cochera.Web

# Ejecutar la aplicación
dotnet run
```

La aplicación estará disponible en: **http://localhost:5000**

### Verificar que funciona:
1. Abrir el navegador en http://localhost:5000
2. Debe cargar la interfaz de Cochera Inteligente
3. El panel lateral izquierdo muestra la navegación

### Páginas Disponibles

**Administrador:**
- Dashboard → http://localhost:5000/admin/dashboard
- Gestión de Entrada → http://localhost:5000/admin/entrada
- Cajones → http://localhost:5000/admin/cajones
- Sesiones Activas → http://localhost:5000/admin/sesiones-activas
- Historial → http://localhost:5000/admin/historial
- Reportes → http://localhost:5000/admin/reportes
- Eventos → http://localhost:5000/admin/eventos
- Tarifas → http://localhost:5000/admin/tarifas

**Usuario:**
- Estacionamiento → http://localhost:5000/usuario/estacionamiento
- Historial → http://localhost:5000/usuario/historial
- Pagos → http://localhost:5000/usuario/pagos

---

## Paso 9: Ejecutar el Worker (Cochera.Worker)

En una **nueva terminal** (separada de Cochera.Web):

```powershell
cd cochera/src/Cochera.Worker

# Ejecutar el worker
dotnet run
```

El Worker:
- Se conecta al broker MQTT
- Escucha el topic `cola_sensores`
- Procesa mensajes del ESP32
- Envía notificaciones SignalR a la Web

> **Nota:** El Worker debe ejecutarse **después** de que Cochera.Web esté en marcha, ya que necesita conectarse al Hub SignalR en http://localhost:5000/cocherahub.

---

## Paso 10: Configurar y Flashear el ESP32

### 10.1 Instalar Arduino IDE

1. Descargar desde https://www.arduino.cc/en/software
2. Instalar con opciones por defecto

### 10.2 Agregar Soporte para ESP32

1. Abrir Arduino IDE
2. Ir a **Archivo → Preferencias**
3. En "URLs adicionales de gestor de tarjetas", agregar:
   ```
   https://dl.espressif.com/dl/package_esp32_index.json
   ```
4. Ir a **Herramientas → Placa → Gestor de tarjetas**
5. Buscar "ESP32"
6. Instalar **"esp32 by Espressif Systems"**
7. Seleccionar la placa: **Herramientas → Placa → ESP32 Dev Module**

### 10.3 Instalar Librerías

En Arduino IDE, ir a **Herramientas → Gestionar bibliotecas**:

| Librería | Autor | Buscar como |
|----------|-------|-------------|
| PubSubClient | Nick O'Leary | "PubSubClient" |
| ArduinoJson | Benoit Blanchon | "ArduinoJson" |

### 10.4 Configurar el Firmware

Abrir el archivo `sketch_jan16a.ino` (en la raíz del repositorio).

**Modificar las siguientes constantes según tu red:**

```cpp
// ==== CONFIGURACIÓN WiFi ====
const char* ssid = "TU_RED_WIFI";           // Cambiar
const char* password = "TU_PASSWORD_WIFI";   // Cambiar

// ==== CONFIGURACIÓN MQTT ====
const char* mqtt_server = "192.168.X.X";     // IP de tu máquina con RabbitMQ
const int mqtt_port = 1883;
const char* mqtt_user = "esp32";
const char* mqtt_password = "123456";
const char* mqtt_topic = "cola_sensores";
```

### 10.5 Flashear el ESP32

1. Conectar el ESP32 por USB
2. En Arduino IDE:
   - **Herramientas → Puerto** → Seleccionar el puerto COM del ESP32
   - **Herramientas → Placa** → "ESP32 Dev Module"
   - **Herramientas → Upload Speed** → 115200
3. Click en **Subir** (flecha →)
4. Esperar a que compile y suba
5. Abrir **Monitor Serie** (115200 baud) para ver los logs

### 10.6 Verificar Conexión del ESP32

En el Monitor Serie debe aparecer:

```
Conectando a WiFi...
WiFi conectado. IP: 192.168.100.XX
Conectando a MQTT...
MQTT conectado!
Sistema iniciado - Cochera Inteligente
```

---

## Paso 11: Verificación Completa del Sistema

### Checklist de Verificación

| # | Verificación | Cómo verificar |
|---|-------------|----------------|
| 1 | PostgreSQL corriendo | `psql -U postgres -c "SELECT 1"` |
| 2 | Base de datos creada | `psql -U postgres -c "\l"` → ver "Cochera" |
| 3 | RabbitMQ corriendo | http://localhost:15672 (guest/guest) |
| 4 | Plugin MQTT habilitado | RabbitMQ Management → Overview → Puertos (1883) |
| 5 | Usuario esp32 creado | RabbitMQ Management → Admin → Users |
| 6 | Cochera.Web corriendo | http://localhost:5000 |
| 7 | Cochera.Worker corriendo | Logs en terminal muestran "MQTT conectado" |
| 8 | ESP32 conectado | Monitor Serie: "MQTT conectado!" |
| 9 | Eventos llegando | Admin → Eventos (se muestran al mover la mano frente al sensor) |
| 10 | SignalR funcionando | Dashboard se actualiza en tiempo real |

### Flujo de Prueba Completo

1. **Verificar estado inicial:**
   - Dashboard muestra 0 sesiones, 2 cajones libres
   
2. **Simular vehículo en cajón:**
   - Colocar un objeto frente al sensor HC-SR04 del Cajón 1 (a menos de 16cm)
   - El LED no debe cambiar si hay espacio
   - En "Eventos" debe aparecer "CAJON_OCUPADO"
   
3. **Crear sesión:**
   - Admin → Gestión de Entrada → Seleccionar usuario y cajón → Iniciar sesión
   
4. **Solicitar cierre:**
   - Admin → Sesiones Activas → Solicitar cierre
   
5. **Pagar:**
   - Cambiar a vista de usuario → Pagos → Confirmar pago
   
6. **Verificar historial:**
   - Admin → Historial de Sesiones
   - Usuario → Historial

---

## Solución de Problemas Comunes

### Error: No se puede conectar a PostgreSQL

```
Connection refused (localhost:5432)
```
**Solución:** Verificar que el servicio PostgreSQL está corriendo:
```powershell
Get-Service postgresql*
# Si está detenido:
Start-Service postgresql-x64-16
```

### Error: No se puede conectar a RabbitMQ (MQTT)

```
MQTT connection failed
```
**Solución:**
1. Verificar servicio: `Get-Service RabbitMQ`
2. Verificar plugin MQTT: 
   ```powershell
   cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.x\sbin"
   .\rabbitmq-plugins.bat list -e
   ```
3. Verificar que el puerto 1883 está escuchando:
   ```powershell
   Test-NetConnection localhost -Port 1883
   ```

### Error: ESP32 no conecta a WiFi

**Solución:**
- Verificar SSID y contraseña en el código
- El ESP32 solo soporta WiFi **2.4 GHz** (no 5 GHz)
- Verificar que el router está en alcance

### Error: Migraciones fallan

```powershell
# Reinstalar la herramienta
dotnet tool update --global dotnet-ef

# Eliminar la base de datos y recrear
psql -U postgres -c "DROP DATABASE \"Cochera\";"
psql -U postgres -c "CREATE DATABASE \"Cochera\";"

# Re-ejecutar la aplicación (auto-migraciones)
cd Cochera.Web
dotnet run
```

### Error: Worker no conecta a SignalR

```
SignalR connection failed: http://localhost:5000/cocherahub
```
**Solución:** Asegurar que `Cochera.Web` esté ejecutándose **antes** de iniciar el Worker.

---

## Variables de Entorno y Puertos Resumen

| Servicio | Puerto | Protocolo |
|----------|--------|-----------|
| Cochera.Web (Blazor) | 5000 | HTTP |
| PostgreSQL | 5432 | TCP |
| RabbitMQ (AMQP) | 5672 | TCP |
| RabbitMQ (MQTT) | 1883 | TCP |
| RabbitMQ (Management) | 15672 | HTTP |
| SignalR Hub | 5000 | WebSocket |
