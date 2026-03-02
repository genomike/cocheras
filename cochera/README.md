# Cochera Inteligente - Sistema de Estacionamiento IoT

Sistema completo de gestión de estacionamiento que integra hardware IoT (ESP32 + sensores ultrasónicos) con una aplicación web en tiempo real desarrollada con .NET 8 y Blazor Server. El sistema monitorea automáticamente la ocupación de cajones, gestiona sesiones de estacionamiento, procesa pagos y proporciona un dashboard administrativo con estadísticas en tiempo real.

---

## Arquitectura del Sistema

```
┌─────────────┐       MQTT        ┌──────────────┐       AMQP        ┌──────────────┐
│   ESP32      │ ───────────────► │  RabbitMQ     │ ◄───────────────► │  .NET Worker │
│ + HC-SR04 x3 │    (puerto 1883) │  (Broker)     │    (puerto 5672)  │              │
│ + LEDs       │                  └──────────────┘                    └──────┬───────┘
│ + Buzzer     │                                                             │
└─────────────┘                                                        SignalR │
                                                                             │
                    ┌──────────────┐                    ┌──────────────┐      │
                    │  PostgreSQL   │ ◄──── EF Core ──► │  Blazor Web  │ ◄────┘
                    │  (puerto 5432)│                    │  (puerto 5000)│
                    └──────────────┘                    └──────────────┘
```

---

## Tecnologías

| Capa | Tecnología |
|------|-----------|
| IoT / Hardware | ESP32, HC-SR04, Arduino C++ |
| Comunicación | MQTT (MQTTnet 4.3.3), RabbitMQ, SignalR |
| Backend | .NET 8, Entity Framework Core 8, Clean Architecture |
| Frontend | Blazor Server, Radzen Blazor 5.5.0 |
| Base de Datos | PostgreSQL 16, Npgsql 8.0.11 |

---

## Estructura del Proyecto

```
├── sketch_jan16a.ino              # Firmware ESP32 (Arduino)
└── cochera/
    └── src/
        ├── Cochera.Domain/        # Entidades, enums, interfaces de repositorio
        ├── Cochera.Application/   # Servicios, DTOs, lógica de negocio
        ├── Cochera.Infrastructure/ # EF Core, repositorios, MQTT consumer
        ├── Cochera.Web/           # Blazor Server, SignalR Hub, páginas
        └── Cochera.Worker/        # BackgroundService MQTT → SignalR
```

---

## Inicio Rápido

### Prerrequisitos

- .NET 8 SDK
- PostgreSQL 16
- RabbitMQ (con plugin MQTT habilitado)
- Arduino IDE (con soporte ESP32)

### 1. Clonar el repositorio

```bash
git clone <URL_DEL_REPOSITORIO>
cd cochera-inteligente
```

### 2. Configurar la base de datos

```sql
CREATE DATABASE "Cochera";
```

### 3. Habilitar MQTT en RabbitMQ

```bash
rabbitmq-plugins enable rabbitmq_mqtt
rabbitmqctl add_user esp32 123456
rabbitmqctl set_permissions -p / esp32 ".*" ".*" ".*"
```

### 4. Configuración de Conexión

