using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;

namespace Scan2BimConnect.Utilities
{
    class PropertyAgent
    {
        IfcStore ifcStore { get; }
        public PropertyAgent(IfcStore ifcstore)
        {
            this.ifcStore = ifcstore;
        }
        public static Dictionary<string, List<PropertySet>> defaultProperties = new Dictionary<string, List<PropertySet>>
        {
            { "IfcWallType", null },
            { "IfcWall", null },
            { "IfcColumnType", null },
            { "IfcBeamType", null },
            { "IfcWindowType", null },
            { "IfcDoorType", null },
            { "IfcSlabType", null}
        };
        public IfcPropertySet generateSet(PropertySet propSet)
        {
            return ifcStore.Instances.New<IfcPropertySet>(propertySet =>
            {
                propertySet.Name = propSet.name;
                propertySet.HasProperties.AddRange(propSet.properties.ConvertAll(prop => ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                {
                    singleValue.Name = prop.name;
                    Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                    Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + prop.type.ToString(), true);
                    singleValue.NominalValue = (IfcValue)Activator.CreateInstance(t, new Object[] { prop.value });
                })));
            });
        }
        public void defineProperties(IfcProduct ifcProduct, Dictionary<string, List<PropertySet>> generalname_n_propertySets)
        {
            if(generalname_n_propertySets[ifcProduct.GetType().Name] != null)
            {
                generalname_n_propertySets[ifcProduct.GetType().Name].ConvertAll(props => generateSet(props)).ForEach(props =>
                {
                    ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                    {
                        relDefinesByProperties.RelatedObjects.Add(ifcProduct);
                        relDefinesByProperties.RelatingPropertyDefinition = props;
                    });
                });
            }
        }
        public void defineProperties(IfcProduct ifcProduct, List<PropertySet> propertySets)
        {
            if (propertySets != null)
            {
                propertySets.ConvertAll(props => generateSet(props)).ForEach(props =>
                {
                    ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                    {
                        relDefinesByProperties.RelatedObjects.Add(ifcProduct);
                        relDefinesByProperties.RelatingPropertyDefinition = props;
                    });
                });
            }
        }
        public void defineProperties(IfcProduct ifcProduct, PropertySet propertySet)
        {
            if (propertySet != null)
            {
                ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                {
                    relDefinesByProperties.RelatedObjects.Add(ifcProduct);
                    relDefinesByProperties.RelatingPropertyDefinition = propertySet.ToIfcPropertySet(ifcStore);
                });
            }
        }
        public static void attachPropsToIfc(string ifcFilePath, string metaDataPath, string outputIfcPath)
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "SJ-NTU Corp Lab",
                ApplicationFullName = "JSON2IFC",
                ApplicationIdentifier = "JSON2IFC",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "SJ-NTU Corp Lab",
                EditorsGivenName = "",
                EditorsOrganisationName = "SJ-NTU Corp Lab"
            };
            using (var ifcStore = IfcStore.Open(ifcFilePath, editor))
            {
                if (!string.IsNullOrEmpty(metaDataPath) && File.Exists(metaDataPath))
                {
                    using (var txn = ifcStore.BeginTransaction("Defines Properties"))
                    {
                        Dictionary<string, MetaObject> metaObjects = new DataReader().readMetaData(metaDataPath);

                        metaObjects.ToList().ForEach(e =>
                        {
                            IfcElement ifcElement = ifcStore.Instances.OfType<IfcElement>().Where(i => i.GetType().Name == e.Value.type).FirstOrDefault(ii => ii.GlobalId == e.Key);
                            new PropertyAgent(ifcStore).defineProperties(ifcElement, e.Value.propertySets);
                        });
                        new DataWriter().writeIfc(ifcStore, outputIfcPath);
                    }
                }
            }
        }
    }
    public class Property
    {
        public string name { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }
    public class PropertySet
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string originalSystemId { get; set; }
        public List<Property> properties { get; set; }
        public IfcPropertySet ToIfcPropertySet(IfcStore ifcStore)
        {
            return new PropertyAgent(ifcStore).generateSet(this);
        }
    }
}
