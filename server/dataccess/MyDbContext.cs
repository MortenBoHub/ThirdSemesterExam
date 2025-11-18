using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dataccess;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Board> Boards { get; set; }

    public virtual DbSet<Drawnnumber> Drawnnumbers { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Playerboard> Playerboards { get; set; }

    public virtual DbSet<Playerboardnumber> Playerboardnumbers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("admin_pkey");

            entity.ToTable("admin", "dødeduer");

            entity.HasIndex(e => e.Email, "admin_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Passwordhash).HasColumnName("passwordhash");
            entity.Property(e => e.Phonenumber).HasColumnName("phonenumber");
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("boards_pkey");

            entity.ToTable("boards", "dødeduer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Enddate).HasColumnName("enddate");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(false)
                .HasColumnName("isactive");
            entity.Property(e => e.Startdate).HasColumnName("startdate");
            entity.Property(e => e.Weeknumber).HasColumnName("weeknumber");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<Drawnnumber>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("drawnnumbers_pkey");

            entity.ToTable("drawnnumbers", "dødeduer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Boardid).HasColumnName("boardid");
            entity.Property(e => e.Drawnat).HasColumnName("drawnat");
            entity.Property(e => e.Drawnby).HasColumnName("drawnby");
            entity.Property(e => e.Drawnnumber1).HasColumnName("drawnnumber");

            entity.HasOne(d => d.Board).WithMany(p => p.Drawnnumbers)
                .HasForeignKey(d => d.Boardid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("drawnnumbers_boardid_fkey");

            entity.HasOne(d => d.DrawnbyNavigation).WithMany(p => p.Drawnnumbers)
                .HasForeignKey(d => d.Drawnby)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("drawnnumbers_drawnby_fkey");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("player_pkey");

            entity.ToTable("player", "dødeduer");

            entity.HasIndex(e => e.Email, "player_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Funds)
                .HasPrecision(10, 2)
                .HasColumnName("funds");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Passwordhash).HasColumnName("passwordhash");
            entity.Property(e => e.Phonenumber).HasColumnName("phonenumber");
        });

        modelBuilder.Entity<Playerboard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("playerboards_pkey");

            entity.ToTable("playerboards", "dødeduer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Boardid).HasColumnName("boardid");
            entity.Property(e => e.Createdat).HasColumnName("createdat");
            entity.Property(e => e.Iswinner)
                .HasDefaultValue(false)
                .HasColumnName("iswinner");
            entity.Property(e => e.Playerid).HasColumnName("playerid");

            entity.HasOne(d => d.Board).WithMany(p => p.Playerboards)
                .HasForeignKey(d => d.Boardid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("playerboards_boardid_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.Playerboards)
                .HasForeignKey(d => d.Playerid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("playerboards_playerid_fkey");
        });

        modelBuilder.Entity<Playerboardnumber>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("playerboardnumbers_pkey");

            entity.ToTable("playerboardnumbers", "dødeduer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Playerboardid).HasColumnName("playerboardid");
            entity.Property(e => e.Selectednumber).HasColumnName("selectednumber");

            entity.HasOne(d => d.Playerboard).WithMany(p => p.Playerboardnumbers)
                .HasForeignKey(d => d.Playerboardid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("playerboardnumbers_playerboardid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
