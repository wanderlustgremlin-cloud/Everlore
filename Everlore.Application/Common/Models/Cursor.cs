using System.Text;
using System.Text.Json;

namespace Everlore.Application.Common.Models;

public static class Cursor
{
    public static string Encode(string sortValue, Guid id)
    {
        var json = JsonSerializer.Serialize(new { s = sortValue, id });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static (string SortValue, Guid Id) Decode(string cursor)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var sortValue = root.GetProperty("s").GetString() ?? "";
        var id = root.GetProperty("id").GetGuid();
        return (sortValue, id);
    }
}
