﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignerApi.Data;

namespace SignerApi.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20210505180736_timestamps")]
    partial class timestamps
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.5");

            modelBuilder.Entity("SignerApi.Models.ApiActivity", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CertExpire")
                        .HasColumnType("TEXT");

                    b.Property<string>("CertHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("CertIssuedBy")
                        .HasColumnType("TEXT");

                    b.Property<string>("CertIssuedTo")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClientIPAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("DownloadUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("EncCertPw")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<string>("Operation")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .HasColumnType("TEXT");

                    b.Property<string>("StatusUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("SystemCertFilename")
                        .HasColumnType("TEXT");

                    b.Property<string>("SystemOfficeFilename")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TsLastUpdate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TsStart")
                        .HasColumnType("TEXT");

                    b.Property<string>("UniqueKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserCertFilename")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserOfficeFilename")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("ApiActivity");
                });
#pragma warning restore 612, 618
        }
    }
}
