using Microsoft.EntityFrameworkCore;

namespace BomApp.Infrastructure.Auth;

/// <summary>
/// EF Core DbContext สำหรับ Authentication database — read-only
/// ใช้ sml_user_list table (keyless entity — HasNoKey)
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    /// <summary>Keyless entity สำหรับ sml_user_list — ใช้ raw SQL query</summary>
    public DbSet<SmlUser> SmlUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmlUser>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("sml_user_list");

            entity.Property(u => u.UserCode)
                .HasColumnName("user_code")
                .HasMaxLength(50);

            entity.Property(u => u.UserName)
                .HasColumnName("user_name")
                .HasMaxLength(100);

            entity.Property(u => u.UserPassword)
                .HasColumnName("user_password")
                .HasMaxLength(25);

            entity.Property(u => u.UserLevel)
                .HasColumnName("user_level");

            entity.Property(u => u.ActiveStatus)
                .HasColumnName("active_status");

            entity.Property(u => u.IsLockRecord)
                .HasColumnName("is_lock_record");
        });
    }
}

/// <summary>
/// Keyless entity สำหรับ sml_user_list — ใช้อ่านข้อมูล user จาก authentication-database
/// </summary>
public class SmlUser
{
    /// <summary>รหัส user — user_code (VARCHAR)</summary>
    public string UserCode { get; set; } = string.Empty;

    /// <summary>รหัสผ่าน — user_password (VARCHAR)</summary>
    public string UserPassword { get; set; } = string.Empty;

    /// <summary>ชื่อแสดงผล — user_name (VARCHAR)</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>ระดับสิทธิ์ — user_level (SMALLINT)</summary>
    public short UserLevel { get; set; }

    /// <summary>สถานะใช้งาน — active_status (SMALLINT): 1 = active</summary>
    public short ActiveStatus { get; set; }

    /// <summary>สถานะ lock — is_lock_record (SMALLINT): 1 = locked</summary>
    public short IsLockRecord { get; set; }
}
