using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgServiceElements;

namespace JSON2IFC
{
    class AttachProp
    {
        public AttachProp(string ifcFilePath, string metaFilePath)
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
                using (var txn = ifcStore.BeginTransaction("Defines Properties"))
                {
                    XbimSchemaVersion version = XbimSchemaVersion.Ifc4;
                    DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                    di = Directory.GetParent(di.FullName);
                    string outputPath = Path.Combine(di.FullName, "models");
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);
                    string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now:ddMMyyyy}.xbim");

                    Dictionary<string, MetaObject> metaObjects = new DataReader().readMetaData(metaFilePath);

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
