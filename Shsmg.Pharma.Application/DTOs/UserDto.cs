namespace Shsmg.Pharma.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Staff, or User
}