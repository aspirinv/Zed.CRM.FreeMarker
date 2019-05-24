using Microsoft.Xrm.Sdk;

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
    }
}
