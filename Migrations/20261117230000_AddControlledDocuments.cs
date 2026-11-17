using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenQMS.Data;

#nullable disable

namespace OpenQMS.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261117230000_AddControlledDocuments")]
public partial class AddControlledDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ControlledDocuments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                RequiresTraining = table.Column<bool>(type: "INTEGER", nullable: false),
                TrainingDueDays = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ControlledDocuments", x => x.Id));

        migrationBuilder.CreateTable(
            name: "QmsAuditEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ActorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EntityType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_QmsAuditEvents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "DocumentAccessGrants",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ControlledDocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                RoleName = table.Column<string>(type: "TEXT", nullable: false),
                CanRead = table.Column<bool>(type: "INTEGER", nullable: false),
                CanDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                CanReview = table.Column<bool>(type: "INTEGER", nullable: false),
                CanApprove = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentAccessGrants", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentAccessGrants_ControlledDocuments_ControlledDocumentId",
                    column: x => x.ControlledDocumentId,
                    principalTable: "ControlledDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DocumentRevisions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ControlledDocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                Revision = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                ChangeSummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                AuthoredByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                AuthoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReviewedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                ApprovedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                EffectiveAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                FileContent = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentRevisions", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentRevisions_ControlledDocuments_ControlledDocumentId",
                    column: x => x.ControlledDocumentId,
                    principalTable: "ControlledDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TrainingAssignments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentRevisionId = table.Column<int>(type: "INTEGER", nullable: false),
                TraineeId = table.Column<int>(type: "INTEGER", nullable: false),
                AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompletedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                CompletionEvidence = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TrainingAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_TrainingAssignments_AspNetUsers_TraineeId",
                    column: x => x.TraineeId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TrainingAssignments_DocumentRevisions_DocumentRevisionId",
                    column: x => x.DocumentRevisionId,
                    principalTable: "DocumentRevisions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ControlledDocuments_DocumentId",
            table: "ControlledDocuments",
            column: "DocumentId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAccessGrants_ControlledDocumentId_RoleName",
            table: "DocumentAccessGrants",
            columns: new[] { "ControlledDocumentId", "RoleName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentRevisions_ControlledDocumentId_Revision",
            table: "DocumentRevisions",
            columns: new[] { "ControlledDocumentId", "Revision" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentRevisions_ControlledDocumentId",
            table: "DocumentRevisions",
            column: "ControlledDocumentId",
            unique: true,
            filter: "\"Status\" = 3");

        migrationBuilder.CreateIndex(
            name: "IX_QmsAuditEvents_OccurredAtUtc",
            table: "QmsAuditEvents",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_TrainingAssignments_DocumentRevisionId_TraineeId",
            table: "TrainingAssignments",
            columns: new[] { "DocumentRevisionId", "TraineeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TrainingAssignments_TraineeId",
            table: "TrainingAssignments",
            column: "TraineeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DocumentAccessGrants");
        migrationBuilder.DropTable(name: "QmsAuditEvents");
        migrationBuilder.DropTable(name: "TrainingAssignments");
        migrationBuilder.DropTable(name: "DocumentRevisions");
        migrationBuilder.DropTable(name: "ControlledDocuments");
    }
}
