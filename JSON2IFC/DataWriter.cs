using System;
using System.IO;
using System.Text.Json;
using Xbim.Ifc;
using Xbim.IO;

namespace Scan2BimConnect.Utilities
{
    class DataWriter
    {
        public DataWriter()
        {

        }
        public Boolean writeIfc(IfcStore ifcStore, string outputIfcFilePath)
        {
            ifcStore.SaveAs(outputIfcFilePath, StorageType.Ifc);
            return true;
        }
        public Boolean WriteError(string s, string path)
        {
            FileStream fs = new FileStream(path + ".Error.txt", FileMode.Create);
            byte[] data = System.Text.Encoding.Default.GetBytes(s);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
            return true;
        }
        public Boolean writeJson(Object o, string path)
        {
            string json = JsonSerializer.Serialize(o);
            File.WriteAllText(path, json);
            return true;
        }
    }
}
