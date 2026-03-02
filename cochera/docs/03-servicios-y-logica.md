# Servicios y Lógica de Negocio - Sistema de Cochera Inteligente

## 1. Arquitectura de Servicios

La capa de **Application** contiene toda la lógica de negocio del sistema. Los servicios se inyectan vía **Dependency Injection** y trabajan a través del patrón **Unit of Work** para garantizar transacciones atómicas.

> **Diagrama PlantUML:** Ver [diagramas/clases-application.puml](../diagramas/clases-application.puml) y [diagramas/secuencia-flujo-completo.puml](../diagramas/secuencia-flujo-completo.puml)

---

## 2. Servicios de Aplicación

### 2.1 EventoSensorService

**Interfaz:** `IEventoSensorService`  
**Responsabilidad:** Procesar eventos MQTT del ESP32: clasificar, almacenar y actualizar el estado global.

#### Métodos

| Método | Descripción |
|--------|-------------|
| `ProcesarMensajeAsync(MensajeSensorMqtt)` | Procesa un mensaje MQTT del ESP32 |
| `ObtenerTodosAsync()` | Retorna todos los eventos registrados |
| `ObtenerRecientesAsync(int cantidad)` | Retorna los N eventos más recientes |

#### Lógica de `ProcesarMensajeAsync`

```
1. Recibe MensajeSensorMqtt del ESP32
2. Clasifica el tipo de evento (mapeo de string a TipoEvento)
3. Crea registro EventoSensor con todos los datos
4. Actualiza cajones (Estado: Libre/Ocupado basado en estados del mensaje)
5. Actualiza/Crea EstadoCochera (singleton) con contadores
6. Ejecuta todo en una transacción (BeginTransactionAsync + CommitAsync)
```

#### Mapeo de Eventos ESP32 → TipoEvento

| Evento ESP32 | TipoEvento | Descripción |
|-------------|------------|-------------|
| `SISTEMA_INICIADO` | SistemaIniciado | ESP32 arrancó |
| `MOVIMIENTO_ENTRADA` | MovimientoEntrada | Vehículo detectado, hay espacio |
| `MOVIMIENTO_ENTRADA_BLOQUEADO` | MovimientoEntradaBloqueado | Cochera llena |
| `VEHICULO_SALIO` | VehiculoSalio | Confirmación de salida |
| `CAJON_OCUPADO` | CajonOcupado | Sensor detectó vehículo en cajón |
| `CAJON_LIBERADO` | CajonLiberado | Cajón se desocupó |
| `PARPADEO_INICIADO` | ParpadeoIniciado | Secuencia de salida |
| `PARPADEO_TIMEOUT` | ParpadeoTimeout | Timeout de parpadeo |
| `COCHERA_LLENA` | CocheraLlena | Sin cajones disponibles |

---

### 2.2 SesionService

**Interfaz:** `ISesionService`  
**Responsabilidad:** Gestionar el ciclo de vida completo de las sesiones de estacionamiento.

#### Métodos

| Método | Parámetros | Retorno | Descripción |
|--------|-----------|---------|-------------|
| `IniciarSesionAsync` | `IniciarSesionRequest` | `SesionEstacionamientoDto` | Crea nueva sesión activa |
| `SolicitarCierreAsync` | `int sesionId` | `SesionEstacionamientoDto` | Admin solicita cierre (→PendientePago) |
| `ConfirmarPagoAsync` | `ConfirmacionPagoDto` | `PagoDto` | Usuario confirma pago |
| `CerrarSesionAsync` | `int sesionId` | `SesionEstacionamientoDto` | Admin cierra directamente |
| `FinalizarSesionAsync` | `FinalizarSesionRequest` | `SesionEstacionamientoDto` | Legacy: finalización clásica |
| `CalcularMontoActualAsync` | `int sesionId` | `decimal` | Calcula monto en tiempo real |
| `ObtenerSesionesActivasAsync` | - | `IEnumerable<SesionDto>` | Lista sesiones activas |
| `ObtenerSesionesPendientesAsync` | - | `IEnumerable<SesionDto>` | Lista sesiones pendientes |
| `ObtenerHistorialAsync` | - | `IEnumerable<SesionDto>` | Historial completo |
| `ObtenerSesionesPorUsuarioAsync` | `int usuarioId` | `IEnumerable<SesionDto>` | Sesiones de un usuario |

