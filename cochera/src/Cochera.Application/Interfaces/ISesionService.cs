using Cochera.Application.DTOs;
using Cochera.Domain.Enums;

namespace Cochera.Application.Interfaces;

public interface ISesionService
{
    Task<SesionEstacionamientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesActivasAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesPendientesPagoAsync(CancellationToken cancellationToken = default);
    Task<SesionEstacionamientoDto?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<SesionEstacionamientoDto?> GetSesionPendientePagoByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SesionEstacionamientoDto>> GetSesionesByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
    
    // Iniciar sesión (Admin asigna cajón a usuario)
    Task<SesionEstacionamientoDto> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken cancellationToken = default);
    
    // Admin solicita cierre: cambia estado a PendientePago, notifica al usuario
    Task<SesionEstacionamientoDto> SolicitarCierreSesionAsync(int sesionId, CancellationToken cancellationToken = default);
    
    // Usuario confirma pago
    Task<SesionEstacionamientoDto> ConfirmarPagoUsuarioAsync(ConfirmacionPagoDto confirmacion, CancellationToken cancellationToken = default);
    
    // Admin cierra la sesión después de que usuario pagó
    Task<SesionEstacionamientoDto> CerrarSesionAsync(int sesionId, CancellationToken cancellationToken = default);
    
    // Método legacy (para compatibilidad)
    Task<SesionEstacionamientoDto> FinalizarSesionAsync(FinalizarSesionRequest request, CancellationToken cancellationToken = default);
    
    Task<decimal> CalcularMontoActualAsync(int sesionId, CancellationToken cancellationToken = default);
}
