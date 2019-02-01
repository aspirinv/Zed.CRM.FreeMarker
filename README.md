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

## Implemented directives

At the moment library contains a limited set of implmented features:

1. Basic value: ${Context.Sms.Subject}
2. Chained field value: ${Context.Sms.from.systemuser.fullname}
3. Default value:  ${Recipient.contact.fullname!Friend}
4. If directive: <#if Recipient.contact.fullname == John Doe> John sample text </#if>
