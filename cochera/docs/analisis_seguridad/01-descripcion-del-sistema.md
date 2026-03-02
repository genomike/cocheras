# 01 - Descripción del Sistema Evaluado

## 1.1 Información General

| Campo | Detalle |
|-------|---------|
| **Nombre del sistema** | Cochera Inteligente - Sistema de Estacionamiento IoT |
| **Versión evaluada** | 1.0 (Proyecto académico - Maestría en Sistemas Embebidos) |
| **Fecha del análisis** | Marzo 2026 |
| **Tipo de aplicación** | Sistema Web + IoT con comunicación en tiempo real |
| **Framework principal** | .NET 8 (Blazor Server) |
| **Base de datos** | PostgreSQL 16 |
| **Broker de mensajería** | RabbitMQ 3.13+ con plugin MQTT |
| **Hardware IoT** | ESP32 DevKit V1 con sensores HC-SR04 |

## 1.2 Objetivo del Sistema

El sistema **Cochera Inteligente** automatiza la gestión de un estacionamiento de 2 cajones mediante la integración de sensores ultrasónicos (HC-SR04) en un microcontrolador ESP32, que comunica eventos en tiempo real a una aplicación web. El sistema permite:

- **Detección automática** de vehículos en cajones y entrada mediante sensores ultrasónicos
- **Gestión de sesiones** de estacionamiento con cálculo de tarifas por minuto
- **Procesamiento de pagos** con múltiples métodos (efectivo, tarjeta, transferencia)
- **Dashboard administrativo** con estadísticas y reportes en tiempo real
- **Notificaciones en tiempo real** vía SignalR entre administradores y usuarios

## 1.3 Arquitectura General

El sistema sigue una **Clean Architecture** con 5 proyectos .NET y un firmware embebido:

```
┌─────────────────────────────────────────────────────────────────┐
│                        CAPA DE PRESENTACIÓN                      │
│  ┌──────────────────────┐    ┌──────────────────────┐            │
│  │   Cochera.Web         │    │   Cochera.Worker      │            │
│  │   (Blazor Server)     │    │   (BackgroundService)  │            │
│  │   Puerto: 5000        │    │                        │            │
│  │   SignalR Hub          │◄──│   Cliente SignalR       │            │
│  └──────────┬────────────┘    └──────────┬────────────┘            │
├─────────────┼────────────────────────────┼─────────────────────────┤
│             │       CAPA DE APLICACIÓN   │                         │
│  ┌──────────▼────────────────────────────▼────────────┐            │
│  │              Cochera.Application                    │            │
│  │   Servicios: Sesion, Evento, Cajon, Tarifa, etc.   │            │
│  └────────────────────────┬───────────────────────────┘            │
├───────────────────────────┼────────────────────────────────────────┤
│             │       CAPA DE INFRAESTRUCTURA                        │
│  ┌──────────▼────────────────────────────────────────┐             │
│  │              Cochera.Infrastructure                │             │
│  │   EF Core + PostgreSQL | MQTT Consumer | Repos    │             │
│  └───────────────────────────────────────────────────┘             │
├────────────────────────────────────────────────────────────────────┤
│                         CAPA DE DOMINIO                            │
│  ┌───────────────────────────────────────────────────┐             │
│  │              Cochera.Domain                        │             │
│  │   Entidades | Enums | Interfaces de Repositorio   │             │
│  └───────────────────────────────────────────────────┘             │
└────────────────────────────────────────────────────────────────────┘

           ▲                                  ▲
           │ MQTT (puerto 1883, sin TLS)      │ HTTP (puerto 5000)
           │                                  │
    ┌──────┴──────┐                    ┌──────┴──────┐
    │  RabbitMQ    │                    │  Navegador   │
    │  (Broker)    │                    │  del Usuario │
    └──────┬──────┘                    └─────────────┘
           │
    ┌──────┴──────┐
    │   ESP32      │
    │ + HC-SR04 x3 │
    │ + LEDs/Buzzer│
    └─────────────┘
```

## 1.4 Tipo de Usuarios

El sistema define dos roles sin autenticación formal:

| Rol | Código de acceso | Permisos | Método de identificación |
|-----|-----------------|----------|-------------------------|
| **Administrador** | `admin` | Dashboard, gestión de entrada/salida, sesiones, tarifas, reportes, eventos | Selección manual en dropdown (sin contraseña) |
| **Usuario regular** | `usuario_1`, `usuario_2`, `usuario_3` | Ver estacionamiento propio, historial personal, confirmar pagos | Selección manual en dropdown (sin contraseña) |

**Hallazgo crítico:** No existe un sistema de autenticación. Los usuarios se seleccionan de un menú desplegable (`UserSelector`) sin verificación de identidad.

## 1.5 Principales Módulos y Componentes

### Backend (.NET 8)

| Módulo | Responsabilidad | Archivos clave |
|--------|----------------|----------------|
| **Cochera.Domain** | Entidades (8), enums (4), interfaces de repositorio (9) | `Entities/*.cs`, `Enums/*.cs`, `Interfaces/*.cs` |
| **Cochera.Application** | Lógica de negocio (7 servicios), DTOs (8) | `Services/*.cs`, `DTOs/*.cs` |
| **Cochera.Infrastructure** | Acceso a datos, MQTT, repositorios | `Data/CocheraDbContext.cs`, `Mqtt/*.cs`, `Repositories/*.cs` |
| **Cochera.Web** | UI Blazor Server, SignalR Hub, 12 páginas | `Components/Pages/**/*.razor`, `Hubs/CocheraHub.cs` |
| **Cochera.Worker** | Consumidor MQTT, puente SignalR | `MqttWorker.cs`, `SignalRNotificationService.cs` |

