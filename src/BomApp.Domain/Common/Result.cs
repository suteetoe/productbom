namespace BomApp.Domain.Common;

/// <summary>
/// Result monad สำหรับ use case — แทนการ throw exception
/// </summary>
/// <typeparam name="T">ประเภทของค่าที่คืน เมื่อสำเร็จ</typeparam>
public class Result<T>
{
    /// <summary>true เมื่อ operation สำเร็จ</summary>
    public bool IsSuccess { get; }

    /// <summary>ค่าที่คืนกลับมา (มีค่าเฉพาะเมื่อ IsSuccess = true)</summary>
    public T? Value { get; }

    /// <summary>ข้อความ error (มีค่าเฉพาะเมื่อ IsSuccess = false)</summary>
    public string? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    /// <summary>สร้าง Result ที่สำเร็จพร้อมค่า</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>สร้าง Result ที่ล้มเหลวพร้อม error message</summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>แปลงค่าอัตโนมัติเป็น Result ที่สำเร็จ</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Result monad สำหรับ operation ที่ไม่คืนค่า
/// </summary>
public class Result
{
    /// <summary>true เมื่อ operation สำเร็จ</summary>
    public bool IsSuccess { get; }

    /// <summary>ข้อความ error (มีค่าเฉพาะเมื่อ IsSuccess = false)</summary>
    public string? Error { get; }

    private Result(bool success, string? error) { IsSuccess = success; Error = error; }

    /// <summary>สร้าง Result ที่สำเร็จ</summary>
    public static Result Success() => new(true, null);

    /// <summary>สร้าง Result ที่ล้มเหลวพร้อม error message</summary>
    public static Result Failure(string error) => new(false, error);
}
