﻿using System;
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
            fullNameField.AddLocalizationText("Full Name");

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
        public void HrefFormatValueTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            var contact = new Entity("contact", customerId)
            {
                ["fullname"] = "Brunhilde Semel"
            };
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity> { contact }));

            var template = "Follow the link <a href='https://mysite.com?user=${Customer.contact.fullname?href}'>${Customer.contact.fullname}</a>";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Follow the link <a href='https://mysite.com?user=Brunhilde%20Semel'>Brunhilde Semel</a>", result);
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

        [TestMethod]
        public void IfEncodedTemplateParseTest()
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

            var template = "&lt;#if Customer.contact.gender == Female&gt;Mrs<#elseif Customer.contact.gender == Male&gt;Mr&lt;#elseif Customer.contact.gender == Div>Msr&lt;#else&gt;Dear&lt;/#if&gt;>, ${Customer.contact.fullname}";
            var parser = new FreeMarkerParser(service.Object, template, Configurations.Default);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mrs>, Brunhilde Semel", result);
            contact["gender"] = new OptionSetValue(0);
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mr>, Brunhilde Semel", result);

            contact["gender"] = new OptionSetValue(2);
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Msr>, Brunhilde Semel", result);

            contact["gender"] = null;
            result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Dear>, Brunhilde Semel", result);
        }

        [TestMethod]
        public void ReplaceValueTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata }.AsResponse());
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
            parser.OnEntityRetrieved += (s, e) => { e.Entity["fullname"] = "Stranger"; };
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Hi, Stranger", result);
        }



        [TestMethod]
        public void NowFunctionTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                 => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity>
                {
                    new Entity("contact", customerId)
                }));

            var template = "Recieve it at ${.now?t} Return at ${.now?d!Whenever}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual($"Recieve it at {DateTime.UtcNow.ToLocalTime():t} Return at {DateTime.UtcNow.ToLocalTime():d}", result);
        }

        [TestMethod]
        public void NewLineTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                .Returns(_contactMetadata.AsResponse());

            var contact = new Entity("contact", customerId)
            {
                ["fullname"] = "Brunhilde Semel"
            };
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity> { contact }));

            var template = @"Dear ${Customer.contact.Full 
Name}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Dear Brunhilde Semel", result);
        }

        [TestMethod]
        public void DirectiveNewLineTemplateParseTest()
        {
            var customerId = Guid.NewGuid();

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata }.AsResponse());
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

            var template = @"<#if
Customer.contact.gender = = Female>Mrs</#if> ${Customer.contact.Full 
Name}";
            var parser = new FreeMarkerParser(service.Object, template);
            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Mrs Brunhilde Semel", result);
        }

        [TestMethod]
        public void ListParseTest()
        {
            var customerId = Guid.NewGuid();

            var nameField = new AttributeMetadata();
            nameField.SetName("name");
            nameField.AddLocalizationText("Name");

            var subMetadata = new[] { nameField }.Compile("sub");

            var service = new Mock<IOrganizationService>();
            service.Setup(s => s.Execute(It.IsAny<RetrieveAllEntitiesRequest>()))
                .Returns(new[] { _contactMetadata, subMetadata }.AsResponse());
            service.Setup(s => s.Execute(It.Is<RetrieveEntityRequest>(r=>r.LogicalName == _contactMetadata.LogicalName)))
                .Returns(_contactMetadata.AsResponse());
            service.Setup(s => s.Execute(It.Is<RetrieveEntityRequest>(r=>r.LogicalName == subMetadata.LogicalName)))
                .Returns(subMetadata.AsResponse());

            var contact = new Entity("contact", customerId)
            {
                ["fullname"] = "Brunhilde Semel",
                ["gender"] = new OptionSetValue(1)
            };
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(e
                => e.Criteria.Conditions.Any(c => c.AttributeName == "contactid" && c.Values.Contains(customerId)))))
                .Returns(new EntityCollection(new List<Entity> { contact }));

            var template = @"${Customer.contact.Full Name}<#list Customer.items as item><p>${item.sub.name}</p></#list>";
            var parser = new FreeMarkerParser(service.Object, template);
            parser.OnEntityRetrieved += (s,e)=> e.Entity["items"] = new[] 
            { 
                new Entity("sub")
                {
                    ["name"] = "a"
                },
                new Entity("sub")
                {
                    ["name"] = "b"
                },
                new Entity("sub")
                {
                    ["name"] = "c"
                }
            };

            var result = parser.Produce(new Dictionary<string, EntityReference>
            {
                ["Customer"] = new EntityReference("contact", customerId)
            });

            Assert.AreEqual("Brunhilde Semel<p>a</p><p>b</p><p>c</p>", result);
        }
    }
}
