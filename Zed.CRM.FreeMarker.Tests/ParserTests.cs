using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace Zed.CRM.FreeMarker.Tests
{
    [TestClass]
    public class ParserTests
    {
        private EntityMetadata _contactMetadata;

        [TestInitialize]
        public void SetUp()
        {
            var fullNameField = new AttributeMetadata();
            fullNameField.SetName("fullname");
            var genderField = new PicklistAttributeMetadata();
            genderField.SetName("gender");
            genderField.OptionSet = new OptionSetMetadata(new OptionMetadataCollection(new List<OptionMetadata>
            {
                new OptionMetadata("Male".AsLabel(), 0),
                new OptionMetadata("Female".AsLabel(), 1),
                new OptionMetadata("Div".AsLabel(), 2)
            }));

            var startField = new DateTimeAttributeMetadata(DateTimeFormat.DateAndTime);
            startField.SetName("startdate");
            var endField = new DateTimeAttributeMetadata(DateTimeFormat.DateAndTime);
            endField.SetName("enddate");

            _contactMetadata = new[] { fullNameField, genderField, startField, endField }.Compile("contact");
        }

        [TestMethod]
        public void SimpleTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new [] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                 => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity>
                {
                    new Entity("contact", customerId) { ["fullname"] = "Alexander Spirin" }
                }));

            var template = "Hi, ${Customer.contact.fullname}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Hi, Alexander Spirin", result);
        }

        [TestMethod]
        public void DefaultValueTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new [] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                 => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity>
                {
                    new Entity("contact", customerId)
                }));

            var template = "Hi, ${Customer.contact.fullname!Noname}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Hi, Noname", result);

            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = null
            });

            Assert.AreEqual("Hi, Noname", result);
        }
        
        [TestMethod]
        public void FormatValueTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new [] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            var startDate = new DateTime(2019, 05, 20, 20, 30, 00, DateTimeKind.Utc);
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                 => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity>
                {
                    new Entity("contact", customerId)
                    {
                        ["startdate"] = startDate
                    }
                }));

            var template = "Recieve it at ${Customer.contact.startdate?t} Return at ${Customer.contact.enddate?t!Whenever}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual($"Recieve it at {startDate.ToLocalTime().ToString("t")} Return at Whenever", result);
        }

        [TestMethod]
        public void IfTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new [] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            var contact = new Entity("contact", customerId)
            {
                ["fullname"] = "Brunhilde Semel",
                ["gender"] = new OptionSetValue(1)
            };
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                 => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity> { contact }));

            var template = "<#if Customer.contact.gender == Female>Mrs<#elseif Customer.contact.gender == Male>Mr<#elseif Customer.contact.gender == Div>Msr<#else>Dear</#if>, ${Customer.contact.fullname}";
            var parser = new FreeMarkerParser(service.Object, template, Configurations.Default);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mrs, Brunhilde Semel", result);
            contact["gender"] = new OptionSetValue(0);
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mr, Brunhilde Semel", result);

            contact["gender"] = new OptionSetValue(2);
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Msr, Brunhilde Semel", result);

            contact["gender"] = null;
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Dear, Brunhilde Semel", result);
        }

    }
}
