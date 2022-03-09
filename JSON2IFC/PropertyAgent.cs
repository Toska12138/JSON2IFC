using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4;
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

namespace JSON2IFC
{
    class PropertyAgent
    {
        IfcStore ifcStore { get; }
        public PropertyAgent(IfcStore ifcstore)
        {
            this.ifcStore = ifcstore;
        }
        public IfcPropertySet generateSet(string Name, Dictionary<string, Dictionary<string, string>> values)
        {
            return ifcStore.Instances.New<IfcPropertySet>(propertySet =>
            {
                propertySet.Name = Name;
                propertySet.HasProperties.AddRange(values.Select(prop => ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                {
                    singleValue.Name = prop.Key;
                    Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                    Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + prop.Value.First().Key.ToString(), true);
                    singleValue.NominalValue = (IfcValue)Activator.CreateInstance(t, new Object[] { prop.Value.First().Value });
                })));
            });
        }
        public void defineProperties(IfcProduct ifcProduct, Dictionary<string, Dictionary<string, Dictionary<string, string>>> properties)
        {
            properties.Select(props => generateSet(props.Key, props.Value)).ToList().ForEach(props => 
            {
                ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                {
                    relDefinesByProperties.RelatedObjects.Add(ifcProduct);
                    relDefinesByProperties.RelatingPropertyDefinition = props;
                });
            });
        }
    }
}
