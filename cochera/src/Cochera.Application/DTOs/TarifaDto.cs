namespace Cochera.Application.DTOs;

public class TarifaDto
{
    public int Id { get; set; }
    public decimal PrecioPorMinuto { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activa { get; set; }
    public string? Descripcion { get; set; }
}

public class ActualizarTarifaRequest
{
    public decimal NuevoPrecioPorMinuto { get; set; }
    public string? Descripcion { get; set; }
}
