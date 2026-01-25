using Cochera.Domain.Enums;

namespace Cochera.Domain.Entities;

public class Pago : BaseEntity
{
    public int SesionId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTime FechaPago { get; set; }
    public string? Referencia { get; set; }
    
    // Navigation
    public virtual SesionEstacionamiento Sesion { get; set; } = null!;
}
