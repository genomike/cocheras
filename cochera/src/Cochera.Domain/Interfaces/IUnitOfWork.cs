namespace Cochera.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUsuarioRepository Usuarios { get; }
    ICajonRepository Cajones { get; }
    ISesionEstacionamientoRepository Sesiones { get; }
    IEventoSensorRepository Eventos { get; }
    ITarifaRepository Tarifas { get; }
    IEstadoCocheraRepository EstadoCochera { get; }
    IPagoRepository Pagos { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
