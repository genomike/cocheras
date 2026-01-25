namespace Cochera.Domain.Entities;

public class Tarifa : BaseEntity
{
    public decimal PrecioPorMinuto { get; set; } = 8.0m; // 8 soles por minuto por defecto
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activa { get; set; } = true;
    public string? Descripcion { get; set; }
}
