using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Scan2BimConnect.Utilities;
using Xbim.Common.Step21;

namespace Scan2BimConnect
{
    public class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            var structureJsonPath = @".\Data\recon.json";
            var mepJsonPath = @".\Data\pipe.json";
            var ductJsonPath = @".\Data\duct.json";
            var beamJsonPath = @".\Data\beam.json";//passed test
            var propJsonPath = @".\Data\properties.json";
            var appearancePath = @".\Data\appearance.json";
            var metaDataPath = @".\Data\metaData.json";
            DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            di = Directory.GetParent(di.FullName);
            XbimSchemaVersion version = XbimSchemaVersion.Ifc4;
            new DataWriter().writeJson(PropertyAgent.defaultProperties, @"defaults.json");
            string outputPath = Path.Combine(di.FullName, "models");
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now:ddMMyyyy}.xbim");
            stopwatch.Start();
            Json2IfcHelper.Result res = Json2IfcHelper.GenerateIFC(version, outputIfcPath, structureJsonPath, mepJsonPath, ductJsonPath, beamJsonPath, propJsonPath, appearancePath);
            stopwatch.Stop();
            Json2IfcHelper.error_msg += "\nNo. of Elements: \t" + res.noElements.ToString();
            Json2IfcHelper.error_msg += "\nConversion Time: \t" + Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds).ToString();

            //XbimSchemaVersion version = XbimSchemaVersion.Ifc4;
            //DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            //di = Directory.GetParent(di.FullName);
            //string outputPath = Path.Combine(di.FullName, "models");
            //if (!Directory.Exists(outputPath))
            //    Directory.CreateDirectory(outputPath);
            //string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now:ddMMyyyy}.xbim");
            ////var ifcFilePath = @".\Data\Model_Ifc4_10032022_no_prop.ifc";
            ////var metaDataPath = @".\Data\Model_Ifc4_10032022_temp.json";
            //PropertyAgent.attachPropsToIfc(ifcFilePath, metaDataPath, outputIfcPath);

            //var ifcFilePath = @".\Data\ini_model.ifc";
            //var metaDataPath = @".\Data\metaData.json";
            //XbimSchemaVersion version = XbimSchemaVersion.Ifc4;
            //DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            //di = Directory.GetParent(di.FullName);
            //string outputPath = Path.Combine(di.FullName, "models");
            //if (!Directory.Exists(outputPath))
            //    Directory.CreateDirectory(outputPath);
            //string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now:ddMMyyyy}.xbim");
            //PropertyAgent.writePropToIFC(ifcFilePath, metaDataPath, outputIfcPath);
            //Console.WriteLine("Done");
            //Console.ReadKey();

        }
    }
}