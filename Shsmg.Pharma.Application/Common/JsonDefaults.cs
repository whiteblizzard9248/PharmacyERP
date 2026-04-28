using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shsmg.Pharma.Application.Common;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions StandardOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}