#### Flujo: Iniciar Sesión

```
1. Buscar usuario por ID
2. Buscar cajón por número
3. Verificar que el cajón esté Libre
4. Obtener tarifa activa vigente
5. Crear SesionEstacionamiento:
   - Estado = Activa
   - HoraEntrada = DateTime.UtcNow
   - TarifaPorMinuto = tarifa.PrecioPorMinuto
6. Guardar en base de datos
```

#### Flujo: Solicitar Cierre (Admin)

```
1. Obtener sesión por ID (incluir Usuario, Cajón)
2. Verificar Estado == Activa
3. Asignar HoraSalida = DateTime.UtcNow
4. Calcular MinutosEstacionado:
   - (HoraSalida - HoraEntrada).TotalMinutes
   - Math.Max(Math.Ceiling(...), 1) → Mínimo 1 minuto
5. Calcular MontoTotal = MinutosEstacionado × TarifaPorMinuto
6. Cambiar Estado → PendientePago
7. Guardar cambios
```

#### Flujo: Confirmar Pago (Usuario)

```
1. Obtener sesión por ID (incluir Usuario, Cajón)
2. Verificar Estado == PendientePago
3. Crear registro Pago:
   - Monto = sesion.MontoTotal
   - MetodoPago = dto.MetodoPago
   - FechaPago = DateTime.UtcNow
   - Referencia = $"PAGO-{fecha:yyyyMMddHHmmss}-{sesionId}"
4. Si ConfirmaCierre (flag):
   - Estado → Finalizada
   - Cajon.Estado → Libre
   - Cajon.UltimoCambioEstado = DateTime.UtcNow
   - Actualizar EstadoCochera
5. Guardar cambios
```

#### Flujo: Cerrar Sesión Directa (Admin)

```
1. Obtener sesión con includes completos
2. Verificar estado válido (Activa o PendientePago)
3. Asignar HoraSalida si no tiene
4. Calcular minutos y monto
5. Estado → Finalizada
6. Liberar cajón
7. Actualizar EstadoCochera
8. Guardar cambios
```

---

### 2.3 CajonService

**Interfaz:** `ICajonService`  
**Responsabilidad:** Consultar y gestionar el estado de los cajones de estacionamiento.

| Método | Descripción |
|--------|-------------|
| `ObtenerTodosAsync()` | Lista todos los cajones con su estado |
| `ObtenerPorIdAsync(int id)` | Obtiene un cajón específico |
| `ObtenerCajonesLibresAsync()` | Lista solo cajones con Estado = Libre |

**Mapeo a DTO:**
```csharp
CajonDto {
    Id, Numero, Estado (string), UltimoCambioEstado
}
```

---

### 2.4 TarifaService

**Interfaz:** `ITarifaService`  
**Responsabilidad:** Gestionar las tarifas de estacionamiento.

| Método | Descripción |
|--------|-------------|
| `ObtenerTarifaVigenteAsync()` | Retorna la tarifa activa actual |
| `ObtenerHistorialAsync()` | Lista todas las tarifas (histórico) |
| `ActualizarTarifaAsync(ActualizarTarifaRequest)` | Crea nueva tarifa, desactiva anterior |

#### Flujo: Actualizar Tarifa

```
1. Obtener tarifa activa actual
2. Si existe, cerrar vigencia:
   - FechaFin = DateTime.UtcNow
   - Activa = false
3. Crear nueva tarifa:
   - PrecioPorMinuto = nuevo precio
   - FechaInicio = DateTime.UtcNow
   - Activa = true
4. Guardar cambios
```

**Nota:** Siempre hay exactamente una tarifa activa. Las sesiones usan la tarifa vigente al momento de su creación ($8.00/min por defecto).

---

