using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scan2BimShared.Models.IfcEntities
{
    public class Style
    {
        public BuildingComponent Name { get; set; }
        public double[] RGB { get; set; }
        public double transparency { get; set; }
    }
    public class jsonStructure
    {
        public jsonBeam[] Beam { get; set; }
        public jsonCeiling[] Ceiling { get; set; }
        public jsonColumn[] Column { get; set; }
        public jsonDoor[] Door { get; set; }
        public jsonFloor[] Floor { get; set; }
        public jsonSlab[] Slab { get; set; }
        public jsonWall[] Wall { get; set; }
        public jsonWindow[] Window { get; set; }

    }
    public class jsonWindow
    {
        public jsonXYZ endPoint { get; set; }
        public double heightOfBottomFace { get; set; }
        public jsonXYZ startPoint { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public int ID { get; set; }
        public jsonXYZ location
        {
            get { return (this.startPoint + this.endPoint) / 2 + new jsonXYZ(0, 0, this.heightOfBottomFace); }
            set { }
        }
        public double length
        {
            get { return this.startPoint.distanceTo(this.endPoint); }
            set { }
        }
    }
    public class jsonFloor
    {
        public jsonXYZ[] baseProfile { get; set; }
        public double thickness { get; set; }
        public int ID { get; set; }
    }
    public class jsonSlab
    {
        public jsonXYZ[] baseProfile { get; set; }
        public double thickness { get; set; }
        public double bottom { get; set; }
        public int ID { get; set; }
        public jsonXYZ location => new jsonXYZ(0, 0, bottom);
    }
    public class jsonCeiling
    {
        public jsonXYZ[] baseProfile { get; set; }
        public double thickness { get; set; }
        public int ID { get; set; }
    }
    public class jsonBeam
    {
        public jsonXYZ[] baseProfile { get; set; }
        public double heightOfBottomFace { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public int id { get; set; }
        public jsonXYZ startPoint
        {
            get { return (this.baseProfile[1].distanceTo(this.baseProfile[0]) <= this.baseProfile[1].distanceTo(this.baseProfile[2]) ? (this.baseProfile[0] + this.baseProfile[1]) / 2 : (this.baseProfile[1] + this.baseProfile[2]) / 2) + new jsonXYZ(0, 0, this.heightOfBottomFace + this.height / 2); }
            set { }
        }
        public jsonXYZ endPoint
        {
            get { return (this.baseProfile[3].distanceTo(this.baseProfile[0]) <= this.baseProfile[3].distanceTo(this.baseProfile[2]) ? (this.baseProfile[0] + this.baseProfile[3]) / 2 : (this.baseProfile[3] + this.baseProfile[2]) / 2) + new jsonXYZ(0, 0, this.heightOfBottomFace + this.height / 2); }
            set { }
        }
        public double length { get; set; }
        public jsonHole[] HoleList { get; set; }
        public jsonXYZ lengthDirection { get; set; }
        public jsonXYZ startPointInfile { get; set; }
        public jsonXYZ endPointInfile { get; set; }
    }
    public class jsonColumn
    {
        public double height { get; set; }
        public jsonXYZ locationPoint { get; set; }
        public double rotationalAngleInRadius { get; set; }
        public double length { get; set; }
        public double width { get; set; }
        public int id { get; set; }
    }
    public class jsonDoor
    {
        public jsonXYZ endPoint { get; set; }
        public jsonXYZ startPoint { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public int ID { get; set; }
        public jsonXYZ location
        {
            get { return (this.startPoint + this.endPoint) / 2; }
            set { }
        }
        public double length
        {
            get { return this.startPoint.distanceTo(this.endPoint); }
            set { }
        }
    }
    public class jsonWall
    {
        public jsonXYZ endPoint { get; set; }
        public jsonXYZ startPoint { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public int id
        { get; set; }
        public WallConnection[] Connections { get; set; }
        public jsonHole[] HoleList { get; set; }
        public jsonXYZ location
        {
            get { return (this.startPoint + this.endPoint) / 2; }
            set { }
        }
        public double length { get { return this.startPoint.distanceTo(this.endPoint); } set { } }

    }
    public class WallConnection
    {
        public double connected_ID { get; set; }
        public double influence { get; set; }
        public double type { get; set; }
    }
    public class jsonHole
    {
        public jsonXYZ[] HoleBaseProfile { get; set; }
        public double height { get; set; }
        public double width
        {
            get { return Math.Min(this.HoleBaseProfile[0].distanceTo(this.HoleBaseProfile[1]), this.HoleBaseProfile[0].distanceTo(this.HoleBaseProfile[3])); }
            set { }
        }
        public double length
        {
            get { return Math.Max(this.HoleBaseProfile[0].distanceTo(this.HoleBaseProfile[1]), this.HoleBaseProfile[0].distanceTo(this.HoleBaseProfile[3])); }
            set { }
        }
        public jsonXYZ location
        {
            get { return this.HoleBaseProfile[0]; }
            set { }
        }
        public jsonXYZ direction
        {
            get
            {
                return (this.HoleBaseProfile[1].distanceTo(this.location) == this.length ? this.HoleBaseProfile[1] : this.HoleBaseProfile[3]) - this.location;
            }
        }
    }
    public class jsonXYZ
    {
        public static jsonXYZ XBasis => new jsonXYZ(1, 0, 0);
        public static jsonXYZ YBasis => new jsonXYZ(0, 1, 0);
        public static jsonXYZ ZBasis => new jsonXYZ(0, 0, 1);
        public static jsonXYZ Zero => new jsonXYZ(0, 0, 0);
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public jsonXYZ(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public jsonXYZ()
        {
        }
        public jsonXYZ crossProduct(jsonXYZ pt)
        {
            return new jsonXYZ(this.y * pt.z - this.z * pt.y, -(this.x * pt.z - this.z * pt.x), this.x * pt.y - this.y * pt.x);
        }
        public double angleTo(jsonXYZ dir)
        {
            return Math.Acos(this.dotProduct(dir) / (dir.distanceTo(jsonXYZ.Zero)) / (this.distanceTo(jsonXYZ.Zero)));
        }
        public jsonXYZ projectTo(jsonXYZ pt1, jsonXYZ pt2)
        {
            return pt2 - (pt2 - this).dotProduct((pt2 - pt1).normalized()) * ((pt2 - pt1).normalized());
        }
        public bool coincide(jsonXYZ obj)
        {
            return (this.x == obj.x && this.y == obj.y && this.z == obj.z);
        }
        public override string ToString()
        {
            return "jsonXYZ(" + this.x.ToString() + ", " + this.y.ToString() + ", " + this.z.ToString() + ")";
        }
        public double distanceTo(jsonXYZ p)
        {
            double d = Math.Sqrt(Math.Pow(this.x - p.x, 2) + Math.Pow(this.y - p.y, 2) + Math.Pow(this.z - p.z, 2));
            return d;
        }
        public double dotProduct(jsonXYZ p)
        {
            return this.x * p.x + this.y * p.y + this.z * p.z;
        }
        public static jsonXYZ operator -(jsonXYZ left, jsonXYZ right)
        {
            return new jsonXYZ(left.x - right.x, left.y - right.y, left.z - right.z);
        }
        public static jsonXYZ operator -(jsonXYZ s)
        {
            return new jsonXYZ(-s.x, -s.y, -s.z);
        }
        public static jsonXYZ operator /(jsonXYZ s, double d)
        {
            return new jsonXYZ(s.x / d, s.y / d, s.z / d);
        }
        public static jsonXYZ operator +(jsonXYZ left, jsonXYZ right)
        {
            return new jsonXYZ(left.x + right.x, left.y + right.y, left.z + right.z);
        }
        public static jsonXYZ operator *(jsonXYZ left, double d)
        {
            return new jsonXYZ(left.x * d, left.y * d, left.z * d);
        }
        public static jsonXYZ operator *(double d, jsonXYZ right)
        {
            return new jsonXYZ(right.x * d, right.y * d, right.z * d);
        }
        public double distanceToLine(jsonXYZ p1, jsonXYZ p2)
        {
            double dis = Math.Sqrt(Math.Pow(distanceTo(p1), 2) - Math.Pow((p2 - p1).dotProduct(this - p1) / (p2.distanceTo(p1)), 2));
            return dis;
        }
        public jsonXYZ normalized()
        {
            if (Math.Sqrt(this.dotProduct(this)) == 0)
            {
                Exception e = new Exception("Invalid Vector for Nomalization: length == 0");
                Debug.Print(e.StackTrace);
                throw e;
            };
            return this / Math.Sqrt(this.dotProduct(this));
        }
        public jsonXYZ transformToijk(jsonXYZ i, jsonXYZ j, jsonXYZ k)
        {
            double c_i = this.dotProduct(i) / Math.Sqrt(i.dotProduct(i));
            double c_j = this.dotProduct(j) / Math.Sqrt(j.dotProduct(j));
            double c_k = this.dotProduct(k) / Math.Sqrt(k.dotProduct(k));
            return new jsonXYZ(c_i, c_j, c_k);
        }
        public jsonXYZ rotate(jsonXYZ pt, jsonXYZ axis, double angle)
        {
            jsonXYZ new_pt = this - pt;
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            double vx = axis.normalized().x;
            double vy = axis.normalized().y;
            double vz = axis.normalized().z;

            double new_x = (vx * vx * (1 - c) + c) * new_pt.x + (vx * vy * (1 - c) - vz * s) * new_pt.y + (vx * vz * (1 - c) + vy * s) * new_pt.z;

            double new_y = (vy * vx * (1 - c) + vz * s) * new_pt.x + (vy * vy * (1 - c) + c) * new_pt.y + (vy * vz * (1 - c) - vx * s) * new_pt.z;

            double new_z = (vx * vz * (1 - c) - vy * s) * new_pt.x + (vy * vz * (1 - c) + vx * s) * new_pt.y + (vz * vz * (1 - c) + c) * new_pt.z;

            new_pt = new jsonXYZ(new_x, new_y, new_z);
            new_pt = new_pt + pt;
            return new_pt;
        }
    }
    //MEP
    public class jsonMEP
    {
        public jsonPipe[] pipe { get; set; }
        public jsonFitting[] Elbow_Pipe_Junction { get; set; }
        public jsonTee[] T_Pipe_Junction { get; set; }
        public jsonS_Trap[] S_Trap { get; set; }
        public jsonP_Trap[] P_Trap { get; set; }
    }
    public class jsonPipe
    {
        public int ID { get; set; }
        public double radius { get; set; }
        public jsonXYZ startPoint { get; set; }
        public jsonXYZ endPoint { get; set; }
        public double length { get { return this.startPoint.distanceTo(this.endPoint); } set { } }
        public jsonXYZ direction { get { return (this.endPoint - this.startPoint).normalized(); } }
    }
    public class jsonFitting
    {
        public int pipe_index_1 { get; set; }
        public int pipe_index_2 { get; set; }
        public jsonXYZ center;
        public double angle { get; set; }
        public double radius { get; set; }
        public jsonXYZ axis { get; set; }
        public jsonXYZ location { get; set; }
        public jsonXYZ refAxis { get; set; }
        public bool isValid { get; set; }
    }
    public class jsonTee
    {
        public int pipe_index_1 { get; set; }
        public int pipe_index_2 { get; set; }
        public int pipe_index_3 { get; set; }
        public jsonXYZ center { get; set; }
        public jsonXYZ Pt1 { get; set; }
        public jsonXYZ Pt2 { get; set; }
        public jsonXYZ Pt3 { get; set; }
        public bool isValid { get; set; }
        public static class config
        {
            public static double Radius_ratio { get { return 1.1; } set { Radius_ratio = value; } }
        }
    }
    public class jsonS_Trap
    {
        public int Pipe_Index { get; set; }
        public string Joint_Type { get; set; }
        public jsonXYZ Startpoint { get; set; }
        public jsonXYZ Vertical_direction { get; set; }
        public jsonXYZ Span_direction { get; set; }
        public double Radius { get; set; }
        public static class config
        {
            public static double vertical_ratio { get { return 10; } set { vertical_ratio = value; } }
            public static double span_ratio { get { return 6; } set { vertical_ratio = value; } }
            public static double revolving_ratio { get { return 1.0; } set { vertical_ratio = value; } }
        }
        public jsonFitting connecting_elbow { get; set; }
        public jsonTee connecting_tee { get; set; }
    }
    public class jsonP_Trap
    {
        public int Pipe_Index { get; set; }
        public string Joint_Type { get; set; }
        public jsonXYZ Startpoint { get; set; }
        public jsonXYZ Vertical_direction { get; set; }
        public jsonXYZ Span_direction { get; set; }
        public double Radius { get; set; }
        public static class config
        {
            public static double vertical_ratio { get { return 10; } set { vertical_ratio = value; } }
            public static double span_ratio { get { return 10; } set { vertical_ratio = value; } }
            public static double revolving_ratio { get { return 1.0; } set { vertical_ratio = value; } }
            public static double turn_point { get { return 0.3; } set { vertical_ratio = value; } }
            public static double turn_revolvingratio { get { return 1.3; } set { vertical_ratio = value; } }
        }
        public jsonFitting connecting_elbow { get; set; }
    }
    public class jsonM
    {
        public jsonDuct[] duct { get; set; }
        public jsonDuctFitting[] Elbow_Duct_Junction { get; set; }
        public jsonDuctTee[] T_Duct_Junction { get; set; }
    }
    public class jsonDuct
    {
        public int ID { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double Volume { get; set; }
        public jsonXYZ[] Baseprofile { get; set; }
        public jsonXYZ Direction { get; set; }
    }
    public class jsonDuctFitting
    {
        public int Pipe_Index_1 { get; set; }
        public int Pipe_Index_2 { get; set; }
        public jsonXYZ center;
        public double angle { get; set; }
        public double radius { get; set; }
        public jsonXYZ axis { get; set; }
        public jsonXYZ location { get; set; }
        public jsonXYZ refAxis { get; set; }
        public bool isValid { get; set; }
    }
    public class jsonDuctTee
    {
        public int Pipe_Index_1 { get; set; }
        public int Pipe_Index_2 { get; set; }
        public int Pipe_Index_3 { get; set; }
        public jsonXYZ center { get; set; }
        public jsonXYZ Pt1 { get; set; }
        public jsonXYZ Pt2 { get; set; }
        public jsonXYZ Pt3 { get; set; }
        public bool isValid { get; set; }
        public static class config
        {
            public static double Radius_ratio { get { return 1.1; } set { Radius_ratio = value; } }
        }
    }
}
