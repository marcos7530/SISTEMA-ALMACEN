using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAlmacen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientesCuentaCorrienteYNotasCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_VentaId",
                table: "Comprobantes");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsCuentaCorriente",
                table: "MediosPago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ComprobanteAsociadoId",
                table: "Comprobantes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CondicionIva = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CuentaCorrienteHabilitada = table.Column<bool>(type: "bit", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCuentaCorriente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCuentaCorriente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorriente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorriente_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorriente_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                column: "EsCuentaCorriente",
                value: false);

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "EsCuentaCorriente",
                value: false);

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "EsCuentaCorriente",
                value: false);

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "EsCuentaCorriente",
                value: false);

            migrationBuilder.InsertData(
                table: "MediosPago",
                columns: new[] { "Id", "Activo", "EsCuentaCorriente", "EsSistema", "Nombre" },
                values: new object[] { 5, true, true, false, "Cuenta Corriente" });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_ComprobanteAsociadoId",
                table: "Comprobantes",
                column: "ComprobanteAsociadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_VentaId",
                table: "Comprobantes",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Documento",
                table: "Clientes",
                column: "Documento",
                unique: true,
                filter: "[Documento] IS NOT NULL AND [Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorriente_ClienteId",
                table: "MovimientosCuentaCorriente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorriente_UsuarioId",
                table: "MovimientosCuentaCorriente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorriente_VentaId",
                table: "MovimientosCuentaCorriente",
                column: "VentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_Comprobantes_ComprobanteAsociadoId",
                table: "Comprobantes",
                column: "ComprobanteAsociadoId",
                principalTable: "Comprobantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_Comprobantes_ComprobanteAsociadoId",
                table: "Comprobantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "MovimientosCuentaCorriente");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_ComprobanteAsociadoId",
                table: "Comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_VentaId",
                table: "Comprobantes");

            migrationBuilder.DeleteData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "EsCuentaCorriente",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "ComprobanteAsociadoId",
                table: "Comprobantes");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_VentaId",
                table: "Comprobantes",
                column: "VentaId",
                unique: true);
        }
    }
}
