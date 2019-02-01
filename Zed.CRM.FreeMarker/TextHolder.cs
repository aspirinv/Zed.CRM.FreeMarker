using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    internal class TextHolder : IPlaceholder
    {
        public TextHolder(string content, int position)
        {
            StaticText = content;
            Position = position;
        }
        public int Position { get; set; }
        public string StaticText { get; set; }
        public string Content(Dictionary<string, Entity> entities) => StaticText;
    }

}
