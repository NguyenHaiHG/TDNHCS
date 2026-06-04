using Microsoft.EntityFrameworkCore;
using TDNHCS.Data;
using TDNHCS.Models;

namespace TDNHCS.Services;

public class UserService
{
    private readonly DocumentDbContext _context;

    public UserService(DocumentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Đăng nhập — trả về User nếu đúng, null nếu sai
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        var hash = DocumentDbContext.HashPassword(password);
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hash);
    }

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        var user = await LoginAsync(username, oldPassword);
        if (user == null) return false;

        user.PasswordHash = DocumentDbContext.HashPassword(newPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
