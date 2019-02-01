using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zed.CRM.FreeMarker.Sample.Plugins
{
    public class CreateSmsBodyPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var pluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();

            var tracingService = serviceProvider.GetService<ITracingService>();
            var serviceFactory = serviceProvider.GetService<IOrganizationServiceFactory>();
            var sms = GetTarget(pluginExecutionContext);


            var service = serviceFactory.CreateOrganizationService(pluginExecutionContext.UserId);
            var templateReference = sms.GetAttributeValue<EntityReference>("zed_templateid");
            var template = service.Retrieve("zed_messagetemplate", templateReference.Id, new ColumnSet("zed_template"));
            var parser = new FreeMarkerParser(service, template.GetAttributeValue<string>("zed_template"));
            var toUpdate = new Entity(sms.LogicalName, sms.Id)
            {
                ["description"] = parser.Produce(new Dictionary<string, EntityReference>
                {
                    ["Recipient"] = sms.GetAttributeValue<EntityCollection>("to")?.Entities?
                        .FirstOrDefault()?.GetAttributeValue<EntityReference>("partyid"),
                    ["Context"] = sms.ToEntityReference()
                })
            };
            service.Update(toUpdate);
        }

        private Entity GetTarget(IPluginExecutionContext pluginExecutionContext)
        {
            if (!pluginExecutionContext.InputParameters.ContainsKey("Target"))
                throw new InvalidPluginExecutionException("Invalid plugin registration. Step");

            if (!(pluginExecutionContext.InputParameters["Target"] is Entity sms) || sms.LogicalName != "zed_sms")
                throw new InvalidPluginExecutionException("Invalid plugin registration. Entity");

            if (pluginExecutionContext.MessageName == "Update")
            {
                if (!pluginExecutionContext.PostEntityImages.ContainsKey("postImage"))
                    throw new InvalidPluginExecutionException("Invalid plugin registration. postImage");
                var image = pluginExecutionContext.PostEntityImages["postImage"];
                foreach (var attribute in image.Attributes)
                {
                    if (!sms.Contains(attribute.Key))
                        sms[attribute.Key] = attribute.Value;
                }
            }
            return sms;
        }
    }
}
