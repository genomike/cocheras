namespace Cochera.Application.DTOs;

public class EstadoCocheraDto
{
    public int Id { get; set; }
    public bool Cajon1Ocupado { get; set; }
    public bool Cajon2Ocupado { get; set; }
    public int CajonesLibres { get; set; }
    public int CajonesOcupados { get; set; }
    public bool CocheraLlena { get; set; }
    public DateTime UltimaActualizacion { get; set; }
    
    public string EstadoGeneral => CocheraLlena ? "LLENO" : $"{CajonesLibres} disponible(s)";
}
