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
    class DataWriter
    {
        public DataWriter()
        {

        }
        public Boolean writerIfc(IfcStore ifcStore, string outputIfcFilePath)
        {
            ifcStore.SaveAs(outputIfcFilePath, StorageType.Ifc);
            return true;
        }
        public Boolean WriteError(string s, string path)
        {
            FileStream fs = new FileStream(Path.Combine(path, "Error.txt"), FileMode.Create);
            byte[] data = System.Text.Encoding.Default.GetBytes(s);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
            return true;
        }
        public Boolean writeJson(Object o, string path)
        {
            string json = JsonSerializer.Serialize(o);
            File.WriteAllText(path + ".json", json);
            return true;
        }
    }
}
