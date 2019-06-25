using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    internal class DirectiveHolder : IPlaceholder
    {
        private Dictionary<string, IList<IPlaceholder>> ContentItems { get; } = new Dictionary<string, IList<IPlaceholder>>
        {
            ["main"] = new List<IPlaceholder>()
        };

        public IList<IPlaceholder> CurrentItems { get; private set; }
        public string Directive { get; }
        public int Position { get; set; }

        private readonly IDirectiveParser _parser;

        public DirectiveHolder(string directive, string value, IDirectiveParser parser, int position)
        {
            _parser = parser;
            Directive = directive;
            parser.SetValue(value);
            Position = position;
            CurrentItems = ContentItems["main"];
        }
        public string Content(Dictionary<string, Entity> entities)
        {
            return _parser.Render(entities, ContentItems);
        }

        internal IList<IPlaceholder> ApplyInDirective(string indirective)
        {
            CurrentItems = new List<IPlaceholder>();
            ContentItems.Add(indirective, CurrentItems);
            return CurrentItems;
        }
    }
}
