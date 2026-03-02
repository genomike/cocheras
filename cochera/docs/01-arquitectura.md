# Arquitectura del Sistema de Cochera Inteligente

## 1. Visión General

El **Sistema de Cochera Inteligente** es una solución completa de IoT (Internet de las Cosas) que integra hardware embebido (ESP32 + sensores ultrasónicos) con una aplicación web moderna (.NET 8 / Blazor Server) para la gestión automatizada de un estacionamiento de 2 cajones.

### 1.1 Objetivos del Sistema

- **Detección automática** de vehículos mediante sensores ultrasónicos HC-SR04
- **Comunicación en tiempo real** entre el hardware y la aplicación web vía MQTT
- **Gestión integral** de sesiones de estacionamiento, pagos y tarifas
- **Dashboard administrativo** con métricas, gráficos y alertas en tiempo real
- **Vista de usuario** para consultar disponibilidad y realizar pagos
- **Notificaciones push** en tiempo real mediante SignalR

---

## 2. Arquitectura de Alto Nivel

El sistema sigue una arquitectura de **múltiples capas** que combina una **Clean Architecture** en el backend con un patrón de **Event-Driven Architecture** para la comunicación IoT.

```
┌─────────────────────────────────────────────────────────────────┐
│                    CAPA FÍSICA (IoT)                            │
│  ESP32 + Sensores HC-SR04 + LEDs + Buzzer                       │
│  Firmware: sketch_jan16a.ino (Arduino/C++)                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │ MQTT (TCP 1883)
                           │ Topic: "cola_sensores"
┌──────────────────────────▼──────────────────────────────────────┐
│                  CAPA DE MENSAJERÍA                              │
│  RabbitMQ con Plugin MQTT                                        │
│  - Broker MQTT para comunicación ESP32 ↔ Backend                │
│  - Cola: mqtt-subscription-CocheraWorkerqos1                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ MQTT Subscribe
┌──────────────────────────▼──────────────────────────────────────┐
│                    CAPA BACKEND                                  │
│  ┌─────────────────┐  ┌──────────────────────────────────────┐  │
│  │ Cochera.Worker   │  │ Cochera.Web (Blazor Server)          │  │
│  │ (BackgroundSvc)  │──│ - Razor Components                   │  │
│  │ - MQTT Consumer  │  │ - SignalR Hub (CocheraHub)           │  │
│  │ - SignalR Client │  │ - Admin Panel / User Panel           │  │
│  └────────┬─────────┘  └────────────────┬─────────────────────┘  │
│           │                              │                       │
│  ┌────────▼──────────────────────────────▼─────────────────────┐ │
│  │            Cochera.Application (Servicios)                   │ │
│  │  CajonService, SesionService, EventoSensorService, etc.     │ │
│  └────────────────────────┬────────────────────────────────────┘ │
│  ┌────────────────────────▼────────────────────────────────────┐ │
│  │              Cochera.Domain (Entidades)                      │ │
│  │  Entities, Enums, Repository Interfaces                      │ │
│  └────────────────────────┬────────────────────────────────────┘ │
│  ┌────────────────────────▼────────────────────────────────────┐ │
│  │          Cochera.Infrastructure (Datos/MQTT)                 │ │
│  │  EF Core + Npgsql, Repositories, MQTT Services               │ │
│  └─────────────────────────┬───────────────────────────────────┘ │
└─────────────────────────────┼────────────────────────────────────┘
                              │ TCP 5432
┌─────────────────────────────▼────────────────────────────────────┐
│                    CAPA DE DATOS                                  │
│  PostgreSQL 16 - Base de datos "Cochera"                         │
│  Tablas: Usuarios, Cajones, Sesiones, Pagos, Eventos, etc.      │
└──────────────────────────────────────────────────────────────────┘
```

> **Diagrama PlantUML:** Ver [diagramas/arquitectura-general.puml](../diagramas/arquitectura-general.puml)

---

## 3. Patrón Arquitectónico: Clean Architecture

La solución sigue los principios de **Clean Architecture** (Robert C. Martin) con inversión de dependencias:

