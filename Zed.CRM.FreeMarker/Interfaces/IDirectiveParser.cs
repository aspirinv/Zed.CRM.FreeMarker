using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace Zed.CRM.FreeMarker.Interfaces
{
    public interface IDirectiveParser
    {
        void SetValue(string value);
        string Render(Dictionary<string, Entity> source, IList<IPlaceholder> contentItems);
        MetadataManager Metadata { get; }
    }
}
