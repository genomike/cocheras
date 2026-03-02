# Modelo de Datos - Sistema de Cochera Inteligente

## 1. Visión General del Modelo

El modelo de datos está diseñado siguiendo los principios de **Domain-Driven Design (DDD)**, con entidades que representan conceptos del negocio de estacionamiento. Se utiliza **Entity Framework Core 8** como ORM y **PostgreSQL 16** como motor de base de datos.

> **Diagrama PlantUML:** Ver [diagramas/entidad-relacion.puml](../diagramas/entidad-relacion.puml) y [diagramas/clases-domain.puml](../diagramas/clases-domain.puml)

---

## 2. Entidades del Dominio

### 2.1 BaseEntity (Clase Abstracta)

Todas las entidades heredan de `BaseEntity`, proporcionando campos comunes de auditoría.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `int` | Clave primaria autoincremental |
| `FechaCreacion` | `DateTime` | Fecha de creación del registro (UTC) |
| `FechaActualizacion` | `DateTime?` | Última fecha de modificación (UTC) |

**Comportamiento automático**: El `DbContext` sobreescribe `SaveChangesAsync` para asignar automáticamente estas fechas.

---

### 2.2 Usuario

Representa a los usuarios del sistema (administradores y usuarios regulares).

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `Nombre` | `string` | MaxLength(100), Required | Nombre completo |
| `Codigo` | `string` | MaxLength(50), Required, Unique | Código de acceso |
| `EsAdmin` | `bool` | - | Indica si es administrador |
| `Sesiones` | `ICollection<SesionEstacionamiento>` | Navigation | Sesiones del usuario |

**Datos semilla (Seed Data):**

| Id | Nombre | Código | EsAdmin |
|----|--------|--------|---------|
| 1 | Administrador | admin | true |
| 2 | Usuario 1 | usuario_1 | false |
| 3 | Usuario 2 | usuario_2 | false |
| 4 | Usuario 3 | usuario_3 | false |

---

### 2.3 Cajon

Representa un espacio físico de estacionamiento.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `Numero` | `int` | Required, Unique | Número del cajón (1 o 2) |
| `Estado` | `EstadoCajon` | enum | Estado actual (Libre/Ocupado) |
| `UltimoCambioEstado` | `DateTime?` | - | Timestamp del último cambio |
| `Sesiones` | `ICollection<SesionEstacionamiento>` | Navigation | Sesiones en este cajón |

**Datos semilla:**

| Id | Numero | Estado |
|----|--------|--------|
| 1 | 1 | Libre |
| 2 | 2 | Libre |

---

### 2.4 SesionEstacionamiento

Entidad central del sistema. Representa una sesión de estacionamiento completa, desde la entrada hasta la salida y pago.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `UsuarioId` | `int` | FK → Usuario, ON DELETE RESTRICT | Usuario asignado |
| `CajonId` | `int` | FK → Cajon, ON DELETE RESTRICT | Cajón asignado |
| `HoraEntrada` | `DateTime` | Required | Hora de inicio (UTC) |
| `HoraSalida` | `DateTime?` | - | Hora de cierre (UTC) |
| `MinutosEstacionado` | `int` | - | Tiempo total en minutos |
| `TarifaPorMinuto` | `decimal(18,2)` | - | Tarifa aplicada al momento |
| `MontoTotal` | `decimal(18,2)` | - | Monto total a pagar |
| `Estado` | `EstadoSesion` | enum | Estado de la sesión |
| `Usuario` | `Usuario` | Navigation | Referencia al usuario |
| `Cajon` | `Cajon` | Navigation | Referencia al cajón |
| `Pago` | `Pago?` | Navigation (1:1) | Pago asociado |

**Ciclo de vida de la sesión:**

```
[Activa] → Admin solicita cierre → [PendientePago] → Usuario paga → [Finalizada]
[Activa] → Pago directo → [Finalizada]
[Activa] → Cancelación → [Cancelada]
```

**Cálculo del monto:**
```
MinutosEstacionado = ceil(HoraSalida - HoraEntrada)  // Mínimo 1 minuto
MontoTotal = MinutosEstacionado × TarifaPorMinuto
```

---

### 2.5 Pago

Registra el pago asociado a una sesión de estacionamiento. Relación 1:1 con SesionEstacionamiento.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `SesionId` | `int` | FK → Sesion, Unique, ON DELETE CASCADE | Sesión pagada |
| `Monto` | `decimal(18,2)` | Required | Monto pagado |
| `MetodoPago` | `MetodoPago` | enum | Método de pago usado |
| `FechaPago` | `DateTime` | Required | Fecha del pago (UTC) |
| `Referencia` | `string?` | MaxLength(100) | Referencia de pago |
| `Sesion` | `SesionEstacionamiento` | Navigation | Sesión asociada |

**Formato de referencia:** `PAGO-{yyyyMMddHHmmss}-{SesionId}`

---

### 2.6 EventoSensor

