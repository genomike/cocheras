using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Entities;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class EventoSensorService : IEventoSensorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEstadoCocheraService _estadoCocheraService;
    private readonly ICajonService _cajonService;

    public EventoSensorService(
        IUnitOfWork unitOfWork,
        IEstadoCocheraService estadoCocheraService,
        ICajonService cajonService)
    {
        _unitOfWork = unitOfWork;
        _estadoCocheraService = estadoCocheraService;
        _cajonService = cajonService;
    }

    public async Task<EventoSensorDto> ProcesarMensajeAsync(MensajeSensorMqtt mensaje, string jsonOriginal, CancellationToken cancellationToken = default)
    {
        // Mapear el tipo de evento
        var tipoEvento = MapearTipoEvento(mensaje.evento);

        // Crear el registro del evento
        var evento = new EventoSensor
        {
            TipoEvento = tipoEvento,
            EventoOriginal = mensaje.evento,
            Detalle = mensaje.detalle,
            TimestampESP32 = mensaje.timestamp,
            EstadoCajon1 = mensaje.cajon1,
            EstadoCajon2 = mensaje.cajon2,
            CajonesLibres = mensaje.libres,
            CajonesOcupados = mensaje.ocupados,
            CocheraLlena = mensaje.lleno,
            JsonOriginal = jsonOriginal
        };

        await _unitOfWork.Eventos.AddAsync(evento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Actualizar estado de cajones según el evento
        bool cajon1Ocupado = mensaje.cajon1.Equals("OCUPADO", StringComparison.OrdinalIgnoreCase);
        bool cajon2Ocupado = mensaje.cajon2.Equals("OCUPADO", StringComparison.OrdinalIgnoreCase);

        await _cajonService.ActualizarEstadoAsync(1, cajon1Ocupado, cancellationToken);
        await _cajonService.ActualizarEstadoAsync(2, cajon2Ocupado, cancellationToken);

        // Actualizar estado general de cochera
        await _estadoCocheraService.ActualizarEstadoAsync(cajon1Ocupado, cajon2Ocupado, cancellationToken);

        return MapToDto(evento);
    }

    public async Task<IEnumerable<EventoSensorDto>> GetUltimosEventosAsync(int cantidad = 50, CancellationToken cancellationToken = default)
    {
        var eventos = await _unitOfWork.Eventos.GetUltimosEventosAsync(cantidad, cancellationToken);
        return eventos.Select(MapToDto);
    }

    public async Task<IEnumerable<EventoSensorDto>> GetEventosByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        var eventos = await _unitOfWork.Eventos.GetEventosByFechaAsync(desde, hasta, cancellationToken);
        return eventos.Select(MapToDto);
    }

    public async Task<EventoSensorDto?> GetUltimoEventoAsync(CancellationToken cancellationToken = default)
    {
        var evento = await _unitOfWork.Eventos.GetUltimoEventoAsync(cancellationToken);
        return evento == null ? null : MapToDto(evento);
    }

    private static TipoEvento MapearTipoEvento(string evento)
    {
        return evento.ToUpperInvariant() switch
        {
            "SISTEMA_INICIADO" => TipoEvento.SistemaIniciado,
            "MOVIMIENTO_ENTRADA" => TipoEvento.MovimientoEntrada,
            "MOVIMIENTO_ENTRADA_BLOQUEADO" => TipoEvento.MovimientoEntradaBloqueado,
            "VEHICULO_SALIO" => TipoEvento.VehiculoSalio,
            "CAJON_OCUPADO" => TipoEvento.CajonOcupado,
            "CAJON_LIBERADO" => TipoEvento.CajonLiberado,
            "PARPADEO_INICIADO" => TipoEvento.ParpadeoIniciado,
            "PARPADEO_TIMEOUT" => TipoEvento.ParpadeoTimeout,
            "COCHERA_LLENA" => TipoEvento.CocheraLlena,
            _ => TipoEvento.SistemaIniciado
        };
    }

    private static EventoSensorDto MapToDto(EventoSensor evento)
    {
        return new EventoSensorDto
        {
            Id = evento.Id,
            TipoEvento = evento.TipoEvento,
            EventoOriginal = evento.EventoOriginal,
            Detalle = evento.Detalle,
            TimestampESP32 = evento.TimestampESP32,
            EstadoCajon1 = evento.EstadoCajon1,
            EstadoCajon2 = evento.EstadoCajon2,
            CajonesLibres = evento.CajonesLibres,
            CajonesOcupados = evento.CajonesOcupados,
            CocheraLlena = evento.CocheraLlena,
            FechaCreacion = evento.FechaCreacion
        };
    }
}
