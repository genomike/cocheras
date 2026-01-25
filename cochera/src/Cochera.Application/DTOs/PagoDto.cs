using Cochera.Domain.Enums;

namespace Cochera.Application.DTOs;

public class PagoDto
{
    public int Id { get; set; }
    public int SesionId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string MetodoPagoTexto => MetodoPago switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        _ => "Desconocido"
    };
    public DateTime FechaPago { get; set; }
    public string? Referencia { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
}
