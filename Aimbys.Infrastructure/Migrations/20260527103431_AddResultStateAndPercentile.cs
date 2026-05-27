using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimbys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResultStateAndPercentile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperQuestions_PaperVersions_PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.DropIndex(
                name: "IX_PaperQuestions_PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.DropColumn(
                name: "PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.AddColumn<double>(
                name: "Percentile",
                table: "Results",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Results",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Percentile",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Results");

            migrationBuilder.AddColumn<Guid>(
                name: "PaperVersionId",
                table: "PaperQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaperQuestions_PaperVersionId",
                table: "PaperQuestions",
                column: "PaperVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperQuestions_PaperVersions_PaperVersionId",
                table: "PaperQuestions",
                column: "PaperVersionId",
                principalTable: "PaperVersions",
                principalColumn: "Id");
        }
    }
}
