using Cochera.Domain.Enums;

namespace Cochera.Domain.Entities;

public class SesionEstacionamiento : BaseEntity
{
    public int UsuarioId { get; set; }
    public int CajonId { get; set; }
    public DateTime HoraEntrada { get; set; }
    public DateTime? HoraSalida { get; set; }
    public int MinutosEstacionado { get; set; }
    public decimal TarifaPorMinuto { get; set; }
    public decimal MontoTotal { get; set; }
    public EstadoSesion Estado { get; set; } = EstadoSesion.Activa;
    
    // Navigation
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Cajon Cajon { get; set; } = null!;
    public virtual Pago? Pago { get; set; }
}