### 3.1 Capas de la Solución

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| **Domain** | `Cochera.Domain` | Entidades de negocio, enums, interfaces de repositorio. No depende de nada externo. |
| **Application** | `Cochera.Application` | Lógica de negocio, servicios, DTOs, interfaces de servicio. Depende solo de Domain. |
| **Infrastructure** | `Cochera.Infrastructure` | Acceso a datos (EF Core), repositorios, servicios MQTT. Implementa interfaces de Domain. |
| **Presentation** | `Cochera.Web` | UI Blazor Server, SignalR Hub, servicios web. Depende de Application e Infrastructure. |
| **Worker** | `Cochera.Worker` | Servicio en background que consume MQTT y notifica via SignalR. |

### 3.2 Regla de Dependencia

```
Cochera.Web ──────────┐
                      ├──▶ Cochera.Application ──▶ Cochera.Domain
Cochera.Worker ───────┘                                  ▲
                                                         │
Cochera.Infrastructure ──────────────────────────────────┘
                (implementa interfaces de Domain)
```

> **Diagrama PlantUML:** Ver [diagramas/capas-clean-architecture.puml](../diagramas/capas-clean-architecture.puml)

---

## 4. Patrones de Diseño Utilizados

### 4.1 Repository Pattern
Todos los accesos a datos están abstraídos mediante interfaces (`IRepository<T>`, `ICajonRepository`, etc.) con implementaciones concretas en `Infrastructure/Repositories`.

### 4.2 Unit of Work Pattern
`IUnitOfWork` agrupa todos los repositorios y gestiona transacciones de base de datos, garantizando la consistencia.

### 4.3 DTO Pattern (Data Transfer Objects)
Los DTOs en `Cochera.Application/DTOs` desacoplan las entidades de dominio de la capa de presentación, evitando exponer detalles internos.

### 4.4 Observer Pattern (Event-Driven)
- **MQTT**: ESP32 publica eventos, Worker los consume (Publisher-Subscriber)
- **SignalR**: El Hub distribuye eventos a clientes conectados en tiempo real

### 4.5 Dependency Injection
Todo el sistema utiliza inyección de dependencias nativa de .NET 8:
```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICajonService, CajonService>();
builder.Services.AddScoped<ISesionService, SesionService>();
// etc.
```

### 4.6 Factory Pattern
`IDbContextFactory<CocheraDbContext>` se usa para crear instancias del contexto de base de datos, especialmente importante en el Worker.

---

## 5. Flujo de Datos Principal

### 5.1 Flujo IoT → Backend → Frontend

```
ESP32 (Sensor) 
    → MQTT Publish (JSON)
        → RabbitMQ (Broker)
            → Cochera.Worker (MQTT Subscribe)
                → EventoSensorService.ProcesarMensajeAsync()
                    → PostgreSQL (INSERT evento, UPDATE cajones/estado)
                → SignalRNotificationService
                    → CocheraHub (WebSocket)
                        → Blazor Components (UI update)
```

### 5.2 Formato de Mensaje MQTT

```json
{
    "evento": "CAJON_OCUPADO",
    "detalle": "Cajon 1 ocupado",
    "timestamp": "2026-03-01T14:30:00",
    "cajon1": "OCUPADO",
    "cajon2": "LIBRE",
    "libres": 1,
    "ocupados": 1,
    "lleno": false
}
```

### 5.3 Tipos de Eventos

| Evento | Descripción | Trigger |
|--------|-------------|---------|
| `SISTEMA_INICIADO` | ESP32 se conectó | Boot |
| `MOVIMIENTO_ENTRADA` | Vehículo en entrada, hay espacio | Sensor entrada < 15cm |
| `MOVIMIENTO_ENTRADA_BLOQUEADO` | Vehículo en entrada, cochera llena | Sensor entrada + cochera llena |
| `VEHICULO_SALIO` | Vehículo confirmó salida | Fin de parpadeo |
| `CAJON_OCUPADO` | Un cajón fue ocupado | Sensor cajón < 16cm |
| `CAJON_LIBERADO` | Un cajón fue liberado | Sensor cajón >= 16cm |
| `PARPADEO_INICIADO` | Cajón liberado tras cochera llena | Cambio de estado |
| `PARPADEO_TIMEOUT` | Timeout de 60s sin salida | Timer |
| `COCHERA_LLENA` | Todos los cajones ocupados | Ambos sensores |

