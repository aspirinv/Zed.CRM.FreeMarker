namespace Zed.CRM.FreeMarker
{
    internal enum PlaceholderType
    {
        /// <summary>
        /// simple field or chain 
        /// <example>${Recipient.contact.fullname}</example>
        /// </summary>
        Field,

        /// <summary>
        /// directive (only if supported)
        /// <example><![CDATA[ <#if Recipient.contact.fullname == John Doe> ]]></example>
        /// </summary>
        Directive,

        /// <summary>
        /// end of previously used directive
        /// <example><![CDATA[ </#if> ]]></example>
        /// </summary>
        DirectiveEnd
    }
}
