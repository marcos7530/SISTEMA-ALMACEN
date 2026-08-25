using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAlmacen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoBarrasToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoBarras",
                table: "Productos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos",
                column: "CodigoBarras",
                unique: true,
                filter: "[CodigoBarras] IS NOT NULL AND [Activo] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CodigoBarras",
                table: "Productos");
        }
    }
}
