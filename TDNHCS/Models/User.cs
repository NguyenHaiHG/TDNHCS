namespace TDNHCS.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    // Lưu mật khẩu dạng SHA256 hash, không lưu plaintext
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
