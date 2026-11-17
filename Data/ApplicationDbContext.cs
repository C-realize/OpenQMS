/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Models;
using OpenQMS.Models.Navigation;

namespace OpenQMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<OpenQMS.Models.AppDocument> AppDocument { get; set; }
        public DbSet<OpenQMS.Models.Training> Training { get; set; }
        public DbSet<UserTraining> UserTraining { get; set; }
        public DbSet<ControlledDocument> ControlledDocuments => Set<ControlledDocument>();
        public DbSet<DocumentRevision> DocumentRevisions => Set<DocumentRevision>();
        public DbSet<DocumentAccessGrant> DocumentAccessGrants => Set<DocumentAccessGrant>();
        public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
        public DbSet<QmsAuditEvent> QmsAuditEvents => Set<QmsAuditEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserTraining>().ToTable("UserTraining");

            modelBuilder.Entity<UserTraining>().HasKey(t => new { t.TraineeId, t.TrainingId });

            modelBuilder.Entity<ControlledDocument>()
                .HasIndex(document => document.DocumentId)
                .IsUnique();

            modelBuilder.Entity<DocumentRevision>()
                .HasIndex(revision => new { revision.ControlledDocumentId, revision.Revision })
                .IsUnique();

            modelBuilder.Entity<DocumentRevision>()
                .HasIndex(revision => revision.ControlledDocumentId)
                .IsUnique()
                .HasFilter("\"Status\" = 3");

            modelBuilder.Entity<DocumentRevision>()
                .HasOne(revision => revision.ControlledDocument)
                .WithMany(document => document.Revisions)
                .HasForeignKey(revision => revision.ControlledDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentAccessGrant>()
                .HasIndex(grant => new { grant.ControlledDocumentId, grant.RoleName })
                .IsUnique();

            modelBuilder.Entity<DocumentAccessGrant>()
                .HasOne(grant => grant.ControlledDocument)
                .WithMany()
                .HasForeignKey(grant => grant.ControlledDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrainingAssignment>()
                .HasIndex(assignment => new { assignment.DocumentRevisionId, assignment.TraineeId })
                .IsUnique();

            modelBuilder.Entity<TrainingAssignment>()
                .HasOne(assignment => assignment.DocumentRevision)
                .WithMany()
                .HasForeignKey(assignment => assignment.DocumentRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainingAssignment>()
                .HasOne(assignment => assignment.Trainee)
                .WithMany()
                .HasForeignKey(assignment => assignment.TraineeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QmsAuditEvent>()
                .HasIndex(audit => audit.OccurredAtUtc);
        }

        public DbSet<OpenQMS.Models.Change>? Change { get; set; }
        public DbSet<OpenQMS.Models.Capa>? Capa { get; set; }
        public DbSet<OpenQMS.Models.Deviation>? Deviation { get; set; }
        public DbSet<OpenQMS.Models.Product> Product { get; set; }
        public DbSet<OpenQMS.Models.Asset> Asset { get; set; }
        public DbSet<OpenQMS.Models.Process> Process { get; set; }
        public DbSet<OpenQMS.Models.Material> Material { get; set; }
    }
}