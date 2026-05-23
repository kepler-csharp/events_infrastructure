using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiGeneral.Migrations
{
    /// <inheritdoc />
    public partial class AddQrImageUrlToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrImageUrl",
                table: "Tickets",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrImageUrl",
                table: "Tickets");
        }
    }
}
