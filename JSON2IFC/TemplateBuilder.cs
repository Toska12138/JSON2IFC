using System.Collections.Generic;
using Xbim.Ifc4.Kernel;
using System;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;

namespace Scan2BimConnect.Utilities
{
    public class TemplateBuilder
    {
        public MetaModel metaModel { get; set; }
        public IfcStore ifcStore { get; set; }
        public TemplateBuilder(IfcStore ifcStore)
        {
            this.metaModel = new MetaModel();
            this.ifcStore = ifcStore;
        }
        public void addObject(IfcProduct ifcProduct, IfcBuildingStorey ifcBuildingStorey)
        {
            new PropertyAgent(ifcStore).defineProperties(ifcProduct, new PropertySet() { name = "Annotations", properties = new List<Property>() { new Property() { name = "Created By", type = "IfcText", value = "SJ-NTU Corp Lab" } } });

            var objectToAdd = Converter.ToObject(ifcProduct);

            objectToAdd.parent = ifcBuildingStorey.GlobalId;

            this.metaModel.metaObjects.Add(objectToAdd);

            this.metaModel.propertySets.AddRange(objectToAdd.propertySets);
        }
        public void addObjects(List<IfcProduct> ifcProducts, IfcBuildingStorey ifcBuildingStorey)
        {
            foreach (IfcProduct ifcProduct in ifcProducts)
            {
                this.addObject(ifcProduct, ifcBuildingStorey);
            }
        }
    }
    public class MetaObject
    {
        public string id { get; set; } = default!;
        public string name { get; set; } = default!;
        public string type { get; set; } = default!;
        public string parent { get; set; } = default!;
        public List<PropertySet> propertySets { get; set; } = new List<PropertySet>();
        public List<string> propertySetIds { get; set; } = new List<string>();
    }
    public class MetaModel
    {
        public MetaModel()
        {
            // this.metaObjects = new Dictionary<string, MetaObject>();
            this.metaObjects = new List<MetaObject>();
            this.propertySets = new List<PropertySet>();
            this.author = "SJ-NTU Corp Lab";
            this.createdAt = "SJ-NTU Corp Lab";
            this.creatingApplication = "JSON2IFC";
            this.schema = "IFC4";
        }
        public string author { get; set; }
        public string createdAt { get; set; }
        public string creatingApplication { get; set; }
        public string schema { get; set; }
        public string projectId { get; set; } = default!;
        public string id { get; set; } = default!;
        public List<MetaObject> metaObjects { get; set; }
        public List<PropertySet> propertySets { get; set; }
    }
}
