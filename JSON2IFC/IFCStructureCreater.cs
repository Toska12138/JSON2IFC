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
using Xbim.Ifc4.UtilityResource;
using Xbim.IO;
using static JSON2IFC.Material;
using static JSON2IFC.SJSONPlugin;


namespace JSON2IFC
{
    class IFCStructureCreater : IFCElementCreater
    {
        const double WINDOW_WIDTH = 0.05;
        const double DOOR_WIDTH = 0.07;
        jsonStructure js { get; }
        public IFCStructureCreater(IfcStore ifcStore, IfcBuilding ifcBuilding, jsonStructure js) : base(ifcStore, ifcBuilding)
        {
            this.js = js;
        }
        public List<IfcColumn> createColumns(List<IfcRepresentation> excludeReps, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcColumn> ret = new List<IfcColumn>();
            foreach (jsonColumn jsonColumn in js.Column)
            {
                double length = jsonColumn.length * 1000;
                double width = jsonColumn.width * 1000;
                double height = jsonColumn.height * 1000;
                jsonXYZ refDirJsonXYZ = new jsonXYZ(1, 0, 0).rotate(new jsonXYZ(0, 0, 0), new jsonXYZ(0, 0, 1), jsonColumn.rotationalAngleInRadius) * 1000;
                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonColumn.locationPoint.x, jsonColumn.locationPoint.y, jsonColumn.locationPoint.z) * 1000;
                jsonXYZ axisJsonXYZ = jsonXYZ.ZBasis * 1000;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir
                IfcColumn ifcColumn = ifcStore.Instances.New<IfcColumn>(column =>
                {
                    column.Name = "";
                    column.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "SweptSolid";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            shapeRepresentation.Items.Add(createCuboid(height, width, length, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                        }));
                    });
                    column.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                setAppearance(ifcColumn.Representation.Representations.First().Items.First(), style);
                attachMaterial(ifcColumn, Concrete);
                ret.Add(ifcColumn);
            }
            return ret;
        }
        public List<IfcBeam> createBeams(List<IfcRepresentation> excludeReps, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcBeam> ret = new List<IfcBeam>();
            foreach (jsonBeam jsonBeam in js.Beam)
            {
                double length = jsonBeam.length * 1000;
                double width = jsonBeam.width * 1000;
                double thickness = jsonBeam.height * 1000;
                if (length == 0 || width == 0 || thickness == 0)
                {
                    Console.WriteLine("Empty Beam: length = " + length + ", width = " + width + ", thickness = " + thickness);
                    continue;
                }
                jsonXYZ refDirJsonXYZ = (jsonBeam.endPoint - jsonBeam.startPoint).rotate(new jsonXYZ(0, 0, 0), new jsonXYZ(0, 0, 1), Math.PI / 2) * 1000;
                jsonXYZ locationJsonXYZ = jsonBeam.startPoint * 1000;
                jsonXYZ axisJsonXYZ = new jsonXYZ((jsonBeam.endPoint - jsonBeam.startPoint).x, (jsonBeam.endPoint - jsonBeam.startPoint).y, (jsonBeam.endPoint - jsonBeam.startPoint).z) * 1000;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                IfcBeam ifcBeam = ifcStore.Instances.New<IfcBeam>(beam =>
                {
                    beam.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "CSG";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            IfcBooleanOperand ifcBooleanOperand = createCuboid(length, width, thickness, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                            shapeRepresentation.Items.Add((IfcRepresentationItem)exclude(excludeReps, ifcBooleanOperand));
                        }));
                    });
                    beam.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });
                setAppearance(ifcBeam.Representation.Representations.First().Items.First(), style);
                attachMaterial(ifcBeam, Concrete);
                ret.Add(ifcBeam);
            }
            return ret;
        }
        public List<IfcWall> createWalls(List<IfcRepresentation> excludeReps, IfcWallType ifcWallType, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcWall> ret = new List<IfcWall>();
            IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(relDefinesByType =>
            {
                relDefinesByType.RelatingType = ifcWallType;
            });
            foreach (jsonWall jsonWall in js.Wall)
            {
                double length = jsonWall.length * UNIT_CONVERSION;
                double width = jsonWall.width * UNIT_CONVERSION;
                double height = jsonWall.height * UNIT_CONVERSION;
                jsonXYZ refDirJsonXYZ = new jsonXYZ((jsonWall.endPoint - jsonWall.startPoint).x, (jsonWall.endPoint - jsonWall.startPoint).y, (jsonWall.endPoint - jsonWall.startPoint).z) * UNIT_CONVERSION;
                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWall.location.x, jsonWall.location.y, jsonWall.location.z) * UNIT_CONVERSION;
                jsonXYZ axisJsonXYZ = jsonXYZ.ZBasis * UNIT_CONVERSION;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir
                IfcWall ifcWall = ifcStore.Instances.New<IfcWall>(wall =>
                {
                    wall.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "CSG";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            IfcBooleanOperand ifcBooleanOperand = createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                            shapeRepresentation.Items.Add((IfcRepresentationItem)exclude(excludeReps, ifcBooleanOperand));
                        }));
                    });
                    wall.ObjectPlacement = ifcBuilding.ObjectPlacement;
                    wall.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
                    wall.Tag = jsonWall.id.ToString();
                });
                setAppearance(ifcWall.Representation.Representations.First().Items.First(), style);
                ifcRelDefinesByType.RelatedObjects.Add(ifcWall);
                attachMaterial(ifcWall, Concrete);
                PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
                propertyAgent.defineProperties(ifcWall, properties);
                ret.Add(ifcWall);
            }
            return ret;
        }
        public List<IfcWindow> createWindows(List<IfcRepresentation> excludeReps, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcWindow> ret = new List<IfcWindow>();
            foreach (jsonWindow jsonWindow in js.Window)
            {
                double length = jsonWindow.length * 1000;
                double width = WINDOW_WIDTH * 1000;
                double height = jsonWindow.height * 1000;

                jsonXYZ refDirJsonXYZ = (jsonWindow.endPoint - jsonWindow.startPoint) * 1000;
                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.x, jsonWindow.location.y, jsonWindow.location.z) * 1000;
                jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                IfcWindow ifcWindow = ifcStore.Instances.New<IfcWindow>(window =>
                {
                    window.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "CSG";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            IfcBooleanOperand ifcBooleanOperand = createCuboid(height, length, jsonWindow.width * UNIT_CONVERSION, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                            cutReps(excludeReps, ifcBooleanOperand);
                            shapeRepresentation.Items.Add(createCuboid(height, length, WINDOW_WIDTH * UNIT_CONVERSION, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                        }));
                    });
                    window.ObjectPlacement = ifcBuilding.ObjectPlacement;
                    window.PredefinedType = IfcWindowTypeEnum.NOTDEFINED;
                });
                setAppearance(ifcWindow.Representation.Representations.First().Items.First(), style);
                IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(p =>
                {
                    p.RelatedObjects.Add(ifcWindow);
                    p.RelatingType = ifcStore.Instances.New<IfcWindowType>(windowType =>
                    {
                        windowType.ParameterTakesPrecedence = new IfcBoolean(true);
                        windowType.PartitioningType = IfcWindowTypePartitioningEnum.SINGLE_PANEL;
                    });
                });
                attachMaterial(ifcWindow, Glass);
                ret.Add(ifcWindow);
            }
            return ret;
        }
        public List<IfcDoor> createDoors(List<IfcRepresentation> excludeReps, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcDoor> ret = new List<IfcDoor>();
            foreach (jsonDoor jsonDoor in js.Door)
            {
                double length = jsonDoor.length * 1000;
                double width = DOOR_WIDTH * 1000;
                double height = jsonDoor.height * 1000;

                jsonXYZ refDirJsonXYZ = (jsonDoor.endPoint - jsonDoor.startPoint) * 1000;
                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.x, jsonDoor.location.y, jsonDoor.location.z) * 1000;
                jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                IfcDoor ifcDoor = ifcStore.Instances.New<IfcDoor>(door =>
                {
                    door.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "CSG";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            IfcBooleanOperand ifcBooleanOperand = createCuboid(height, length, jsonDoor.width * UNIT_CONVERSION, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                            cutReps(excludeReps, ifcBooleanOperand);
                            shapeRepresentation.Items.Add(createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                        }));
                    });

                    door.ObjectPlacement = ifcBuilding.ObjectPlacement;
                    door.PredefinedType = IfcDoorTypeEnum.NOTDEFINED;
                });
                setAppearance(ifcDoor.Representation.Representations.First().Items.First(), style);
                IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(p =>
                {
                    p.RelatedObjects.Add(ifcDoor);
                    p.RelatingType = ifcStore.Instances.New<IfcDoorType>(doorType =>
                    {
                        doorType.OperationType = IfcDoorTypeOperationEnum.SINGLE_SWING_RIGHT;
                        doorType.ParameterTakesPrecedence = new IfcBoolean(true);
                    });
                });
                attachMaterial(ifcDoor, Wood);
            }
            return ret;
        }
        public List<IfcSlab> createSlabs(List<IfcRepresentation> excludeReps, KeyValuePair<BuildingComponent, Style> style)
        {
            List<IfcSlab> ret = new List<IfcSlab>();
            foreach (jsonSlab jsonSlab in js.Slab)
            {
                double thickness = jsonSlab.thickness * UNIT_CONVERSION;
                jsonXYZ refDirJsonXYZ = new jsonXYZ(1, 0, 0) * UNIT_CONVERSION;
                jsonXYZ locationJsonXYZ = jsonSlab.location * UNIT_CONVERSION;
                jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * UNIT_CONVERSION;
                //axis: extrude dir/Z dir; refDirection: width dir/X dir
                //showcase appearance
                IfcSlab ifcSlab1 = ifcStore.Instances.New<IfcSlab>(slab =>
                {
                    slab.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "SweptSolid";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            shapeRepresentation.Items.Add(createArbitrary(thickness, jsonSlab.baseProfile, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                        }));
                    });
                    slab.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });//floor
                attachMaterial(ifcSlab1, Concrete);

                IfcSlab ifcSlab2 = ifcStore.Instances.New<IfcSlab>(slab =>
                {
                    slab.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                    {
                        productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                        {
                            shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                            shapeRepresentation.RepresentationType = "SweptSolid";
                            shapeRepresentation.RepresentationIdentifier = "Body";
                            shapeRepresentation.Items.Add(createArbitrary(thickness, jsonSlab.baseProfile, locationJsonXYZ + jsonXYZ.ZBasis * (js.Wall[0].height + jsonSlab.thickness) * UNIT_CONVERSION, axisJsonXYZ, refDirJsonXYZ));
                        }));
                    });
                    slab.ObjectPlacement = ifcBuilding.ObjectPlacement;
                });//ceiling
                attachMaterial(ifcSlab2, Concrete);
                setAppearance(ifcSlab1.Representation.Representations.First().Items.First(), style);
                setAppearance(ifcSlab2.Representation.Representations.First().Items.First(), style);
                ret.Add(ifcSlab1);
                ret.Add(ifcSlab2);
            }
            return ret;
        }
        public IfcWallType createWallType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcWallType>(wallType =>
            {
                if(properties["IfcWallType"] != null) wallType.HasPropertySets.AddRange(properties["IfcWallType"].ConvertAll(props => propertyAgent.generateSet(props)));
                wallType.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
            });
        }
        public IfcExtrudedAreaSolid createCuboid(double depth, double XDim, double YDim, jsonXYZ locationJsonXYZ, jsonXYZ axisJsonXYZ, jsonXYZ refDirJsonXYZ)
        {
            return ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
            {
                extrudedAreaSolid.Depth = depth;
                extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                {
                    rectangleProfileDef.XDim = XDim;
                    rectangleProfileDef.YDim = YDim;
                    rectangleProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                    rectangleProfileDef.Position = ifcStore.Instances.New<IfcAxis2Placement2D>(axis2Placement2D =>
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
            });
        }
        public IfcExtrudedAreaSolid createArbitrary(double thickness, jsonXYZ[] pts, jsonXYZ locationJsonXYZ, jsonXYZ axisJsonXYZ, jsonXYZ refDirJsonXYZ)
        {
            return ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
            {
                extrudedAreaSolid.Depth = thickness;
                extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcArbitraryClosedProfileDef>(arbitraryClosedProfileDef =>
                {
                    arbitraryClosedProfileDef.OuterCurve = ifcStore.Instances.New<IfcPolyline>(polyline =>
                    {
                        List<IfcCartesianPoint> points = new List<IfcCartesianPoint>();
                        foreach (jsonXYZ jsonXYZ in pts)
                        {
                            points.Add(ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                            {
                                cartesianPoint.SetXY(jsonXYZ.x * UNIT_CONVERSION, jsonXYZ.y * UNIT_CONVERSION);
                            }));
                        }
                        polyline.Points.AddRange(points);
                        polyline.Points.Add(points[0]);
                    });
                    arbitraryClosedProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                });
                extrudedAreaSolid.ExtrudedDirection = ifcStore.Instances.New<IfcDirection>(direction => direction.SetXYZ(0, 0, 1));//ceilling
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
            });
        }
    }
}
