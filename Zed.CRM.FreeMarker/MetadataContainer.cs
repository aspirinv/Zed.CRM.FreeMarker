using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zed.CRM.FreeMarker
{
    public class MetadataManager
    {
        private readonly EntityMetadata[] _entities;
        private IOrganizationService _organizationService;
        private Dictionary<string, List<QueryExpression>> _queries;

        public Configurations Configurations { get; }

        public MetadataManager(IOrganizationService organizationService,
            Dictionary<string, List<QueryExpression>> queries, Configurations configurations)
        {
            _organizationService = organizationService;
            _queries = queries;
            Configurations = configurations;
            _entities = GetAllEntities();
        }

        public QueryExpression GetQuery(string type, string entity)
        {
            if (!_queries.ContainsKey(type))
            {
                _queries[type] = new List<QueryExpression>();
            }
            var holder = _queries[type];
            var query = holder.FirstOrDefault(item => item.EntityName == entity);
            if (query == null)
            {
                query = new QueryExpression(entity);
                holder.Add(query);
            }
            return query;
        }

        public string DefineField(string fieldName, string entityName)
        {
            fieldName = fieldName.Trim();
            entityName = entityName.Trim();
            var property = GetAttributesMetadata(entityName).Attributes.FirstOrDefault(item =>
                item.LogicalName.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase) ||
                item.DisplayName.LocalizedLabels.Any(label => label.Label.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)));
            if (property == null)
            {
                throw new Exception($"Field {fieldName} is not belongs to entity {entityName}");
            }
            return property.LogicalName;
        }

        public string DefineEntityName(string entityName)
        {
            entityName = entityName.Trim();
            var entity = _entities.FirstOrDefault(item =>
                item.LogicalName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) ||
                item.DisplayName.LocalizedLabels.Any(label => label.Label.Equals(entityName, StringComparison.InvariantCultureIgnoreCase)));
            if (entity == null)
            {
                throw new Exception($"Entity {entityName} not found in system");
            }
            return entity.LogicalName;
        }

        public string GetOptionsetText(OptionSetValue entityValue, string entityName, string field)
        {
            return (GetAttributesMetadata(entityName).Attributes
                .FirstOrDefault(attribute => attribute.LogicalName == field.Trim()) as PicklistAttributeMetadata)?.OptionSet.Options
                .FirstOrDefault(item => item.Value == entityValue.Value)?.Label.UserLocalizedLabel.Label;
        }

        public string GetEntityId(string entityName)
        {
            return _entities.First(item => item.LogicalName == entityName.Trim()).PrimaryIdAttribute;
        }

        private EntityMetadata[] GetAllEntities()
        {
            return ((RetrieveAllEntitiesResponse)_organizationService.Execute(new RetrieveAllEntitiesRequest
            {
                RetrieveAsIfPublished = false,
                EntityFilters = EntityFilters.Entity
            })).EntityMetadata;
        }

        private EntityMetadata GetAttributesMetadata(string entityName)
        {
            return ((RetrieveEntityResponse)_organizationService.Execute(new RetrieveEntityRequest
            {
                LogicalName = entityName.Trim(),
                RetrieveAsIfPublished = false,
                EntityFilters = EntityFilters.Attributes
            })).EntityMetadata;
        }
    }
}
