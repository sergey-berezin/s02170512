﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RecognitionLibrary;

namespace RecognitionUI.Migrations
{
    [DbContext(typeof(RecognitionLibraryContext))]
    [Migration("20201122191608_init")]
    partial class init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("RecognitionLibrary.Blob", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Image")
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("Blobs");
                });

            modelBuilder.Entity("RecognitionLibrary.RecognitionImage", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<float>("Confidence")
                        .HasColumnType("REAL");

                    b.Property<long?>("ImageDetailsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Label")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<long>("Statistic")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ImageDetailsId");

                    b.ToTable("RecognitionImages");
                });

            modelBuilder.Entity("RecognitionLibrary.RecognitionImage", b =>
                {
                    b.HasOne("RecognitionLibrary.Blob", "ImageDetails")
                        .WithMany()
                        .HasForeignKey("ImageDetailsId");

                    b.Navigation("ImageDetails");
                });
#pragma warning restore 612, 618
        }
    }
}
