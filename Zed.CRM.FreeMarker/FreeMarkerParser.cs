using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    public class FreeMarkerParser
    {
        private readonly IOrganizationService _service;
        private readonly Dictionary<string, List<QueryExpression>> _queries;
        private readonly MetadataManager _metadataContainer;
        private IList<IPlaceholder> _placeholders;

        public bool ValidateVariables { get; set; }
        public event EventHandler<EntityRetrievedEventArgs> OnEntityRetrieved;
        private IDirectiveParser CreateParser(string directive, DirectiveHolder current)
        {
            switch (directive.ToLower())
            {
                case "if": return new IfParser(_metadataContainer);
                case "elseif":
                    {
                        current.ApplyInDirective("else");
                        return new IfParser(_metadataContainer);
                    }
            }
            throw new Exception($"Directive {directive} is not supported.");
        }

        public FreeMarkerParser(IOrganizationService orgService, string template, Configurations configurations = null)
        {
            configurations = configurations ?? Configurations.Default;
            ValidateVariables = configurations.ValidataVariables;
            _service = orgService;
            _queries = new Dictionary<string, List<QueryExpression>>();
            _metadataContainer = new MetadataManager(orgService, _queries, configurations);
            SetTemplate(template);
        }

        public string Produce(Dictionary<string, EntityReference> references)
        {
            if (ValidateVariables && !_queries.Keys.SequenceEqual(references.Keys))
            {
                throw new Exception($"Keys in template doesn't fit to the provided entities. Query: {string.Join(",", _queries.Keys)}. Provided: {string.Join(",", references.Keys)}");
            }
            var entities = _queries.ToDictionary(q => q.Key, q => GetEntity(q.Key, references[q.Key]));
            return string.Concat(_placeholders.OrderBy(item => item.Position)
                .Select(holder => holder.Content(entities)));
        }

        private void SetTemplate(string template)
        {
            var matches =
                Regex.Matches(template, @"\$\{([^\}]*)\}").OfType<Match>()
                    .Select(item => new { match = item, type = PlaceholderType.Field })
                    .Union(
                        Regex.Matches(template, @"<#([[a-zA-Z]*) ([^>]*)>")
                            .OfType<Match>()
                            .Select(item => new { match = item, type = PlaceholderType.Directive }))
                    .Union(
                        Regex.Matches(template, @"<#([[a-zA-Z]*)>")
                            .OfType<Match>()
                            .Select(item => new { match = item, type = PlaceholderType.InDirective }))
                    .Union(
                        Regex.Matches(template, @"<\/#([[a-zA-Z]*)>")
                            .OfType<Match>()
                            .Select(item => new { match = item, type = PlaceholderType.DirectiveEnd }));

            _placeholders = new List<IPlaceholder>();

            var templateIndex = 0;
            var position = 0;
            var directives = new Stack<DirectiveHolder>();
            DirectiveHolder currentDirective = null;
            var holders = _placeholders;
            foreach (var match in matches.OrderBy(item => item.match.Index))
            {
                var text = template.Substring(templateIndex, match.match.Index - templateIndex);
                holders.Add(new TextHolder(text, position++));

                templateIndex = match.match.Index + match.match.Length;

                switch (match.type)
                {
                    case PlaceholderType.Field:
                        {
                            var value = match.match.Value.Trim('$', '{', '}');
                            holders.Add(value.StartsWith(".")
                                ? (IPlaceholder)new FunctionHolder(_metadataContainer, value, position++)
                                : new Placeholder(_metadataContainer, value, position++));
                            break;
                        }
                    case PlaceholderType.Directive:
                        {
                            var directive = match.match.Groups[1].Value;
                            var parser = CreateParser(directive, currentDirective);
                            if (currentDirective != null)
                            {
                                directives.Push(currentDirective);
                                holders = currentDirective.CurrentItems;
                            }
                            currentDirective = new DirectiveHolder(directive, match.match.Groups[2].Value, parser, position++);
                            holders.Add(currentDirective);
                            holders = currentDirective.CurrentItems;
                            break;
                        }
                    case PlaceholderType.InDirective:
                        {
                            var indirective = match.match.Groups[1].Value;
                            if (currentDirective == null)
                                throw new Exception($"Incorrect In directive {indirective} outside of any other directives");
                            holders = currentDirective.ApplyInDirective(indirective);
                            break;
                        }
                    case PlaceholderType.DirectiveEnd:
                        {
                            if (currentDirective == null )
                                throw new Exception($"End of the directive {match.match.Value} don't fit any directive beginning");

                            while (currentDirective.Directive == "elseif")
                            {
                                currentDirective = directives.Pop();
                            }
                            if(!currentDirective.Directive.Equals(match.match.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase))
                                throw new Exception($"End of the directive {match.match.Value} don't fit any directive beginning");

                            if (directives.Count > 0)
                            {
                                currentDirective = directives.Pop();
                                holders = currentDirective.CurrentItems;
                            }
                            else
                            {
                                currentDirective = null;
                                holders = _placeholders;
                            }
                            break;
                        }
                }
            }
            if (directives.Count > 0)
            {
                throw new Exception($"Not all directives ({directives.Count}) are closed");
            }
            _placeholders.Add(new TextHolder(template.Substring(templateIndex), position));
        }

        private Entity GetEntity(string type, EntityReference reference)
        {
            if (reference == null)
            {
                return null;
            }
            var query = _queries.ContainsKey(type)
                ? _queries[type].FirstOrDefault(item => item.EntityName == reference.LogicalName)
                : null;
            if (query == null)
            {
                return null;
            }
            var condition = new ConditionExpression(_metadataContainer.GetEntityId(reference.LogicalName), ConditionOperator.Equal, reference.Id);
            query.Criteria.Conditions.Add(condition);
            var result = _service.RetrieveMultiple(query).Entities.FirstOrDefault() ?? new Entity(reference.LogicalName);
            query.Criteria.Conditions.Remove(condition);
            OnEntityRetrieved?.Invoke(this, new EntityRetrievedEventArgs
            {
                Entity = result,
                Key = type,
                LogicalName = reference.LogicalName
            });
            return result;
        }
    }
}