### Firmware (ESP32)

| Componente | Función |
|-----------|---------|
| Sensores HC-SR04 (x3) | Detección de vehículos (entrada + 2 cajones) |
| LEDs (verde/rojo) | Indicadores visuales de disponibilidad |
| Buzzer | Señales acústicas diferenciadas |
| WiFi + MQTT | Comunicación con el backend |

### Servicios Externos

| Servicio | Puerto | Protocolo | Función |
|----------|--------|-----------|---------|
| PostgreSQL 16 | 5432 | TCP | Base de datos principal |
| RabbitMQ | 5672 (AMQP), 1883 (MQTT), 15672 (Management) | TCP/HTTP | Broker de mensajería |

## 1.6 Flujo de Datos Principal

```
1. ESP32 detecta vehículo → publica JSON en topic MQTT "cola_sensores"
2. RabbitMQ recibe mensaje → enruta al consumidor suscrito
3. Cochera.Worker consume mensaje MQTT → invoca EventoSensorService
4. EventoSensorService procesa → actualiza DB (eventos, cajones, estado)
5. Worker envía notificación → SignalR Hub → clientes web conectados
6. Blazor actualiza UI en tiempo real para administradores y usuarios
7. Admin gestiona sesiones → usuario recibe notificación de pago pendiente
8. Usuario confirma pago → admin recibe confirmación → sesión finalizada
```

## 1.7 Justificación de la Elección del Sistema

Este sistema fue elegido para el análisis de seguridad por las siguientes razones:

1. **Superficie de ataque amplia:** Combina una aplicación web, un servicio de background, un broker de mensajería (RabbitMQ/MQTT), una base de datos (PostgreSQL), comunicación en tiempo real (SignalR) y un dispositivo IoT (ESP32). Esto proporciona múltiples vectores de ataque para analizar.

2. **Protocolos heterogéneos:** El sistema utiliza HTTP, WebSocket (SignalR), MQTT y TCP (PostgreSQL), cada uno con sus propias consideraciones de seguridad.

3. **Datos sensibles:** Maneja información de pagos, sesiones de usuarios e información de identificación que requieren protección adecuada.

4. **IoT como vector de ataque:** El ESP32 se comunica por WiFi y MQTT en texto plano, representando un punto de entrada potencial para atacantes en la red local.

5. **Acceso completo al código fuente:** Se tiene acceso total al código fuente de todas las capas (código .NET y firmware Arduino C++), archivos de configuración y esquema de base de datos, lo que permite un análisis de caja blanca exhaustivo.

6. **Representatividad:** Las vulnerabilidades encontradas son representativas de problemas comunes en proyectos IoT + Web, lo que permite extraer aprendizajes aplicables a la industria.

## 1.8 Nivel de Acceso para la Evaluación

| Aspecto | Nivel de acceso |
|---------|----------------|
| Código fuente (.NET) | Completo (caja blanca) |
| Código fuente (ESP32/Arduino) | Completo (caja blanca) |
| Archivos de configuración | Completo (incluyendo credenciales) |
| Base de datos (esquema + datos) | Completo (incluyendo datos semilla) |
| Broker MQTT/RabbitMQ | Configuración completa |
| Infraestructura de red | Topología conocida (red local) |
| Dependencias y paquetes NuGet | Lista completa con versiones |

Este nivel de acceso permite realizar un análisis de seguridad de **caja blanca** completo, combinando revisión manual del código, análisis estático (SAST) y recomendaciones para análisis dinámico (DAST).

## 1.9 Resumen de la Superficie de Ataque

```
┌─────────────────────────────────────────────────────────────┐
│                   SUPERFICIE DE ATAQUE                       │
├─────────────────┬───────────────────────────────────────────┤
│ Capa Web        │ • Puerto 5000 HTTP (sin HTTPS forzado)    │
│                 │ • SignalR Hub sin autenticación             │
│                 │ • Blazor Server con estado en servidor      │
│                 │ • Selector de usuario sin contraseña        │
│                 │ • AllowedHosts: "*"                        │
├─────────────────┼───────────────────────────────────────────┤
│ Capa MQTT/IoT   │ • Puerto 1883 MQTT sin TLS                │
│                 │ • Credenciales hardcoded (esp32/123456)    │
│                 │ • Sin validación de origen de mensajes     │
│                 │ • WiFi credentials en firmware              │
├─────────────────┼───────────────────────────────────────────┤
│ Base de Datos   │ • PostgreSQL con user postgres/postgres    │
│                 │ • Puerto 5432 expuesto                     │
│                 │ • Sin cifrado de datos sensibles            │
├─────────────────┼───────────────────────────────────────────┤
│ RabbitMQ        │ • Management UI en puerto 15672            │
│                 │ • Credenciales por defecto guest/guest     │
│                 │ • AMQP sin TLS en puerto 5672              │
├─────────────────┼───────────────────────────────────────────┤
│ Firmware ESP32  │ • Credenciales WiFi en texto plano         │
│                 │ • Sin OTA seguro                            │
│                 │ • Sin validación de certificados            │
│                 │ • Puerto serial expuesto                    │
└─────────────────┴───────────────────────────────────────────┘
```
