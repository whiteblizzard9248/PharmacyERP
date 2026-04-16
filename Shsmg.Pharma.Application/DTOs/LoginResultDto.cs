namespace Shsmg.Pharma.Application.DTOs;

public sealed class LoginResultDto
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}
