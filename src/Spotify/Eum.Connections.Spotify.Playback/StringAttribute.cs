using System;
using System.Linq;
using System.Reflection;

namespace Eum.Connections.Spotify.Playback
{
    [AttributeUsage(AttributeTargets.Field)]
    public class StringAttribute : Attribute
    {
        public StringAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
#nullable enable
        public static bool GetValue(Type enumType, Enum enumValue, out string? result)
        {
            if (enumType
              .GetMember(enumValue.ToString())[0]
              .GetCustomAttributes(typeof(StringAttribute))
              .FirstOrDefault() is StringAttribute stringAttr)
            {
                result = stringAttr.Value;
                return true;
            }
            result = null;
            return false;
        }
#nullable disable
    }

    public static class EnumAttributes
    {
        public static string GetValue(this Enum enumInput)
        {
            if (StringAttribute.GetValue(enumInput.GetType(), enumInput, out var val))
            {
                return val;
            }
            throw new NotImplementedException();
        }
    }
}