using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace Demo;

public static class Extensions
{
    public static bool IsAjax(this HttpRequest request) => request.Headers.XRequestedWith == "XMLHttpRequest";
    public static bool IsValid(this ModelStateDictionary ms, string key) => ms.GetFieldValidationState(key) == ModelValidationState.Valid;
    public static DateOnly ToDateOnly(this DateTime dt) => DateOnly.FromDateTime(dt);
    public static TimeOnly ToTimeOnly(this DateTime dt) => TimeOnly.FromDateTime(dt);
    public static T? Get<T>(this ISession session, string key)
    {
        string? value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
    public static void Set<T>(this ISession session, string key, T value) => session.SetString(key, JsonSerializer.Serialize(value));
}
