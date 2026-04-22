using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AIS.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.GetName() ?? value.ToString();
    }
}
