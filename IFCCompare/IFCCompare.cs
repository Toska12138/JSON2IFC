using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IFCCompare
{
    public class IFCCompare
    {
        public static void test()
        {
            using (var model = IfcStore.Open("SampleHouse.ifc"))
            {
                var slabs = model.Instances.OfType<IIfcSlab>().ToList();
                foreach (var slab in slabs)
                {
                    var volume = GetVolume(slab);
                    if (volume != null)
                    {
                        if (volume.UnderlyingSystemType == typeof(double))
                            Console.WriteLine(volume.Value);
                        else
                            Console.WriteLine(volume.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Volume is null");
                    }
                }
            }
            Console.ReadKey();
        }
        private static IIfcValue GetVolume(IIfcProduct product)
        {
            var volume = product.IsDefinedBy
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcElementQuantity>()
                .SelectMany(qset => qset.Quantities)
                .OfType<IIfcQuantityVolume>()
                .FirstOrDefault()?.VolumeValue;
            if (volume != null)
                return volume;
            return GetProperty(product, "Volume");
        }
        private static IIfcValue GetProperty(IIfcProduct product, string name)
        {
            return
                product.IsDefinedBy
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcPropertySet>()
                .SelectMany(pset => pset.HasProperties)
                .OfType<IIfcPropertySingleValue>()
                .Where(p =>
                    string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase) ||
                    p.Name.ToString().ToLower().Contains(name.ToLower()))
                .FirstOrDefault()?.NominalValue;
        }
    }
}
