using Cochera.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cochera.Infrastructure.Data;

public class CocheraDbContext : DbContext
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
