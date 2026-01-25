using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEstadoCocheraService _estadoCocheraService;
    private readonly IEventoSensorService _eventoSensorService;
    private readonly ISesionService _sesionService;

    public DashboardService(
        IUnitOfWork unitOfWork,
        IEstadoCocheraService estadoCocheraService,
        IEventoSensorService eventoSensorService,
        ISesionService sesionService)
    {
        _unitOfWork = unitOfWork;
        _estadoCocheraService = estadoCocheraService;
        _eventoSensorService = eventoSensorService;
        _sesionService = sesionService;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finHoy = DateTime.SpecifyKind(hoy.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        var finMes = DateTime.SpecifyKind(inicioMes.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
        var hoyUtc = DateTime.SpecifyKind(hoy, DateTimeKind.Utc);

        var estado = await _estadoCocheraService.GetEstadoActualAsync(cancellationToken);
        var ultimosEventos = await _eventoSensorService.GetUltimosEventosAsync(10, cancellationToken);
        var sesionesActivas = await _sesionService.GetSesionesActivasAsync(cancellationToken);
        var sesionesHoy = await _sesionService.GetSesionesByFechaAsync(hoyUtc, finHoy, cancellationToken);
        
        var recaudacionHoy = await _unitOfWork.Pagos.GetTotalRecaudadoAsync(hoyUtc, finHoy, cancellationToken);
        var recaudacionMes = await _unitOfWork.Pagos.GetTotalRecaudadoAsync(inicioMes, finMes, cancellationToken);

        var eventosHoy = await _unitOfWork.Eventos.GetEventosByFechaAsync(hoyUtc, finHoy, cancellationToken);

        var usoPorHora = await GetUsoPorHoraAsync(hoyUtc, cancellationToken);
        var usoPorDia = await GetUsoPorDiaAsync(inicioMes, finMes, cancellationToken);

        return new DashboardDto
        {
            EstadoActual = estado ?? new EstadoCocheraDto(),
            TotalSesionesHoy = sesionesHoy.Count(),
            RecaudacionHoy = recaudacionHoy,
            RecaudacionMes = recaudacionMes,
            TotalEventosHoy = eventosHoy.Count(),
            UltimosEventos = ultimosEventos.ToList(),
            SesionesActivas = sesionesActivas.ToList(),
            UsoPorHora = usoPorHora.ToList(),
            UsoPorDia = usoPorDia.ToList()
        };
    }

    public async Task<IEnumerable<ResumenPorHora>> GetUsoPorHoraAsync(DateTime fecha, CancellationToken cancellationToken = default)
    {
        var inicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
        var fin = DateTime.SpecifyKind(inicio.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        
        var sesiones = await _unitOfWork.Sesiones.GetSesionesByFechaAsync(inicio, fin, cancellationToken);
        var pagos = await _unitOfWork.Pagos.GetPagosByFechaAsync(inicio, fin, cancellationToken);

        var resultado = new List<ResumenPorHora>();
        for (int hora = 0; hora < 24; hora++)
        {
            var sesionesHora = sesiones.Count(s => s.HoraEntrada.Hour == hora);
            var recaudacionHora = pagos.Where(p => p.FechaPago.Hour == hora).Sum(p => p.Monto);
            
            resultado.Add(new ResumenPorHora
            {
                Hora = hora,
                CantidadSesiones = sesionesHora,
                Recaudacion = recaudacionHora
            });
        }

        return resultado;
    }

    public async Task<IEnumerable<ResumenPorDia>> GetUsoPorDiaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        var desdeUtc = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        
        var sesiones = await _unitOfWork.Sesiones.GetSesionesByFechaAsync(desdeUtc, hastaUtc, cancellationToken);
        var pagos = await _unitOfWork.Pagos.GetPagosByFechaAsync(desdeUtc, hastaUtc, cancellationToken);

        var resultado = new List<ResumenPorDia>();
        var fechaActual = desdeUtc.Date;
        
        while (fechaActual <= hasta.Date)
        {
            var sesionesDia = sesiones.Count(s => s.HoraEntrada.Date == fechaActual);
            var recaudacionDia = pagos.Where(p => p.FechaPago.Date == fechaActual).Sum(p => p.Monto);
            
            resultado.Add(new ResumenPorDia
            {
                Fecha = DateTime.SpecifyKind(fechaActual, DateTimeKind.Utc),
                CantidadSesiones = sesionesDia,
                Recaudacion = recaudacionDia
            });
            
            fechaActual = fechaActual.AddDays(1);
        }

        return resultado;
    }
}
