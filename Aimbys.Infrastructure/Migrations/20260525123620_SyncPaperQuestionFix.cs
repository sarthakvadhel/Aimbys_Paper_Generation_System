using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimbys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPaperQuestionFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperQuestions_PaperVersions_VersionId",
                table: "PaperQuestions");

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

            migrationBuilder.AddForeignKey(
                name: "FK_PaperQuestions_PaperVersions_VersionId",
                table: "PaperQuestions",
                column: "VersionId",
                principalTable: "PaperVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperQuestions_PaperVersions_PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaperQuestions_PaperVersions_VersionId",
                table: "PaperQuestions");

            migrationBuilder.DropIndex(
                name: "IX_PaperQuestions_PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.DropColumn(
                name: "PaperVersionId",
                table: "PaperQuestions");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperQuestions_PaperVersions_VersionId",
                table: "PaperQuestions",
                column: "VersionId",
                principalTable: "PaperVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
