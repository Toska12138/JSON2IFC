using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.HvacDomain;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.TopologyResource;
using Xbim.IO;
using static JSON2IFC.Material;
using static JSON2IFC.SJSONPlugin;
using static JSON2IFC.IFCStructureCreater;

namespace JSON2IFC
{
    class TemplateBuilder
    {
        public Dictionary<string,MetaObject> metaObjects { get; set; }
        public TemplateBuilder()
        {
            this.metaObjects = new Dictionary<string, MetaObject>();
        }
        public void addObject(IfcProduct ifcProduct)
        {
            Console.WriteLine(ifcProduct.Name);
            Console.WriteLine(ifcProduct.GetType().Name);
            Console.WriteLine(ifcProduct.GlobalId);
            Console.WriteLine();
            this.metaObjects.Add(ifcProduct.GlobalId, new MetaObject() 
            { 
                name = ifcProduct.Name,
                type = ifcProduct.GetType().Name,
                id = ifcProduct.GlobalId,
                propertySets = ifcProduct.IsDefinedBy.ToList().ConvertAll(e => 
                {
                    return new PropertySet()
                    {
                        name = (e.RelatingPropertyDefinition as IfcPropertySet).Name,
                        properties = (e.RelatingPropertyDefinition as IfcPropertySet).HasProperties.ToList().ConvertAll(props => (IfcPropertySingleValue)props).ConvertAll(p => new Property() { name = p.Name, label = p.GetType().Name, value = p.NominalValue.ToString() })
                    };
                    })
            });
        }
        public void addObject(List<IfcProduct> ifcProducts)
        {
            foreach(IfcProduct ifcProduct in ifcProducts)
            {
                this.addObject(ifcProduct);
            }
        }
    }
    class MetaObject
    {
        public string name { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public List<PropertySet> propertySets { get; set; }
    }
}
