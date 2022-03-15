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
    public class TemplateBuilder
    {
        public MetaModel metaModel { get; set; }
        public TemplateBuilder()
        {
            this.metaModel = new MetaModel();
        }
        public void addObject(IfcProduct ifcProduct)
        {
            this.metaModel.metaObjects.Add(Converter.ToObject(ifcProduct));
        }
        public void addObject(List<IfcProduct> ifcProducts)
        {
            foreach (IfcProduct ifcProduct in ifcProducts)
            {
                this.addObject(ifcProduct);
            }
        }
    }
    public class MetaObject
    {
        public string name { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public List<PropertySet> propertySets { get; set; }
    }
    public class MetaModel
    {
        public MetaModel(){
            this.metaObjects = new List<MetaObject>();
            this.revisionId = "none";
        }
        public string projectId { get; set; }
        public string revisionId { get; set; }
        public string id { get; set; }
        public List<MetaObject> metaObjects { get; set; }
    }
}
