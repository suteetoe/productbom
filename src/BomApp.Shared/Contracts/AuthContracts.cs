namespace BomApp.Shared.Contracts;

/// <summary>ผลลัพธ์จาก login — ใช้สร้าง session</summary>
public record AuthUserDto(
    string  UserCode,
    string  UserName,
    short   UserLevel
);
