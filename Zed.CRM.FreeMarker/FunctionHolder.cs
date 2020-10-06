using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    public class FunctionHolder : IPlaceholder
    {
        private readonly MetadataManager _metadataContainer;
        private string function;

        public string Default { get; }
        public int Position { get; set; }
        public string Format { get; }


        public FunctionHolder(MetadataManager metadataContainer, string value, int position)
        {
            _metadataContainer = metadataContainer;
            Position = position;            
            (function, Format, Default) = value.SplitFieldValue();
        }

        public string Content(Dictionary<string, Entity> entities)
        {
            switch (function)
            {
                case ".now":
                    return _metadataContainer.Configurations.ToLocalDateTime(DateTime.UtcNow)
                        .ToString(_metadataContainer.Configurations.DefineDateTimeFormat(Format == string.Empty ? "g" : Format));
            }
            return Default;
        }
    }
}