### 2.5 UsuarioService

**Interfaz:** `IUsuarioService`  
**Responsabilidad:** Gestionar usuarios del sistema.

| Método | Descripción |
|--------|-------------|
| `ObtenerTodosAsync()` | Lista todos los usuarios |
| `ObtenerPorIdAsync(int id)` | Obtiene un usuario por ID |
| `ObtenerPorCodigoAsync(string codigo)` | Busca usuario por código de acceso |
| `CrearUsuarioAsync(UsuarioDto dto)` | Crea nuevo usuario |

---

### 2.6 EstadoCocheraService

**Interfaz:** `IEstadoCocheraService`  
**Responsabilidad:** Obtener el estado consolidado actual de toda la cochera.

| Método | Descripción |
|--------|-------------|
| `ObtenerEstadoActualAsync()` | Retorna el registro singleton de estado |

**Retorna** `EstadoCocheraDto`:
```csharp
{
    Cajon1Ocupado: bool,
    Cajon2Ocupado: bool,
    CajonesLibres: int,     // 0-2
    CajonesOcupados: int,   // 0-2
    CocheraLlena: bool
}
```

---

### 2.7 DashboardService

**Interfaz:** `IDashboardService`  
**Responsabilidad:** Generar estadísticas y KPIs para el panel administrativo.

| Método | Descripción |
|--------|-------------|
| `ObtenerDashboardAsync()` | Retorna todas las métricas del dashboard |
| `ObtenerUsoPorHoraAsync(DateTime fecha)` | Uso por hora para una fecha específica |
| `ObtenerUsoPorDiaAsync(int mes, int anio)` | Uso por día para un mes específico |

**DashboardDto contiene:**
```
- TotalSesionesHoy: Sesiones creadas hoy
- SesionesActivas: Sesiones con Estado == Activa
- SesionesPendientesPago: Sesiones con Estado == PendientePago
- IngresosTotalesHoy: Suma de pagos del día
- TarifaActual: PrecioPorMinuto de la tarifa vigente
- TiempoPromedioMinutos: Promedio de MinutosEstacionado del día
- CajonesLibres / CajonesOcupados: del EstadoCochera
- UsoPorHora: List<ResumenPorHora> { Hora, TotalSesiones, TiempoPromedio }
- UsoPorDia: List<ResumenPorDia> { Fecha, TotalSesiones, IngresoTotal }
```

---

## 3. Procesamiento de Mensajes MQTT

### 3.1 Formato del Mensaje del ESP32

```json
{
  "evento": "CAJON_OCUPADO",
  "detalle": "Vehiculo detectado en cajon 1",
  "timestamp": "2025-01-16T14:30:00",
  "estado": {
    "cajon1": "OCUPADO",
    "cajon2": "LIBRE",
    "cajones_libres": 1,
    "cajones_ocupados": 1,
    "cochera_llena": false
  }
}
```

### 3.2 Pipeline de Procesamiento

```
ESP32 → [MQTT Broker] → MqttConsumerService → MqttWorker → EventoSensorService
                                                          → CajonService (update)
                                                          → EstadoCocheraService (update)
                                                          → SignalR Notifications
```

### 3.3 MqttWorker (BackgroundService)

El Worker es un servicio independiente que:
1. Se suscribe a eventos del `IMqttConsumerService`
2. Procesa cada mensaje JSON recibido
3. Invoca `EventoSensorService.ProcesarMensajeAsync()`
4. Envía notificaciones en tiempo real vía SignalR

---

## 4. Notificaciones en Tiempo Real (SignalR)

### 4.1 Hub: CocheraHub

**Endpoint:** `/cocherahub`

**Grupos:**
- `admins` → Usuarios con EsAdmin = true
- `usuario_{id}` → Cada usuario regular en su grupo individual

#### Métodos del Hub (Server → Client)

