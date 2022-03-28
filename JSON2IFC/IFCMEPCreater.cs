using System;
using System.Collections.Generic;
using System.Linq;
using Scan2BimShared.Models.IfcEntities;
using Scan2BimShared.StaticData.IfcEnums;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.HvacDomain;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;

namespace Scan2BimConnect.Utilities
{
    class IFCMEPCreater : IFCElementCreater
    {
        jsonMEP jmep { get; }
        jsonM jm { get; }
        List<jsonPipe> pipes { get; }
        public MEPAdjuster MEPAdjuster { get; set; }
        public IFCMEPCreater(jsonMEP jmep, IfcStore ifcStore, IfcBuilding ifcBuilding) : base(ifcStore, ifcBuilding)
        {
            this.jmep = jmep;
            this.jm = null;
            this.MEPAdjuster = new MEPAdjuster(jmep);
            this.pipes = jmep.pipe.ToList();
        }
        public IFCMEPCreater(jsonM jm, IfcStore ifcStore, IfcBuilding ifcBuilding) : base(ifcStore, ifcBuilding)
        {
            this.jmep = null;
            this.jm = jm;
            this.MEPAdjuster = new MEPAdjuster(jm);
        }
        public List<IIfcFlowSegment> createPipes()
        {
            List<IIfcFlowSegment> ret = new List<IIfcFlowSegment>();
            foreach (jsonPipe jsonPipe in jmep.pipe)
            {
                if (jsonPipe.length == 0)
                {
                    Console.WriteLine("ERROR: Creating Pipe but the pipe is too short: #" + jsonPipe.ID.ToString() + "\n");
                    continue;
                }

                double length = jsonPipe.length * UNIT_CONVERSION;
                double radius = jsonPipe.radius * UNIT_CONVERSION;
                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonPipe.startPoint.x, jsonPipe.startPoint.y, jsonPipe.startPoint.z) * UNIT_CONVERSION;
                jsonXYZ axisJsonXYZ = new jsonXYZ((jsonPipe.endPoint - jsonPipe.startPoint).x, (jsonPipe.endPoint - jsonPipe.startPoint).y, (jsonPipe.endPoint - jsonPipe.startPoint).z) * UNIT_CONVERSION;
                jsonXYZ refDirJsonXYZ = !(axisJsonXYZ.x == 0 && axisJsonXYZ.y == 0) ? (new jsonXYZ(axisJsonXYZ.y, -axisJsonXYZ.x, 0) * UNIT_CONVERSION) : (new jsonXYZ(axisJsonXYZ.z, 0, -axisJsonXYZ.x) * UNIT_CONVERSION);
                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                IIfcFlowSegment ifcFlowSegment = new Create(ifcStore).FlowSegment(flowSegment =>
                {
                    flowSegment.Name = "";
                    flowSegment.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "SweptSolid";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            shapeRepresentation.Items.Add(createCylinder(length, radius, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ, appearance.First(p => p.Key == BuildingComponent.pipe)));
                        }));
                    });
                    flowSegment.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                attachMaterial((IfcProduct)ifcFlowSegment, Material.PVC);
                ret.Add(ifcFlowSegment);
            }
            return ret;
        }
        public List<IIfcFlowFitting> createPipeElbows()
        {
            List<IIfcFlowFitting> ret = new List<IIfcFlowFitting>();
            jsonPipe pipe1 = null, pipe2 = null;
            foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
            {
                pipe1 = pipes.Find(x => x.ID == jsonFitting.pipe_index_1);
                pipe2 = pipes.Find(x => x.ID == jsonFitting.pipe_index_2);
                if (pipe1 != null && pipe2 != null && jsonFitting.isValid)
                {
                    double angle = jsonFitting.angle;
                    double radius = jsonFitting.radius * UNIT_CONVERSION;
                    jsonXYZ rotationAxis = jsonXYZ.YBasis * UNIT_CONVERSION;
                    jsonXYZ rotationCenter = jsonFitting.center.distanceTo(jsonFitting.location) * jsonXYZ.XBasis * UNIT_CONVERSION;

                    jsonXYZ locationJsonXYZ = jsonFitting.location * UNIT_CONVERSION;
                    jsonXYZ axisJsonXYZ = jsonFitting.refAxis * UNIT_CONVERSION;
                    jsonXYZ refDirJsonXYZ = (jsonFitting.center - jsonFitting.location) * UNIT_CONVERSION;
                    if (radius > rotationCenter.x)
                    {
                        Console.WriteLine("WARNING: Creating Ellbow but the pipe is too short for revolving\n");
                        continue;
                    }
                    //axis: Z dir; refDirection: X dir
                    IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
                    {
                        flowFitting.Name = "Ellbow: " + jsonFitting.pipe_index_1.ToString() + " & " + jsonFitting.pipe_index_2.ToString();
                        flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                        {
                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                            {
                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                shapeRepresentation.RepresentationIdentifier = "SweptSolid";
                                shapeRepresentation.RepresentationIdentifier = "Body";
                                shapeRepresentation.Items.Add(createRevolved(angle, radius, rotationAxis, rotationCenter, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ, appearance.First(p => p.Key == BuildingComponent.pipeElbow)));
                            }));
                        });
                        flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                    });
                    attachMaterial((IfcProduct)ifcFlowFitting, Material.PVC);
                    ret.Add(ifcFlowFitting);
                }
            }
            return ret;
        }
        public List<IIfcFlowFitting> createPipeTee()
        {
            jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
            List<IIfcFlowFitting> ret = new List<IIfcFlowFitting>();
            foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
            {
                pipe1 = pipes.Find(x => x.ID == jsonTee.pipe_index_1);
                pipe2 = pipes.Find(x => x.ID == jsonTee.pipe_index_2);
                pipe3 = pipes.Find(x => x.ID == jsonTee.pipe_index_3);
                if (pipe1 != null && pipe2 != null && pipe3 != null && jsonTee.isValid)
                {
                    List<jsonXYZ> pts = new List<jsonXYZ>() { jsonTee.Pt1, jsonTee.Pt2, jsonTee.Pt3 };
                    List<jsonPipe> connectedPipes = new List<jsonPipe>() { pipe1, pipe2, pipe3 };
                    double radius;
                    double depth;
                    jsonXYZ locationJsonXYZ = null, axisJsonXYZ = null, refDirJsonXYZ = null;

                    //axis: Z dir; refDirection: X dir
                    IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
                    {
                        flowFitting.Name = "Tee: " + jsonTee.pipe_index_1.ToString() + " , " + jsonTee.pipe_index_2.ToString() + " & " + jsonTee.pipe_index_3.ToString();
                        flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                        {
                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                            {
                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                shapeRepresentation.RepresentationIdentifier = "Body";
                                var zip = pts.Zip(connectedPipes, (pt, pipe) => new { pt, pipe });
                                foreach (var z in zip)
                                {
                                    radius = z.pipe.radius * UNIT_CONVERSION * jsonTee.config.Radius_ratio;
                                    depth = z.pt.distanceTo(jsonTee.center) * UNIT_CONVERSION;
                                    locationJsonXYZ = z.pt * UNIT_CONVERSION;
                                    axisJsonXYZ = (jsonTee.center - z.pt) * UNIT_CONVERSION;
                                    refDirJsonXYZ = !(axisJsonXYZ.x == 0 && axisJsonXYZ.y == 0) ? (new jsonXYZ(axisJsonXYZ.y, -axisJsonXYZ.x, 0) * UNIT_CONVERSION) : (new jsonXYZ(axisJsonXYZ.z, 0, axisJsonXYZ.x) * UNIT_CONVERSION);
                                    shapeRepresentation.Items.Add(createCylinder(depth, radius, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ, appearance.First(p => p.Key == BuildingComponent.pipeTee)));
                                }
                            }));
                        });
                        flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                        ret.Add(flowFitting);
                    });
                    attachMaterial((IfcProduct)ifcFlowFitting, Material.PVC);
                }
            }
            return ret;
        }
        public List<IIfcFlowFitting> createPipeSTrap()
        {
            jsonPipe pipe = null;
            List<IIfcFlowFitting> ret = new List<IIfcFlowFitting>();
            foreach (jsonS_Trap jsonS_Trap in jmep.S_Trap)
            {
                pipe = pipes.Find(x => x.ID == jsonS_Trap.Pipe_Index);
                jsonXYZ locationJsonXYZ = jsonS_Trap.Startpoint;
                jsonXYZ axisJsonXYZ = jsonS_Trap.Vertical_direction.normalized();
                jsonXYZ refDirJsonXYZ = (jsonS_Trap.Span_direction - jsonS_Trap.Span_direction.projectTo(jsonXYZ.Zero, jsonS_Trap.Vertical_direction)).normalized();
                List<jsonXYZ> locationPoints = new List<jsonXYZ>();

                locationPoints.Add(jsonS_Trap.Startpoint);
                locationPoints.Add(jsonS_Trap.Startpoint + jsonS_Trap.Vertical_direction.normalized() * jsonS_Trap.config.vertical_ratio * jsonS_Trap.Radius);
                locationPoints.Add(jsonS_Trap.Startpoint + jsonS_Trap.Span_direction.normalized() * jsonS_Trap.config.span_ratio * jsonS_Trap.Radius);
                locationPoints.Add(jsonS_Trap.Startpoint + (jsonS_Trap.Vertical_direction.normalized() * jsonS_Trap.config.vertical_ratio + jsonS_Trap.Span_direction.normalized() * jsonS_Trap.config.span_ratio) * jsonS_Trap.Radius);

                double revolving_angle = (locationPoints[2] - locationPoints[1]).angleTo(locationPoints[0] - locationPoints[1]);
                double revolving_cutratio = jsonS_Trap.config.revolving_ratio / Math.Tan(revolving_angle / 2);
                double radius = jsonS_Trap.Radius * UNIT_CONVERSION;

                jsonXYZ temp_location = null;
                jsonXYZ temp_axis = null;
                jsonXYZ temp_ref = null;

                double depth = 0.0;
                double angle = 0.0;

                jsonXYZ rotationAxis = null;
                jsonXYZ rotationCenter = null;

                KeyValuePair<BuildingComponent, Style> S_trap_appearance = appearance.First(p => p.Key == BuildingComponent.S_trap);

                IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
                {
                    flowFitting.Name = "S Trap: " + jsonS_Trap.Pipe_Index;
                    flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            //segment 1 (vertical)
                            temp_location = locationPoints[0] * UNIT_CONVERSION;
                            temp_axis = axisJsonXYZ * UNIT_CONVERSION;
                            temp_ref = refDirJsonXYZ * UNIT_CONVERSION;
                            depth = (jsonS_Trap.config.vertical_ratio - revolving_cutratio) * jsonS_Trap.Radius * UNIT_CONVERSION;


                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, S_trap_appearance));

                            //segment 2 (elbow)
                            temp_location = (locationPoints[1] + (locationPoints[0] - locationPoints[1]).normalized() * jsonS_Trap.Radius * revolving_cutratio) * UNIT_CONVERSION;
                            temp_axis = axisJsonXYZ * UNIT_CONVERSION;
                            temp_ref = refDirJsonXYZ * UNIT_CONVERSION;
                            rotationAxis = new jsonXYZ(0, 1, 0) * UNIT_CONVERSION;
                            rotationCenter = jsonXYZ.XBasis * jsonS_Trap.config.revolving_ratio * jsonS_Trap.Radius * UNIT_CONVERSION;
                            angle = Math.PI - revolving_angle;

                            shapeRepresentation.Items.Add(createRevolved(angle, radius, rotationAxis, rotationCenter, temp_location, temp_axis, temp_ref, S_trap_appearance));

                            //segment 3 (slanted)
                            temp_location = (locationPoints[1] + (locationPoints[2] - locationPoints[1]).normalized() * revolving_cutratio * jsonS_Trap.Radius) * UNIT_CONVERSION;
                            temp_axis = (locationPoints[2] - locationPoints[1]).normalized() * UNIT_CONVERSION;
                            temp_ref = (refDirJsonXYZ.crossProduct(axisJsonXYZ)).crossProduct(temp_axis).normalized() * UNIT_CONVERSION;
                            depth = (locationPoints[2].distanceTo(locationPoints[1]) - 2 * revolving_cutratio * jsonS_Trap.Radius) * UNIT_CONVERSION;

                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, S_trap_appearance));

                            //segment 4 (elbow)
                            temp_location = (locationPoints[2] + (locationPoints[1] - locationPoints[2]).normalized() * jsonS_Trap.Radius * revolving_cutratio) * UNIT_CONVERSION;
                            temp_axis = temp_axis.normalized() * UNIT_CONVERSION;
                            temp_ref = temp_ref.normalized() * UNIT_CONVERSION;
                            rotationAxis = jsonXYZ.YBasis * UNIT_CONVERSION;
                            rotationCenter = jsonXYZ.XBasis * jsonS_Trap.config.revolving_ratio * jsonS_Trap.Radius * UNIT_CONVERSION;
                            angle = angle;

                            shapeRepresentation.Items.Add(createRevolved(angle, radius, rotationAxis, rotationCenter, temp_location, temp_axis, temp_ref, S_trap_appearance));

                            //segment 5 (vertical)
                            temp_location = (locationPoints[2] + (locationPoints[3] - locationPoints[2]).normalized() * revolving_cutratio * jsonS_Trap.Radius) * UNIT_CONVERSION;
                            temp_axis = axisJsonXYZ * UNIT_CONVERSION;
                            temp_ref = refDirJsonXYZ * UNIT_CONVERSION;
                            depth = (jsonS_Trap.config.vertical_ratio - revolving_cutratio) * jsonS_Trap.Radius * UNIT_CONVERSION;

                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, S_trap_appearance));
                        }));
                    });
                    flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                ret.Add(ifcFlowFitting);
                attachMaterial((IfcProduct)ifcFlowFitting, Material.PVC);
            }
            return ret;
        }
        public List<IIfcFlowFitting> createPipePTrap()
        {
            jsonPipe pipe = null;
            List<IIfcFlowFitting> ret = new List<IIfcFlowFitting>();
            foreach (jsonP_Trap jsonP_Trap in jmep.P_Trap)
            {
                pipe = pipes.Find(x => x.ID == jsonP_Trap.Pipe_Index);

                jsonXYZ locationJsonXYZ = jsonP_Trap.Startpoint;
                jsonXYZ axisJsonXYZ = jsonP_Trap.Vertical_direction.normalized();
                jsonXYZ refDirJsonXYZ = (jsonP_Trap.Span_direction - jsonP_Trap.Span_direction.projectTo(jsonXYZ.Zero, jsonP_Trap.Vertical_direction)).normalized();

                List<jsonXYZ> locationPoints = new List<jsonXYZ>();

                locationPoints.Add(jsonP_Trap.Startpoint);
                locationPoints.Add(jsonP_Trap.Startpoint + jsonP_Trap.Span_direction.normalized() * jsonP_Trap.config.span_ratio * jsonP_Trap.Radius * jsonP_Trap.config.turn_point);
                locationPoints.Add(jsonP_Trap.Startpoint + jsonP_Trap.Span_direction.normalized() * jsonP_Trap.config.span_ratio * jsonP_Trap.Radius - jsonP_Trap.Vertical_direction.normalized() * jsonP_Trap.config.vertical_ratio * jsonP_Trap.Radius);
                locationPoints.Add(jsonP_Trap.Startpoint + jsonP_Trap.Span_direction.normalized() * jsonP_Trap.config.span_ratio * jsonP_Trap.Radius);

                double turn_angle = (locationPoints[2] - locationPoints[1]).angleTo(locationPoints[0] - locationPoints[1]);
                double turn_cutratio = jsonP_Trap.config.turn_revolvingratio / Math.Tan(turn_angle / 2);
                double revolving_angle = (locationPoints[3] - locationPoints[2]).angleTo(locationPoints[1] - locationPoints[2]);
                double revolving_cutratio = jsonP_Trap.config.revolving_ratio / Math.Tan(revolving_angle / 2);

                double radius = jsonP_Trap.Radius * UNIT_CONVERSION;
                jsonXYZ temp_location = null;
                jsonXYZ temp_axis = null;
                jsonXYZ temp_ref = null;
                double depth = 0.0;
                double angle = 0.0;
                jsonXYZ rotationAxis = null;
                jsonXYZ rotationCenter = null;

                KeyValuePair<BuildingComponent, Style> P_trap_appearance = appearance.First(p => p.Key == BuildingComponent.P_trap);

                IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
                {
                    flowFitting.Name = "P Trap: " + jsonP_Trap.Pipe_Index;
                    flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            //segment 1 (vertical)
                            temp_location = locationPoints[0] * UNIT_CONVERSION;
                            temp_axis = refDirJsonXYZ * UNIT_CONVERSION;
                            temp_ref = axisJsonXYZ * UNIT_CONVERSION;
                            depth = (locationPoints[1].distanceTo(locationPoints[0]) - turn_cutratio * jsonP_Trap.Radius) * UNIT_CONVERSION;

                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, P_trap_appearance));

                            //segment 2 (elbow)
                            temp_location = (locationPoints[1] + (locationPoints[0] - locationPoints[1]).normalized() * jsonP_Trap.Radius * turn_cutratio) * UNIT_CONVERSION;
                            temp_axis = refDirJsonXYZ * UNIT_CONVERSION;
                            temp_ref = -axisJsonXYZ * UNIT_CONVERSION;
                            rotationAxis = new jsonXYZ(0, 1, 0) * UNIT_CONVERSION;
                            rotationCenter = jsonXYZ.XBasis * jsonP_Trap.config.turn_revolvingratio * jsonP_Trap.Radius * UNIT_CONVERSION;
                            angle = Math.PI - turn_angle;

                            shapeRepresentation.Items.Add(createRevolved(angle, radius, rotationAxis, rotationCenter, temp_location, temp_axis, temp_ref, P_trap_appearance));

                            //segment 3 (slanted)
                            temp_location = (locationPoints[1] + (locationPoints[2] - locationPoints[1]).normalized() * turn_cutratio * jsonP_Trap.Radius) * UNIT_CONVERSION;
                            temp_axis = (locationPoints[2] - locationPoints[1]).normalized() * UNIT_CONVERSION;
                            temp_ref = refDirJsonXYZ.crossProduct(axisJsonXYZ).normalized() * UNIT_CONVERSION;
                            depth = (locationPoints[2].distanceTo(locationPoints[1]) - turn_cutratio * jsonP_Trap.Radius - revolving_cutratio * jsonP_Trap.Radius) * UNIT_CONVERSION;

                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, P_trap_appearance));

                            //segment 4 (elbow)
                            temp_location = (locationPoints[2] + (locationPoints[1] - locationPoints[2]).normalized() * jsonP_Trap.Radius * revolving_cutratio) * UNIT_CONVERSION;
                            temp_axis = temp_axis.normalized() * UNIT_CONVERSION;
                            temp_ref = temp_ref.crossProduct(temp_axis).normalized() * UNIT_CONVERSION;
                            rotationAxis = jsonXYZ.YBasis * UNIT_CONVERSION;
                            rotationCenter = jsonXYZ.XBasis * jsonP_Trap.config.revolving_ratio * jsonP_Trap.Radius * UNIT_CONVERSION;
                            angle = Math.PI - revolving_angle;

                            shapeRepresentation.Items.Add(createRevolved(angle, radius, rotationAxis, rotationCenter, temp_location, temp_axis, temp_ref, P_trap_appearance));

                            //segment 5 (vertical)
                            temp_location = (locationPoints[2] + (locationPoints[3] - locationPoints[2]).normalized() * revolving_cutratio * jsonP_Trap.Radius) * UNIT_CONVERSION;
                            temp_axis = (locationPoints[3] - locationPoints[2]).normalized() * UNIT_CONVERSION;
                            temp_ref = temp_ref.crossProduct(temp_axis).normalized() * UNIT_CONVERSION;
                            depth = (locationPoints[2].distanceTo(locationPoints[3]) - revolving_cutratio * jsonP_Trap.Radius) * UNIT_CONVERSION;

                            shapeRepresentation.Items.Add(createCylinder(depth, radius, temp_location, temp_axis, temp_ref, P_trap_appearance));
                        }));
                    });
                    flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                attachMaterial((IfcProduct)ifcFlowFitting, Material.PVC);
                ret.Add(ifcFlowFitting);
            }
            return ret;
        }
        public List<IfcDuctSegment> createDucts()
        {
            List<IfcDuctSegment> ret = new List<IfcDuctSegment>();
            foreach (jsonDuct jsonDuct in jm.duct)
            {
                if (jsonDuct.Length == 0 || jsonDuct.Baseprofile[0].distanceTo(jsonDuct.Baseprofile[4]) == 0)
                {
                    Console.WriteLine("ERROR: Creating Pipe but the pipe is too short: #" + jsonDuct.ID.ToString() + "\n");
                    continue;
                }
                IfcDuctSegment ifcDuctSegment = ifcStore.Instances.New<IfcDuctSegment>(flowSegment =>
                {
                    flowSegment.Name = "Duct Segment: " + jsonDuct.ID.ToString();
                    flowSegment.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "Tessellation";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            shapeRepresentation.Items.Add(createPolygonalFaceSet(jsonDuct.Baseprofile, appearance.First(p => p.Key == BuildingComponent.Duct)));
                        }));
                    });
                    flowSegment.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                attachMaterial(ifcDuctSegment, Material.Steel);
                ret.Add(ifcDuctSegment);
            }
            return ret;
        }
        private void createDuctFittings()
        {
            //Console.WriteLine("Creating Duct Elbows");
            //using (var txn = ifcStore.BeginTransaction("Create Duct Ellbows"))
            //{
            //    //create pipe ellbows
            //    jsonPipe pipe1 = null, pipe2 = null;
            //    if (jmep.Elbow_Pipe_Junction != null)
            //    {
            //        //create material
            //        IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
            //        {
            //            relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
            //            {
            //                material.Name = TypicalMaterial.PVC;
            //            });
            //        });
            //        foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
            //        {
            //            pipe1 = pipes.Find(x => x.ID == jsonFitting.pipe_index_1);
            //            pipe2 = pipes.Find(x => x.ID == jsonFitting.pipe_index_2);
            //            if (pipe1 != null && pipe2 != null && jsonFitting.isValid)
            //            {
            //                double angle = jsonFitting.angle;
            //                double radius = jsonFitting.radius * UNIT_CONVERSION;
            //                jsonXYZ rotationAxis = jsonXYZ.YBasis * UNIT_CONVERSION;
            //                jsonXYZ rotationCenter = jsonFitting.center.distanceTo(jsonFitting.location) * jsonXYZ.XBasis * UNIT_CONVERSION;

            //                jsonXYZ locationJsonXYZ = jsonFitting.location * UNIT_CONVERSION;
            //                jsonXYZ axisJsonXYZ = jsonFitting.refAxis * UNIT_CONVERSION;
            //                jsonXYZ refDirJsonXYZ = (jsonFitting.center - jsonFitting.location) * UNIT_CONVERSION;
            //                if (radius > rotationCenter.x)
            //                {
            //                    error_msg += "WARNING: Creating Ellbow but the pipe is too short for revolving\n";
            //                    continue;
            //                }
            //                //axis: Z dir; refDirection: X dir
            //                IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
            //                {
            //                    flowFitting.Name = "Ellbow: " + jsonFitting.pipe_index_1.ToString() + " & " + jsonFitting.pipe_index_2.ToString();
            //                    flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
            //                    {
            //                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
            //                        {
            //                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            //                            shapeRepresentation.RepresentationIdentifier = "SweptSolid";
            //                            shapeRepresentation.RepresentationIdentifier = "Body";
            //                            //showcase appearance
            //                            IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
            //                            {
            //                                styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
            //                                {
            //                                    presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
            //                                    {
            //                                        surfaceStyle.Name = "PVC, common";
            //                                        surfaceStyle.Side = IfcSurfaceSide.BOTH;
            //                                        surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
            //                                        {
            //                                            surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
            //                                            {
            //                                                colorRGB.Red = 0.5;
            //                                                colorRGB.Green = 0.5;
            //                                                colorRGB.Blue = 0.5;
            //                                            });
            //                                            surfaceStyleRendering.Transparency = 0;
            //                                            surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
            //                                            surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
            //                                            surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
            //                                        }));
            //                                    }));
            //                                }));
            //                            });
            //                            shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcRevolvedAreaSolid>(revolvedAreaSolid =>
            //                            {
            //                                revolvedAreaSolid.Angle = angle;
            //                                revolvedAreaSolid.SweptArea = ifcStore.Instances.New<IfcCircleProfileDef>(circleProfileDef =>
            //                                {
            //                                    circleProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
            //                                    circleProfileDef.ProfileName = "";
            //                                    circleProfileDef.Radius = radius;
            //                                    circleProfileDef.Position = ifcStore.Instances.New<IfcAxis2Placement2D>(axis2Placement2D =>
            //                                    {
            //                                        axis2Placement2D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
            //                                        {
            //                                            cartesianPoint.SetXY(0, 0);
            //                                        });
            //                                    });
            //                                });
            //                                revolvedAreaSolid.Axis = ifcStore.Instances.New<IfcAxis1Placement>(axis1Placement =>
            //                                {
            //                                    axis1Placement.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
            //                                    {
            //                                        direction.SetXYZ(rotationAxis.x, rotationAxis.y, rotationAxis.z);
            //                                    });
            //                                    axis1Placement.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
            //                                    {
            //                                        cartesianPoint.SetXYZ(rotationCenter.x, rotationCenter.y, rotationCenter.z);
            //                                    });
            //                                });
            //                                revolvedAreaSolid.Position = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
            //                                {
            //                                    axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
            //                                    {
            //                                        cartesianPoint.SetXYZ(locationJsonXYZ.x, locationJsonXYZ.y, locationJsonXYZ.z);
            //                                    });
            //                                    axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
            //                                    {
            //                                        direction.SetXYZ(axisJsonXYZ.x, axisJsonXYZ.y, axisJsonXYZ.z);
            //                                    });
            //                                    axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
            //                                    {
            //                                        direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
            //                                    });
            //                                });
            //                                ifcEllbowRepresentations.Add(revolvedAreaSolid);
            //                                ifcStyledItem.Item = revolvedAreaSolid;
            //                            }));
            //                        }));
            //                    });
            //                    flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
            //                });
            //                ifcRelAssociatesMaterial.RelatedObjects.Add(ifcFlowFitting);
            //                ifcProducts.Add((IfcProduct)ifcFlowFitting);
            //            }
            //        }
            //    }
            //    txn.Commit();
            //}
            //Console.WriteLine("Creating Duct Tees");
            //using (var txn = ifcStore.BeginTransaction("Create Duct Tees"))
            //{
            //    //create pipe Tees
            //    if (jmep.T_Pipe_Junction != null)
            //    {
            //        jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
            //        //create material
            //        IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
            //        {
            //            relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
            //            {
            //                material.Name = TypicalMaterial.PVC;
            //            });
            //        });
            //        foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
            //        {
            //            pipe1 = pipes.Find(x => x.ID == jsonTee.pipe_index_1);
            //            pipe2 = pipes.Find(x => x.ID == jsonTee.pipe_index_2);
            //            pipe3 = pipes.Find(x => x.ID == jsonTee.pipe_index_3);
            //            if (pipe1 != null && pipe2 != null && pipe3 != null && jsonTee.isValid)
            //            {
            //                List<jsonXYZ> pts = new List<jsonXYZ>() { jsonTee.Pt1, jsonTee.Pt2, jsonTee.Pt3 };
            //                List<jsonPipe> connectedPipes = new List<jsonPipe>() { pipe1, pipe2, pipe3 };
            //                double radius;
            //                double depth;
            //                jsonXYZ locationJsonXYZ = null, axisJsonXYZ = null, refDirJsonXYZ = null;

            //                //axis: Z dir; refDirection: X dir
            //                IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
            //                {
            //                    flowFitting.Name = "Tee: " + jsonTee.pipe_index_1.ToString() + " , " + jsonTee.pipe_index_2.ToString() + " & " + jsonTee.pipe_index_3.ToString();
            //                    flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
            //                    {
            //                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
            //                        {
            //                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            //                            shapeRepresentation.RepresentationIdentifier = "Body";
            //                            var zip = pts.Zip(connectedPipes, (pt, pipe) => new { pt, pipe });
            //                            foreach (var z in zip)
            //                            {

            //                                //showcase appearance
            //                                IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
            //                                {
            //                                    styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
            //                                    {
            //                                        presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
            //                                        {
            //                                            surfaceStyle.Name = "PVC, common";
            //                                            surfaceStyle.Side = IfcSurfaceSide.BOTH;
            //                                            surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
            //                                            {
            //                                                surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
            //                                                {
            //                                                    colorRGB.Red = 0.5;
            //                                                    colorRGB.Green = 0.5;
            //                                                    colorRGB.Blue = 0.5;
            //                                                });
            //                                                surfaceStyleRendering.Transparency = 0;
            //                                                surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
            //                                                surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
            //                                                surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
            //                                            }));
            //                                        }));
            //                                    }));
            //                                });
            //                                radius = z.pipe.radius * UNIT_CONVERSION * jsonTee.config.Radius_ratio;
            //                                depth = z.pt.distanceTo(jsonTee.center) * UNIT_CONVERSION;
            //                                locationJsonXYZ = z.pt * UNIT_CONVERSION;
            //                                axisJsonXYZ = (jsonTee.center - z.pt) * UNIT_CONVERSION;
            //                                refDirJsonXYZ = !(axisJsonXYZ.x == 0 && axisJsonXYZ.y == 0) ? (new jsonXYZ(axisJsonXYZ.y, -axisJsonXYZ.x, 0) * UNIT_CONVERSION) : (new jsonXYZ(axisJsonXYZ.z, 0, axisJsonXYZ.x) * UNIT_CONVERSION);
            //                                shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
            //                                {
            //                                    extrudedAreaSolid.Depth = depth;
            //                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcCircleProfileDef>(circleProfileDef =>
            //                                    {
            //                                        circleProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
            //                                        circleProfileDef.ProfileName = "";
            //                                        circleProfileDef.Radius = radius;
            //                                        circleProfileDef.Position = ifcStore.Instances.New<IfcAxis2Placement2D>(axis2Placement2D =>
            //                                        {
            //                                            axis2Placement2D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
            //                                            {
            //                                                cartesianPoint.SetXY(0, 0);
            //                                            });
            //                                        });
            //                                    });
            //                                    extrudedAreaSolid.ExtrudedDirection = ifcStore.Instances.New<IfcDirection>(direction => direction.SetXYZ(0, 0, 1));
            //                                    extrudedAreaSolid.Position = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
            //                                    {
            //                                        axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
            //                                        {
            //                                            cartesianPoint.SetXYZ(locationJsonXYZ.x, locationJsonXYZ.y, locationJsonXYZ.z);
            //                                        });
            //                                        axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
            //                                        {
            //                                            direction.SetXYZ(axisJsonXYZ.x, axisJsonXYZ.y, axisJsonXYZ.z);
            //                                        });
            //                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
            //                                        {
            //                                            direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
            //                                        });
            //                                    });
            //                                    ifcTFittingRepresentations.Add(extrudedAreaSolid);
            //                                    ifcStyledItem.Item = extrudedAreaSolid;
            //                                }));
            //                            }
            //                        }));
            //                    });
            //                    flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
            //                });
            //                ifcRelAssociatesMaterial.RelatedObjects.Add(ifcFlowFitting);
            //                ifcProducts.Add((IfcProduct)ifcFlowFitting);
            //            }
            //        }
            //    }
            //    txn.Commit();
            //}
        }
        public IfcPolygonalFaceSet createPolygonalFaceSet(jsonXYZ[] baseProfile, KeyValuePair<BuildingComponent, Style> pair)
        {
            return ifcStore.Instances.New<IfcPolygonalFaceSet>(polygonalFaceSet =>
            {
                polygonalFaceSet.Coordinates = ifcStore.Instances.New<IfcCartesianPointList3D>(cartesianPointList3D =>
                {
                    for (int i = 0; i < 8; i++)
                    {
                        jsonXYZ pt = baseProfile[i];
                        cartesianPointList3D.CoordList.GetAt(i).AddRange(new IfcLengthMeasure[] { pt.x * UNIT_CONVERSION, pt.y * UNIT_CONVERSION, pt.z * UNIT_CONVERSION });
                    }
                });
                polygonalFaceSet.Closed = true;
                for (int i = 1; i <= 4; i++)
                {
                    polygonalFaceSet.Faces.Add(ifcStore.Instances.New<IfcIndexedPolygonalFace>(indexedPolygonalFace =>
                    {
                        indexedPolygonalFace.CoordIndex.AddRange(new IfcPositiveInteger[] { i, i != 4 ? i + 1 : 1, i != 4 ? i + 5 : 5, i + 4 });
                    }));
                }
                setAppearance(polygonalFaceSet, pair);
            });
        }
        private IfcRevolvedAreaSolid createRevolved(double angle, double radius, jsonXYZ rotationAxis, jsonXYZ rotationCenter, jsonXYZ locationJsonXYZ, jsonXYZ axisJsonXYZ, jsonXYZ refDirJsonXYZ, KeyValuePair<BuildingComponent, Style> pair)
        {
            return ifcStore.Instances.New<IfcRevolvedAreaSolid>(revolvedAreaSolid =>
            {
                revolvedAreaSolid.Angle = angle;
                revolvedAreaSolid.SweptArea = ifcStore.Instances.New<IfcCircleProfileDef>(circleProfileDef =>
                {
                    circleProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                    circleProfileDef.ProfileName = "";
                    circleProfileDef.Radius = radius;
                    circleProfileDef.Position = ifcStore.Instances.New<IfcAxis2Placement2D>(axis2Placement2D =>
                    {
                        axis2Placement2D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                        {
                            cartesianPoint.SetXY(0, 0);
                        });
                    });
                });
                revolvedAreaSolid.Axis = ifcStore.Instances.New<IfcAxis1Placement>(axis1Placement =>
                {
                    axis1Placement.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                    {
                        direction.SetXYZ(rotationAxis.x, rotationAxis.y, rotationAxis.z);
                    });
                    axis1Placement.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                    {
                        cartesianPoint.SetXYZ(rotationCenter.x, rotationCenter.y, rotationCenter.z);
                    });
                });
                revolvedAreaSolid.Position = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
                {
                    axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                    {
                        cartesianPoint.SetXYZ(locationJsonXYZ.x, locationJsonXYZ.y, locationJsonXYZ.z);
                    });
                    axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                    {
                        direction.SetXYZ(axisJsonXYZ.x, axisJsonXYZ.y, axisJsonXYZ.z);
                    });
                    axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                    {
                        direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
                    });
                });
                setAppearance(revolvedAreaSolid, pair);
            });
        }
        private IfcExtrudedAreaSolid createCylinder(double depth, double radius, jsonXYZ locationJsonXYZ, jsonXYZ axisJsonXYZ, jsonXYZ refDirJsonXYZ, KeyValuePair<BuildingComponent, Style> pair)
        {
            return ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
            {
                extrudedAreaSolid.Depth = depth;
                extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcCircleProfileDef>(circleProfileDef =>
                {
                    circleProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                    circleProfileDef.ProfileName = "";
                    circleProfileDef.Radius = radius;
                    circleProfileDef.Position = ifcStore.Instances.New<IfcAxis2Placement2D>(axis2Placement2D =>
                    {
                        axis2Placement2D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                        {
                            cartesianPoint.SetXY(0, 0);
                        });
                    });
                });
                extrudedAreaSolid.ExtrudedDirection = ifcStore.Instances.New<IfcDirection>(direction => direction.SetXYZ(0, 0, 1));
                extrudedAreaSolid.Position = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
                {
                    axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                    {
                        cartesianPoint.SetXYZ(locationJsonXYZ.x, locationJsonXYZ.y, locationJsonXYZ.z);
                    });
                    axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                    {
                        direction.SetXYZ(axisJsonXYZ.x, axisJsonXYZ.y, axisJsonXYZ.z);
                    });
                    axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                    {
                        direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
                    });
                });
                setAppearance(extrudedAreaSolid, pair);
            });
        } 
    }
}
