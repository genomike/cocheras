using Cochera.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Data;

public class CocheraDbContext : IdentityDbContext<IdentityUser>
{
    public CocheraDbContext(DbContextOptions<CocheraDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cajon> Cajones => Set<Cajon>();
    public DbSet<SesionEstacionamiento> Sesiones => Set<SesionEstacionamiento>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<EventoSensor> Eventos => Set<EventoSensor>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<EstadoCochera> EstadosCochera => Set<EstadoCochera>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        // Cajon
        modelBuilder.Entity<Cajon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Numero).IsRequired();
            entity.HasIndex(e => e.Numero).IsUnique();
        });

        // SesionEstacionamiento
        modelBuilder.Entity<SesionEstacionamiento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TarifaPorMinuto).HasPrecision(18, 2);
            entity.Property(e => e.MontoTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Sesiones)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Cajon)
                .WithMany(c => c.Sesiones)
                .HasForeignKey(e => e.CajonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Pago
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasPrecision(18, 2);
            entity.Property(e => e.Referencia).HasMaxLength(100);

            entity.HasOne(e => e.Sesion)
                .WithOne(s => s.Pago)
                .HasForeignKey<Pago>(e => e.SesionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EventoSensor
        modelBuilder.Entity<EventoSensor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventoOriginal).HasMaxLength(100);
            entity.Property(e => e.Detalle).HasMaxLength(500);
            entity.Property(e => e.TimestampESP32).HasMaxLength(50);
            entity.Property(e => e.EstadoCajon1).HasMaxLength(20);
            entity.Property(e => e.EstadoCajon2).HasMaxLength(20);
            entity.Property(e => e.JsonOriginal).HasColumnType("text");
        });

        // Tarifa
        modelBuilder.Entity<Tarifa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioPorMinuto).HasPrecision(18, 2);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
        });

        // EstadoCochera
        modelBuilder.Entity<EstadoCochera>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Fecha fija para seed data (evita problemas con migraciones)
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var adminRoleId = "f7a1e7bf-3027-4e58-bb9d-3d95539b06b1";
        var userRoleId = "228f9a8f-5b1b-45c0-8b2e-c735e3e2f1df";

        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "3f748c6f-89ef-44a4-a5df-b6e11c3d6699"
            },
            new IdentityRole
            {
                Id = userRoleId,
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "9cd40291-2524-4a53-844d-dfb89d897eb3"
            }
        );

        var passwordHasher = new PasswordHasher<IdentityUser>();

        var adminIdentityUser = new IdentityUser
        {
            Id = "f20c5015-e40a-4a96-b9eb-715ea40e861b",
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@cochera.local",
            NormalizedEmail = "ADMIN@COCHERA.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = "ba649f7f-6875-4542-94f4-5fb5052e54a8",
            ConcurrencyStamp = "af39c7f3-dd2c-4d91-a660-07ad4ec7b410",
            LockoutEnabled = false
        };
        adminIdentityUser.PasswordHash = passwordHasher.HashPassword(adminIdentityUser, "Admin12345");

        var user1IdentityUser = new IdentityUser
        {
            Id = "53014967-9f8a-4c35-a315-ae5c8a4c47fd",
            UserName = "usuario_1",
            NormalizedUserName = "USUARIO_1",
            Email = "usuario_1@cochera.local",
            NormalizedEmail = "USUARIO_1@COCHERA.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = "d17a34f4-2bfd-40be-adba-f53e8b0f5a9c",
            ConcurrencyStamp = "de040aa3-772f-4f14-9ff1-1a7fd51f2413",
            LockoutEnabled = false
        };
        user1IdentityUser.PasswordHash = passwordHasher.HashPassword(user1IdentityUser, "Usuario12345");

        var user2IdentityUser = new IdentityUser
        {
            Id = "e5b8e407-9928-4e9e-a3ef-587f1b055fe6",
            UserName = "usuario_2",
            NormalizedUserName = "USUARIO_2",
            Email = "usuario_2@cochera.local",
            NormalizedEmail = "USUARIO_2@COCHERA.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = "41fef75e-01fd-49e5-a467-2868cb131404",
            ConcurrencyStamp = "1ad2c9bc-3fd9-4f13-b65a-2a95ecf5e8f7",
            LockoutEnabled = false
        };
        user2IdentityUser.PasswordHash = passwordHasher.HashPassword(user2IdentityUser, "Usuario12345");

        var user3IdentityUser = new IdentityUser
        {
            Id = "49f95ed4-6ba5-4d36-9f20-8958410839a0",
            UserName = "usuario_3",
            NormalizedUserName = "USUARIO_3",
            Email = "usuario_3@cochera.local",
            NormalizedEmail = "USUARIO_3@COCHERA.LOCAL",
            EmailConfirmed = true,
            SecurityStamp = "43981f52-dc35-4cf1-8702-a034ea7e7f53",
            ConcurrencyStamp = "6f6d5f68-7a58-4c37-baf4-0ea61f60fc00",
            LockoutEnabled = false
        };
        user3IdentityUser.PasswordHash = passwordHasher.HashPassword(user3IdentityUser, "Usuario12345");

        modelBuilder.Entity<IdentityUser>().HasData(
            adminIdentityUser,
            user1IdentityUser,
            user2IdentityUser,
            user3IdentityUser
        );

        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { UserId = adminIdentityUser.Id, RoleId = adminRoleId },
            new IdentityUserRole<string> { UserId = user1IdentityUser.Id, RoleId = userRoleId },
            new IdentityUserRole<string> { UserId = user2IdentityUser.Id, RoleId = userRoleId },
            new IdentityUserRole<string> { UserId = user3IdentityUser.Id, RoleId = userRoleId }
        );
        
        // Usuarios fijos
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario { Id = 1, Nombre = "Administrador", Codigo = "admin", EsAdmin = true, FechaCreacion = seedDate },
            new Usuario { Id = 2, Nombre = "Usuario 1", Codigo = "usuario_1", EsAdmin = false, FechaCreacion = seedDate },
            new Usuario { Id = 3, Nombre = "Usuario 2", Codigo = "usuario_2", EsAdmin = false, FechaCreacion = seedDate },
            new Usuario { Id = 4, Nombre = "Usuario 3", Codigo = "usuario_3", EsAdmin = false, FechaCreacion = seedDate }
        );

        // Cajones
        modelBuilder.Entity<Cajon>().HasData(
            new Cajon { Id = 1, Numero = 1, Estado = Domain.Enums.EstadoCajon.Libre, FechaCreacion = seedDate },
            new Cajon { Id = 2, Numero = 2, Estado = Domain.Enums.EstadoCajon.Libre, FechaCreacion = seedDate }
        );

        // Tarifa inicial (8 soles por minuto)
        modelBuilder.Entity<Tarifa>().HasData(
            new Tarifa { Id = 1, PrecioPorMinuto = 8.0m, FechaInicio = seedDate, Activa = true, Descripcion = "Tarifa estándar", FechaCreacion = seedDate }
        );

        // Estado inicial de cochera
        modelBuilder.Entity<EstadoCochera>().HasData(
            new EstadoCochera { Id = 1, Cajon1Ocupado = false, Cajon2Ocupado = false, CajonesLibres = 2, CajonesOcupados = 0, CocheraLlena = false, UltimaActualizacion = seedDate, FechaCreacion = seedDate }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.FechaCreacion = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.FechaActualizacion = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
