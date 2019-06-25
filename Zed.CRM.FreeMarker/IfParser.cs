using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    internal class IfParser : IDirectiveParser
    {
        private IPlaceholder _checkPlaceholder;
        private string _operation;
        private string _expected;
        public MetadataManager Metadata { get; }
        public IfParser(MetadataManager metadata)
        {
            Metadata = metadata;
        }

        public void SetValue(string value)
        {
            var match = Regex.Match(value, @"(.*) (==|!=) (.*)");
            if (!match.Success)
            {
                _checkPlaceholder = new Placeholder(Metadata, value, 0);
                _operation = "!=";
                _expected = "NULL";
            }
            else
            {
                _checkPlaceholder = new Placeholder(Metadata, match.Groups[1].Value, 0);
                _operation = match.Groups[2].Value;
                _expected = match.Groups[3].Value;
            }
        }

        public string Render(Dictionary<string, Entity> source, Dictionary<string, IList<IPlaceholder>> contentItems)
        {
            var items = Pass(_checkPlaceholder.Content(source))
                ? contentItems["main"]
                : contentItems.ContainsKey("else") 
                    ? contentItems["else"] 
                    : new List<IPlaceholder>();
            return string.Concat(items.OrderBy(item => item.Position)
                .Select(holder => holder.Content(source)));
        }

        private bool Pass(string actual)
        {
            switch (_operation)
            {
                case "==": return IsExpected(actual);
                case "!=": return !IsExpected(actual);
                default: return false;
            }
        }
        private bool IsExpected(string actual)
        {
            if(_expected == "NULL")
            {
                return string.IsNullOrWhiteSpace(actual);
            }
            return _expected.Equals(actual, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
