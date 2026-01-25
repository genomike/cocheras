using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class EstadoCocheraService : IEstadoCocheraService
{
    private readonly IUnitOfWork _unitOfWork;

    public EstadoCocheraService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EstadoCocheraDto?> GetEstadoActualAsync(CancellationToken cancellationToken = default)
    {
        var estado = await _unitOfWork.EstadoCochera.GetEstadoActualAsync(cancellationToken);
        if (estado == null) return null;

        return new EstadoCocheraDto
        {
            Id = estado.Id,
            Cajon1Ocupado = estado.Cajon1Ocupado,
            Cajon2Ocupado = estado.Cajon2Ocupado,
            CajonesLibres = estado.CajonesLibres,
            CajonesOcupados = estado.CajonesOcupados,
            CocheraLlena = estado.CocheraLlena,
            UltimaActualizacion = estado.UltimaActualizacion
        };
    }

    public async Task ActualizarEstadoAsync(bool cajon1Ocupado, bool cajon2Ocupado, CancellationToken cancellationToken = default)
    {
        var estado = await _unitOfWork.EstadoCochera.GetEstadoActualAsync(cancellationToken);
        if (estado != null)
        {
            estado.Cajon1Ocupado = cajon1Ocupado;
            estado.Cajon2Ocupado = cajon2Ocupado;
            estado.CajonesOcupados = (cajon1Ocupado ? 1 : 0) + (cajon2Ocupado ? 1 : 0);
            estado.CajonesLibres = 2 - estado.CajonesOcupados;
            estado.CocheraLlena = estado.CajonesLibres == 0;
            estado.UltimaActualizacion = DateTime.UtcNow;
            
            _unitOfWork.EstadoCochera.Update(estado);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
