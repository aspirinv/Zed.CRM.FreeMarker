using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using System.Reflection;

namespace Zed.CRM.FreeMarker.Tests
{
    internal static class MetadataExtensions
    {
        private static FieldInfo _attributes;
        private static FieldInfo _primaryIdAttribute;

        static MetadataExtensions()
        {
            var mType = typeof(EntityMetadata);
            _attributes = mType.GetField("_attributes", BindingFlags.Instance | BindingFlags.NonPublic);
            _primaryIdAttribute = mType.GetField("_primaryIdAttribute", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static void SetName(this AttributeMetadata attribute, string name)
        {
            attribute.LogicalName = name;
            attribute.DisplayName = name.AsLabel();
        }

        internal static void AddLocalizationText(this AttributeMetadata attribute, string localization)
        {
            attribute.DisplayName.LocalizedLabels.Add(new LocalizedLabel(localization, 0));
        }

        internal static EntityMetadata Compile(this AttributeMetadata[] attributes, string name)
        {
            var result = new EntityMetadata 
            { 
                LogicalName = name,
                DisplayName = new Label(name, 0)
            };
            var mType = typeof(EntityMetadata);
            mType
                .GetField("_attributes", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(result, attributes);
            mType
                .GetField("_primaryIdAttribute", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(result, name + "id");
            return result;
        }

        internal static RetrieveAllEntitiesResponse AsResponse(this EntityMetadata[] entityMetadata)
        {
            return new RetrieveAllEntitiesResponse
            {
                Results = new ParameterCollection
                {
                    { "EntityMetadata", entityMetadata }
                }
            };
        }

        internal static RetrieveEntityResponse AsResponse(this EntityMetadata entityMetadata)
        {
            return new RetrieveEntityResponse
            {
                Results = new ParameterCollection
                {
                    { "EntityMetadata", entityMetadata }
                }
            };
        }

        internal static Label AsLabel(this string text)
        {
            return new Label(text, 0)
            {
                UserLocalizedLabel = new LocalizedLabel(text,1)
            };
        }
    }
}
