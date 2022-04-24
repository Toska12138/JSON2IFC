using System;
using System.Collections.Generic;
using System.Linq;
using Scan2BimShared.Models;
using Scan2BimShared.Models.IfcEntities;
using Scan2BimShared.StaticData.IfcEnums;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace Scan2BimConnect.Utilities
{
    class IFCStructureCreater : IFCElementCreater
    {
        const double WINDOW_WIDTH = 0.05;
        const double DOOR_WIDTH = 0.07;
        jsonStructure js { get; }
        public IFCStructureCreater(IfcStore ifcStore, IfcBuilding ifcBuilding, jsonStructure? js) : base(ifcStore, ifcBuilding)
        {
            this.js = js ?? throw new ArgumentNullException("jsonStructure error: empty js");
        }
        public IfcWallType createWallType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            properties = properties ?? throw new ArgumentNullException();
            return ifcStore.Instances.New<IfcWallType>(wallType =>
            {
                if (properties["IfcWallType"] != null) wallType.HasPropertySets.AddRange(properties["IfcWallType"].ConvertAll(props => propertyAgent.generateSet(props)));
                wallType.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
            });
        }
        public IfcColumnType createColumnType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcColumnType>(columnType =>
            {
                if (properties["IfcColumnType"] != null) columnType.HasPropertySets.AddRange(properties["IfcColumnType"].ConvertAll(props => props.ToIfcPropertySet(ifcStore)));
                columnType.PredefinedType = IfcColumnTypeEnum.COLUMN;
            });
        }
        public IfcBeamType createBeamType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcBeamType>(beamType =>
            {
                if (properties["IfcBeamType"] != null) beamType.HasPropertySets.AddRange(properties["IfcBeamType"].ConvertAll(props => props.ToIfcPropertySet(ifcStore)));
                beamType.PredefinedType = IfcBeamTypeEnum.BEAM;
            });
        }
        public IfcWindowType createWindowType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcWindowType>(windowType =>
            {
                if (properties["IfcWindowType"] != null) windowType.HasPropertySets.AddRange(properties["IfcWindowType"].ConvertAll(props => props.ToIfcPropertySet(ifcStore)));
                windowType.PredefinedType = IfcWindowTypeEnum.WINDOW;
            });
        }
        public IfcDoorType createDoorType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcDoorType>(doorType =>
            {
                if (properties["IfcDoorType"] != null) doorType.HasPropertySets.AddRange(properties["IfcDoorType"].ConvertAll(props => props.ToIfcPropertySet(ifcStore)));
                doorType.PredefinedType = IfcDoorTypeEnum.DOOR;
            });
        }
        public IfcSlabType createSlabType(Dictionary<string, List<PropertySet>> properties)
        {
            PropertyAgent propertyAgent = new PropertyAgent(ifcStore);
            return ifcStore.Instances.New<IfcSlabType>(slabType =>
            {
                if (properties["IfcSlabType"] != null) slabType.HasPropertySets.AddRange(properties["IfcSlabType"].ConvertAll(props => props.ToIfcPropertySet(ifcStore)));
                slabType.PredefinedType = IfcSlabTypeEnum.NOTDEFINED;
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
        public IfcColumn createColumn(jsonColumn jsonColumn, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            jsonColumn.locationPoint = jsonColumn.locationPoint ?? throw new ArgumentNullException("jsonColumn error: empty location");

            double length = jsonColumn.length * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonColumn error: empty length");
            double width = jsonColumn.width * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonColumn error: empty width");
            double height = jsonColumn.height * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonColumn error: empty height");

            jsonXYZ refDirJsonXYZ = new jsonXYZ(1, 0, 0).rotate(new jsonXYZ(0, 0, 0), new jsonXYZ(0, 0, 1), jsonColumn.rotationalAngleInRadius ?? throw new ArgumentNullException("jsonColumn error: empty rotation")) * UNIT_CONVERSION;
            jsonXYZ locationJsonXYZ = new jsonXYZ(jsonColumn.locationPoint.x, jsonColumn.locationPoint.y, jsonColumn.locationPoint.z) * UNIT_CONVERSION;
            jsonXYZ axisJsonXYZ = jsonXYZ.ZBasis * UNIT_CONVERSION;
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
            propertyAgent.defineProperties(ifcColumn, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Height", type = "IfcLengthMeasure", value = (Math.Truncate(height) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Length", type = "IfcLengthMeasure", value = (Math.Truncate(length) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Width", type = "IfcLengthMeasure", value = (Math.Truncate(width) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Volume", type = "IfcVolumeMeasure", value = (Math.Truncate(height * width * length) / UNIT_CONVERSION / UNIT_CONVERSION / UNIT_CONVERSION).ToString() } }
                }
            });
            new PropertyAgent(ifcStore).defineProperties(ifcColumn, properties);
            setAppearance(ifcColumn.Representation.Representations.First().Items.First(), style);
            attachMaterial(ifcColumn, Material.Concrete);
            return ifcColumn;
        }
        public IfcBeam? createBeam(jsonBeam jsonBeam, List<IfcRepresentation> excludeReps, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            double length = jsonBeam.length * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonBeam error: empty length");
            double width = jsonBeam.width * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonBeam error: empty width");
            double height = jsonBeam.height * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonBeam error: empty height");
            if (length == 0 || width == 0 || height == 0)
            {
                Console.WriteLine("Empty Beam: length = " + length + ", width = " + width + ", thickness = " + height);
                return null;
            }
            jsonXYZ refDirJsonXYZ = (jsonBeam.endPoint - jsonBeam.startPoint).rotate(new jsonXYZ(0, 0, 0), new jsonXYZ(0, 0, 1), Math.PI / 2) * UNIT_CONVERSION;
            jsonXYZ locationJsonXYZ = jsonBeam.startPoint * UNIT_CONVERSION;
            jsonXYZ axisJsonXYZ = new jsonXYZ((jsonBeam.endPoint - jsonBeam.startPoint).x, (jsonBeam.endPoint - jsonBeam.startPoint).y, (jsonBeam.endPoint - jsonBeam.startPoint).z) * UNIT_CONVERSION;
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
                        IfcBooleanOperand ifcBooleanOperand = createCuboid(length, width, height, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                        shapeRepresentation.Items.Add((IfcRepresentationItem)exclude(excludeReps, ifcBooleanOperand));
                    }));
                });
                beam.ObjectPlacement = ifcBuilding.ObjectPlacement;
            });
            propertyAgent.defineProperties(ifcBeam, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Height", type = "IfcLengthMeasure", value = (Math.Truncate(height) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Length", type = "IfcLengthMeasure", value = (Math.Truncate(length) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Width", type = "IfcLengthMeasure", value = (Math.Truncate(width) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Volume", type = "IfcVolumeMeasure", value = (Math.Truncate(height * width * length) / UNIT_CONVERSION / UNIT_CONVERSION / UNIT_CONVERSION).ToString() } }
                }
            });
            new PropertyAgent(ifcStore).defineProperties(ifcBeam, properties);
            setAppearance(ifcBeam.Representation.Representations.First().Items.First(), style);
            attachMaterial(ifcBeam, Material.Concrete);
            return ifcBeam;
        }
        public IfcWall createWall(jsonWall jsonWall, List<IfcRepresentation> excludeReps, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            jsonWall.endPoint = jsonWall.endPoint ?? throw new ArgumentNullException("jsonWall error: empty endPoint");
            jsonWall.startPoint = jsonWall.startPoint ?? throw new ArgumentNullException("jsonWall error: empty startPoint");
            double length = jsonWall.length * UNIT_CONVERSION;
            double width = jsonWall.width * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonWall error: empty height");
            double height = jsonWall.height * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonWall error: empty height");
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
            propertyAgent.defineProperties(ifcWall, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Height", type = "IfcLengthMeasure", value = (Math.Truncate(height * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Length", type = "IfcLengthMeasure", value = (Math.Truncate(length * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Width", type = "IfcLengthMeasure", value = (Math.Truncate(width * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Volume", type = "IfcVolumeMeasure", value = (Math.Truncate(height * width * length) / UNIT_CONVERSION / UNIT_CONVERSION / UNIT_CONVERSION).ToString() } }
                }
            });
            setAppearance(ifcWall.Representation.Representations.First().Items.First(), style);
            attachMaterial(ifcWall, Material.Concrete);
            new PropertyAgent(ifcStore).defineProperties(ifcWall, properties);
            return ifcWall;
        }
        public IfcWindow createWindow(jsonWindow jsonWindow, List<IfcRepresentation> excludeReps, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            jsonWindow.endPoint = jsonWindow.endPoint ?? throw new ArgumentNullException("jsonWindow error: empty endpoint");
            jsonWindow.startPoint = jsonWindow.startPoint ?? throw new ArgumentNullException("jsonWindow error: empty startPoint");

            double length = jsonWindow.length * UNIT_CONVERSION;
            double width = WINDOW_WIDTH * UNIT_CONVERSION;
            double shapeWidth = jsonWindow.width * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonWall error: empty width");
            double height = jsonWindow.height * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonWindow error: empty height");

            jsonXYZ refDirJsonXYZ = (jsonWindow.endPoint - jsonWindow.startPoint) * UNIT_CONVERSION;
            jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.x, jsonWindow.location.y, jsonWindow.location.z) * UNIT_CONVERSION;
            jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * UNIT_CONVERSION;
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
                        IfcBooleanOperand ifcBooleanOperand = createCuboid(height, length, shapeWidth, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                        cutReps(excludeReps, ifcBooleanOperand);
                        shapeRepresentation.Items.Add(createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                    }));
                });
                window.ObjectPlacement = ifcBuilding.ObjectPlacement;
                window.PredefinedType = IfcWindowTypeEnum.NOTDEFINED;
            });
            propertyAgent.defineProperties(ifcWindow, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Height", type = "IfcLengthMeasure", value = (Math.Truncate(height * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Length", type = "IfcLengthMeasure", value = (Math.Truncate(length * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Width", type = "IfcLengthMeasure", value = (Math.Truncate(WINDOW_WIDTH * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Volume", type = "IfcVolumeMeasure", value = (Math.Truncate(height * WINDOW_WIDTH * length) / UNIT_CONVERSION / UNIT_CONVERSION / UNIT_CONVERSION).ToString() } }
                }
            });
            setAppearance(ifcWindow.Representation.Representations.First().Items.First(), style);
            new PropertyAgent(ifcStore).defineProperties(ifcWindow, properties);
            attachMaterial(ifcWindow, Material.Glass);
            return ifcWindow;
        }
        public IfcDoor createDoor(jsonDoor jsonDoor, List<IfcRepresentation> excludeReps, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            jsonDoor.endPoint = jsonDoor.endPoint ?? throw new ArgumentNullException("jsonDoor error: empty endpoint");
            jsonDoor.startPoint = jsonDoor.startPoint ?? throw new ArgumentNullException("jsonDoor error: empty startPoint");
            double length = jsonDoor.length * UNIT_CONVERSION;
            double width = DOOR_WIDTH * UNIT_CONVERSION;
            double shapeWidth = jsonDoor.width * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonDoor error: empty width");
            double height = jsonDoor.height * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonDoor error: empty height");

            jsonXYZ refDirJsonXYZ = (jsonDoor.endPoint - jsonDoor.startPoint) * UNIT_CONVERSION;
            jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.x, jsonDoor.location.y, jsonDoor.location.z) * UNIT_CONVERSION;
            jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * UNIT_CONVERSION;
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
                        IfcBooleanOperand ifcBooleanOperand = createCuboid(height, length, shapeWidth, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                        cutReps(excludeReps, ifcBooleanOperand);
                        shapeRepresentation.Items.Add(createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ));
                    }));
                });
                door.ObjectPlacement = ifcBuilding.ObjectPlacement;
                door.PredefinedType = IfcDoorTypeEnum.NOTDEFINED;
            });
            propertyAgent.defineProperties(ifcDoor, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Height", type = "IfcLengthMeasure", value = (Math.Truncate(height * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Length", type = "IfcLengthMeasure", value = (Math.Truncate(length * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Width", type = "IfcLengthMeasure", value = (Math.Truncate(DOOR_WIDTH * UNIT_CONVERSION) / UNIT_CONVERSION).ToString() } },
                    { new Property(){name = "Volume", type = "IfcVolumeMeasure", value = (Math.Truncate(height * DOOR_WIDTH * length) / UNIT_CONVERSION / UNIT_CONVERSION / UNIT_CONVERSION).ToString() } },
                }
            });
            setAppearance(ifcDoor.Representation.Representations.First().Items.First(), style);
            new PropertyAgent(ifcStore).defineProperties(ifcDoor, properties);
            attachMaterial(ifcDoor, Material.Wood);
            return ifcDoor;
        }
        public Tuple<IfcSlab, IfcSlab> createSlab(jsonSlab jsonSlab, List<IfcRepresentation> excludeReps, Dictionary<string, List<PropertySet>> properties, KeyValuePair<BuildingComponent, Style> style)
        {
            jsonSlab.baseProfile = jsonSlab.baseProfile ?? throw new ArgumentNullException("jsonSlab error: empty baseprofile");
            double thickness = jsonSlab.thickness * UNIT_CONVERSION ?? throw new ArgumentNullException("jsonSlab error: empty thickness");
            jsonXYZ refDirJsonXYZ = new jsonXYZ(1, 0, 0) * UNIT_CONVERSION;
            jsonXYZ locationJsonXYZ = jsonSlab.location * UNIT_CONVERSION;
            jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * UNIT_CONVERSION;
            double height = jsonSlab.bottom ?? throw new ArgumentNullException("Slab Empty bottom");//(js.Wall ?? throw new ArgumentNullException("Creating slab error: empty walls"))[0].height ?? throw new ArgumentNullException("wall empty height");
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
            attachMaterial(ifcSlab1, Material.Concrete);

            IfcSlab ifcSlab2 = ifcStore.Instances.New<IfcSlab>(slab =>
            {
                slab.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                {
                    productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                    {
                        shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                        shapeRepresentation.RepresentationType = "SweptSolid";
                        shapeRepresentation.RepresentationIdentifier = "Body";
                        shapeRepresentation.Items.Add(createArbitrary(thickness, jsonSlab.baseProfile, locationJsonXYZ + jsonXYZ.ZBasis * (height * UNIT_CONVERSION + thickness), axisJsonXYZ, refDirJsonXYZ));
                    }));
                });
                slab.ObjectPlacement = ifcBuilding.ObjectPlacement;
            });//ceiling
            propertyAgent.defineProperties(ifcSlab1, new PropertySet()
            {
                name = "Dimensional",
                properties = new List<Property>()
                {
                    { new Property(){name = "Thickness", type = "IfcLengthMeasure", value = (Math.Truncate(thickness) / UNIT_CONVERSION).ToString() } }
                }
            });
            new PropertyAgent(ifcStore).defineProperties(ifcSlab2, properties);
            new PropertyAgent(ifcStore).defineProperties(ifcSlab1, properties);
            attachMaterial(ifcSlab2, Material.Concrete);
            setAppearance(ifcSlab1.Representation.Representations.First().Items.First(), style);
            setAppearance(ifcSlab2.Representation.Representations.First().Items.First(), style);
            return new Tuple<IfcSlab, IfcSlab>(ifcSlab1, ifcSlab2);
        }
    }
}
