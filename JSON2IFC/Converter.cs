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
    public class Converter
    {
        //TODO: ToIfc element level convert
        //public IfcColumn ToIfc(jsonColumn column, IfcStore ifcStore, IfcBuilding ifcBuilding)
        //{
        //    return new IFCStructureCreater(ifcStore, ifcBuilding, null).createColumn(column, null, );
        //}
        public static PropertySet ToObject(IfcPropertySet ifcPropertySet)
        {
            return new PropertySet()
            {
                name = ifcPropertySet.Name,
                properties = ifcPropertySet.HasProperties.OfType<IIfcPropertySingleValue>().ToList().ConvertAll(e =>
                {
                    return new Property()
                    {
                        name = e.Name,
                        type = e.GetType().Name,
                        value = (e as IfcPropertySingleValue).NominalValue.ToString()
                    };
                })
            };
        }
        public static PropertySet ToObject(IIfcPropertySet ifcPropertySet)
        {
            return new PropertySet()
            {
                name = ifcPropertySet.Name,
                properties = ifcPropertySet.HasProperties.OfType<IIfcPropertySingleValue>().ToList().ConvertAll(e =>
                {
                    return new Property()
                    {
                        name = e.Name,
                        type = e.NominalValue.GetType().Name,
                        value = e.NominalValue.ToString()
                    };
                })
            };
        }
       public static MetaObject ToObject(IfcProduct ifcProduct)
        {
            return new MetaObject()
            {
                name = ifcProduct.Name,
                type = ifcProduct.GetType().Name,
                id = ifcProduct.GlobalId,
                propertySets = ifcProduct.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IfcPropertySet).ToList().ConvertAll(e => { return ToObject(e.RelatingPropertyDefinition as IIfcPropertySet); })
            };
        }
    }
}
