using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Zed.CRM.FreeMarker
{
    internal class QueryPart
    {
        private readonly LinkEntity _link;
        private readonly QueryExpression _query;

        public DataCollection<LinkEntity> LinkEntities => _query?.LinkEntities ?? _link.LinkEntities;
        public string EntityName => _query?.EntityName ?? _link.LinkToEntityName;

        public QueryPart(QueryExpression query)
        {
            _query = query;
        }

        public QueryPart(LinkEntity link)
        {
            _link = link;
        }

        public LinkEntity AddLink(string linkToEntityName, string linkFromAttributeName, string linkToAttributeName, ConditionExpression linkCriteria = null)
        {
            var link = _query?.AddLink(linkToEntityName, linkFromAttributeName, linkToAttributeName, JoinOperator.LeftOuter)
                ?? _link.AddLink(linkToEntityName, linkFromAttributeName, linkToAttributeName, JoinOperator.LeftOuter);
            if (linkCriteria != null)
            {
                link.LinkCriteria.AddCondition(linkCriteria);
            }
            return link;
        }

        public void AddColumn(string column) => (_query?.ColumnSet ?? _link?.Columns).AddColumn(column);
    }
}
