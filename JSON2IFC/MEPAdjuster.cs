using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JSON2IFC.SJSONPlugin;

namespace JSON2IFC
{
    class MEPAdjuster
    {
        const double FITTING_RADIUS_RATIO = 1.5;
        const double PIPE_LENGTH_LIMIT = 0.01;
        public jsonMEP jmep { get; }
        public jsonM jm { get; }
        List<jsonPipe> pipes { get; }

        public MEPAdjuster(jsonMEP jmep)
        {
            this.jmep = jmep;
            this.pipes = jmep.pipe.ToList();
        }
        public MEPAdjuster(jsonM jm)
        {
            this.jm = jm;
        }
        public void adjustPipeElbow()
        {
            if (jmep.Elbow_Pipe_Junction != null)
            {
                jsonPipe pipe1 = null, pipe2 = null;
                jsonXYZ center = null, shifting_dir, p1_dir, p2_dir;
                List<jsonXYZ> pipe1Points = null, pipe2Points = null;
                double average_radius, angle, shifting;
                foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
                {
                    pipe1 = pipes.Find(x => x.ID.Equals(jsonFitting.pipe_index_1));
                    pipe2 = pipes.Find(x => x.ID.Equals(jsonFitting.pipe_index_2));
                    if (pipe1 != null && pipe2 != null && pipe1.length != 0 && pipe2.length != 0)
                    {

                        pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
                        pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
                        double minDis = Double.MaxValue;
                        foreach (jsonXYZ pt1 in pipe1Points)
                        {
                            foreach (jsonXYZ pt2 in pipe2Points)
                            {
                                if (minDis > pt1.distanceTo(pt2))
                                {
                                    minDis = pt1.distanceTo(pt2);
                                    pipe1.startPoint = pt1;
                                    pipe2.startPoint = pt2;
                                }
                            }
                        }
                        pipe1.endPoint = pipe1Points.Find(x => !x.Equals(pipe1.startPoint));
                        pipe2.endPoint = pipe2Points.Find(x => !x.Equals(pipe2.startPoint));

                        p1_dir = pipe1.direction;
                        p2_dir = pipe2.direction;

                        shifting_dir = (p1_dir + p2_dir).normalized();
                        average_radius = (pipe1.radius + pipe2.radius) / 2;
                        center = (pipe1.startPoint + pipe2.startPoint) / 2;
                        angle = pipe1.direction.angleTo(pipe2.direction);
                        shifting = Math.Min(Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), pipe1.length * (1 - PIPE_LENGTH_LIMIT)), average_radius * FITTING_RADIUS_RATIO);
                        pipe1.startPoint += pipe1.direction * shifting;
                        pipe2.startPoint += pipe2.direction * shifting;

                        center += pipe1.startPoint.distanceTo(center) / Math.Sin(Math.PI / 2 - angle / 2) * shifting_dir;


                        jsonFitting.angle = Math.PI - angle;
                        jsonFitting.radius = average_radius;

                        jsonFitting.center = center;
                        jsonFitting.axis = p1_dir.crossProduct(p2_dir).normalized();

                        jsonFitting.refAxis = -p1_dir;
                        jsonFitting.location = pipe1.startPoint;
                        jsonFitting.isValid = true;
                    }
                    else if (pipe1 == null || pipe2 == null) Console.WriteLine("ERROR: Creating Ellbow but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonFitting.pipe_index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonFitting.pipe_index_2.ToString()) : "") + "\n");
                    else if (pipe1.length == 0 && pipe2.length == 0) Console.WriteLine("ERROR: Creating Ellbow but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonFitting.pipe_index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonFitting.pipe_index_2.ToString()) : "") + "\n");
                }
            }
        }
        public void adjustPipeTee()
        {
            if (jmep.T_Pipe_Junction != null)
            {
                jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
                List<jsonXYZ> pipe1Points = null, pipe2Points = null, pipe3Points = null;
                jsonXYZ p1_dir = null, p2_dir = null, p3_dir = null, center = null;
                double shifting = Double.MaxValue, average_radius;
                foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
                {
                    pipe1 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_1));
                    pipe2 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_2));
                    pipe3 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_3));
                    if (pipe1 != null && pipe2 != null && pipe3 != null && pipe1.length != 0 && pipe2.length != 0 && pipe3.length != 0)
                    {
                        if (pipe1.ID != pipe2.ID)
                        {
                            pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
                            pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
                            pipe3Points = new List<jsonXYZ>() { pipe3.startPoint, pipe3.endPoint };
                            double minDis = Double.MaxValue;
                            foreach (jsonXYZ pt1 in pipe1Points)
                            {
                                foreach (jsonXYZ pt2 in pipe2Points)
                                {
                                    if (minDis > pt1.distanceTo(pt2))
                                    {
                                        minDis = pt1.distanceTo(pt2);
                                        pipe1.startPoint = pt1;
                                        pipe2.startPoint = pt2;
                                    }
                                }
                            }
                            pipe1.endPoint = pipe1Points.Find(x => !x.Equals(pipe1.startPoint));
                            pipe2.endPoint = pipe2Points.Find(x => !x.Equals(pipe2.startPoint));
                            minDis = Double.MaxValue;
                            foreach (jsonXYZ pt3 in pipe3Points)
                            {
                                if (minDis > pt3.distanceTo((pipe1.startPoint + pipe2.startPoint) / 2))
                                {
                                    minDis = pt3.distanceTo((pipe1.startPoint + pipe2.startPoint) / 2);
                                    pipe3.startPoint = pt3;
                                }
                            }
                            pipe3.endPoint = pipe3Points.Find(x => !x.Equals(pipe3.startPoint));
                            p1_dir = pipe1.direction;
                            p2_dir = pipe2.direction;
                            p3_dir = pipe3.direction;

                            average_radius = (pipe1.radius + pipe2.radius + pipe3.radius) / 3;

                            center = (pipe1.startPoint + pipe2.startPoint) / 2;

                            shifting = Math.Min(pipe1.length * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
                            shifting = Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), shifting);
                            shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

                            pipe1.startPoint += pipe1.direction * shifting;
                            pipe2.startPoint += pipe2.direction * shifting;
                            pipe3.startPoint += pipe3.direction * shifting;

                            jsonTee.center = center;

                            jsonTee.Pt1 = pipe1.startPoint;
                            jsonTee.Pt2 = pipe2.startPoint;
                            jsonTee.Pt3 = pipe3.startPoint;
                            jsonTee.isValid = true;
                        }
                        else if (jsonTee.center != null)
                        {
                            pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
                            pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
                            pipe3Points = new List<jsonXYZ>() { pipe3.startPoint, pipe3.endPoint };
                            double minDis = Double.MaxValue;
                            foreach (jsonXYZ pt3 in pipe3Points)
                            {
                                if (minDis > pt3.distanceTo(jsonTee.center))
                                {
                                    minDis = pt3.distanceTo(jsonTee.center);
                                    pipe3.startPoint = pt3;
                                }
                            }
                            pipe3.endPoint = pipe3Points.Find(x => !x.Equals(pipe3.startPoint));
                            p1_dir = pipe1.direction;
                            p2_dir = pipe2.direction;
                            p3_dir = pipe3.direction;

                            average_radius = (pipe1.radius + pipe2.radius + pipe3.radius) / 3;

                            shifting = Math.Min(pipe1.startPoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
                            shifting = Math.Min(pipe1.endPoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), shifting);
                            shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

                            pipe3.startPoint += pipe3.direction * shifting;
                            jsonTee.Pt1 = jsonTee.center + p1_dir * shifting;
                            jsonTee.Pt2 = jsonTee.center - p1_dir * shifting;

                            jsonTee.Pt3 = pipe3.startPoint;
                            jsonTee.isValid = true;
                        }
                    }
                    else if (pipe1 == null || pipe2 == null || pipe3 == null) Console.WriteLine("ERROR: Creating Tee but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonTee.pipe_index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonTee.pipe_index_2.ToString()) : "") + (((pipe1 == null || pipe2 == null) && pipe3 == null) ? " & " : "") + (pipe3 == null ? ("#" + jsonTee.pipe_index_3.ToString()) : "") + "\n");
                    else if (pipe1.length == 0 || pipe2.length == 0 || pipe3.length == 0) Console.WriteLine("ERROR: Creating Tee but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonTee.pipe_index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonTee.pipe_index_2.ToString()) : "") + (((pipe1.length == 0 || pipe2.length == 0) && pipe3.length == 0) ? " & " : "") + (pipe3.length == 0 ? ("#" + jsonTee.pipe_index_3.ToString()) : "") + "\n");
                }
            }
        }
        public void adjustPipeSTrap()
        {
            if (jmep.S_Trap != null)
            {
                jsonPipe pipe = null;
                jsonXYZ center = null, S_pt, shifting_dir, p_dir;
                List<jsonXYZ> pipePoints = null;
                double average_radius, angle, shifting;
                foreach (jsonS_Trap jsonS_Trap in jmep.S_Trap)
                {
                    pipe = pipes.Find(x => x.ID.Equals(jsonS_Trap.Pipe_Index));
                    if (pipe != null && pipe.length != 0 && jsonS_Trap.Joint_Type == "end")
                    {
                        S_pt = jsonS_Trap.Startpoint;
                        pipePoints = new List<jsonXYZ>() { pipe.startPoint, pipe.endPoint };
                        double minDis = Double.MaxValue;
                        pipe.startPoint = pipePoints[0].distanceTo(S_pt) < pipePoints[1].distanceTo(S_pt) ? pipePoints[0] : pipePoints[1];
                        pipe.endPoint = pipePoints.Find(x => !x.Equals(pipe.startPoint));
                        p_dir = pipe.direction;

                        shifting_dir = (p_dir.normalized() + jsonS_Trap.Vertical_direction.normalized()).normalized();
                        average_radius = (pipe.radius + jsonS_Trap.Radius) / 2;
                        center = (pipe.startPoint + S_pt) / 2;
                        angle = pipe.direction.angleTo(jsonS_Trap.Vertical_direction);
                        shifting = Math.Min(Math.Min(pipe.length * (1 - PIPE_LENGTH_LIMIT), jsonS_Trap.Radius * jsonS_Trap.config.vertical_ratio * (1 - PIPE_LENGTH_LIMIT)), average_radius * FITTING_RADIUS_RATIO);
                        pipe.startPoint += pipe.direction * shifting;
                        jsonS_Trap.Startpoint += jsonS_Trap.Vertical_direction.normalized() * shifting;

                        center += pipe.startPoint.distanceTo(center) / Math.Sin(Math.PI / 2 - angle / 2) * shifting_dir;


                        jsonS_Trap.connecting_elbow = new jsonFitting();
                        jsonS_Trap.connecting_elbow.angle = Math.PI - angle;
                        jsonS_Trap.connecting_elbow.radius = average_radius;

                        jsonS_Trap.connecting_elbow.center = center;
                        jsonS_Trap.connecting_elbow.axis = p_dir.crossProduct(jsonS_Trap.Vertical_direction).normalized();

                        jsonS_Trap.connecting_elbow.refAxis = -p_dir;
                        jsonS_Trap.connecting_elbow.location = pipe.startPoint;
                        jsonS_Trap.connecting_elbow.isValid = true;

                        jmep.Elbow_Pipe_Junction = jmep.Elbow_Pipe_Junction.Concat(new jsonFitting[] { jsonS_Trap.connecting_elbow }).ToArray();
                    }
                    else if (pipe != null && pipe.length != 0 && jsonS_Trap.Joint_Type == "mid")
                    {
                        jsonXYZ trap_dir = null;
                        center = jsonS_Trap.Startpoint.projectTo(pipe.startPoint, pipe.endPoint);
                        pipePoints = new List<jsonXYZ>() { pipe.startPoint, pipe.endPoint };
                        p_dir = pipe.direction;
                        trap_dir = jsonS_Trap.Vertical_direction.normalized();

                        average_radius = (2 * pipe.radius + jsonS_Trap.Radius) / 3;

                        shifting = Math.Min(pipe.startPoint.distanceTo(center) * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
                        shifting = Math.Min(pipe.endPoint.distanceTo(center) * (1 - PIPE_LENGTH_LIMIT), shifting);
                        shifting = Math.Min(jsonS_Trap.config.vertical_ratio * jsonS_Trap.Radius * (1 - PIPE_LENGTH_LIMIT), shifting);

                        jsonS_Trap.Startpoint += jsonS_Trap.Vertical_direction.normalized() * shifting;
                        jsonS_Trap.connecting_tee = new jsonTee();

                        jsonS_Trap.connecting_tee.center = center;
                        jsonS_Trap.connecting_tee.Pt1 = center + p_dir * shifting;
                        jsonS_Trap.connecting_tee.Pt2 = center - p_dir * shifting;

                        jsonS_Trap.connecting_tee.Pt3 = jsonS_Trap.Startpoint;
                        jsonS_Trap.connecting_tee.isValid = true;

                        jmep.T_Pipe_Junction = jmep.T_Pipe_Junction.Concat(new jsonTee[] { jsonS_Trap.connecting_tee }).ToArray();

                    }
                    else if (pipe == null) Console.WriteLine("ERROR: Creating S trap but cannot find pipe, index: " + "#" + jsonS_Trap.Pipe_Index.ToString());
                    else if (pipe.length == 0) Console.WriteLine("ERROR: Creating S trap but the pipe is too short, index: " + "#" + jsonS_Trap.Pipe_Index.ToString());
                }
            }
        }
        public void adjsutPipeTTrap()
        {
            if (jmep.P_Trap != null)
            {
                jsonPipe pipe = null;
                jsonXYZ center = null, P_pt, shifting_dir, p_dir;
                List<jsonXYZ> pipePoints = null;
                double average_radius, angle, shifting;
                foreach (jsonP_Trap jsonP_Trap in jmep.P_Trap)
                {
                    pipe = pipes.Find(x => x.ID.Equals(jsonP_Trap.Pipe_Index));
                    if (pipe != null && pipe.length != 0)
                    {
                        P_pt = jsonP_Trap.Startpoint;
                        pipePoints = new List<jsonXYZ>() { pipe.startPoint, pipe.endPoint };
                        double minDis = Double.MaxValue;
                        pipe.startPoint = pipePoints[0].distanceTo(P_pt) < pipePoints[1].distanceTo(P_pt) ? pipePoints[0] : pipePoints[1];
                        pipe.endPoint = pipePoints.Find(x => !x.Equals(pipe.startPoint));
                        p_dir = pipe.direction;

                        shifting_dir = (p_dir.normalized() + jsonP_Trap.Span_direction.normalized()).normalized();
                        average_radius = (pipe.radius + jsonP_Trap.Radius) / 2;
                        center = (pipe.startPoint + P_pt) / 2;
                        angle = pipe.direction.angleTo(jsonP_Trap.Span_direction);
                        shifting = Math.Min(Math.Min(pipe.length * (1 - PIPE_LENGTH_LIMIT), jsonP_Trap.Radius * jsonP_Trap.config.vertical_ratio * (1 - PIPE_LENGTH_LIMIT)), average_radius * FITTING_RADIUS_RATIO);
                        pipe.startPoint += pipe.direction * shifting;
                        jsonP_Trap.Startpoint += jsonP_Trap.Span_direction.normalized() * shifting;

                        center += pipe.startPoint.distanceTo(center) / Math.Sin(Math.PI / 2 - angle / 2) * shifting_dir;


                        jsonP_Trap.connecting_elbow = new jsonFitting();
                        jsonP_Trap.connecting_elbow.angle = Math.PI - angle;
                        jsonP_Trap.connecting_elbow.radius = average_radius;

                        jsonP_Trap.connecting_elbow.center = center;
                        jsonP_Trap.connecting_elbow.axis = p_dir.crossProduct(jsonP_Trap.Span_direction).normalized();

                        jsonP_Trap.connecting_elbow.refAxis = -p_dir;
                        jsonP_Trap.connecting_elbow.location = pipe.startPoint;
                        jsonP_Trap.connecting_elbow.isValid = true;

                        jmep.Elbow_Pipe_Junction = jmep.Elbow_Pipe_Junction.Concat(new jsonFitting[] { jsonP_Trap.connecting_elbow }).ToArray();
                    }
                    else if (pipe == null) Console.WriteLine("ERROR: Creating P trap but cannot find pipe, index: " + "#" + jsonP_Trap.Pipe_Index.ToString());
                    else if (pipe.length == 0) Console.WriteLine("ERROR: Creating P trap but the pipe is too short, index: " + "#" + jsonP_Trap.Pipe_Index.ToString());
                }
            }
        }
        private void adjustDucts()
        {
            //if (jmep.Elbow_Pipe_Junction != null)
            //{
            //    jsonPipe pipe1 = null, pipe2 = null;
            //    jsonXYZ center = null, shifting_dir, p1_dir, p2_dir;
            //    List<jsonXYZ> pipe1Points = null, pipe2Points = null;
            //    double average_radius, angle, shifting;
            //    foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
            //    {
            //        pipe1 = pipes.Find(x => x.ID.Equals(jsonFitting.pipe_index_1));
            //        pipe2 = pipes.Find(x => x.ID.Equals(jsonFitting.pipe_index_2));
            //        if (pipe1 != null && pipe2 != null && pipe1.length != 0 && pipe2.length != 0)
            //        {

            //            pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
            //            pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
            //            double minDis = Double.MaxValue;
            //            foreach (jsonXYZ pt1 in pipe1Points)
            //            {
            //                foreach (jsonXYZ pt2 in pipe2Points)
            //                {
            //                    if (minDis > pt1.distanceTo(pt2))
            //                    {
            //                        minDis = pt1.distanceTo(pt2);
            //                        pipe1.startPoint = pt1;
            //                        pipe2.startPoint = pt2;
            //                    }
            //                }
            //            }
            //            pipe1.endPoint = pipe1Points.Find(x => !x.Equals(pipe1.startPoint));
            //            pipe2.endPoint = pipe2Points.Find(x => !x.Equals(pipe2.startPoint));

            //            p1_dir = pipe1.direction;
            //            p2_dir = pipe2.direction;

            //            shifting_dir = (p1_dir + p2_dir).normalized();
            //            average_radius = (pipe1.radius + pipe2.radius) / 2;
            //            center = (pipe1.startPoint + pipe2.startPoint) / 2;
            //            angle = pipe1.direction.angleTo(pipe2.direction);
            //            shifting = Math.Min(Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), pipe1.length * (1 - PIPE_LENGTH_LIMIT)), average_radius * FITTING_RADIUS_RATIO);
            //            pipe1.startPoint += pipe1.direction * shifting;
            //            pipe2.startPoint += pipe2.direction * shifting;

            //            center += pipe1.startPoint.distanceTo(center) / Math.Sin(Math.PI / 2 - angle / 2) * shifting_dir;


            //            jsonFitting.angle = Math.PI - angle;
            //            jsonFitting.radius = average_radius;

            //            jsonFitting.center = center;
            //            jsonFitting.axis = p1_dir.crossProduct(p2_dir).normalized();

            //            jsonFitting.refAxis = -p1_dir;
            //            jsonFitting.location = pipe1.startPoint;
            //            jsonFitting.isValid = true;
            //        }
            //        else if (pipe1 == null || pipe2 == null) error_msg += "ERROR: Creating Ellbow but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonFitting.pipe_index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonFitting.pipe_index_2.ToString()) : "") + "\n";
            //        else if (pipe1.length == 0 && pipe2.length == 0) error_msg += "ERROR: Creating Ellbow but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonFitting.pipe_index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonFitting.pipe_index_2.ToString()) : "") + "\n";
            //    }
            //}
            //if (jmep.T_Pipe_Junction != null)
            //{
            //    jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
            //    List<jsonXYZ> pipe1Points = null, pipe2Points = null, pipe3Points = null;
            //    jsonXYZ p1_dir = null, p2_dir = null, p3_dir = null, center = null;
            //    double shifting = Double.MaxValue, average_radius;
            //    foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
            //    {
            //        pipe1 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_1));
            //        pipe2 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_2));
            //        pipe3 = pipes.Find(x => x.ID.Equals(jsonTee.pipe_index_3));
            //        if (pipe1 != null && pipe2 != null && pipe3 != null && pipe1.length != 0 && pipe2.length != 0 && pipe3.length != 0)
            //        {
            //            if (pipe1.ID != pipe2.ID)
            //            {
            //                pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
            //                pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
            //                pipe3Points = new List<jsonXYZ>() { pipe3.startPoint, pipe3.endPoint };
            //                double minDis = Double.MaxValue;
            //                foreach (jsonXYZ pt1 in pipe1Points)
            //                {
            //                    foreach (jsonXYZ pt2 in pipe2Points)
            //                    {
            //                        if (minDis > pt1.distanceTo(pt2))
            //                        {
            //                            minDis = pt1.distanceTo(pt2);
            //                            pipe1.startPoint = pt1;
            //                            pipe2.startPoint = pt2;
            //                        }
            //                    }
            //                }
            //                pipe1.endPoint = pipe1Points.Find(x => !x.Equals(pipe1.startPoint));
            //                pipe2.endPoint = pipe2Points.Find(x => !x.Equals(pipe2.startPoint));
            //                minDis = Double.MaxValue;
            //                foreach (jsonXYZ pt3 in pipe3Points)
            //                {
            //                    if (minDis > pt3.distanceTo((pipe1.startPoint + pipe2.startPoint) / 2))
            //                    {
            //                        minDis = pt3.distanceTo((pipe1.startPoint + pipe2.startPoint) / 2);
            //                        pipe3.startPoint = pt3;
            //                    }
            //                }
            //                pipe3.endPoint = pipe3Points.Find(x => !x.Equals(pipe3.startPoint));
            //                p1_dir = pipe1.direction;
            //                p2_dir = pipe2.direction;
            //                p3_dir = pipe3.direction;

            //                average_radius = (pipe1.radius + pipe2.radius + pipe3.radius) / 3;

            //                center = (pipe1.startPoint + pipe2.startPoint) / 2;

            //                shifting = Math.Min(pipe1.length * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
            //                shifting = Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), shifting);
            //                shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

            //                pipe1.startPoint += pipe1.direction * shifting;
            //                pipe2.startPoint += pipe2.direction * shifting;
            //                pipe3.startPoint += pipe3.direction * shifting;

            //                jsonTee.center = center;

            //                jsonTee.Pt1 = pipe1.startPoint;
            //                jsonTee.Pt2 = pipe2.startPoint;
            //                jsonTee.Pt3 = pipe3.startPoint;
            //                jsonTee.isValid = true;
            //            }
            //            else if (jsonTee.center != null)
            //            {
            //                pipe1Points = new List<jsonXYZ>() { pipe1.startPoint, pipe1.endPoint };
            //                pipe2Points = new List<jsonXYZ>() { pipe2.startPoint, pipe2.endPoint };
            //                pipe3Points = new List<jsonXYZ>() { pipe3.startPoint, pipe3.endPoint };
            //                double minDis = Double.MaxValue;
            //                foreach (jsonXYZ pt3 in pipe3Points)
            //                {
            //                    if (minDis > pt3.distanceTo(jsonTee.center))
            //                    {
            //                        minDis = pt3.distanceTo(jsonTee.center);
            //                        pipe3.startPoint = pt3;
            //                    }
            //                }
            //                pipe3.endPoint = pipe3Points.Find(x => !x.Equals(pipe3.startPoint));
            //                p1_dir = pipe1.direction;
            //                p2_dir = pipe2.direction;
            //                p3_dir = pipe3.direction;

            //                average_radius = (pipe1.radius + pipe2.radius + pipe3.radius) / 3;

            //                shifting = Math.Min(pipe1.startPoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
            //                shifting = Math.Min(pipe1.endPoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), shifting);
            //                shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

            //                pipe3.startPoint += pipe3.direction * shifting;
            //                jsonTee.Pt1 = jsonTee.center + p1_dir * shifting;
            //                jsonTee.Pt2 = jsonTee.center - p1_dir * shifting;

            //                jsonTee.Pt3 = pipe3.startPoint;
            //                jsonTee.isValid = true;
            //            }
            //        }
            //        else if (pipe1 == null || pipe2 == null || pipe3 == null) error_msg += "ERROR: Creating Tee but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonTee.pipe_index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonTee.pipe_index_2.ToString()) : "") + (((pipe1 == null || pipe2 == null) && pipe3 == null) ? " & " : "") + (pipe3 == null ? ("#" + jsonTee.pipe_index_3.ToString()) : "") + "\n";
            //        else if (pipe1.length == 0 || pipe2.length == 0 || pipe3.length == 0) error_msg += "ERROR: Creating Tee but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonTee.pipe_index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonTee.pipe_index_2.ToString()) : "") + (((pipe1.length == 0 || pipe2.length == 0) && pipe3.length == 0) ? " & " : "") + (pipe3.length == 0 ? ("#" + jsonTee.pipe_index_3.ToString()) : "") + "\n";
            //    }
            //}
        }
    }
}
