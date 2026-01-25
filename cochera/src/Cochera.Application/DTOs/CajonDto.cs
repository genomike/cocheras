using Cochera.Domain.Enums;

namespace Cochera.Application.DTOs;

public class CajonDto
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public EstadoCajon Estado { get; set; }
    public string EstadoTexto => Estado == EstadoCajon.Ocupado ? "Ocupado" : "Libre";
    public DateTime? UltimoCambioEstado { get; set; }
}
