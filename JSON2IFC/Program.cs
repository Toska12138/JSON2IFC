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
using Xbim.IO;


namespace JSON2IFC
{
    public class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo di = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            di = Directory.GetParent(di.FullName);

            string output_path = Path.Combine(di.FullName, "models");
            if (!Directory.Exists(output_path))
                Directory.CreateDirectory(output_path);
            GenerateIFC(TypeIFC.Model, XbimSchemaVersion.Ifc4, output_path);
        }
        internal enum TypeIFC { Structure, MEP, Model };
        internal static class TypicalMaterial
        {
            public static string Concrete { get { return "Concrete"; } }
            public static string Glass { get { return "Glass"; } }
            public static string Wood { get { return "Wood"; } }
            public static string Air { get { return "Air"; } }
            public static string PVC { get { return "PVC"; } }
        }
        internal const double UNIT_CONVERSION = 1000;
        internal const double WINDOW_WIDTH = 0.05;
        internal const double DOOR_WIDTH = 0.07;
        internal const double FITTING_RADIUS_RATIO = 1.5;
        internal const double PIPE_LENGTH_LIMIT = 0.01;
        static jsonStructure readJSONStructure()
        {
            string strReadFilePath = @".\Data\recon.json";
            StreamReader srReadFile = new StreamReader(strReadFilePath);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            jsonStructure jo = JsonSerializer.Deserialize<jsonStructure>(jsonText);
            return jo;
        }
        static jsonMEP readJSONMEP()
        {
            string strReadFilePath = @".\Data\pipe.json";
            StreamReader srReadFile = new StreamReader(strReadFilePath);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            jsonMEP jo = JsonSerializer.Deserialize<jsonMEP>(jsonText, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true,
            });
            return jo;
        }
        static void GenerateIFC(TypeIFC type, XbimSchemaVersion release, string path)
        {
            string error_msg = "";
            using (var ifcStore = createandInitModel("Model"))
            {
                if (ifcStore != null)
                {
                    IfcBuilding ifcBuilding = createBuilding(ifcStore, "Building");
                    IfcBuildingStorey ifcBuildingStorey = createStorey(ifcBuilding);
                    if (type == TypeIFC.Structure || type == TypeIFC.Model)
                    {
                        jsonStructure js = readJSONStructure();
                        List<IfcExtrudedAreaSolid> ifcColumnRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcBeamRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcWallRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcWindowRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcDoorRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcSlabRepresentations = new List<IfcExtrudedAreaSolid>();

                        List<IfcProduct> ifcProducts = new List<IfcProduct>();
                        using (var txn = ifcStore.BeginTransaction("Create Columns"))
                        {
                            //create columns
                            if (js.Column != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Concrete;
                                    });
                                });
                                foreach (jsonColumn jsonColumn in js.Column)
                                {
                                    double length = jsonColumn.length * 1000;
                                    double width = jsonColumn.width * 1000;
                                    double height = jsonColumn.height * 1000;
                                    jsonXYZ refDirJsonXYZ = new jsonXYZ(1, 0, 0).rotate(new jsonXYZ(0, 0, 0), new jsonXYZ(0, 0, 1), jsonColumn.RotationalAngleInRadius) * 1000;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonColumn.LocationPoint.x, jsonColumn.LocationPoint.y, jsonColumn.LocationPoint.z) * 1000;
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    //showcase appearance
                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                    {
                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                        {
                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                            {
                                                surfaceStyle.Name = "Concrete, Column";
                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                {
                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                    {
                                                        colorRGB.Red = 0.250980;
                                                        colorRGB.Green = 0.250980;
                                                        colorRGB.Blue = 0.250980;
                                                    });
                                                    surfaceStyleRendering.Transparency = 0;
                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                }));
                                            }));
                                        }));
                                    });
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
                                                shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = height;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                                    {
                                                        rectangleProfileDef.XDim = width;
                                                        rectangleProfileDef.YDim = length;
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
                                                            direction.SetXYZ(0, 0, 1);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
                                                        });
                                                    });
                                                    ifcColumnRepresentations.Add(extrudedAreaSolid);
                                                    ifcStyledItem.Item = extrudedAreaSolid;
                                                }));
                                            }));
                                        });
                                        column.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcColumn);
                                    ifcProducts.Add(ifcColumn);
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create Beams"))
                        {
                            //create beams
                            if (js.Beam != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Concrete;
                                    });
                                });
                                //show material
                                IfcPresentationLayerAssignment ifcPresentationLayerAssignment = ifcStore.Instances.New<IfcPresentationLayerAssignment>(presentationLayerAssignment =>
                                {
                                    presentationLayerAssignment.Name = "Presentation Layer Assignment";
                                });
                                foreach (jsonBeam jsonBeam in js.Beam)
                                {
                                    double length = jsonBeam.length * 1000;
                                    double width = jsonBeam.width * 1000;
                                    double thickness = jsonBeam.thickness * 1000;
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
                                                IfcBooleanOperand ifcBooleanOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = length;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                                    {
                                                        rectangleProfileDef.XDim = width;
                                                        rectangleProfileDef.YDim = thickness;
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
                                                    ifcBeamRepresentations.Add(extrudedAreaSolid);
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add((IfcRepresentationItem)ifcBooleanOperand);
                                                ifcPresentationLayerAssignment.AssignedItems.Add(shapeRepresentation);
                                            }));
                                        });
                                        beam.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcBeam);
                                    ifcProducts.Add(ifcBeam);
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create Window/Doors Representations"))
                        {
                            //create window/doors representations
                            if (js.Window != null) if (js.Door != null)
                                {
                                    foreach (jsonWindow jsonWindow in js.Window)
                                    {
                                        double length = jsonWindow.length * 1000;
                                        double width = jsonWindow.width * 1000;
                                        double height = jsonWindow.height * 1000;

                                        jsonXYZ refDirJsonXYZ = (jsonWindow.endPoint - jsonWindow.startPoint) * 1000;
                                        jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.x, jsonWindow.location.y, jsonWindow.location.z) * 1000;
                                        jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                                        //axis: extrude dir/Z dir; refDirection: width dir/X dir

                                        IfcExtrudedAreaSolid ifcExtrudedAreaSolid = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                        {
                                            extrudedAreaSolid.Depth = height;
                                            extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                            {
                                                rectangleProfileDef.XDim = length;
                                                rectangleProfileDef.YDim = width;
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
                                            ifcWindowRepresentations.Add(extrudedAreaSolid);
                                        });
                                    }
                                    foreach (jsonDoor jsonDoor in js.Door)
                                    {
                                        double length = jsonDoor.length * UNIT_CONVERSION;
                                        double width = jsonDoor.width * UNIT_CONVERSION;
                                        double height = jsonDoor.height * UNIT_CONVERSION;

                                        jsonXYZ refDirJsonXYZ = (jsonDoor.endPoint - jsonDoor.startPoint) * 1000;
                                        jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.x, jsonDoor.location.y, jsonDoor.location.z) * 1000;
                                        jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                                        //axis: extrude dir/Z dir; refDirection: width dir/X dir

                                        IfcExtrudedAreaSolid ifcExtrudedAreaSolid = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                        {
                                            extrudedAreaSolid.Depth = height;
                                            extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                            {
                                                rectangleProfileDef.XDim = length;
                                                rectangleProfileDef.YDim = width;
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
                                            ifcDoorRepresentations.Add(extrudedAreaSolid);
                                        });
                                    }
                                }
                            txn.Commit();
                        }

                        using (var txn = ifcStore.BeginTransaction("Create Walls"))
                        {
                            //create walls
                            if (js.Wall != null)
                            {
                                //create Wall-Ext_102Bwk-75Ins-100LBlk-12P
                                IfcWallType ifcWallType = ifcStore.Instances.New<IfcWallType>(wallType =>
                                {
                                    wallType.Name = "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P";
                                    wallType.HasPropertySets.AddRange(new IfcPropertySet[]
                                    {
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Analytical Properties";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Absorptance";
                                                    singleValue.NominalValue = new IfcReal(0.7);
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Heat Transfer Coefficient (U)";
                                                    singleValue.NominalValue = new IfcReal(0.235926059936681);
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Roughness";
                                                    singleValue.NominalValue = new IfcInteger(3);
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Thermal mass";
                                                    singleValue.NominalValue = new IfcReal(300303.0);
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Thermal Resistance (R)";
                                                    singleValue.NominalValue = new IfcReal(4.23861611671209);
                                                })
                                            });
                                        }),
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Construction";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Function";
                                                    singleValue.NominalValue = new IfcIdentifier("Exterior");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Width";
                                                    singleValue.NominalValue = new IfcLengthMeasure(290.0);//Testing Value
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Wrapping at Ends";
                                                    singleValue.NominalValue = new IfcIdentifier("None");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Wrapping at Inserts";
                                                    singleValue.NominalValue = new IfcIdentifier("Both");
                                                })
                                            });
                                        }),
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Graphics";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Coarse Scale Fill Color";
                                                    singleValue.NominalValue = new IfcInteger("12632256");//Testing Value
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Coarse Scale Fill Pattern";
                                                    singleValue.NominalValue = new IfcLabel("Solid fill");
                                                })
                                            });
                                        }),
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Identity Data";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Assembly Code";
                                                    singleValue.NominalValue = new IfcText("");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Assembly Description";
                                                    singleValue.NominalValue = new IfcText("");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Keynote";
                                                    singleValue.NominalValue = new IfcText("31120");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Type Name";
                                                    singleValue.NominalValue = new IfcText("Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                                })
                                            });
                                        }),
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Materials and Finishes";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Structural Material";
                                                    singleValue.NominalValue = new IfcLabel("Concrete Masonry Units _Low Density");
                                                })
                                            });
                                        }),
                                        ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                        {
                                            propertySet.Name = "Other";
                                            propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                            {
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Category";
                                                    singleValue.NominalValue = new IfcLabel("Walls");
                                                }),
                                                ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                                {
                                                    singleValue.Name = "Family Name";
                                                    singleValue.NominalValue = new IfcText("Basic Wall");
                                                })
                                            });
                                        })
                                    });
                                    wallType.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
                                    wallType.Tag = "654321";

                                });
                                IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(relDefinesByType =>
                                {
                                    relDefinesByType.RelatingType = ifcWallType;
                                });
                                //create standard properties for wall entities
                                IfcPropertySet otherPropertySet = ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                {
                                    propertySet.Name = "Other";
                                    propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                    {
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Category";
                                            singleValue.NominalValue = new IfcLabel("Walls");
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Family";
                                            singleValue.NominalValue = new IfcLabel("Basic Wall: Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Family and Type";
                                            singleValue.NominalValue = new IfcLabel("Basic Wall: Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Type";
                                            singleValue.NominalValue = new IfcLabel("Basic Wall: Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Type Id";
                                            singleValue.NominalValue = new IfcLabel("Basic Wall: Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                        })
                                    });
                                });
                                IfcPropertySet PsetPropertySet = ifcStore.Instances.New<IfcPropertySet>(propertySet =>
                                {
                                    propertySet.Name = "Pset_WallCommon";
                                    propertySet.HasProperties.AddRange(new IfcPropertySingleValue[]
                                    {
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "Reference";
                                            singleValue.NominalValue = new IfcIdentifier("Wall-Ext_102Bwk-75Ins-100LBlk-12P");
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "LoadBearing";
                                            singleValue.NominalValue = new IfcBoolean(false);
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "ExtendToStructure";
                                            singleValue.NominalValue = new IfcBoolean(true);
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "IsExternal";
                                            singleValue.NominalValue = new IfcBoolean(true);
                                        }),
                                        ifcStore.Instances.New<IfcPropertySingleValue>(singleValue =>
                                        {
                                            singleValue.Name = "ThermalTransmittance";
                                            singleValue.NominalValue = new IfcThermalTransmittanceMeasure(0.235926059936681);
                                        })
                                    });
                                });
                                foreach (jsonWall jsonWall in js.Wall)
                                {
                                    //create material
                                    IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                    {
                                        relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                        {
                                            material.Name = TypicalMaterial.Concrete;
                                        });
                                    });
                                    //showcase appearance
                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                    {
                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                        {
                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                            {
                                                surfaceStyle.Name = "Wall, Common";
                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                {
                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                    {
                                                        colorRGB.Red = 0.666666666666667;
                                                        colorRGB.Green = 0.392156862745098;
                                                        colorRGB.Blue = 0.411764705882353;
                                                    });
                                                    surfaceStyleRendering.Transparency = 0;
                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                }));
                                            }));
                                        }));
                                    });
                                    double length = jsonWall.length * UNIT_CONVERSION;
                                    double width = jsonWall.width * UNIT_CONVERSION;
                                    double height = jsonWall.height * UNIT_CONVERSION;
                                    jsonXYZ refDirJsonXYZ = new jsonXYZ((jsonWall.endPoint - jsonWall.startPoint).x, (jsonWall.endPoint - jsonWall.startPoint).y, (jsonWall.endPoint - jsonWall.startPoint).z) * UNIT_CONVERSION;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWall.location.x, jsonWall.location.y, jsonWall.location.z) * UNIT_CONVERSION;
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    IfcWall ifcWall = ifcStore.Instances.New<IfcWall>(wall =>
                                    {
                                        wall.Name = "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:" + jsonWall.id.ToString();
                                        wall.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "CSG";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                IfcBooleanOperand ifcBooleanOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = height;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                                    {
                                                        rectangleProfileDef.XDim = length;
                                                        rectangleProfileDef.YDim = width;
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
                                                            direction.SetXYZ(0, 0, 1);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.x, refDirJsonXYZ.y, refDirJsonXYZ.z);
                                                        });
                                                    });
                                                    ifcWallRepresentations.Add(extrudedAreaSolid);
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcWindowRepresentation in ifcWindowRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcWindowRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcDoorRepresentation in ifcDoorRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcDoorRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add((IfcRepresentationItem)ifcBooleanOperand);
                                                ifcStyledItem.Item = ifcBooleanOperand as IfcBooleanResult;
                                            }));
                                        });
                                        wall.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        wall.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
                                        wall.Tag = jsonWall.id.ToString();
                                        wall.ObjectType = "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:654321";
                                    });
                                    ifcRelDefinesByType.RelatedObjects.Add(ifcWall);
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcWall);
                                    ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                                    {
                                        relDefinesByProperties.RelatedObjects.Add(ifcWall);
                                        relDefinesByProperties.RelatingPropertyDefinition = otherPropertySet;
                                    });
                                    ifcStore.Instances.New<IfcRelDefinesByProperties>(relDefinesByProperties =>
                                    {
                                        relDefinesByProperties.RelatedObjects.Add(ifcWall);
                                        relDefinesByProperties.RelatingPropertyDefinition = PsetPropertySet;
                                    });
                                    ifcProducts.Add(ifcWall);
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create Windows/Doors"))
                        {
                            if (ifcWindowRepresentations != null)
                            {
                                IfcMaterialLayerSetUsage ifcMaterialLayerSetUsage = ifcStore.Instances.New<IfcMaterialLayerSetUsage>(materialLayerSetUsage =>
                                {
                                    materialLayerSetUsage.ForLayerSet = ifcStore.Instances.New<IfcMaterialLayerSet>(materialLayerSet =>
                                    {
                                        materialLayerSet.MaterialLayers.Add(ifcStore.Instances.New<IfcMaterialLayer>(materialLayer =>
                                        {
                                            materialLayer.LayerThickness = 470;
                                            materialLayer.Material = ifcStore.Instances.New<IfcMaterial>(material =>
                                            {
                                                material.Name = TypicalMaterial.Air;
                                            });
                                        }));
                                    });
                                    materialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS3;
                                    materialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
                                    materialLayerSetUsage.OffsetFromReferenceLine = 0;
                                });
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Glass;
                                    });
                                });
                                //create properties

                                foreach (jsonWindow jsonWindow in js.Window)
                                {
                                    double length = jsonWindow.length * 1000;
                                    double width = WINDOW_WIDTH * 1000;
                                    double height = jsonWindow.height * 1000;

                                    jsonXYZ refDirJsonXYZ = (jsonWindow.endPoint - jsonWindow.startPoint) * 1000;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.x, jsonWindow.location.y, jsonWindow.location.z) * 1000;
                                    jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    //showcase appearance
                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                    {
                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                        {
                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                            {
                                                surfaceStyle.Name = "Glass, Column";
                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                {
                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                    {
                                                        colorRGB.Red = 0.137255;
                                                        colorRGB.Green = 0.403922;
                                                        colorRGB.Blue = 0.870588;
                                                    });
                                                    surfaceStyleRendering.Transparency = 0.4;
                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                }));
                                            }));
                                        }));
                                    });

                                    IfcWindow ifcWindow = ifcStore.Instances.New<IfcWindow>(window =>
                                    {
                                        window.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "CSG";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                IfcBooleanOperand ifcBooleanOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = height;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                                    {
                                                        rectangleProfileDef.XDim = length;
                                                        rectangleProfileDef.YDim = width;
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
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add((IfcRepresentationItem)ifcBooleanOperand);
                                                ifcStyledItem.Item = ifcBooleanOperand as IfcBooleanResult;
                                            }));
                                        });
                                        window.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        window.PredefinedType = IfcWindowTypeEnum.NOTDEFINED;
                                    });
                                    IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(p =>
                                    {
                                        p.RelatedObjects.Add(ifcWindow);
                                        p.RelatingType = ifcStore.Instances.New<IfcWindowType>(windowType =>
                                        {
                                            windowType.ParameterTakesPrecedence = new IfcBoolean(true);
                                            windowType.PartitioningType = IfcWindowTypePartitioningEnum.SINGLE_PANEL;
                                        });
                                    });
                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Glass;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcWindow);

                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;
                                    ifcProducts.Add(ifcWindow);
                                }
                            }
                            if (ifcDoorRepresentations != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Wood;
                                    });
                                });

                                foreach (jsonDoor jsonDoor in js.Door)
                                {
                                    double length = jsonDoor.length * 1000;
                                    double width = DOOR_WIDTH * 1000;
                                    double height = jsonDoor.height * 1000;

                                    jsonXYZ refDirJsonXYZ = (jsonDoor.endPoint - jsonDoor.startPoint) * 1000;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.x, jsonDoor.location.y, jsonDoor.location.z) * 1000;
                                    jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    //showcase appearance
                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                    {
                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                        {
                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                            {
                                                surfaceStyle.Name = "Wood, Common";
                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                {
                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                    {
                                                        colorRGB.Red = 0.637255;
                                                        colorRGB.Green = 0.603922;
                                                        colorRGB.Blue = 0.670588;
                                                    });
                                                    surfaceStyleRendering.Transparency = 0;
                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                }));
                                            }));
                                        }));
                                    });

                                    IfcDoor ifcDoor = ifcStore.Instances.New<IfcDoor>(door =>
                                    {
                                        door.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "CSG";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                IfcBooleanOperand ifcBooleanOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = height;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcRectangleProfileDef>(rectangleProfileDef =>
                                                    {
                                                        rectangleProfileDef.XDim = length;
                                                        rectangleProfileDef.YDim = width;
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
                                                //foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                //{
                                                //    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                //    {
                                                //        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                //        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                //        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                //    });
                                                //}
                                                //foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                //{
                                                //    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                //    {
                                                //        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                //        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                //        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                //    });
                                                //}
                                                shapeRepresentation.Items.Add((IfcRepresentationItem)ifcBooleanOperand);
                                                ifcStyledItem.Item = ifcBooleanOperand as IfcBooleanResult;
                                            }));
                                        });
                                        door.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        door.PredefinedType = IfcDoorTypeEnum.NOTDEFINED;
                                    });
                                    IfcRelDefinesByType ifcRelDefinesByType = ifcStore.Instances.New<IfcRelDefinesByType>(p =>
                                    {
                                        p.RelatedObjects.Add(ifcDoor);
                                        p.RelatingType = ifcStore.Instances.New<IfcDoorType>(doorType =>
                                        {
                                            doorType.OperationType = IfcDoorTypeOperationEnum.SINGLE_SWING_RIGHT;
                                            doorType.ParameterTakesPrecedence = new IfcBoolean(true);
                                        });
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcDoor);
                                    ifcProducts.Add(ifcDoor);
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create Slabs"))
                        {
                            if (js.Slab != null)
                            {
                                IfcMaterialLayerSetUsage ifcMaterialLayerSetUsage = ifcStore.Instances.New<IfcMaterialLayerSetUsage>(materialLayerSetUsage =>
                                {
                                    materialLayerSetUsage.ForLayerSet = ifcStore.Instances.New<IfcMaterialLayerSet>(materialLayerSet =>
                                    {
                                        materialLayerSet.MaterialLayers.Add(ifcStore.Instances.New<IfcMaterialLayer>(materialLayer =>
                                        {
                                            materialLayer.LayerThickness = 470;
                                            materialLayer.Material = ifcStore.Instances.New<IfcMaterial>(material =>
                                            {
                                                material.Name = TypicalMaterial.Concrete;
                                            });
                                        }));
                                    });
                                    materialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS3;
                                    materialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
                                    materialLayerSetUsage.OffsetFromReferenceLine = 0;
                                });
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Concrete;
                                    });
                                });
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
                                        IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                        {
                                            styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                            {
                                                presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                                {
                                                    surfaceStyle.Name = "Glass, Column";
                                                    surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                    surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                    {
                                                        surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                        {
                                                            colorRGB.Red = 0.837255;
                                                            colorRGB.Green = 0.603922;
                                                            colorRGB.Blue = 0.670588;
                                                        });
                                                        surfaceStyleRendering.Transparency = 0.4;
                                                        surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                        surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                        surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                    }));
                                                }));
                                            }));
                                        });
                                        slab.Name = "";
                                        slab.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "SweptSolid";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                //ceilling
                                                shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = thickness;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcArbitraryClosedProfileDef>(arbitraryClosedProfileDef =>
                                                    {
                                                        arbitraryClosedProfileDef.OuterCurve = ifcStore.Instances.New<IfcPolyline>(polyline =>
                                                        {
                                                            List<IfcCartesianPoint> points = new List<IfcCartesianPoint>();
                                                            foreach (jsonXYZ jsonXYZ in jsonSlab.baseProfile)
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
                                                    ifcSlabRepresentations.Add(extrudedAreaSolid);
                                                    ifcStyledItem.Item = extrudedAreaSolid;
                                                }));
                                            }));
                                        });
                                        slab.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });//ceiling
                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Concrete;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcSlab1);

                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;
                                    ifcProducts.Add(ifcSlab1);

                                    IfcSlab ifcSlab2 = ifcStore.Instances.New<IfcSlab>(slab =>
                                    {
                                        IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                        {
                                            styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                            {
                                                presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                                {
                                                    surfaceStyle.Name = "Glass, Column";
                                                    surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                    surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                    {
                                                        surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                        {
                                                            colorRGB.Red = 0.837255;
                                                            colorRGB.Green = 0.603922;
                                                            colorRGB.Blue = 0.670588;
                                                        });
                                                        surfaceStyleRendering.Transparency = 0.4;
                                                        surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                        surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                        surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                    }));
                                                }));
                                            }));
                                        });
                                        slab.Name = "";
                                        slab.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "SweptSolid";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                //floor
                                                shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = thickness;
                                                    extrudedAreaSolid.SweptArea = ifcStore.Instances.New<IfcArbitraryClosedProfileDef>(arbitraryClosedProfileDef =>
                                                    {
                                                        arbitraryClosedProfileDef.OuterCurve = ifcStore.Instances.New<IfcPolyline>(polyline =>
                                                        {
                                                            List<IfcCartesianPoint> points = new List<IfcCartesianPoint>();
                                                            foreach (jsonXYZ jsonXYZ in jsonSlab.baseProfile)
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
                                                    extrudedAreaSolid.ExtrudedDirection = ifcStore.Instances.New<IfcDirection>(direction => direction.SetXYZ(0, 0, -1));//floor
                                                    extrudedAreaSolid.Position = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
                                                    {
                                                        axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                                                        {
                                                            cartesianPoint.SetXYZ(locationJsonXYZ.x, locationJsonXYZ.y, locationJsonXYZ.z + (js.Wall[0].height + jsonSlab.thickness * 2) * UNIT_CONVERSION);
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
                                                    ifcSlabRepresentations.Add(extrudedAreaSolid);
                                                    ifcStyledItem.Item = extrudedAreaSolid;
                                                }));
                                            }));
                                        });
                                        slab.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });//floor
                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.Concrete;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcSlab2);

                                    ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;
                                    ifcProducts.Add(ifcSlab2);
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction(""))
                        {
                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
                        if (type == TypeIFC.Structure) ifcStore.SaveAs(Path.Combine(path, type.ToString() + " " + release.ToString() + ".ifc"), StorageType.Ifc);
                    }
                    if (type == TypeIFC.MEP || type == TypeIFC.Model)
                    {
                        jsonMEP jmep = readJSONMEP();
                        List<IfcExtrudedAreaSolid> ifcPipeRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcRevolvedAreaSolid> ifcEllbowRepresentations = new List<IfcRevolvedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcTFittingRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<jsonPipe> pipes = new List<jsonPipe>();
                        if (jmep.pipe != null)
                        {
                            pipes.AddRange(jmep.pipe);
                        }
                        if (jmep.Elbow_Pipe_Junction != null)
                        {
                            jsonPipe pipe1 = null, pipe2 = null;
                            jsonXYZ center = null, shifting_dir, p1_dir, p2_dir;
                            List<jsonXYZ> pipe1Points = null, pipe2Points = null;
                            double average_radius, angle, shifting;
                            foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
                            {
                                pipe1 = pipes.Find(x => x.ID.Equals(jsonFitting.Pipe_Index_1));
                                pipe2 = pipes.Find(x => x.ID.Equals(jsonFitting.Pipe_Index_2));
                                if (pipe1 != null && pipe2 != null && pipe1.length != 0 && pipe2.length != 0)
                                {

                                    pipe1Points = new List<jsonXYZ>() { pipe1.Startpoint, pipe1.Endpoint };
                                    pipe2Points = new List<jsonXYZ>() { pipe2.Startpoint, pipe2.Endpoint };
                                    double minDis = Double.MaxValue;
                                    foreach (jsonXYZ pt1 in pipe1Points)
                                    {
                                        foreach (jsonXYZ pt2 in pipe2Points)
                                        {
                                            if (minDis > pt1.distanceTo(pt2))
                                            {
                                                minDis = pt1.distanceTo(pt2);
                                                pipe1.Startpoint = pt1;
                                                pipe2.Startpoint = pt2;
                                            }
                                        }
                                    }
                                    pipe1.Endpoint = pipe1Points.Find(x => !x.Equals(pipe1.Startpoint));
                                    pipe2.Endpoint = pipe2Points.Find(x => !x.Equals(pipe2.Startpoint));

                                    p1_dir = pipe1.direction;
                                    p2_dir = pipe2.direction;

                                    shifting_dir = (p1_dir + p2_dir).normalized();
                                    average_radius = (pipe1.Radius + pipe2.Radius) / 2;
                                    center = (pipe1.Startpoint + pipe2.Startpoint) / 2;
                                    angle = pipe1.direction.angleTo(pipe2.direction);
                                    shifting = Math.Min(Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), pipe1.length * (1 - PIPE_LENGTH_LIMIT)), average_radius * FITTING_RADIUS_RATIO);
                                    pipe1.Startpoint += pipe1.direction * shifting;
                                    pipe2.Startpoint += pipe2.direction * shifting;

                                    center += pipe1.Startpoint.distanceTo(center) / Math.Sin(Math.PI / 2 - angle / 2) * shifting_dir;


                                    jsonFitting.angle = Math.PI - angle;
                                    jsonFitting.radius = average_radius;

                                    jsonFitting.center = center;
                                    jsonFitting.axis = p1_dir.crossProduct(p2_dir).normalized();

                                    jsonFitting.refAxis = -p1_dir;
                                    jsonFitting.location = pipe1.Startpoint;
                                    jsonFitting.isValid = true;
                                }
                                else if (pipe1 == null || pipe2 == null) error_msg += "ERROR: Creating Ellbow but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonFitting.Pipe_Index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonFitting.Pipe_Index_2.ToString()) : "") + "\n";
                                else if (pipe1.length == 0 && pipe2.length == 0) error_msg += "ERROR: Creating Ellbow but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonFitting.Pipe_Index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonFitting.Pipe_Index_2.ToString()) : "") + "\n";
                            }
                        }
                        if (jmep.T_Pipe_Junction != null)
                        {
                            jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
                            List<jsonXYZ> pipe1Points = null, pipe2Points = null, pipe3Points = null;
                            jsonXYZ p1_dir = null, p2_dir = null, p3_dir = null, center = null;
                            double shifting = Double.MaxValue, average_radius;
                            foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
                            {
                                pipe1 = pipes.Find(x => x.ID.Equals(jsonTee.Pipe_Index_1));
                                pipe2 = pipes.Find(x => x.ID.Equals(jsonTee.Pipe_Index_2));
                                pipe3 = pipes.Find(x => x.ID.Equals(jsonTee.Pipe_Index_3));
                                if (pipe1 != null && pipe2 != null && pipe3 != null && pipe1.length != 0 && pipe2.length != 0 && pipe3.length != 0)
                                {
                                    if(pipe1.ID != pipe2.ID)
                                    {
                                        pipe1Points = new List<jsonXYZ>() { pipe1.Startpoint, pipe1.Endpoint };
                                        pipe2Points = new List<jsonXYZ>() { pipe2.Startpoint, pipe2.Endpoint };
                                        pipe3Points = new List<jsonXYZ>() { pipe3.Startpoint, pipe3.Endpoint };
                                        double minDis = Double.MaxValue;
                                        foreach (jsonXYZ pt1 in pipe1Points)
                                        {
                                            foreach (jsonXYZ pt2 in pipe2Points)
                                            {
                                                if (minDis > pt1.distanceTo(pt2))
                                                {
                                                    minDis = pt1.distanceTo(pt2);
                                                    pipe1.Startpoint = pt1;
                                                    pipe2.Startpoint = pt2;
                                                }
                                            }
                                        }
                                        pipe1.Endpoint = pipe1Points.Find(x => !x.Equals(pipe1.Startpoint));
                                        pipe2.Endpoint = pipe2Points.Find(x => !x.Equals(pipe2.Startpoint));
                                        minDis = Double.MaxValue;
                                        foreach (jsonXYZ pt3 in pipe3Points)
                                        {
                                            if (minDis > pt3.distanceTo((pipe1.Startpoint + pipe2.Startpoint) / 2))
                                            {
                                                minDis = pt3.distanceTo((pipe1.Startpoint + pipe2.Startpoint) / 2);
                                                pipe3.Startpoint = pt3;
                                            }
                                        }
                                        pipe3.Endpoint = pipe3Points.Find(x => !x.Equals(pipe3.Startpoint));
                                        p1_dir = pipe1.direction;
                                        p2_dir = pipe2.direction;
                                        p3_dir = pipe3.direction;

                                        average_radius = (pipe1.Radius + pipe2.Radius + pipe3.Radius) / 3;

                                        center = (pipe1.Startpoint + pipe2.Startpoint) / 2;

                                        shifting = Math.Min(pipe1.length * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
                                        shifting = Math.Min(pipe2.length * (1 - PIPE_LENGTH_LIMIT), shifting);
                                        shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

                                        pipe1.Startpoint += pipe1.direction * shifting;
                                        pipe2.Startpoint += pipe2.direction * shifting;
                                        pipe3.Startpoint += pipe3.direction * shifting;

                                        jsonTee.center = center;

                                        jsonTee.Pt1 = pipe1.Startpoint;
                                        jsonTee.Pt2 = pipe2.Startpoint;
                                        jsonTee.Pt3 = pipe3.Startpoint;
                                        jsonTee.isValid = true;
                                    }
                                    else if(jsonTee.center != null)
                                    {
                                        pipe1Points = new List<jsonXYZ>() { pipe1.Startpoint, pipe1.Endpoint };
                                        pipe2Points = new List<jsonXYZ>() { pipe2.Startpoint, pipe2.Endpoint };
                                        pipe3Points = new List<jsonXYZ>() { pipe3.Startpoint, pipe3.Endpoint };
                                        double minDis = Double.MaxValue;
                                        foreach (jsonXYZ pt3 in pipe3Points)
                                        {
                                            if (minDis > pt3.distanceTo(jsonTee.center))
                                            {
                                                minDis = pt3.distanceTo(jsonTee.center);
                                                pipe3.Startpoint = pt3;
                                            }
                                        }
                                        pipe3.Endpoint = pipe3Points.Find(x => !x.Equals(pipe3.Startpoint));
                                        p1_dir = pipe1.direction;
                                        p2_dir = pipe2.direction;
                                        p3_dir = pipe3.direction;

                                        average_radius = (pipe1.Radius + pipe2.Radius + pipe3.Radius) / 3;

                                        shifting = Math.Min(pipe1.Startpoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), average_radius * FITTING_RADIUS_RATIO);
                                        shifting = Math.Min(pipe1.Endpoint.distanceTo(jsonTee.center) * (1 - PIPE_LENGTH_LIMIT), shifting);
                                        shifting = Math.Min(pipe3.length * (1 - PIPE_LENGTH_LIMIT), shifting);

                                        pipe3.Startpoint += pipe3.direction * shifting;
                                        jsonTee.Pt1 = jsonTee.center + p1_dir * shifting;
                                        jsonTee.Pt2 = jsonTee.center - p1_dir * shifting;

                                        jsonTee.Pt3 = pipe3.Startpoint;
                                        jsonTee.isValid = true;
                                    }
                                }
                                else if (pipe1 == null || pipe2 == null || pipe3 == null) error_msg += "ERROR: Creating Tee but cannot find pipe, index: " + (pipe1 == null ? ("#" + jsonTee.Pipe_Index_1.ToString()) : "") + (pipe1 == null && pipe2 == null ? " & " : "") + (pipe2 == null ? ("#" + jsonTee.Pipe_Index_2.ToString()) : "") + (((pipe1 == null || pipe2 == null) && pipe3 == null) ? " & " : "") + (pipe3 == null ? ("#" + jsonTee.Pipe_Index_3.ToString()) : "") + "\n";
                                else if (pipe1.length == 0 || pipe2.length == 0 || pipe3.length == 0) error_msg += "ERROR: Creating Tee but the pipe(s) is/are too short, index: " + (pipe1.length == 0 ? ("#" + jsonTee.Pipe_Index_1.ToString()) : "") + (pipe1.length == 0 && pipe2.length == 0 ? " & " : "") + (pipe2.length == 0 ? ("#" + jsonTee.Pipe_Index_2.ToString()) : "") + (((pipe1.length == 0 || pipe2.length == 0) && pipe3.length == 0) ? " & " : "") + (pipe3.length == 0 ? ("#" + jsonTee.Pipe_Index_3.ToString()) : "") + "\n";
                            }
                        }
                        List<IfcProduct> ifcProducts = new List<IfcProduct>();
                        Console.WriteLine("Creating Pipes");
                        using (var txn = ifcStore.BeginTransaction("Create Pipes"))
                        {
                            //create pipes
                            if (jmep.pipe != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.PVC;
                                    });
                                });
                                foreach (jsonPipe jsonPipe in jmep.pipe)
                                {
                                    if (jsonPipe.length == 0)
                                    {
                                        error_msg += "ERROR: Creating Pipe but the pipe is too short: #" + jsonPipe.ID.ToString() + "\n";
                                        continue;
                                    }

                                    double length = jsonPipe.length * UNIT_CONVERSION;
                                    double radius = jsonPipe.Radius * UNIT_CONVERSION;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonPipe.Startpoint.x, jsonPipe.Startpoint.y, jsonPipe.Startpoint.z) * UNIT_CONVERSION;
                                    jsonXYZ axisJsonXYZ = new jsonXYZ((jsonPipe.Endpoint - jsonPipe.Startpoint).x, (jsonPipe.Endpoint - jsonPipe.Startpoint).y, (jsonPipe.Endpoint - jsonPipe.Startpoint).z) * UNIT_CONVERSION;
                                    jsonXYZ refDirJsonXYZ = !(axisJsonXYZ.x == 0 && axisJsonXYZ.y == 0) ? (new jsonXYZ(axisJsonXYZ.y, -axisJsonXYZ.x, 0) * UNIT_CONVERSION) : (new jsonXYZ(axisJsonXYZ.z, 0, -axisJsonXYZ.x) * UNIT_CONVERSION);
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    Console.WriteLine(refDirJsonXYZ.ToString());
                                    if (refDirJsonXYZ.distanceTo(jsonXYZ.Zero) == 0) Console.ReadLine();
                                    //showcase appearance
                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                    {
                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                        {
                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                            {
                                                surfaceStyle.Name = "PVC, common";
                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                {
                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                    {
                                                        colorRGB.Red = 0.1;
                                                        colorRGB.Green = 0.1;
                                                        colorRGB.Blue = 0.1;
                                                    });
                                                    surfaceStyleRendering.Transparency = 0;
                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                }));
                                            }));
                                        }));
                                    });
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
                                                shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
                                                {
                                                    extrudedAreaSolid.Depth = length;
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
                                                    ifcPipeRepresentations.Add(extrudedAreaSolid);
                                                    ifcStyledItem.Item = extrudedAreaSolid;
                                                }));
                                            }));
                                        });
                                        flowSegment.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcFlowSegment);
                                    ifcProducts.Add((IfcProduct)ifcFlowSegment);
                                }
                            }
                            txn.Commit();
                        }
                        Console.WriteLine("Creating Elbows");
                        using (var txn = ifcStore.BeginTransaction("Create Pipe Ellbows"))
                        {
                            //create pipe ellbows
                            jsonPipe pipe1 = null, pipe2 = null;
                            if (jmep.Elbow_Pipe_Junction != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.PVC;
                                    });
                                });
                                foreach (jsonFitting jsonFitting in jmep.Elbow_Pipe_Junction)
                                {
                                    pipe1 = pipes.Find(x => x.ID == jsonFitting.Pipe_Index_1);
                                    pipe2 = pipes.Find(x => x.ID == jsonFitting.Pipe_Index_2);
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
                                            error_msg += "WARNING: Creating Ellbow but the pipe is too short for revolving\n";
                                            continue;
                                        }
                                        //axis: Z dir; refDirection: X dir
                                        IIfcFlowFitting ifcFlowFitting = new Create(ifcStore).FlowFitting(flowFitting =>
                                        {
                                            flowFitting.Name = "Ellbow: " + jsonFitting.Pipe_Index_1.ToString() + " & " + jsonFitting.Pipe_Index_2.ToString();
                                            flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                            {
                                                productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                                {   
                                                    shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                    shapeRepresentation.RepresentationIdentifier = "SweptSolid";
                                                    shapeRepresentation.RepresentationIdentifier = "Body";
                                                    //showcase appearance
                                                    IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                                    {
                                                        styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                                        {
                                                            presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                                            {
                                                                surfaceStyle.Name = "PVC, common";
                                                                surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                                surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                                {
                                                                    surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                                    {
                                                                        colorRGB.Red = 0.5;
                                                                        colorRGB.Green = 0.5;
                                                                        colorRGB.Blue = 0.5;
                                                                    });
                                                                    surfaceStyleRendering.Transparency = 0;
                                                                    surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                                    surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                                    surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                                }));
                                                            }));
                                                        }));
                                                    });
                                                    shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcRevolvedAreaSolid>(revolvedAreaSolid =>
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
                                                        ifcEllbowRepresentations.Add(revolvedAreaSolid);
                                                        ifcStyledItem.Item = revolvedAreaSolid;
                                                    }));
                                                }));
                                            });
                                            flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        });
                                        ifcRelAssociatesMaterial.RelatedObjects.Add(ifcFlowFitting);
                                        ifcProducts.Add((IfcProduct)ifcFlowFitting);
                                    }
                                }
                            }
                            txn.Commit();
                        }
                        Console.WriteLine("Creating Tees");
                        using (var txn = ifcStore.BeginTransaction("Create Pipe Tees"))
                        {
                            //create pipe Tees
                            jsonPipe pipe1 = null, pipe2 = null, pipe3 = null;
                            if (jmep.T_Pipe_Junction != null)
                            {
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = TypicalMaterial.PVC;
                                    });
                                });
                                foreach (jsonTee jsonTee in jmep.T_Pipe_Junction)
                                {
                                    pipe1 = pipes.Find(x => x.ID == jsonTee.Pipe_Index_1);
                                    pipe2 = pipes.Find(x => x.ID == jsonTee.Pipe_Index_2);
                                    pipe3 = pipes.Find(x => x.ID == jsonTee.Pipe_Index_3);
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
                                            flowFitting.Name = "Tee: " + jsonTee.Pipe_Index_1.ToString() + " , " + jsonTee.Pipe_Index_2.ToString() + " & " + jsonTee.Pipe_Index_3.ToString();
                                            flowFitting.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                            {
                                                productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                                {
                                                    shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                    shapeRepresentation.RepresentationIdentifier = "SweptSolid";
                                                    shapeRepresentation.RepresentationIdentifier = "Body";
                                                    var zip = pts.Zip(connectedPipes, (pt, pipe) => new { pt, pipe });
                                                    foreach (var z in zip)
                                                    {

                                                        //showcase appearance
                                                        IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
                                                        {
                                                            styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                                                            {
                                                                presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                                                                {
                                                                    surfaceStyle.Name = "PVC, common";
                                                                    surfaceStyle.Side = IfcSurfaceSide.BOTH;
                                                                    surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                                                                    {
                                                                        surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                                                                        {
                                                                            colorRGB.Red = 0.5;
                                                                            colorRGB.Green = 0.5;
                                                                            colorRGB.Blue = 0.5;
                                                                        });
                                                                        surfaceStyleRendering.Transparency = 0;
                                                                        surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                                                                        surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                                                                        surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                                                                    }));
                                                                }));
                                                            }));
                                                        });
                                                        radius = z.pipe.Radius * UNIT_CONVERSION * 1.1;
                                                        depth = z.pt.distanceTo(jsonTee.center) * UNIT_CONVERSION;
                                                        locationJsonXYZ = z.pt * UNIT_CONVERSION;
                                                        axisJsonXYZ = (jsonTee.center - z.pt) * UNIT_CONVERSION;
                                                        refDirJsonXYZ = !(axisJsonXYZ.x == 0 && axisJsonXYZ.y == 0) ? (new jsonXYZ(axisJsonXYZ.y, -axisJsonXYZ.x, 0) * UNIT_CONVERSION) : (new jsonXYZ(axisJsonXYZ.z, 0, axisJsonXYZ.x) * UNIT_CONVERSION);
                                                        shapeRepresentation.Items.Add(ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
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
                                                            ifcTFittingRepresentations.Add(extrudedAreaSolid);
                                                            ifcStyledItem.Item = extrudedAreaSolid;
                                                        }));
                                                    }
                                                }));
                                            });
                                            flowFitting.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        });
                                        ifcRelAssociatesMaterial.RelatedObjects.Add(ifcFlowFitting);
                                        ifcProducts.Add((IfcProduct)ifcFlowFitting);
                                    }
                                }
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction(""))
                        {
                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
                        if(error_msg != "") WriteError(error_msg, path);
                        ifcStore.SaveAs(Path.Combine(path, type.ToString() + " " + release.ToString().ToUpper() + ".ifc"), StorageType.Ifc);
                    }

                }
            }
        }
        private static IfcStore createandInitModel(string projectName)
        {
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "SJ-NTU Corporate Lab",
                ApplicationFullName = "JSON to IFC",
                ApplicationIdentifier = "JSON2IFC.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "SJ-NTU Corporate Lab Team",
                EditorsGivenName = "SJ-NTU Corporate Lab",
                EditorsOrganisationName = "SJ-NTU Corporate Lab"
            };
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                var project = model.Instances.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                txn.Commit();
            }
            return model;
        }
        private static IfcBuilding createBuilding(IfcStore ifcStore, string name)
        {
            using (var txn = ifcStore.BeginTransaction("Create Building"))
            {
                var ifcBuilding = ifcStore.Instances.New<IfcBuilding>(building =>
                {
                    building.Name = name;
                    building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    building.ObjectPlacement = ifcStore.Instances.New<IfcLocalPlacement>(localPlacement =>
                    {
                        localPlacement.RelativePlacement = ifcStore.Instances.New<IfcAxis2Placement3D>(axis2Placement3D =>
                        {
                            axis2Placement3D.Location = ifcStore.Instances.New<IfcCartesianPoint>(cartesianPoint =>
                            {
                                cartesianPoint.SetXYZ(0, 0, 0);
                            });
                        });
                    });
                });

                var ifcProject = ifcStore.Instances.OfType<IfcProject>().FirstOrDefault();
                if (ifcProject != null) ifcProject.AddBuilding(ifcBuilding);
                txn.Commit();
                return ifcBuilding;
            }
        }
        private static IfcBuildingStorey createStorey(IfcBuilding building)
        {
            var model = building.Model;
            IfcBuildingStorey storey;
            using (var txn = model.BeginTransaction("Storey creation"))
            {
                storey = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = "Default storey";
                    s.Elevation = 0.0;
                });
                txn.Commit();
            }
            return storey;
        }
        public static void WriteError(string s, string path)
        {
            FileStream fs = new FileStream(Path.Combine(path, "Error.txt"), FileMode.Create);
            byte[] data = System.Text.Encoding.Default.GetBytes(s);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
        //Structure
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
            public double thickness { get; set; }
            public double width
            {
                get { return Math.Min(this.baseProfile[0].distanceTo(this.baseProfile[1]), this.baseProfile[0].distanceTo(this.baseProfile[3])); }
                set { }
            }
            public int ID { get; set; }
            public jsonXYZ startPoint
            {
                get
                {
                    return (this.baseProfile[1].distanceTo(this.baseProfile[0]) == this.width ? (this.baseProfile[0] + this.baseProfile[1]) / 2 : (this.baseProfile[1] + this.baseProfile[2]) / 2) + new jsonXYZ(0, 0, this.heightOfBottomFace + this.thickness / 2);
                }
                set { }
            }
            public jsonXYZ endPoint
            {
                get { return (this.baseProfile[3].distanceTo(this.baseProfile[0]) == this.width ? (this.baseProfile[0] + this.baseProfile[3]) / 2 : (this.baseProfile[3] + this.baseProfile[2]) / 2) + new jsonXYZ(0, 0, this.heightOfBottomFace + this.thickness / 2); }
                set { }
            }
            public double length { get { return this.startPoint.distanceTo(this.endPoint); } set { } }
            public jsonHole[] HoleList { get; set; }
        }
        public class jsonColumn
        {
            public double height { get; set; }
            public jsonXYZ LocationPoint { get; set; }
            public double RotationalAngleInRadius { get; set; }
            public double length { get; set; }
            public double width { get; set; }
            public int ID { get; set; }
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
            public jsonXYZ() {
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
            public double distanceTo2Points(jsonXYZ p1, jsonXYZ p2)
            {
                double dis = Math.Sqrt(Math.Pow(distanceTo(p1), 2) - Math.Pow((p2 - p1).dotProduct(this - p1) / (p2.distanceTo(p1)), 2));
                return dis;
            }
            public jsonXYZ normalized()
            {
                if (Math.Sqrt(this.dotProduct(this)) == 0)
                {
                    Exception e =  new Exception("Invalid Vector for Nomalization: length == 0");
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
        }
        public class jsonPipe
        {
            public int ID { get; set; }
            public double Radius { get; set; }
            public jsonXYZ Startpoint { get; set; }
            public jsonXYZ Endpoint { get; set; }
            public double length { get { return this.Startpoint.distanceTo(this.Endpoint); } set { } }
            public jsonXYZ direction { get { return (this.Endpoint - this.Startpoint).normalized(); } }
        }
        public class jsonFitting
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
        public class jsonTee
        {
            public int Pipe_Index_1 { get; set; }
            public int Pipe_Index_2 { get; set; }
            public int Pipe_Index_3 { get; set; }
            public jsonXYZ center { get; set; }
            public jsonXYZ Pt1 { get; set; }
            public jsonXYZ Pt2 { get; set; }
            public jsonXYZ Pt3 { get; set; }
            public bool isValid { get; set; }
        }
    }
}
