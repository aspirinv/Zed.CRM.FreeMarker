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

            _contactMetadata = new[] { fullNameField, genderField }.Compile("contact");
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
        public void IfTemplateParseTest()
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
                    {
                        ["fullname"] = "Brunhilde Semel",
                        ["gender"] = new OptionSetValue(1)
                    }
                }));

            var template = "<#if Customer.contact.gender == Female>Mrs</#if><#if Customer.contact.gender == Male>Mr</#if>, ${Customer.contact.fullname}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mrs, Brunhilde Semel", result);
        }
    }
}
