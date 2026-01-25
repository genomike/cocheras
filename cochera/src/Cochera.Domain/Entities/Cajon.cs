using Cochera.Domain.Enums;

namespace Cochera.Domain.Entities;

public class Cajon : BaseEntity
{
    public int Numero { get; set; } // 1 o 2
    public EstadoCajon Estado { get; set; } = EstadoCajon.Libre;
    public DateTime? UltimoCambioEstado { get; set; }
    
    // Navigation
    public virtual ICollection<SesionEstacionamiento> Sesiones { get; set; } = new List<SesionEstacionamiento>();
}
