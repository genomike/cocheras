using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Entities;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class TarifaService : ITarifaService
{
    private readonly IUnitOfWork _unitOfWork;

    public TarifaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TarifaDto?> GetTarifaActivaAsync(CancellationToken cancellationToken = default)
    {
        var tarifa = await _unitOfWork.Tarifas.GetTarifaActivaAsync(cancellationToken);
        return tarifa == null ? null : MapToDto(tarifa);
    }

    public async Task<IEnumerable<TarifaDto>> GetHistorialTarifasAsync(CancellationToken cancellationToken = default)
    {
        var tarifas = await _unitOfWork.Tarifas.GetAllAsync(cancellationToken);
        return tarifas.OrderByDescending(t => t.FechaInicio).Select(MapToDto);
    }

    public async Task<TarifaDto> ActualizarTarifaAsync(ActualizarTarifaRequest request, CancellationToken cancellationToken = default)
    {
        // Desactivar tarifa actual
        var tarifaActual = await _unitOfWork.Tarifas.GetTarifaActivaAsync(cancellationToken);
        if (tarifaActual != null)
        {
            tarifaActual.Activa = false;
            tarifaActual.FechaFin = DateTime.UtcNow;
            _unitOfWork.Tarifas.Update(tarifaActual);
        }

        // Crear nueva tarifa
        var nuevaTarifa = new Tarifa
        {
            PrecioPorMinuto = request.NuevoPrecioPorMinuto,
            FechaInicio = DateTime.UtcNow,
            Activa = true,
            Descripcion = request.Descripcion ?? "Tarifa actualizada"
        };

        await _unitOfWork.Tarifas.AddAsync(nuevaTarifa, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(nuevaTarifa);
    }

    private static TarifaDto MapToDto(Tarifa tarifa)
    {
        return new TarifaDto
        {
            Id = tarifa.Id,
            PrecioPorMinuto = tarifa.PrecioPorMinuto,
            FechaInicio = tarifa.FechaInicio,
            FechaFin = tarifa.FechaFin,
            Activa = tarifa.Activa,
            Descripcion = tarifa.Descripcion
        };
    }
}
