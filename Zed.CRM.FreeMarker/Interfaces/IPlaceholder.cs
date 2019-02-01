using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace Zed.CRM.FreeMarker.Interfaces
{
    public interface IPlaceholder
    {
        int Position { get; set; }
        string Content(Dictionary<string, Entity> entities);
    }
}