| Método | Destino | Datos | Descripción |
|--------|---------|-------|-------------|
| `ActualizarEstadoCochera` | admins + usuario | EstadoCocheraDto | Cambio de estado global |
| `NuevoEventoSensor` | admins | EventoSensorDto | Nuevo evento del ESP32 |
| `SesionIniciada` | admins + usuario | SesionDto | Sesión creada |
| `SesionPendientePago` | admins + usuario | SesionDto | Admin solicitó cierre |
| `SesionFinalizada` | admins + usuario | SesionDto | Sesión cerrada |
| `PagoConfirmado` | admins + usuario | PagoDto | Pago procesado |
| `ActualizarDashboard` | admins | DashboardDto | KPIs actualizados |

### 4.2 SesionNotificationService

Servicio del proyecto Web que envía notificaciones via `IHubContext<CocheraHub>`.

### 4.3 SignalRNotificationService (Worker)

El Worker se conecta al Hub como **cliente** SignalR para enviar notificaciones desde el proceso independiente.

```
URL de conexión: http://localhost:5000/cocherahub
Reconexión automática: delays [0s, 2s, 10s, 30s]
```

---

## 5. Patrón Repository + Unit of Work

### 5.1 IRepository\<T\> (Genérico)

```csharp
interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

### 5.2 Repositorios Especializados

| Repositorio | Métodos Adicionales |
|------------|-------------------|
| `ICajonRepository` | `GetByNumeroAsync(int)`, `GetCajonesLibresAsync()` |
| `IUsuarioRepository` | `GetByCodigoAsync(string)` |
| `ISesionEstacionamientoRepository` | `GetActivasAsync()`, `GetPendientesPagoAsync()`, `GetByUsuarioAsync(int)`, `GetByIdWithIncludesAsync(int)` |
| `IEventoSensorRepository` | `GetRecientesAsync(int)` |
| `ITarifaRepository` | `GetTarifaActivaAsync()` |
| `IEstadoCocheraRepository` | `GetEstadoActualAsync()` |
| `IPagoRepository` | `GetBySesionIdAsync(int)` |

### 5.3 IUnitOfWork

```csharp
interface IUnitOfWork : IDisposable
{
    ICajonRepository Cajones { get; }
    IUsuarioRepository Usuarios { get; }
    ISesionEstacionamientoRepository Sesiones { get; }
    IEventoSensorRepository EventosSensor { get; }
    ITarifaRepository Tarifas { get; }
    IEstadoCocheraRepository EstadoCochera { get; }
    IPagoRepository Pagos { get; }

    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

La implementación usa **lazy loading** para los repositorios y `IDbContextFactory` para crear instancias frescas del DbContext.

---

## 6. DTOs (Data Transfer Objects)

### 6.1 MensajeSensorMqtt (Entrada MQTT)

```csharp
class MensajeSensorMqtt
{
    string Evento { get; set; }
    string Detalle { get; set; }
    string Timestamp { get; set; }
    EstadoMqtt Estado { get; set; }
}

class EstadoMqtt
{
    string Cajon1 { get; set; }
    string Cajon2 { get; set; }
    int CajonesLibres { get; set; }
    int CajonesOcupados { get; set; }
    bool CocheraLlena { get; set; }
}
```

### 6.2 SesionEstacionamientoDto (Datos calculados en tiempo real)

```csharp
class SesionEstacionamientoDto
{
    // ... campos base ...
    int MinutosEstacionado => sesion tiene salida ? valor guardado : calcular ahora
    decimal MontoTotal => minutos × TarifaPorMinuto
    string TiempoFormateado => $"{horas}h {minutos}m"
}
```

### 6.3 Request DTOs

| DTO | Campos | Uso |
|-----|--------|-----|
| `IniciarSesionRequest` | UsuarioId, CajonNumero | Crear nueva sesión |
| `FinalizarSesionRequest` | SesionId, (cálculos) | Cerrar sesión (legacy) |
| `SolicitudPagoDto` | SesionId | Solicitar cierre |
| `ConfirmacionPagoDto` | SesionId, MetodoPago, Referencia?, ConfirmaCierre | Confirmar pago |
| `ActualizarTarifaRequest` | NuevoPrecio, Descripcion? | Cambiar tarifa |
