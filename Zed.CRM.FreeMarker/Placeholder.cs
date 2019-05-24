using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    internal class Placeholder : IPlaceholder
    {
        public string Default { get; set; }
        public string EntityPath { get; set; }
        public string Variable { get; set; }
        public string EntityName { get; set; }
        public string Format { get; set; }
        public int Position { get; set; }
        public bool InUrl { get; set; }

        private readonly MetadataManager _metadataContainer;

        public Placeholder(MetadataManager metadataContainer, string value, int position, bool inUrl = false)
        {
            InUrl = inUrl;
            _metadataContainer = metadataContainer;
            var mask = (int)ZedActivityPartyParticipationTypeMask.Owner;
            if (value.Contains(".from."))
            {
                value = value.Replace(".from.", ".activityid.activityparty.partyid.");
                mask = (int)ZedActivityPartyParticipationTypeMask.Sender;
            }

            var majorParts = value.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);

            var defParts = (majorParts.Length > 1 ? majorParts[1] : majorParts[0])
                .Split(new[] { '!' }, StringSplitOptions.RemoveEmptyEntries);
            var major = majorParts.Length > 1 ? majorParts[0] : defParts[0];
            Format = majorParts.Length > 1 ? defParts[0] : string.Empty;
            
            var parts = major.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                throw new Exception($"Invalid template parameter {value}");
            }
            Default = defParts.Length > 1 ? defParts.Last().Trim('\"') : "";
            Position = position;
            Variable = parts[0];
            EntityName = metadataContainer.DefineEntityName(parts[1]);

            var alias = string.Empty;
            var root = new QueryPart(metadataContainer.GetQuery(Variable, EntityName));
            for (var i = 2; i < parts.Length - 1; i += 2)
            {
                var currentField = metadataContainer.DefineField(parts[i], root.EntityName);
                var entity = metadataContainer.DefineEntityName(parts[i + 1]);
                alias += currentField + entity;
                var link = root.LinkEntities.FirstOrDefault(item => item.LinkFromAttributeName == currentField
                                                                    && item.LinkToEntityName == entity);
                if (link == null)
                {
                    ConditionExpression criterion = null;
                    string entityId;
                    if (entity == "activityparty")
                    {
                        entityId = "activityid";
                        criterion = new ConditionExpression("participationtypemask", ConditionOperator.Equal, mask);
                    }
                    else
                    {
                        entityId = _metadataContainer.GetEntityId(entity);
                    }
                    link = root.AddLink(entity, currentField, entityId, criterion);
                    link.EntityAlias = alias;
                }
                root = new QueryPart(link);
            }
            var field = metadataContainer.DefineField(parts[parts.Length - 1], root.EntityName);
            root.AddColumn(field);
            EntityPath = string.IsNullOrWhiteSpace(alias) ? field : $"{alias}.{field}";
        }

        public string Content(Dictionary<string, Entity> entities)
        {
            if (!entities.ContainsKey(Variable))
            {
                return string.Empty;
            }
            var entity = entities[Variable];
            if (entity == null)
            {
                return Default;
            }
            if (entity.LogicalName != EntityName)
            {
                return string.Empty;
            }
            var result = entity.Contains(EntityPath) ? GetRawValue(entity) : Default;
            return InUrl ? Uri.EscapeUriString(result) : result;
        }

        private string GetRawValue(Entity entity)
        {
            var entityValue = entity[EntityPath];
            var entityName = entity.LogicalName;
            var field = EntityPath;
            if (entityValue is AliasedValue)
            {
                var aliased = (entityValue as AliasedValue);
                entityValue = aliased.Value;
                entityName = aliased.EntityLogicalName;
                field = aliased.AttributeLogicalName;
            }
            if (entityValue is Money)
            {
                return (entityValue as Money).Value.ToString(Format == string.Empty? "n": Format);
            }
            if (entityValue is DateTime)
            {
                return _metadataContainer.Configurations.ToLocalDateTime((DateTime)entityValue)
                    .ToString(_metadataContainer.Configurations.DefineDateTimeFormat(Format == string.Empty ? "g" : Format));
            }
            if (entityValue is OptionSetValue)
            {
                return _metadataContainer.GetOptionsetText(entityValue as OptionSetValue, entityName, field);
            }
            return Convert.ToString(entityValue);
        }
    }
}
