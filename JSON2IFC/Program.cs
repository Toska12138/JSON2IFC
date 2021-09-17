using System;
using System.IO;
using System.Reflection;
using Xbim.Common.Step21;

namespace JSON2IFC
{
    public class Program
    {
        static void Main(string[] args)
        {
            var structureJsonPath = @".\Data\recon.json";
            var mepJsonPath = @".\Data\pipe.json";
            DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            di = Directory.GetParent(di.FullName);
            XbimSchemaVersion version = XbimSchemaVersion.Ifc4;

            string outputPath = Path.Combine(di.FullName, "models");
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            string outputIfcPath = Path.Combine(outputPath, "Model_" + version.ToString() + $"_{DateTime.Now.ToString("ddMMyyyy")}.xbim");
            string outputErrorPath = outputPath;
            Json2IfcHelper.GenerateIFC(version, outputIfcPath, structureJsonPath, mepJsonPath);
            if(Json2IfcHelper.error_msg != null) Json2IfcHelper.WriteError(Json2IfcHelper.error_msg, outputErrorPath);
        }
    }
}