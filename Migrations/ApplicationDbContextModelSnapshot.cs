﻿/*
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

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenQMS.Data;

#nullable disable

namespace OpenQMS.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.29")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("OpenQMS.Models.AppDocument", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("AuthoredBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("AuthoredOn")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("Content")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DocumentId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FileExtension")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FileType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GeneratedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("AppDocument");
                });

            modelBuilder.Entity("OpenQMS.Models.AppRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("OpenQMS.Models.AppUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("OpenQMS.Models.Asset", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("AssetId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EditedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EditedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GeneratedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("ProcessId");

                    b.ToTable("Asset");
                });

            modelBuilder.Entity("OpenQMS.Models.Capa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AcceptedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("AcceptedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("AssessedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("AssessedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Assessment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("AssetId")
                        .HasColumnType("int");

                    b.Property<string>("CapaId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CorrectiveAction")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeterminedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DeterminedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("DeviationId")
                        .HasColumnType("int");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Implementation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImplementedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ImplementedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("MaterialId")
                        .HasColumnType("int");

                    b.Property<string>("PreventiveAction")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int?>("ProductId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("DeviationId");

                    b.HasIndex("MaterialId");

                    b.HasIndex("ProcessId");

                    b.HasIndex("ProductId");

                    b.ToTable("Capa");
                });

            modelBuilder.Entity("OpenQMS.Models.Change", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AcceptedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("AcceptedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("AssessedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("AssessedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Assessment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("AssetId")
                        .HasColumnType("int");

                    b.Property<int?>("CapaId")
                        .HasColumnType("int");

                    b.Property<string>("ChangeId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Implementation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImplementedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ImplementedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("MaterialId")
                        .HasColumnType("int");

                    b.Property<int?>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int?>("ProductId")
                        .HasColumnType("int");

                    b.Property<string>("Proposal")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProposedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ProposedOn")
                        .HasColumnType("datetime2");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("CapaId");

                    b.HasIndex("MaterialId");

                    b.HasIndex("ProcessId");

                    b.HasIndex("ProductId");

                    b.ToTable("Change");
                });

            modelBuilder.Entity("OpenQMS.Models.Deviation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AcceptedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("AcceptedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("AssetId")
                        .HasColumnType("int");

                    b.Property<string>("DeviationId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EvaluatedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EvaluatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Evaluation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Identification")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IdentifiedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("IdentifiedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("MaterialId")
                        .HasColumnType("int");

                    b.Property<int?>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int?>("ProductId")
                        .HasColumnType("int");

                    b.Property<string>("Resolution")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ResolvedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ResolvedOn")
                        .HasColumnType("datetime2");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AssetId");

                    b.HasIndex("MaterialId");

                    b.HasIndex("ProcessId");

                    b.HasIndex("ProductId");

                    b.ToTable("Deviation");
                });

            modelBuilder.Entity("OpenQMS.Models.Material", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EditedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EditedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GeneratedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<string>("MaterialId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("ProcessId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("ProcessId");

                    b.ToTable("Material");
                });

            modelBuilder.Entity("OpenQMS.Models.Navigation.UserTraining", b =>
                {
                    b.Property<int>("TraineeId")
                        .HasColumnType("int");

                    b.Property<int>("TrainingId")
                        .HasColumnType("int");

                    b.HasKey("TraineeId", "TrainingId");

                    b.HasIndex("TrainingId");

                    b.ToTable("UserTraining", (string)null);
                });

            modelBuilder.Entity("OpenQMS.Models.Process", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EditedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EditedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GeneratedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProcessId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("ProductId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.ToTable("Process");
                });

            modelBuilder.Entity("OpenQMS.Models.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ApprovedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EditedBy")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EditedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExportFilePath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GeneratedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProductId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("Version")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("Product");
                });

            modelBuilder.Entity("OpenQMS.Models.Training", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int?>("CompletedBy")
                        .HasColumnType("int");

                    b.Property<DateTime?>("CompletedOn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PolicyId")
                        .HasColumnType("int");

                    b.Property<string>("PolicyTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<string>("TrainerEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TrainerId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TrainingId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CompletedBy");

                    b.ToTable("Training");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("OpenQMS.Models.AppRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("OpenQMS.Models.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("OpenQMS.Models.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("OpenQMS.Models.AppRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenQMS.Models.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("OpenQMS.Models.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OpenQMS.Models.Asset", b =>
                {
                    b.HasOne("OpenQMS.Models.Process", "Process")
                        .WithMany("Assets")
                        .HasForeignKey("ProcessId");

                    b.Navigation("Process");
                });

            modelBuilder.Entity("OpenQMS.Models.Capa", b =>
                {
                    b.HasOne("OpenQMS.Models.Asset", "Asset")
                        .WithMany("Capas")
                        .HasForeignKey("AssetId");

                    b.HasOne("OpenQMS.Models.Deviation", "Deviation")
                        .WithMany("Capas")
                        .HasForeignKey("DeviationId");

                    b.HasOne("OpenQMS.Models.Material", "Material")
                        .WithMany("Capas")
                        .HasForeignKey("MaterialId");

                    b.HasOne("OpenQMS.Models.Process", "Process")
                        .WithMany("Capas")
                        .HasForeignKey("ProcessId");

                    b.HasOne("OpenQMS.Models.Product", "Product")
                        .WithMany("Capas")
                        .HasForeignKey("ProductId");

                    b.Navigation("Asset");

                    b.Navigation("Deviation");

                    b.Navigation("Material");

                    b.Navigation("Process");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("OpenQMS.Models.Change", b =>
                {
                    b.HasOne("OpenQMS.Models.Asset", "Asset")
                        .WithMany("Changes")
                        .HasForeignKey("AssetId");

                    b.HasOne("OpenQMS.Models.Capa", "Capa")
                        .WithMany("Changes")
                        .HasForeignKey("CapaId");

                    b.HasOne("OpenQMS.Models.Material", "Material")
                        .WithMany("Changes")
                        .HasForeignKey("MaterialId");

                    b.HasOne("OpenQMS.Models.Process", "Process")
                        .WithMany("Changes")
                        .HasForeignKey("ProcessId");

                    b.HasOne("OpenQMS.Models.Product", "Product")
                        .WithMany("Changes")
                        .HasForeignKey("ProductId");

                    b.Navigation("Asset");

                    b.Navigation("Capa");

                    b.Navigation("Material");

                    b.Navigation("Process");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("OpenQMS.Models.Deviation", b =>
                {
                    b.HasOne("OpenQMS.Models.Asset", "Asset")
                        .WithMany("Deviations")
                        .HasForeignKey("AssetId");

                    b.HasOne("OpenQMS.Models.Material", "Material")
                        .WithMany("Deviations")
                        .HasForeignKey("MaterialId");

                    b.HasOne("OpenQMS.Models.Process", "Process")
                        .WithMany("Deviations")
                        .HasForeignKey("ProcessId");

                    b.HasOne("OpenQMS.Models.Product", "Product")
                        .WithMany("Deviations")
                        .HasForeignKey("ProductId");

                    b.Navigation("Asset");

                    b.Navigation("Material");

                    b.Navigation("Process");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("OpenQMS.Models.Material", b =>
                {
                    b.HasOne("OpenQMS.Models.Process", "Process")
                        .WithMany("Materials")
                        .HasForeignKey("ProcessId");

                    b.Navigation("Process");
                });

            modelBuilder.Entity("OpenQMS.Models.Navigation.UserTraining", b =>
                {
                    b.HasOne("OpenQMS.Models.AppUser", "Trainee")
                        .WithMany("Trainings")
                        .HasForeignKey("TraineeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenQMS.Models.Training", "Training")
                        .WithMany("Trainees")
                        .HasForeignKey("TrainingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trainee");

                    b.Navigation("Training");
                });

            modelBuilder.Entity("OpenQMS.Models.Process", b =>
                {
                    b.HasOne("OpenQMS.Models.Product", "Product")
                        .WithMany("Processes")
                        .HasForeignKey("ProductId");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("OpenQMS.Models.Training", b =>
                {
                    b.HasOne("OpenQMS.Models.AppUser", "CompletedByUser")
                        .WithMany()
                        .HasForeignKey("CompletedBy");

                    b.Navigation("CompletedByUser");
                });

            modelBuilder.Entity("OpenQMS.Models.AppUser", b =>
                {
                    b.Navigation("Trainings");
                });

            modelBuilder.Entity("OpenQMS.Models.Asset", b =>
                {
                    b.Navigation("Capas");

                    b.Navigation("Changes");

                    b.Navigation("Deviations");
                });

            modelBuilder.Entity("OpenQMS.Models.Capa", b =>
                {
                    b.Navigation("Changes");
                });

            modelBuilder.Entity("OpenQMS.Models.Deviation", b =>
                {
                    b.Navigation("Capas");
                });

            modelBuilder.Entity("OpenQMS.Models.Material", b =>
                {
                    b.Navigation("Capas");

                    b.Navigation("Changes");

                    b.Navigation("Deviations");
                });

            modelBuilder.Entity("OpenQMS.Models.Process", b =>
                {
                    b.Navigation("Assets");

                    b.Navigation("Capas");

                    b.Navigation("Changes");

                    b.Navigation("Deviations");

                    b.Navigation("Materials");
                });

            modelBuilder.Entity("OpenQMS.Models.Product", b =>
                {
                    b.Navigation("Capas");

                    b.Navigation("Changes");

                    b.Navigation("Deviations");

                    b.Navigation("Processes");
                });

            modelBuilder.Entity("OpenQMS.Models.Training", b =>
                {
                    b.Navigation("Trainees");
                });
#pragma warning restore 612, 618
        }
    }
}
