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
                pSet.properties.ForEach(prop => IfcPSet.HasProperties.Add(ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                {
                    singleValue.Name = prop.name;
                    Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                    Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + prop.type.ToString(), true);
                    singleValue.NominalValue = (IfcValue)Activator.CreateInstance(t, new Object[] { prop.value });
                })));
            });
        }
        public static PropertySet ToObject(IfcPropertySet ifcPropertySet)
        {
            return new PropertySet()
            {
                id = ifcPropertySet.GlobalId,
                name = ifcPropertySet.Name,
                type = ifcPropertySet.GetType().Name,
                originalSystemId = ifcPropertySet.GlobalId,
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
                id = ifcPropertySet.GlobalId,
                name = ifcPropertySet.Name,
                type = ifcPropertySet.GetType().Name,
                originalSystemId = ifcPropertySet.GlobalId,
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
            var propertySetsToAdd = ifcProduct.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IfcPropertySet).ToList().ConvertAll(e => { return ToObject(e.RelatingPropertyDefinition as IIfcPropertySet); });
            return new MetaObject()
            {
                id = ifcProduct.GlobalId,
                name = ifcProduct.Name,
                type = ifcProduct.GetType().Name,
                propertySets = propertySetsToAdd,
                propertySetIds = propertySetsToAdd.ConvertAll(e => e.id)
            };
        }
    }
}
