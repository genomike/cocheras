namespace Cochera.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty; // admin, usuario_1, usuario_2, usuario_3
    public bool EsAdmin { get; set; }
    
    // Navigation
    public virtual ICollection<SesionEstacionamiento> Sesiones { get; set; } = new List<SesionEstacionamiento>();
}
