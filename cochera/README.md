# Sistema de Cochera - Aplicación .NET

Sistema de gestión de estacionamiento con arquitectura hexagonal que consume eventos de sensores ESP32 vía MQTT.

## 🏗️ Arquitectura

```
cochera/
├── src/
│   ├── Cochera.Domain/          # Entidades, enums e interfaces
│   ├── Cochera.Application/     # DTOs, servicios e interfaces de aplicación
│   ├── Cochera.Infrastructure/  # DbContext, repositorios, MQTT consumer
│   ├── Cochera.Web/             # Blazor Server + SignalR Hub
│   └── Cochera.Worker/          # Background worker para MQTT
└── Cochera.sln
```

## 📋 Requisitos

- .NET 8 SDK
- PostgreSQL
- RabbitMQ/MQTT Broker (en 192.168.100.16:1883)
- ESP32 con sensores configurados

## 🚀 Configuración

### 1. Base de Datos PostgreSQL

Crear la base de datos y aplicar migraciones:

```bash
# Crear base de datos (si no existe)
createdb -U postgres Cochera

# Aplicar migraciones
cd src/Cochera.Infrastructure
dotnet ef database update --startup-project ../Cochera.Web/Cochera.Web.csproj
```

### 2. Configuración de Conexión

Editar `appsettings.json` en Cochera.Web y Cochera.Worker:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  }
}
```

### 3. Configuración MQTT (Worker)

En `Cochera.Worker/appsettings.json`:

```json
{
  "Mqtt": {
    "Server": "192.168.100.16",
    "Port": 1883,
    "Username": "esp32",
    "Password": "123456",
    "Topic": "cola_sensores"
  }
}
```

## ▶️ Ejecución

### Ejecutar la aplicación Web:

```bash
cd src/Cochera.Web
dotnet run
```

Acceder a: http://localhost:5000

### Ejecutar el Worker MQTT (en terminal separada):

```bash
cd src/Cochera.Worker
dotnet run
```

## 📱 Características

### Dashboard
- Estado en tiempo real de la cochera
- Gráficos de uso por hora
- Últimos eventos de sensores
- Sesiones activas

### Estacionar
- Selector de usuario (usuario_1, usuario_2, usuario_3)
- Visualización de cajones disponibles
- Inicio/fin de sesiones
- Cálculo automático de montos

### Eventos de Sensores
- Lista en tiempo real de todos los eventos
- Filtros por fecha
- Estado de conexión SignalR

### Historial de Sesiones
- Listado completo de sesiones
- Filtros por fecha
- Estadísticas de uso

### Administración
- **Tarifas**: Configurar precio por minuto (default: 8 soles/min)
- **Reportes**: Gráficos de recaudación y uso

## 📡 Eventos ESP32 Soportados

| Evento | Descripción |
|--------|-------------|
| SISTEMA_INICIADO | ESP32 se ha iniciado |
| MOVIMIENTO_ENTRADA | Vehículo detectado en entrada |
| MOVIMIENTO_ENTRADA_BLOQUEADO | Intento de entrada con cochera llena |
| VEHICULO_SALIO | Vehículo salió de la entrada |
| CAJON_OCUPADO | Un cajón ha sido ocupado |
| CAJON_LIBERADO | Un cajón ha sido liberado |
| PARPADEO_INICIADO | LED verde parpadeando |
| PARPADEO_TIMEOUT | Timeout del parpadeo |
| COCHERA_LLENA | La cochera está completamente llena |

## 🗄️ Datos Iniciales (Seed)

- **Usuarios**: admin, usuario_1, usuario_2, usuario_3
- **Cajones**: 2 cajones disponibles
- **Tarifa**: 8 soles por minuto

## 🔧 Tecnologías

- **Frontend**: Blazor Server + Radzen Blazor Components
- **Backend**: .NET 8
- **Base de Datos**: PostgreSQL + Entity Framework Core
- **Mensajería**: MQTT (MQTTnet)
- **Real-time**: SignalR
- **Arquitectura**: Hexagonal (DDD)
