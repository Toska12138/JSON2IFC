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
            KeyValuePair<string, MetaObject> pair = Converter.ToObject(ifcProduct);
            this.metaObjects.Add(pair.Key, pair.Value);
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
