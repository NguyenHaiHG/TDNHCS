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
    /// Đăng nhập — trả về User nếu đúng, null nếu sai.
    /// Trước khi có database, chỉ chấp nhận tài khoản admin mặc định.
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        if (!AppPaths.IsInitialized)
        {
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase)
                && password == "Admin@123")
            {
                return CreateDefaultAdminUser();
            }

            return null;
        }

        var hash = DocumentDbContext.HashPassword(password);
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hash);
    }

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        if (!AppPaths.IsInitialized)
        {
            return false;
        }

        var user = await LoginAsync(username, oldPassword);
        if (user == null) return false;

        user.PasswordHash = DocumentDbContext.HashPassword(newPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public bool IsUsingDefaultAdminPassword(User? user)
    {
        return user?.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) == true
            && user.PasswordHash == DocumentDbContext.HashPassword("Admin@123");
    }

    public bool CanChangePassword => AppPaths.IsInitialized;

    private static User CreateDefaultAdminUser()
    {
        return new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = DocumentDbContext.HashPassword("Admin@123"),
            FullName = "Quản trị viên",
            Role = "Admin",
            CreatedDate = new DateTime(2024, 1, 1)
        };
    }
}
