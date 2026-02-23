using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace API.DB;

public partial class _1135InventorySystemContext : DbContext
{
    public _1135InventorySystemContext()
    {
    }

    public _1135InventorySystemContext(DbContextOptions<_1135InventorySystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assignmenthistory> Assignmenthistories { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<Inventoryrecord> Inventoryrecords { get; set; }

    public virtual DbSet<Systemsetting> Systemsettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;user=root;database=1135_inventory_system", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Assignmenthistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("assignmenthistory");

            entity.HasIndex(e => e.AssignedByAccountantId, "AssignedByAccountantId");

            entity.HasIndex(e => e.EquipmentId, "EquipmentId");

            entity.HasIndex(e => e.NewUserId, "NewUserId");

            entity.HasIndex(e => e.PreviousUserId, "PreviousUserId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Action).HasColumnType("enum('Assign','Return','Reassign')");
            entity.Property(e => e.AssignedByAccountantId).HasColumnType("int(11)");
            entity.Property(e => e.AssignmentDate)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.EquipmentId).HasColumnType("int(11)");
            entity.Property(e => e.NewUserId).HasColumnType("int(11)");
            entity.Property(e => e.PreviousUserId).HasColumnType("int(11)");
            entity.Property(e => e.Reason).HasColumnType("text");

            entity.HasOne(d => d.AssignedByAccountant).WithMany(p => p.AssignmenthistoryAssignedByAccountants)
                .HasForeignKey(d => d.AssignedByAccountantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("AssignmentHistory_ibfk_4");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Assignmenthistories)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("AssignmentHistory_ibfk_1");

            entity.HasOne(d => d.NewUser).WithMany(p => p.AssignmenthistoryNewUsers)
                .HasForeignKey(d => d.NewUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("AssignmentHistory_ibfk_3");

            entity.HasOne(d => d.PreviousUser).WithMany(p => p.AssignmenthistoryPreviousUsers)
                .HasForeignKey(d => d.PreviousUserId)
                .HasConstraintName("AssignmentHistory_ibfk_2");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("equipment");

            entity.HasIndex(e => e.AssignedToUserId, "AssignedToUserId");

            entity.HasIndex(e => e.InventoryNumber, "InventoryNumber").IsUnique();

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.AssignedToUserId).HasColumnType("int(11)");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Cost).HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.DateAssigned).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.InventoryNumber).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.LastInventoryDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Available'")
                .HasColumnType("enum('Available','Assigned','UnderRepair','Missing','Decommissioned')");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("Equipment_ibfk_1");
        });

        modelBuilder.Entity<Inventoryrecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("inventoryrecords");

            entity.HasIndex(e => e.EmployeeId, "EmployeeId");

            entity.HasIndex(e => e.EquipmentId, "EquipmentId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Comments).HasColumnType("text");
            entity.Property(e => e.EmployeeId).HasColumnType("int(11)");
            entity.Property(e => e.EquipmentCondition).HasColumnType("enum('New','Good','RequiresRepair','Unusable')");
            entity.Property(e => e.EquipmentId).HasColumnType("int(11)");
            entity.Property(e => e.InventoryDate)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.PhotoPath).HasMaxLength(255);

            entity.HasOne(d => d.Employee).WithMany(p => p.Inventoryrecords)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("InventoryRecords_ibfk_2");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Inventoryrecords)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("InventoryRecords_ibfk_1");
        });

        modelBuilder.Entity<Systemsetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("systemsettings");

            entity.HasIndex(e => e.SettingKey, "SettingKey").IsUnique();

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.SettingKey).HasMaxLength(50);
            entity.Property(e => e.SettingValue).HasMaxLength(255);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Username, "Username").IsUnique();

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasColumnType("enum('Accountant','Employee')");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
