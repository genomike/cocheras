using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class CajonService : ICajonService
{
    private readonly IUnitOfWork _unitOfWork;

    public CajonService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CajonDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cajones = await _unitOfWork.Cajones.GetAllAsync(cancellationToken);
        return cajones.Select(c => new CajonDto
        {
            Id = c.Id,
            Numero = c.Numero,
            Estado = c.Estado,
            UltimoCambioEstado = c.UltimoCambioEstado
        });
    }

    public async Task<CajonDto?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default)
    {
        var cajon = await _unitOfWork.Cajones.GetByNumeroAsync(numero, cancellationToken);
        if (cajon == null) return null;

        return new CajonDto
        {
            Id = cajon.Id,
            Numero = cajon.Numero,
            Estado = cajon.Estado,
            UltimoCambioEstado = cajon.UltimoCambioEstado
        };
    }

    public async Task<IEnumerable<CajonDto>> GetCajonesLibresAsync(CancellationToken cancellationToken = default)
    {
        var cajones = await _unitOfWork.Cajones.GetCajonesLibresAsync(cancellationToken);
        return cajones.Select(c => new CajonDto
        {
            Id = c.Id,
            Numero = c.Numero,
            Estado = c.Estado,
            UltimoCambioEstado = c.UltimoCambioEstado
        });
    }

    public async Task ActualizarEstadoAsync(int numero, bool ocupado, CancellationToken cancellationToken = default)
    {
        var cajon = await _unitOfWork.Cajones.GetByNumeroAsync(numero, cancellationToken);
        if (cajon != null)
        {
            cajon.Estado = ocupado ? EstadoCajon.Ocupado : EstadoCajon.Libre;
            cajon.UltimoCambioEstado = DateTime.UtcNow;
            _unitOfWork.Cajones.Update(cajon);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ResetearEstadoAsync(CancellationToken cancellationToken = default)
    {
        var cajones = await _unitOfWork.Cajones.GetAllAsync(cancellationToken);
        foreach (var cajon in cajones)
        {
            cajon.Estado = EstadoCajon.Libre;
            cajon.UltimoCambioEstado = DateTime.UtcNow;
            _unitOfWork.Cajones.Update(cajon);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
