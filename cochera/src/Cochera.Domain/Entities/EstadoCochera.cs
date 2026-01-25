namespace Cochera.Domain.Entities;

public class EstadoCochera : BaseEntity
{
    public bool Cajon1Ocupado { get; set; }
    public bool Cajon2Ocupado { get; set; }
    public int CajonesLibres { get; set; } = 2;
    public int CajonesOcupados { get; set; } = 0;
    public bool CocheraLlena { get; set; } = false;
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;
}
