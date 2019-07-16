using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Zed.CRM.FreeMarker
{
    public class Configurations
    {
        public static Configurations Current(IOrganizationService service)
        {
            return Get(service);
        }

        public static Configurations Get(IOrganizationService service, Guid? userId = null)
        {
            var result = new Configurations();

            var query = new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("dateformatstring", "timeformatstring", "dateseparator", "timeseparator")
            };

            var condition = userId == null
                ? new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
                : new ConditionExpression("systemuserid", ConditionOperator.Equal, userId);
            query.Criteria.AddCondition(condition);

            var link = query.AddLink("timezonedefinition", "timezonecode", "timezonecode");
            link.Columns = new ColumnSet("standardname");
            link.EntityAlias = "tz";
            var entity = service.RetrieveMultiple(query).Entities.First();

            result.UserZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(entity.GetAliased<string>("tz.standardname"));
            result.DateFormat = entity.GetAttributeValue<string>("dateformatstring")
                .Replace("/", entity.GetAttributeValue<string>("dateseparator"));
            result.TimeFormat = entity.GetAttributeValue<string>("timeformatstring")
                .Replace(":", entity.GetAttributeValue<string>("timeseparator"));
            result.DateTimeFormat = $"{result.DateFormat} {result.TimeFormat}";

            return result;
        }

        public static Configurations Default = new Configurations
        {
            DateFormat = "d",
            DateTimeFormat = "g",
            TimeFormat = "t",
            UserZoneInfo = TimeZoneInfo.Local
        };


        public TimeZoneInfo UserZoneInfo { get; set; }
        public bool ValidataVariables { get; set; } = false;

        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }
        public string DateTimeFormat { get; set; }

        public string DefineDateTimeFormat(string format)
        {
            switch (format)
            {
                case "d": return DateFormat;
                case "t": return TimeFormat;
                case "g": return DateTimeFormat;
                default: return format;
            }
        }

        public DateTime ToLocalDateTime(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, UserZoneInfo);
        }
    }
}
