using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Zed.CRM.FreeMarker
{
    public static class Extensions
    {
        public static T GetAliased<T>(this Entity source, string name)
        {
            if (!source.Contains(name))
            {
                return default;
            }
            var aliased = source[name] as AliasedValue;
            return aliased == null ? default : (T)aliased.Value;
        }

        public static (string, string, string) SplitFieldValue(this string value)
        {
            var majorParts = value.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            var defParts = (majorParts.Length > 1 ? majorParts[1] : majorParts[0])
                .Split(new[] { '!' }, StringSplitOptions.RemoveEmptyEntries);
            
            return (majorParts.Length > 1 ? majorParts[0] : defParts[0], 
                majorParts.Length > 1 ? defParts[0] : string.Empty, 
                defParts.Length > 1 ? defParts.Last().Trim('\"') : string.Empty);
        }

        public static string CleanUpMetadataName(this string name)
            => name.Trim().Replace(Environment.NewLine, "").Replace("\r", "").Replace("\n", "");
    }
}
