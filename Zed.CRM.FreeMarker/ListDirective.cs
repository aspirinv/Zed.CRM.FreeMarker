using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zed.CRM.FreeMarker.Interfaces;

namespace Zed.CRM.FreeMarker
{
    public class ListDirective : IDirectiveParser
    {
        public MetadataManager Metadata { get; }
        private string _collection;
        private string _item;

        public ListDirective(MetadataManager metadata)
        {
            Metadata = metadata;
        }

        public string Render(Dictionary<string, Entity> source, Dictionary<string, IList<IPlaceholder>> contentItems)
        {
            var items = contentItems["main"];
            var itemSplit = _collection.Split('.');
            var entity = source[itemSplit[0]];
            var values = entity[string.Join(".", itemSplit.Skip(1).ToArray())] as IEnumerable<Entity>;

            return string.Concat(values.Select(v =>
                string.Concat(items.OrderBy(item => item.Position)
                    .Select(holder => holder.Content(new Dictionary<string, Entity> { [_item] = v })))));
        }

        public void SetValue(string value)
        {
            value = value.CleanUpMetadataName();
            var split = value.Split(new[] { " as " }, StringSplitOptions.RemoveEmptyEntries);
            if(split.Length != 2)
            {
                throw new Exception($"Value: {value} is not supported as a list directive");
            }
            _collection = split[0];
            _item = split[1];
        }
    }
}