> **Diagrama PlantUML:** Ver [diagramas/secuencia-flujo-completo.puml](../diagramas/secuencia-flujo-completo.puml)

---

## 6. Comunicación en Tiempo Real

### 6.1 MQTT (ESP32 ↔ Backend)

- **Protocolo**: MQTT v3.1.1 sobre TCP
- **Broker**: RabbitMQ con plugin `rabbitmq_mqtt`
- **Puerto**: 1883
- **QoS**: 1 (At Least Once)
- **Topic**: `cola_sensores`
- **Formato**: JSON

### 6.2 SignalR (Backend ↔ Frontend)

- **Protocolo**: WebSocket (con fallback a Long Polling)
- **Endpoint**: `/cocherahub`
- **Grupos**:
  - `admins`: Reciben eventos de vehículos, notificaciones de pago
  - `usuario_{id}`: Recibe notificaciones específicas de su sesión

### 6.3 Canales de SignalR

| Canal | Dirección | Destinatario | Descripción |
|-------|-----------|-------------|-------------|
| `RecibirEvento` | Hub → All | Todos | Nuevo evento del sensor |
| `RecibirEstado` | Hub → All | Todos | Cambio de estado cochera |
| `VehiculoDetectadoEnEntrada` | Hub → Admins | Admin | Vehículo en entrada |
| `NuevaSesionCreada` | Hub → Admin+User | Ambos | Sesión registrada |
| `SolicitudCierreSesion` | Hub → User | Usuario | Admin solicita pago |
| `UsuarioPagoConfirmado` | Hub → Admin | Admin | Usuario confirmó pago |
| `SesionCerrada` | Hub → Admin+User | Ambos | Sesión finalizada |
| `ActualizarMontoSesion` | Hub → User | Usuario | Monto actualizado |

> **Diagrama PlantUML:** Ver [diagramas/comunicacion-signalr.puml](../diagramas/comunicacion-signalr.puml)

---

## 7. Stack Tecnológico

| Componente | Tecnología | Versión |
|-----------|------------|---------|
| Microcontrolador | ESP32 DevKit | - |
| Firmware | Arduino (C++) | - |
| Sensores | HC-SR04 (Ultrasónico) | - |
| Broker MQTT | RabbitMQ + MQTT Plugin | 3.x |
| Runtime Backend | .NET | 8.0 |
| ORM | Entity Framework Core | 8.0.11 |
| Base de Datos | PostgreSQL | 16 |
| UI Framework | Blazor Server | 8.0 |
| Componentes UI | Radzen Blazor | 5.5.0 |
| Tiempo Real (Web) | SignalR | 8.0.11 |
| MQTT Client (.NET) | MQTTnet | 4.3.3 |
| RabbitMQ Client | RabbitMQ.Client | 6.8.1 |
| Mediator | MediatR | 12.2.0 |

---

## 8. Seguridad y Consideraciones

### 8.1 Autenticación
- **Sistema actual**: Selector de usuario en sesión (sin autenticación formal)
- Usuarios predefinidos vía seed data (admin, usuario_1, usuario_2, usuario_3)
- `UsuarioActualService` gestiona la sesión del usuario en `ProtectedSessionStorage`

### 8.2 Comunicación MQTT
- Autenticación por usuario/contraseña en el broker MQTT
- Credenciales: `esp32` / `123456` (ambiente de desarrollo)

### 8.3 Base de Datos
- Cadena de conexión en `appsettings.json`
- Migraciones automáticas al iniciar la aplicación web

---

## 9. Escalabilidad y Extensibilidad

### Puntos de Extensión
1. **Más cajones**: Agregar sensores al ESP32 y registrar nuevos cajones en BD
2. **Autenticación**: Integrar ASP.NET Identity o un proveedor OAuth
3. **Notificaciones móviles**: Agregar push notifications via Firebase
4. **Múltiples cocheras**: Extender el modelo para soportar múltiples ubicaciones
5. **Métricas avanzadas**: Integrar con sistemas de BI o dashboards externos
6. **API REST**: Agregar controladores API para integraciones externas
