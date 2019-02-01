using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    internal class DirectiveHolder : IPlaceholder
    {
        public IList<IPlaceholder> ContentItems { get; } = new List<IPlaceholder>();
        public string Directive { get; }
        public int Position { get; set; }

        private readonly IDirectiveParser _parser;

        public DirectiveHolder(string directive, string value, IDirectiveParser parser, int position)
        {
            _parser = parser;
            Directive = directive;
            parser.SetValue(value);
            Position = position;
        }
        public string Content(Dictionary<string, Entity> entities)
        {
            return _parser.Render(entities, ContentItems);
        }
    }
}
