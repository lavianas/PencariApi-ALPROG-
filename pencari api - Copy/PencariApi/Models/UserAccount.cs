using SQLite;

namespace PencariApi.Models;

public class UserAccount
{
    [PrimaryKey]
    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string FaceImagePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}