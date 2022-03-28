using System.Collections.Generic;
using Xbim.Ifc4.Kernel;
using System;
using Xbim.Ifc;


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
        public void addObject(IfcProduct ifcProduct)
        {
            new PropertyAgent(ifcStore).defineProperties(ifcProduct, new PropertySet() { name = "Annotations", properties = new List<Property>() { new Property() { name = "Created By", type = "IfcText", value = "SJ-NTU Corp Lab" } } });

            var objectToAdd = Converter.ToObject(ifcProduct);

            objectToAdd.parent = this.metaModel.metaObjects.Find(e => e.type == "Model").id;

            this.metaModel.metaObjects.Add(objectToAdd);

            this.metaModel.propertySets.AddRange(objectToAdd.propertySets);
        }
        public void addObjects(List<IfcProduct> ifcProducts)
        {
            foreach (IfcProduct ifcProduct in ifcProducts)
            {
                this.addObject(ifcProduct);
            }
        }
    }
    public class MetaObject
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string parent {get; set;}
        public List<PropertySet> propertySets { get; set; }
        public List<string> propertySetIds { get; set; }
    }
    public class MetaModel
    {
        public MetaModel(){
            // this.metaObjects = new Dictionary<string, MetaObject>();
            this.metaObjects = new List<MetaObject>();
            this.propertySets = new List<PropertySet>();
            this.metaObjects.Add(new MetaObject(){ type = "Model", propertySets = new List<PropertySet>(), propertySetIds = new List<string>() });
            this.author = "SJ-NTU Corp Lab";
            this.createdAt = "SJ-NTU Corp Lab";
            this.creatingApplication = "JSON2IFC";
            this.schema = "IFC4";
        }
        public string author { get; set; }
        public string createdAt { get; set; }
        public string creatingApplication { get; set; }
        public string schema { get; set; }
        public string projectId { get; set; }
        public string id { get; set; }
        public List<MetaObject> metaObjects { get; set; }
        public List<PropertySet> propertySets { get; set; }
    }
}
