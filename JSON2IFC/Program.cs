using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xbim.Common.Step21;

namespace JSON2IFC
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
            DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            di = Directory.GetParent(di.FullName);
            XbimSchemaVersion version = XbimSchemaVersion.Ifc4;

            string outputPath = Path.Combine(di.FullName, "models");
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now:ddMMyyyy}.xbim");
            stopwatch.Start();
            Json2IfcHelper.Result res = Json2IfcHelper.GenerateIFC(version, outputIfcPath, structureJsonPath, mepJsonPath, ductJsonPath, beamJsonPath, propJsonPath, appearancePath);
            stopwatch.Stop();
            Json2IfcHelper.error_msg += "\nNo. of Elements: \t" + res.noElements.ToString();
            Json2IfcHelper.error_msg += "\nConversion Time: \t" + Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds).ToString();

        }
    }
}