using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BomApp.Infrastructure.Auth;

public class AuthRepository(AuthDbContext context) : IAuthRepository
{
    public async Task<AuthUserDto?> ValidateUserAsync(
        string userCode,
        string password,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT user_code, user_name, user_level
            FROM sml_user_list
            WHERE user_code     = @code
              AND user_password = @pwd
              AND active_status = 1
              AND is_lock_record = 0
            LIMIT 1";

        var conn = (NpgsqlConnection)context.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync(ct);

        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", userCode);
            cmd.Parameters.AddWithValue("pwd",  password);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new AuthUserDto(
                UserCode:  reader.GetString(0),
                UserName:  reader.GetString(1),
                UserLevel: reader.GetInt16(2));
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }
}
