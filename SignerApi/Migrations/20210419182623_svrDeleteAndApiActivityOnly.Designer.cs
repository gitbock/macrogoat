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
    [Migration("20210419182623_svrDeleteAndApiActivityOnly")]
    partial class svrDeleteAndApiActivityOnly
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

                    b.Property<string>("FileHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<string>("Operation")
                        .HasColumnType("TEXT");

                    b.Property<string>("Result")
                        .HasColumnType("TEXT");

                    b.Property<string>("SystemFilename")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("UniqueKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserFilename")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("ApiActivity");
                });
#pragma warning restore 612, 618
        }
    }
}
