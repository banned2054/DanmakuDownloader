using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DanmakuDownloader.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanmakuJobs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    subjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    episode = table.Column<int>(type: "INTEGER", nullable: false),
                    targetPath = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    retryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    nextRunTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    lastError = table.Column<string>(type: "TEXT", nullable: true),
                    rowVersion = table.Column<Guid>(type: "TEXT", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanmakuJobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanmakuJobs_status_nextRunTime",
                table: "DanmakuJobs",
                columns: new[] { "status", "nextRunTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanmakuJobs");
        }
    }
}