Editar `appsettings.json` en Cochera.Web y Cochera.Worker:

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
    "Topic": "cola_sensores"
  }
}
```

### 5. Ejecutar la aplicación web

```bash
cd cochera/src/Cochera.Web
dotnet run
```
Acceder a: http://localhost:5000

### 6. Ejecutar el worker (en otra terminal)

```bash
cd cochera/src/Cochera.Worker
dotnet run
```

### 7. Flashear el ESP32

Abrir `sketch_jan16a.ino` en Arduino IDE, configurar WiFi y MQTT, y subir al ESP32.

> Para instrucciones detalladas paso a paso, consultar [docs/05-guia-instalacion.md](docs/05-guia-instalacion.md)

---

## Funcionalidades

### Panel Administrador
- Dashboard con KPIs en tiempo real (sesiones, ingresos, ocupación)
- Gestión de entrada de vehículos
- Monitoreo de cajones en tiempo real
- Gestión de sesiones activas y pendientes de pago
- Historial de sesiones con filtros
- Reportes de uso por hora y día
- Registro de eventos del sensor
- Configuración de tarifas

### Panel Usuario
- Vista de estado de estacionamiento en tiempo real
- Historial de sesiones personales
- Confirmación de pagos pendientes

### Hardware (ESP32)
- Detección automática de vehículos con sensores ultrasónicos
- LEDs indicadores (verde = espacio, rojo = lleno, parpadeo = salida)
- Señales sonoras diferenciadas (bienvenida, bloqueado, salida)
- Comunicación MQTT con el backend

---

## Eventos ESP32 Soportados

| Evento | Descripción |
|--------|-------------|
| SISTEMA_INICIADO | ESP32 se ha iniciado |
| MOVIMIENTO_ENTRADA | Vehículo detectado en entrada (hay espacio) |
| MOVIMIENTO_ENTRADA_BLOQUEADO | Intento de entrada con cochera llena |
| VEHICULO_SALIO | Vehículo confirmó salida |
| CAJON_OCUPADO | Sensor detectó vehículo en cajón |
| CAJON_LIBERADO | Cajón se desocupó |
| PARPADEO_INICIADO | Secuencia de salida iniciada |
| PARPADEO_TIMEOUT | Timeout del parpadeo (60s) |
| COCHERA_LLENA | Todos los cajones ocupados |

---

## Datos Iniciales (Seed)

| Código | Nombre | Rol |
|--------|--------|-----|
| admin | Administrador | Admin |
| usuario_1 | Usuario 1 | Usuario |
| usuario_2 | Usuario 2 | Usuario |
| usuario_3 | Usuario 3 | Usuario |

- **Cajones:** 2 cajones disponibles (Libres)
- **Tarifa:** $8.00 por minuto (activa)

---

## Documentación Completa

| Documento | Descripción |
|-----------|-------------|
| [01 - Arquitectura](docs/01-arquitectura.md) | Arquitectura del sistema, patrones, capas, stack tecnológico |
| [02 - Modelo de Datos](docs/02-modelo-de-datos.md) | Entidades, relaciones, enumeraciones, configuración EF Core |
| [03 - Servicios y Lógica](docs/03-servicios-y-logica.md) | Servicios de aplicación, flujos de negocio, DTOs, SignalR |
| [04 - IoT y Arduino](docs/04-iot-y-arduino.md) | Firmware ESP32, sensores, LEDs, buzzer, protocolo MQTT |
| [05 - Guía de Instalación](docs/05-guia-instalacion.md) | Instrucciones paso a paso para configurar y ejecutar todo |

### Diagramas UML (PlantUML)

| Diagrama | Archivo |
|----------|---------|
| Arquitectura General | [diagramas/arquitectura-general.puml](docs/diagramas/arquitectura-general.puml) |
| Clases - Domain | [diagramas/clases-domain.puml](docs/diagramas/clases-domain.puml) |
| Clases - Application | [diagramas/clases-application.puml](docs/diagramas/clases-application.puml) |
| Clases - Infrastructure | [diagramas/clases-infrastructure.puml](docs/diagramas/clases-infrastructure.puml) |
| Capas Clean Architecture | [diagramas/capas-clean-architecture.puml](docs/diagramas/capas-clean-architecture.puml) |
| Secuencia - Flujo Completo | [diagramas/secuencia-flujo-completo.puml](docs/diagramas/secuencia-flujo-completo.puml) |
| Secuencia - MQTT | [diagramas/secuencia-procesamiento-mqtt.puml](docs/diagramas/secuencia-procesamiento-mqtt.puml) |
| Estados - Sesión | [diagramas/estados-sesion.puml](docs/diagramas/estados-sesion.puml) |
| Estados - ESP32 | [diagramas/estados-esp32.puml](docs/diagramas/estados-esp32.puml) |
| Entidad-Relación | [diagramas/entidad-relacion.puml](docs/diagramas/entidad-relacion.puml) |
| Despliegue | [diagramas/despliegue.puml](docs/diagramas/despliegue.puml) |
| Comunicación SignalR | [diagramas/comunicacion-signalr.puml](docs/diagramas/comunicacion-signalr.puml) |

---

## Licencia

Proyecto académico - Maestría en Sistemas Embebidos.
