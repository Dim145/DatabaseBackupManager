using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseBackupManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyBackupJobAndBBackupStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackupJobs_Servers_ServerId",
                table: "BackupJobs");

            migrationBuilder.DropIndex(
                name: "IX_BackupJobs_ServerId",
                table: "BackupJobs");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Backups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ServerType",
                table: "BackupJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Databases",
                table: "Agents",
                type: "json",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "JobQueue",
                table: "Agents",
                type: "json",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "Backups");

            migrationBuilder.DropColumn(
                name: "ServerType",
                table: "BackupJobs");

            migrationBuilder.DropColumn(
                name: "Databases",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "JobQueue",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_ServerId",
                table: "BackupJobs",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_BackupJobs_Servers_ServerId",
                table: "BackupJobs",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
