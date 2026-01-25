using Cochera.Domain.Entities;
using Cochera.Domain.Enums;
using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Repositories;

public class SesionEstacionamientoRepository : Repository<SesionEstacionamiento>, ISesionEstacionamientoRepository
{
    public SesionEstacionamientoRepository(CocheraDbContext context) : base(context)
    {
    }

    public async Task<SesionEstacionamiento?> GetSesionActivaByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Usuario)
            .Include(s => s.Cajon)
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Estado == EstadoSesion.Activa, cancellationToken);
    }

    public async Task<SesionEstacionamiento?> GetSesionActivaByCajonAsync(int cajonId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Usuario)
            .Include(s => s.Cajon)
            .FirstOrDefaultAsync(s => s.CajonId == cajonId && s.Estado == EstadoSesion.Activa, cancellationToken);
    }

    public async Task<IEnumerable<SesionEstacionamiento>> GetSesionesActivasAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Usuario)
            .Include(s => s.Cajon)
            .Where(s => s.Estado == EstadoSesion.Activa)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SesionEstacionamiento>> GetSesionesByUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Cajon)
            .Include(s => s.Pago)
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.HoraEntrada)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SesionEstacionamiento>> GetSesionesByFechaAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Usuario)
            .Include(s => s.Cajon)
            .Include(s => s.Pago)
            .Where(s => s.HoraEntrada >= desde && s.HoraEntrada <= hasta)
            .OrderByDescending(s => s.HoraEntrada)
            .ToListAsync(cancellationToken);
    }

    public async Task<SesionEstacionamiento?> GetWithPagoAsync(int sesionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Usuario)
            .Include(s => s.Cajon)
            .Include(s => s.Pago)
            .FirstOrDefaultAsync(s => s.Id == sesionId, cancellationToken);
    }
}
