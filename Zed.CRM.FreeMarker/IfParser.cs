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
            var match = Regex.Match(value, @"([a-zA-Z.]*) (\S*) (.*)");
            if (!match.Success)
            {
                throw new Exception($"Invalid If directive content: {value}");
            }
            _checkPlaceholder = new Placeholder(Metadata, match.Groups[1].Value, 0);
            _operation = match.Groups[2].Value;
            _expected = match.Groups[3].Value;
        }

        public string Render(Dictionary<string, Entity> source, IList<IPlaceholder> contentItems)
        {
            if (Pass(_checkPlaceholder.Content(source)))
            {
                return string.Concat(contentItems.OrderBy(item => item.Position)
                    .Select(holder => holder.Content(source)));
            }
            return string.Empty;
        }

        private bool Pass(string actual)
        {
            switch (_operation)
            {
                case "==":
                    {
                        return _expected.Equals(actual, StringComparison.InvariantCultureIgnoreCase);
                    }
                case "!=":
                    {
                        return !_expected.Equals(actual, StringComparison.InvariantCultureIgnoreCase);
                    }
                default:
                    {
                        return false;
                    }
            }
        }
    }
}
