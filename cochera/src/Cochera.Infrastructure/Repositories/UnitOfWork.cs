using Cochera.Domain.Interfaces;
using Cochera.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cochera.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CocheraDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUsuarioRepository? _usuarios;
    private ICajonRepository? _cajones;
    private ISesionEstacionamientoRepository? _sesiones;
    private IEventoSensorRepository? _eventos;
    private ITarifaRepository? _tarifas;
    private IEstadoCocheraRepository? _estadoCochera;
    private IPagoRepository? _pagos;

    public UnitOfWork(CocheraDbContext context)
    {
        _context = context;
    }

    public IUsuarioRepository Usuarios => _usuarios ??= new UsuarioRepository(_context);
    public ICajonRepository Cajones => _cajones ??= new CajonRepository(_context);
    public ISesionEstacionamientoRepository Sesiones => _sesiones ??= new SesionEstacionamientoRepository(_context);
    public IEventoSensorRepository Eventos => _eventos ??= new EventoSensorRepository(_context);
    public ITarifaRepository Tarifas => _tarifas ??= new TarifaRepository(_context);
    public IEstadoCocheraRepository EstadoCochera => _estadoCochera ??= new EstadoCocheraRepository(_context);
    public IPagoRepository Pagos => _pagos ??= new PagoRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
