using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Entities;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class SesionService : ISesionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SesionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SesionEstacionamientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(id, cancellationToken);
        return sesion == null ? null : MapToDto(sesion);
    }

    public async Task<IEnumerable<SesionEstacionamientoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sesiones = await _unitOfWork.Sesiones.GetAllAsync(cancellationToken);
        return sesiones.Select(MapToDto);
    }

    public async Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesActivasAsync(CancellationToken cancellationToken = default)
    {
        var sesiones = await _unitOfWork.Sesiones.GetSesionesActivasAsync(cancellationToken);
        return sesiones.Select(MapToDto);
    }

    public async Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesPendientesPagoAsync(CancellationToken cancellationToken = default)
    {
        var sesiones = await _unitOfWork.Sesiones.GetSesionesPendientesPagoAsync(cancellationToken);
        return sesiones.Select(MapToDto);
    }

    public async Task<SesionEstacionamientoDto?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetSesionActivaByUsuarioAsync(usuarioId, cancellationToken);
        return sesion == null ? null : MapToDto(sesion);
    }

    public async Task<SesionEstacionamientoDto?> GetSesionPendientePagoByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetSesionPendientePagoByUsuarioAsync(usuarioId, cancellationToken);
        return sesion == null ? null : MapToDto(sesion);
    }

    public async Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sesiones = await _unitOfWork.Sesiones.GetSesionesByUsuarioAsync(usuarioId, cancellationToken);
        return sesiones.Select(MapToDto);
    }

    public async Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        var sesiones = await _unitOfWork.Sesiones.GetSesionesByFechaAsync(desde, hasta, cancellationToken);
        return sesiones.Select(MapToDto);
    }

    public async Task<SesionEstacionamientoDto> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken cancellationToken = default)
    {
        // Verificar que el usuario existe
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(request.UsuarioId, cancellationToken)
            ?? throw new InvalidOperationException("Usuario no encontrado");

        // Verificar que el cajón existe y está libre
        var cajon = await _unitOfWork.Cajones.GetByIdAsync(request.CajonId, cancellationToken)
            ?? throw new InvalidOperationException("Cajón no encontrado");

        if (cajon.Estado == EstadoCajon.Ocupado)
            throw new InvalidOperationException("El cajón ya está ocupado");

        // Verificar que el usuario no tenga una sesión activa o pendiente de pago
        var sesionActiva = await _unitOfWork.Sesiones.GetSesionActivaByUsuarioAsync(request.UsuarioId, cancellationToken);
        if (sesionActiva != null)
            throw new InvalidOperationException("El usuario ya tiene una sesión activa");

        var sesionPendiente = await _unitOfWork.Sesiones.GetSesionPendientePagoByUsuarioAsync(request.UsuarioId, cancellationToken);
        if (sesionPendiente != null)
            throw new InvalidOperationException("El usuario tiene una sesión pendiente de pago");

        // Obtener tarifa activa
        var tarifa = await _unitOfWork.Tarifas.GetTarifaActivaAsync(cancellationToken)
            ?? throw new InvalidOperationException("No hay tarifa activa configurada");

        // Crear sesión
        var sesion = new SesionEstacionamiento
        {
            UsuarioId = request.UsuarioId,
            CajonId = request.CajonId,
            HoraEntrada = DateTime.UtcNow,
            TarifaPorMinuto = tarifa.PrecioPorMinuto,
            Estado = EstadoSesion.Activa
        };

        await _unitOfWork.Sesiones.AddAsync(sesion, cancellationToken);

        // Actualizar estado del cajón
        cajon.Estado = EstadoCajon.Ocupado;
        cajon.UltimoCambioEstado = DateTime.UtcNow;
        _unitOfWork.Cajones.Update(cajon);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        sesion.Usuario = usuario;
        sesion.Cajon = cajon;

        return MapToDto(sesion);
    }

    /// <summary>
    /// Admin solicita cierre de sesión: cambia estado a PendientePago
    /// El usuario recibirá notificación para proceder al pago
    /// </summary>
    public async Task<SesionEstacionamientoDto> SolicitarCierreSesionAsync(int sesionId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(sesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new InvalidOperationException("Solo se puede solicitar cierre de sesiones activas");

        // Calcular tiempo y monto al momento de solicitar cierre
        sesion.HoraSalida = DateTime.UtcNow;
        var diferencia = sesion.HoraSalida.Value - sesion.HoraEntrada;
        sesion.MinutosEstacionado = (int)diferencia.TotalMinutes;
        if (diferencia.Seconds > 0 || diferencia.Milliseconds > 0)
            sesion.MinutosEstacionado++; // Fracción cuenta como minuto completo
        sesion.MinutosEstacionado = Math.Max(1, sesion.MinutosEstacionado); // Mínimo 1 minuto
        
        sesion.MontoTotal = sesion.MinutosEstacionado * sesion.TarifaPorMinuto;
        sesion.Estado = EstadoSesion.PendientePago;

        _unitOfWork.Sesiones.Update(sesion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(sesion);
    }

    /// <summary>
    /// Usuario confirma que realizó el pago
    /// </summary>
    public async Task<SesionEstacionamientoDto> ConfirmarPagoUsuarioAsync(ConfirmacionPagoDto confirmacion, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(confirmacion.SesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.PendientePago)
            throw new InvalidOperationException("La sesión no está pendiente de pago");

        if (sesion.UsuarioId != confirmacion.UsuarioId)
            throw new InvalidOperationException("El usuario no coincide con la sesión");

        // Crear registro de pago
        var pago = new Pago
        {
            SesionId = sesion.Id,
            Monto = sesion.MontoTotal,
            MetodoPago = confirmacion.MetodoPago,
            FechaPago = DateTime.UtcNow,
            Referencia = $"PAGO-{DateTime.UtcNow:yyyyMMddHHmmss}-{sesion.Id}"
        };

        await _unitOfWork.Pagos.AddAsync(pago, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        sesion.Pago = pago;
        return MapToDto(sesion);
    }

    /// <summary>
    /// Admin cierra la sesión después de que el usuario pagó
    /// Libera el cajón y finaliza la sesión
    /// </summary>
    public async Task<SesionEstacionamientoDto> CerrarSesionAsync(int sesionId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(sesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.PendientePago)
            throw new InvalidOperationException("Solo se pueden cerrar sesiones pendientes de pago");

        if (sesion.Pago == null)
            throw new InvalidOperationException("El usuario aún no ha confirmado el pago");

        // Actualizar estado del cajón
        var cajon = await _unitOfWork.Cajones.GetByIdAsync(sesion.CajonId, cancellationToken);
        if (cajon != null)
        {
            cajon.Estado = EstadoCajon.Libre;
            cajon.UltimoCambioEstado = DateTime.UtcNow;
            _unitOfWork.Cajones.Update(cajon);
        }

        sesion.Estado = EstadoSesion.Finalizada;
        _unitOfWork.Sesiones.Update(sesion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(sesion);
    }

    // Método legacy para compatibilidad (proceso directo sin flujo de eventos)
    public async Task<SesionEstacionamientoDto> FinalizarSesionAsync(FinalizarSesionRequest request, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(request.SesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.Activa && sesion.Estado != EstadoSesion.PendientePago)
            throw new InvalidOperationException("La sesión no puede ser finalizada");

        // Calcular tiempo y monto
        sesion.HoraSalida = DateTime.UtcNow;
        var diferencia = sesion.HoraSalida.Value - sesion.HoraEntrada;
        sesion.MinutosEstacionado = (int)diferencia.TotalMinutes;
        if (diferencia.Seconds > 0 || diferencia.Milliseconds > 0)
            sesion.MinutosEstacionado++;
        sesion.MinutosEstacionado = Math.Max(1, sesion.MinutosEstacionado);
        
        sesion.MontoTotal = sesion.MinutosEstacionado * sesion.TarifaPorMinuto;
        sesion.Estado = EstadoSesion.Finalizada;

        // Crear pago si no existe
        if (sesion.Pago == null)
        {
            var pago = new Pago
            {
                SesionId = sesion.Id,
                Monto = sesion.MontoTotal,
                MetodoPago = request.MetodoPago,
                FechaPago = DateTime.UtcNow,
                Referencia = $"PAGO-{DateTime.UtcNow:yyyyMMddHHmmss}-{sesion.Id}"
            };
            await _unitOfWork.Pagos.AddAsync(pago, cancellationToken);
            sesion.Pago = pago;
        }

        // Actualizar estado del cajón
        var cajon = await _unitOfWork.Cajones.GetByIdAsync(sesion.CajonId, cancellationToken);
        if (cajon != null)
        {
            cajon.Estado = EstadoCajon.Libre;
            cajon.UltimoCambioEstado = DateTime.UtcNow;
            _unitOfWork.Cajones.Update(cajon);
        }

        _unitOfWork.Sesiones.Update(sesion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(sesion);
    }

    public async Task<decimal> CalcularMontoActualAsync(int sesionId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetByIdAsync(sesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado == EstadoSesion.Finalizada)
            return sesion.MontoTotal;

        var diferencia = DateTime.UtcNow - sesion.HoraEntrada;
        var minutos = (int)diferencia.TotalMinutes;
        if (diferencia.Seconds > 0 || diferencia.Milliseconds > 0)
            minutos++;
        minutos = Math.Max(1, minutos);
        
        return minutos * sesion.TarifaPorMinuto;
    }

    private static SesionEstacionamientoDto MapToDto(SesionEstacionamiento sesion)
    {
        return new SesionEstacionamientoDto
        {
            Id = sesion.Id,
            UsuarioId = sesion.UsuarioId,
            UsuarioNombre = sesion.Usuario?.Nombre ?? string.Empty,
            UsuarioCodigo = sesion.Usuario?.Codigo ?? string.Empty,
            CajonId = sesion.CajonId,
            CajonNumero = sesion.Cajon?.Numero ?? 0,
            HoraEntrada = sesion.HoraEntrada,
            HoraSalida = sesion.HoraSalida,
            TarifaPorMinuto = sesion.TarifaPorMinuto,
            Estado = sesion.Estado,
            TienePago = sesion.Pago != null,
            FechaPago = sesion.Pago?.FechaPago,
            MetodoPago = sesion.Pago?.MetodoPago
        };
    }
}
