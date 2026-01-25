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

    public async Task<SesionEstacionamientoDto?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetSesionActivaByUsuarioAsync(usuarioId, cancellationToken);
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

        // Verificar que el usuario no tenga una sesión activa
        var sesionActiva = await _unitOfWork.Sesiones.GetSesionActivaByUsuarioAsync(request.UsuarioId, cancellationToken);
        if (sesionActiva != null)
            throw new InvalidOperationException("El usuario ya tiene una sesión activa");

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

    public async Task<SesionEstacionamientoDto> FinalizarSesionAsync(FinalizarSesionRequest request, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetWithPagoAsync(request.SesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new InvalidOperationException("La sesión no está activa");

        // Calcular tiempo y monto
        sesion.HoraSalida = DateTime.UtcNow;
        sesion.MinutosEstacionado = (int)Math.Ceiling((sesion.HoraSalida.Value - sesion.HoraEntrada).TotalMinutes);
        sesion.MontoTotal = sesion.MinutosEstacionado * sesion.TarifaPorMinuto;
        sesion.Estado = EstadoSesion.Finalizada;

        // Crear pago
        var pago = new Pago
        {
            SesionId = sesion.Id,
            Monto = sesion.MontoTotal,
            MetodoPago = request.MetodoPago,
            FechaPago = DateTime.UtcNow,
            Referencia = $"PAGO-{DateTime.UtcNow:yyyyMMddHHmmss}-{sesion.Id}"
        };

        await _unitOfWork.Pagos.AddAsync(pago, cancellationToken);

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

        sesion.Pago = pago;
        return MapToDto(sesion);
    }

    public async Task<decimal> CalcularMontoActualAsync(int sesionId, CancellationToken cancellationToken = default)
    {
        var sesion = await _unitOfWork.Sesiones.GetByIdAsync(sesionId, cancellationToken)
            ?? throw new InvalidOperationException("Sesión no encontrada");

        if (sesion.Estado != EstadoSesion.Activa)
            return sesion.MontoTotal;

        var minutos = (int)Math.Ceiling((DateTime.UtcNow - sesion.HoraEntrada).TotalMinutes);
        return minutos * sesion.TarifaPorMinuto;
    }

    private static SesionEstacionamientoDto MapToDto(SesionEstacionamiento sesion)
    {
        return new SesionEstacionamientoDto
        {
            Id = sesion.Id,
            UsuarioId = sesion.UsuarioId,
            UsuarioNombre = sesion.Usuario?.Nombre ?? string.Empty,
            CajonId = sesion.CajonId,
            CajonNumero = sesion.Cajon?.Numero ?? 0,
            HoraEntrada = sesion.HoraEntrada,
            HoraSalida = sesion.HoraSalida,
            MinutosEstacionado = sesion.Estado == EstadoSesion.Activa 
                ? (int)Math.Ceiling((DateTime.UtcNow - sesion.HoraEntrada).TotalMinutes)
                : sesion.MinutosEstacionado,
            TarifaPorMinuto = sesion.TarifaPorMinuto,
            MontoTotal = sesion.Estado == EstadoSesion.Activa
                ? (int)Math.Ceiling((DateTime.UtcNow - sesion.HoraEntrada).TotalMinutes) * sesion.TarifaPorMinuto
                : sesion.MontoTotal,
            Estado = sesion.Estado,
            TienePago = sesion.Pago != null
        };
    }
}
