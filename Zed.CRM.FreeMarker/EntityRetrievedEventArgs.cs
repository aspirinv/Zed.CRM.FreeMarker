using System;
using Microsoft.Xrm.Sdk;

namespace Zed.CRM.FreeMarker
{
    public class EntityRetrievedEventArgs : EventArgs
    {
        public Entity Entity { get; internal set; }
        public string Key { get; internal set; }
        public string LogicalName { get; internal set; }
    }
}
