# Zed.CRM.FreeMarker

[![License](https://img.shields.io/badge/license-MIT-yellowgreen.svg?style=flat)](https://github.com/aspirinv/Zed.CRM.FreeMarker/blob/master/LICENSE)

CRM specific [FreeMarker](https://freemarker.apache.org/) template engine. It allows to process FreeMarker templates to produce text result based on data from the MS Dynamics CRM. 
It can be used in 3rd party projects or as part of the CRM Plugin or CustomActivity.

## Sample

Template: 

Dear, ${Recipient.contact.fullname!Friend}${Recipient.account.name!Friends}! It's time to use text processing engines. Best Regards, ${Context.Sms.from.systemuser.fullname} by ${Context.Sms.Subject}

Result:

Dear, ZeD inc! It's time to use text processing engines. Best Regards, Alexander Spirin by Demo sms

Check also [SamplePlugins](https://github.com/aspirinv/Zed.CRM.FreeMarker/tree/master/Zed.CRM.FreeMarker.Sample.Plugins) project

## Directives

At the moment library contains a next set of implemented features:

### Basic value
Show the value of the entity `${Context.Sms.Subject}`
Both Schema and display name (any supported language) can be used in value directive 

### Chained field value
Access values from related fields. Works for lookup fields. 

According to specification, lookup field name should be defined with its type, means the chain should follow the structure:

`${Context.[Context Type].[lookup].[lookup type].[field]}`

Block [lookup].[lookup type] can be repeated more than once, it is limited by the CRM query limitation of 10 entities.
In theory that means, that the length of the lookup chain can be maximal of 10 joins, but in fact the limitation can be reached if there are more than single chain using different entities what is still quite extremal case.

*from* shortcut is get's replaced in fact with *activityid.activityparty.partyid* with filtering by Sender, that way it will take the sender from the activity.

E.g. `${Context.Sms.from.systemuser.fullname}`

### **If** directive

Compares the entity value with a constant, in case of success renders the if body.
if body can contains other directives and other ifs as well
`<#if Recipient.contact.fullname == John Doe> John sample text </#if>`

Supports only operators '==' and '!='.
Implemented NOT NULL check `<#if Context.contact.Title Academic>`, which is similar to default value extended with complicated body

Subdirectives:

**elseif** <#elseif Customer.contact.gender == Male>Mr</#if>

**else** <#else>Dear</#if> 

Both else and elseif doesn't have own closing tag

## Value hints
### Default value
Show the default value if the entity field is empty `${Recipient.contact.fullname!Friend}`

### Format

Values can be shown using specified format. Format should be set after the value directive started with '?' sign
`${Customer.contact.startdate?t}` - will displays only the time part of the date

### Hints sequense

In case of usage format and default value for directive format should goes first
`${Customer.contact.enddate?t!Whenever}`

## Locale settings

Parser uses the language of the running user. That **will impact** optionset values and comparisons based on optionset values, as If directive is using the text representation of the value in comparison process.

Default formats and time zone could be changed with object of `Configurations` passed as constructor parameter.
Default values are: DateFormat = "d", DateTimeFormat = "g", TimeFormat = "t", UserZoneInfo = TimeZoneInfo.Local.

Configuration can be build for current user `Configurations.Current(orgService)` or for specified `Configurations.Get(orgService, userId)` 

