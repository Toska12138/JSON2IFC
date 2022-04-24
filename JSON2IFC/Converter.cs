using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.MeasureResource;
using System;

namespace Scan2BimConnect.Utilities
{
    public class Converter
    {
        //TODO: ToIfc element level convert
        //public IfcColumn ToIfc(jsonColumn column, IfcStore ifcStore, IfcBuilding ifcBuilding)
        //{
        //    return new IFCStructureCreater(ifcStore, ifcBuilding, null).createColumn(column, null, );
        //}
        public static IfcPropertySet ToIfc(PropertySet pSet, IfcStore ifcStore)
        {
            return ifcStore.Instances.New<IfcPropertySet>(IfcPSet =>
            {
                IfcPSet.Name = pSet.name;
                if (pSet.properties != null)
                {
                    pSet.properties.ForEach(prop => IfcPSet.HasProperties.Add(ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                {
                    singleValue.Name = prop.name;
                    prop.type = prop.type ?? throw new ArgumentNullException("IfcPropertySingleValue error: Null Type");
                    prop.value = prop.value ?? "<null>";
                    Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                    Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + (prop.type ?? "IfcText").ToString()) ?? new IfcText().GetType();
                    singleValue.NominalValue = (IfcValue?)Activator.CreateInstance(t, new Object[] { prop.value }) ?? new IfcText(prop.value.ToString() + "--Type Error: auto-generated IfcText");

                })));
                }
            });
        }
        public static PropertySet ToObject(IfcPropertySet ifcPropertySet)
        {
            return new PropertySet()
            {
                id = ifcPropertySet.GlobalId,
                name = ifcPropertySet.Name ?? "<null>",
                type = ifcPropertySet.GetType().Name,
                originalSystemId = ifcPropertySet.GlobalId,
                properties = ifcPropertySet.HasProperties.OfType<IIfcPropertySingleValue>().ToList().ConvertAll(e =>
                {
                    return new Property()
                    {
                        name = e.Name,
                        type = e.GetType().Name,
                        value = e.NominalValue.ToString() ?? "<null>"
                    };
                })
            };
        }
        public static PropertySet ToObject(IIfcPropertySet ifcPropertySet)
        {
            return new PropertySet()
            {
                id = ifcPropertySet.GlobalId,
                name = ifcPropertySet.Name ?? "<null>",
                type = ifcPropertySet.GetType().Name,
                originalSystemId = ifcPropertySet.GlobalId,
                properties = ifcPropertySet.HasProperties.OfType<IIfcPropertySingleValue>().ToList().ConvertAll(e =>
                {
                    return new Property()
                    {
                        name = e.Name,
                        type = e.NominalValue.GetType().Name,
                        value = e.NominalValue.ToString() ?? "<null>"
                    };
                })
            };
        }
        public static MetaObject ToObject(IfcProduct ifcProduct)
        {
            var propertySetsToAdd = ifcProduct.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IfcPropertySet).ToList().ConvertAll(e => { return ToObject(e.RelatingPropertyDefinition as IIfcPropertySet ?? throw new NullReferenceException("Failed to cast pSet: " + e.Name)); });
            return new MetaObject()
            {
                id = ifcProduct.GlobalId,
                name = ifcProduct.Name ?? "<null>",
                type = ifcProduct.GetType().Name,
                propertySets = propertySetsToAdd,
                propertySetIds = propertySetsToAdd.ConvertAll(e => e.id ?? throw new ArgumentNullException("pSet null ids"))
            };
        }
    }
}
