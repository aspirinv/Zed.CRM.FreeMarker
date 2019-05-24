using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Zed.CRM.FreeMarker
{
    public class Configurations
    {
        public static Configurations Default(IOrganizationService service)
        {
            var result = new Configurations();

            var query = new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("dateformatstring", "timeformatstring", "dateseparator", "timeseparator")
            };
            query.Criteria.AddCondition("systemuserid", ConditionOperator.EqualUserId);
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
