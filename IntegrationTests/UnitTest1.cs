using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Zed.CRM.FreeMarker;

namespace IntegrationTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var srv = new OrgServiceFactory().CreateOrganizationService(null);
            var context = new EntityReference("contact", new Guid("099EC552-D270-E911-A82C-000D3AB182A9"));
            var template = @"<#if Context.contact.Gender == Male>Sehr geehrter Herr</#if>
<#if Context.contact.Gender == Female>Sehr geehrte Frau</#if>
<#if Context.contact.Title Academic>
  <#if Context.contact.Title Academic.Title.Plus Name == false>
    ${Context.contact.Title Academic.Title.Name}
  <#else>
    <#if Context.contact.Title Academic.Title.Position == false>${Context.contact.Title Academic.Title.Name}</#if>
    ${Context.contact.Last Name}
    <#if Context.contact.Title Academic.Title.Position == true>${Context.contact.Title Academic.Title.Name}</#if>
  </#if>
  <#else>
    ${Context.contact.Last Name}
</#if>
";

            template = @"${Context.contact.Gender}";
            var actual = new FreeMarkerParser(srv, template).Produce(new Dictionary<string, EntityReference>
            {
                ["Context"] = context
            });

            Assert.Inconclusive(Environment.NewLine + Environment.NewLine + 
                Regex.Replace(actual.Replace(Environment.NewLine, " "), @"\s+", " "));
        }
    }


    class OrgServiceFactory : IOrganizationServiceFactory
    {
        string key = "aFzej0JrgdGEeTSCJuonGoTD+dZK27eHjmnA0IvQC8Q=";
        string appId = "0dd8469d-77ae-4d02-8b00-fa9c9aa8c5b0";
        string url = "https://devkommapp.crm4.dynamics.com";
        string tenant = "BMWPressAT.onmicrosoft.com";
        public OrgServiceFactory() { }
        public OrgServiceFactory(string url)
        {
            this.url = url;
        }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            string authority = new Uri(new Uri("https://login.microsoftonline.com/"), tenant).ToString();
            var authContext = new AuthenticationContext(authority);
            var clientCredential = new ClientCredential(appId, key);
            var x = authContext.AcquireTokenAsync(url, clientCredential).Result;

            var srvUrl = new Uri($"{url}/XRMServices/2011/Organization.svc/web?SdkClientVersion=9.0");
            return new OrganizationWebProxyClient(srvUrl, TimeSpan.MaxValue, false)
            {
                HeaderToken = x.AccessToken,
                CallerId = userId ?? Guid.Empty
            };
        }
    }
}
