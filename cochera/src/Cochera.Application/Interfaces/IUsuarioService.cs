using Cochera.Application.DTOs;

namespace Cochera.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsuarioDto?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<IEnumerable<UsuarioDto>> GetUsuariosNoAdminAsync(CancellationToken cancellationToken = default);
}
