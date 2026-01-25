using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cochera.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cajones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    UltimoCambioEstado = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstadosCochera",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cajon1Ocupado = table.Column<bool>(type: "boolean", nullable: false),
                    Cajon2Ocupado = table.Column<bool>(type: "boolean", nullable: false),
                    CajonesLibres = table.Column<int>(type: "integer", nullable: false),
                    CajonesOcupados = table.Column<int>(type: "integer", nullable: false),
                    CocheraLlena = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosCochera", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoEvento = table.Column<int>(type: "integer", nullable: false),
                    EventoOriginal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TimestampESP32 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstadoCajon1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstadoCajon2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CajonesLibres = table.Column<int>(type: "integer", nullable: false),
                    CajonesOcupados = table.Column<int>(type: "integer", nullable: false),
                    CocheraLlena = table.Column<bool>(type: "boolean", nullable: false),
                    JsonOriginal = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tarifas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PrecioPorMinuto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarifas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sesiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CajonId = table.Column<int>(type: "integer", nullable: false),
                    HoraEntrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HoraSalida = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MinutosEstacionado = table.Column<int>(type: "integer", nullable: false),
                    TarifaPorMinuto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sesiones_Cajones_CajonId",
                        column: x => x.CajonId,
                        principalTable: "Cajones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sesiones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SesionId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<int>(type: "integer", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Sesiones_SesionId",
                        column: x => x.SesionId,
                        principalTable: "Sesiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Cajones",
                columns: new[] { "Id", "Estado", "FechaActualizacion", "FechaCreacion", "Numero", "UltimoCambioEstado" },
                values: new object[,]
                {
                    { 1, 0, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null },
                    { 2, 0, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null }
                });

            migrationBuilder.InsertData(
                table: "EstadosCochera",
                columns: new[] { "Id", "Cajon1Ocupado", "Cajon2Ocupado", "CajonesLibres", "CajonesOcupados", "CocheraLlena", "FechaActualizacion", "FechaCreacion", "UltimaActualizacion" },
                values: new object[] { 1, false, false, 2, 0, false, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Tarifas",
                columns: new[] { "Id", "Activa", "Descripcion", "FechaActualizacion", "FechaCreacion", "FechaFin", "FechaInicio", "PrecioPorMinuto" },
                values: new object[] { 1, true, "Tarifa estándar", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8.0m });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Codigo", "EsAdmin", "FechaActualizacion", "FechaCreacion", "Nombre" },
                values: new object[,]
                {
                    { 1, "admin", true, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador" },
                    { 2, "usuario_1", false, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Usuario 1" },
                    { 3, "usuario_2", false, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Usuario 2" },
                    { 4, "usuario_3", false, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Usuario 3" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cajones_Numero",
                table: "Cajones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_SesionId",
                table: "Pagos",
                column: "SesionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_CajonId",
                table: "Sesiones",
                column: "CajonId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_UsuarioId",
                table: "Sesiones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Codigo",
                table: "Usuarios",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadosCochera");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Tarifas");

            migrationBuilder.DropTable(
                name: "Sesiones");

            migrationBuilder.DropTable(
                name: "Cajones");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
