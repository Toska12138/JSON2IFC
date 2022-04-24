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
            { "IfcWallType", new List<PropertySet>() },
            { "IfcWall", new List<PropertySet>() },
            { "IfcColumnType", new List<PropertySet>() },
            { "IfcBeamType", new List<PropertySet>() },
            { "IfcWindowType", new List<PropertySet>() },
            { "IfcDoorType", new List<PropertySet>() },
            { "IfcSlabType", new List<PropertySet>()}
        };
        public IfcPropertySet generateSet(PropertySet propSet)
        {
            return ifcStore.Instances.New<IfcPropertySet>(propertySet =>
            {
                propertySet.Name = propSet.name;
                if (propSet.properties != null) propertySet.HasProperties.AddRange(propSet.properties.ConvertAll(prop => ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                 {
                     singleValue.Name = prop.name;
                     prop.type = prop.type ?? throw new ArgumentNullException("IfcPropertySingleValue error: Null Type");
                     prop.value = prop.value ?? "<null>";
                     Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                     Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + prop.type.ToString()) ?? new IfcText().GetType();
                     singleValue.NominalValue = (Activator.CreateInstance(t, new Object[] { prop.value }) as IfcValue) ?? new IfcText(prop.value + "--TypeError: IfcText auto-generated");
                 })));
            });
        }
        public void defineProperties(IfcProduct ifcProduct, Dictionary<string, List<PropertySet>> generalname_n_propertySets)
        {
            if (generalname_n_propertySets[ifcProduct.GetType().Name] != null)
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
        public static void writePropToIFC(string ifcFilePath, string metaDataPath, string outputIfcPath)
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
                        MetaModel metaModel = new DataReader().readMetaData(metaDataPath) ?? throw new ArgumentNullException("Failed to load mataModel");
                        metaModel.metaObjects.ToList().ForEach(o =>
                        {
                            if (o.type != "Model")
                            {
                                IfcProduct ifcProduct = ifcStore.Instances.OfType<IfcProduct>().First(e => e.GlobalId == o.id);
                                List<string?> ids = o.propertySets.ConvertAll(pSet => pSet.originalSystemId);
                                foreach (IfcRelDefinesByProperties ifcRelDefinesByProperties in ifcProduct.IsDefinedBy)
                                {
                                    if (!ids.Contains((ifcRelDefinesByProperties.RelatingPropertyDefinition as IfcPropertySet ?? throw new ArgumentNullException("Couldn't cast pSet")).GlobalId))
                                    {
                                        ifcRelDefinesByProperties.RelatedObjects.Remove(ifcProduct);
                                    }
                                }
                                if (o.propertySets.Count > 0)
                                {
                                    o.propertySets.ForEach(pSet =>
                                    {
                                        if (pSet.originalSystemId != null && !string.IsNullOrEmpty(pSet.originalSystemId))
                                        {
                                            IfcPropertySet ifcPropertySet = ifcStore.Instances.OfType<IfcPropertySet>().First(ifcPset => ifcPset.GlobalId == pSet.originalSystemId);
                                            ifcPropertySet.Name = pSet.name;
                                            List<IfcPropertySingleValue> elementToDelete = new List<IfcPropertySingleValue>();
                                            foreach (IfcPropertySingleValue ifcPropertySingleValue in ifcPropertySet.HasProperties)
                                            {
                                                elementToDelete.Add(ifcPropertySingleValue);
                                            }
                                            elementToDelete.ForEach(e => ifcStore.Delete(e));
                                            ifcPropertySet.HasProperties.Clear();
                                            if (pSet.properties != null) pSet.properties.ForEach(p => ifcPropertySet.HasProperties.Add(ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                             {
                                                 singleValue.Name = p.name;
                                                 p.type = p.type ?? throw new ArgumentNullException("IfcPropertySingleValue error: Null Type");
                                                 p.value = p.value ?? "<null>";
                                                 Assembly xbimAssem = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Xbim.Ifc4");
                                                 Type t = xbimAssem.GetType("Xbim.Ifc4.MeasureResource." + p.type.ToString()) ?? new IfcText().GetType();
                                                 singleValue.NominalValue = (Activator.CreateInstance(t, new Object[] { p.value }) as IfcValue) ?? new IfcText(p.value + "--TypeError: IfcText auto-generated");
                                             })));
                                        }
                                        else
                                        {
                                            Console.WriteLine("Adding pSet");
                                            ifcProduct.AddPropertySet(Converter.ToIfc(pSet, ifcStore));
                                        }
                                    });
                                }
                            }
                        });
                        txn.Commit();
                    }
                }
                List<IfcRelDefinesByProperties> relDefinesByProperties = ifcStore.Instances.OfType<IfcRelDefinesByProperties>().ToList();
                using (var txn = ifcStore.BeginTransaction("Delete Useless Pset"))
                {
                    relDefinesByProperties.ForEach(e =>
                    {
                        if (e.RelatedObjects.Count == 0)
                        {
                            List<IfcPropertySingleValue> propToDelete = new List<IfcPropertySingleValue>();
                            foreach (IfcPropertySingleValue ifcPropertySingleValue in (e.RelatingPropertyDefinition as IfcPropertySet ?? throw new ArgumentNullException("Failed to cast pSet")).HasProperties)
                            {
                                propToDelete.Add(ifcPropertySingleValue);
                            }
                            propToDelete.ForEach(p => ifcStore.Delete(p));
                            ifcStore.Delete(e);
                            ifcStore.Delete(e.RelatingPropertyDefinition as IfcPropertySet);
                        }
                    });
                    txn.Commit();
                }
                new DataWriter().writeIfc(ifcStore, outputIfcPath);
            }
        }
    }
    public class Property
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public string? value { get; set; }
    }
    public class PropertySet
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? type { get; set; }
        public string? originalSystemId { get; set; }
        public List<Property>? properties { get; set; }
        public IfcPropertySet ToIfcPropertySet(IfcStore ifcStore)
        {
            return new PropertyAgent(ifcStore).generateSet(this);
        }
    }
}