Almacena cada evento recibido del ESP32 vía MQTT. No tiene relaciones con otras entidades para mantener independencia como registro de auditoría.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `TipoEvento` | `TipoEvento` | enum | Tipo de evento clasificado |
| `EventoOriginal` | `string` | MaxLength(100) | Nombre original del ESP32 |
| `Detalle` | `string` | MaxLength(500) | Descripción del evento |
| `TimestampESP32` | `string` | MaxLength(50) | Timestamp del dispositivo |
| `EstadoCajon1` | `string` | MaxLength(20) | "OCUPADO" o "LIBRE" |
| `EstadoCajon2` | `string` | MaxLength(20) | "OCUPADO" o "LIBRE" |
| `CajonesLibres` | `int` | - | Cantidad de cajones libres |
| `CajonesOcupados` | `int` | - | Cantidad de cajones ocupados |
| `CocheraLlena` | `bool` | - | Si la cochera está llena |
| `JsonOriginal` | `string` | text (PostgreSQL) | JSON crudo del mensaje |

---

### 2.7 Tarifa

Gestiona las tarifas de estacionamiento con historial de cambios.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|--------------|-------------|
| `Id` | `int` | PK | Identificador |
| `PrecioPorMinuto` | `decimal(18,2)` | Required | Precio por minuto |
| `FechaInicio` | `DateTime` | Required | Inicio de vigencia |
| `FechaFin` | `DateTime?` | - | Fin de vigencia |
| `Activa` | `bool` | - | Si es la tarifa vigente |
| `Descripcion` | `string?` | MaxLength(200) | Descripción |

**Datos semilla:**

| PrecioPorMinuto | Activa | Descripción |
|----------------|--------|-------------|
| 8.00 | true | Tarifa estándar |

**Nota:** Solo puede haber **una tarifa activa** a la vez. Al crear una nueva, la anterior se desactiva automáticamente.

---

### 2.8 EstadoCochera

Refleja el estado actual en tiempo real de la cochera. Funciona como un **registro singleton** que se actualiza con cada evento del sensor.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `int` | PK (siempre = 1) |
| `Cajon1Ocupado` | `bool` | Estado del cajón 1 |
| `Cajon2Ocupado` | `bool` | Estado del cajón 2 |
| `CajonesLibres` | `int` | Cajones disponibles (0-2) |
| `CajonesOcupados` | `int` | Cajones en uso (0-2) |
| `CocheraLlena` | `bool` | Si no hay cajones disponibles |
| `UltimaActualizacion` | `DateTime` | Timestamp de última actualización |

---

## 3. Enumeraciones

### 3.1 EstadoCajon

```
Libre = 0      → El cajón está disponible
Ocupado = 1    → Hay un vehículo estacionado
```

### 3.2 EstadoSesion

```
Activa = 0          → Sesión en curso, tiempo corriendo
PendientePago = 1   → Admin solicitó cierre, esperando pago del usuario
Finalizada = 2      → Pago confirmado, sesión cerrada
Cancelada = 3       → Sesión cancelada sin cobro
```

### 3.3 MetodoPago

```
Efectivo = 0        → Pago en efectivo
Tarjeta = 1         → Pago con tarjeta
Transferencia = 2   → Pago por transferencia bancaria
```

### 3.4 TipoEvento

```
SistemaIniciado = 0             → ESP32 boot
MovimientoEntrada = 1           → Vehículo detectado en entrada (hay espacio)
MovimientoEntradaBloqueado = 2  → Vehículo en entrada (cochera llena)
VehiculoSalio = 3               → Vehículo confirmó salida
CajonOcupado = 4                → Cajón detectó vehículo
CajonLiberado = 5               → Cajón se liberó
ParpadeoIniciado = 6            → Secuencia de salida iniciada
ParpadeoTimeout = 7             → Timeout de secuencia de salida
CocheraLlena = 8                → Todos los cajones ocupados
```

---

## 4. Relaciones entre Entidades

```
Usuario (1) ────── (*) SesionEstacionamiento  │ Un usuario puede tener muchas sesiones
Cajon (1) ────── (*) SesionEstacionamiento    │ Un cajón puede alojar muchas sesiones
SesionEstacionamiento (1) ── (0..1) Pago      │ Una sesión puede tener un pago
```

### 4.1 Restricciones de Integridad

| Relación | Comportamiento ON DELETE |
|----------|------------------------|
| Usuario → Sesión | RESTRICT (no se puede eliminar usuario con sesiones) |
| Cajón → Sesión | RESTRICT (no se puede eliminar cajón con sesiones) |
| Sesión → Pago | CASCADE (eliminar sesión elimina su pago) |

### 4.2 Índices Únicos

| Tabla | Campo | Tipo |
|-------|-------|------|
| Usuarios | Codigo | UNIQUE |
| Cajones | Numero | UNIQUE |
| Pagos | SesionId | UNIQUE (1:1) |

---

## 5. Configuración de Entity Framework Core

### 5.1 DbContext

El `CocheraDbContext` configura:
- **Fluent API** para mapeo de entidades (no usa Data Annotations)
- **Precisión decimal**: (18, 2) para montos monetarios
- **Seed Data**: Datos iniciales para usuarios, cajones, tarifa y estado
- **Auditoría automática**: `FechaCreacion` y `FechaActualizacion`

### 5.2 Cadena de Conexión

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Cochera;Username=postgres;Password=postgres;"
  }
}
```

### 5.3 Migraciones

Las migraciones se ejecutan **automáticamente** al iniciar `Cochera.Web`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CocheraDbContext>>();
    await using var db = await contextFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
```

### 5.4 Configuración NTP (Timestamps)

```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

Esta configuración permite el uso de `DateTime` sin zona horaria en PostgreSQL, simplificando el manejo de timestamps.
