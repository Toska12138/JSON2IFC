using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scan2BimShared.StaticData.IfcEnums;

namespace Scan2BimShared.Models.IfcEntities
{
    class Style
    {
        public BuildingComponent Name { get; set; }
        public double[] RGB { get; set; }
        public double transparency { get; set; }

    }
}
