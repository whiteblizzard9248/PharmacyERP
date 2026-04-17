namespace Shsmg.Pharma.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class RoleSelection
{
    public string Name { get; set; } = "";
    public bool Selected { get; set; }
}