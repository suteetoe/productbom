using BomApp.Shared.Contracts;

namespace BomApp.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface สำหรับ Authentication — อ่านจาก authentication-database
/// อยู่ใน Application layer ตาม Clean Architecture
/// </summary>
public interface IAuthRepository
{
    /// <summary>
    /// ตรวจสอบ user จาก sml_user_list
    /// WHERE user_code = @code AND user_password = @password
    ///   AND active_status = 1 AND is_lock_record = 0
    /// คืนค่า null ถ้าไม่ผ่าน
    /// </summary>
    Task<AuthUserDto?> ValidateUserAsync(
        string userCode,
        string password,
        CancellationToken ct = default);
}
