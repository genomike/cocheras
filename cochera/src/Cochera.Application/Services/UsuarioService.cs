using Cochera.Application.DTOs;
using Cochera.Application.Interfaces;
using Cochera.Domain.Interfaces;

namespace Cochera.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UsuarioDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _unitOfWork.Usuarios.GetAllAsync(cancellationToken);
        return usuarios.Select(u => new UsuarioDto
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Codigo = u.Codigo,
            EsAdmin = u.EsAdmin
        });
    }

    public async Task<UsuarioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id, cancellationToken);
        if (usuario == null) return null;

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Codigo = usuario.Codigo,
            EsAdmin = usuario.EsAdmin
        };
    }

    public async Task<UsuarioDto?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var usuario = await _unitOfWork.Usuarios.GetByCodigoAsync(codigo, cancellationToken);
        if (usuario == null) return null;

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Codigo = usuario.Codigo,
            EsAdmin = usuario.EsAdmin
        };
    }

    public async Task<IEnumerable<UsuarioDto>> GetUsuariosNoAdminAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _unitOfWork.Usuarios.GetUsuariosNoAdminAsync(cancellationToken);
        return usuarios.Select(u => new UsuarioDto
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Codigo = u.Codigo,
            EsAdmin = u.EsAdmin
        });
    }
}
