using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssociateCustomersWithPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerSubject",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerSubject",
                table: "Checkouts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerSubject",
                table: "Orders",
                column: "CustomerSubject");

            migrationBuilder.CreateIndex(
                name: "IX_Checkouts_CustomerSubject",
                table: "Checkouts",
                column: "CustomerSubject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerSubject",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Checkouts_CustomerSubject",
                table: "Checkouts");

            migrationBuilder.DropColumn(
                name: "CustomerSubject",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerSubject",
                table: "Checkouts");
        }
    }
}
