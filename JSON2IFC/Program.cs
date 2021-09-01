using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.PresentationAppearanceResource;


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
		internal enum TypeIFC { Structure, MEP, Model};
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
        static jsonStructure readJSONStructure()
		{
			string strReadFilePath = @".\Data\recon.json";
			StreamReader srReadFile = new StreamReader(strReadFilePath);
			string jsonText = "";
			while (!srReadFile.EndOfStream)
			{
				jsonText += srReadFile.ReadLine();
			}
			jsonStructure jo = JsonConvert.DeserializeObject<jsonStructure>(jsonText);
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
            jsonMEP jo = JsonConvert.DeserializeObject<jsonMEP>(jsonText);
            return jo;
        }
        static void GenerateIFC(TypeIFC type, XbimSchemaVersion release, string path)
		{
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
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonColumn.LocationPoint.X, jsonColumn.LocationPoint.Y, jsonColumn.LocationPoint.Z) * 1000;
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
                                                            cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(0, 0, 1);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                                    jsonXYZ axisJsonXYZ = new jsonXYZ((jsonBeam.endPoint - jsonBeam.startPoint).X, (jsonBeam.endPoint - jsonBeam.startPoint).Y, (jsonBeam.endPoint - jsonBeam.startPoint).Z) * 1000;
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
                                                IfcBooleanResult ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(initialBooleanResult =>
                                                {
                                                    initialBooleanResult.Operator = IfcBooleanOperator.INTERSECTION;
                                                    initialBooleanResult.FirstOperand = initialBooleanResult.SecondOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
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
                                                                cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                            });
                                                            axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                            {
                                                                direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                            });
                                                            axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                            {
                                                                direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
                                                            });
                                                        });
                                                        ifcBeamRepresentations.Add(extrudedAreaSolid);
                                                    });
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add(ifcBooleanResult);
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
                                        jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.X, jsonWindow.location.Y, jsonWindow.location.Z) * 1000;
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
                                                    cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                });
                                                axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                {
                                                    direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                });
                                                axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                {
                                                    direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                                        jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.X, jsonDoor.location.Y, jsonDoor.location.Z) * 1000;
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
                                                    cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                });
                                                axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                {
                                                    direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                });
                                                axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                {
                                                    direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                                    jsonXYZ refDirJsonXYZ = new jsonXYZ((jsonWall.endPoint - jsonWall.startPoint).X, (jsonWall.endPoint - jsonWall.startPoint).Y, (jsonWall.endPoint - jsonWall.startPoint).Z) * UNIT_CONVERSION;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWall.location.X, jsonWall.location.Y, jsonWall.location.Z) * UNIT_CONVERSION;

                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
                                    IfcWall ifcWall = ifcStore.Instances.New<IfcWall>(wall =>
                                    {
                                        wall.Name = "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:" + jsonWall.ID.ToString();
                                        wall.Representation = ifcStore.Instances.New<IfcProductDefinitionShape>(productDefinitionShape =>
                                        {
                                            productDefinitionShape.Representations.Add(ifcStore.Instances.New<IfcShapeRepresentation>(shapeRepresentation =>
                                            {
                                                shapeRepresentation.ContextOfItems = ifcStore.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                                                shapeRepresentation.RepresentationType = "CSG";
                                                shapeRepresentation.RepresentationIdentifier = "Body";
                                                IfcBooleanResult ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(initialBooleanResult =>
                                                {
                                                    initialBooleanResult.Operator = IfcBooleanOperator.INTERSECTION;
                                                    initialBooleanResult.FirstOperand = initialBooleanResult.SecondOperand = ifcStore.Instances.New<IfcExtrudedAreaSolid>(extrudedAreaSolid =>
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
                                                                cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                            });
                                                            axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                            {
                                                                direction.SetXYZ(0, 0, 1);
                                                            });
                                                            axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                            {
                                                                direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
                                                            });
                                                        });
                                                        ifcWallRepresentations.Add(extrudedAreaSolid);
                                                    });
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcWindowRepresentation in ifcWindowRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcWindowRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcDoorRepresentation in ifcDoorRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcDoorRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add(ifcBooleanResult);
                                                ifcStyledItem.Item = ifcBooleanResult;
                                            }));
                                        });
                                        wall.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                        wall.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
                                        wall.Tag = jsonWall.ID.ToString();
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
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.X, jsonWindow.location.Y, jsonWindow.location.Z) * 1000;
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
                                                cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                            });
                                            axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                            {
                                                direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                            });
                                            axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                            {
                                                direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
                                            });
                                        });
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
                                                IfcBooleanResult ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(initialBooleanResult =>
                                                {
                                                    initialBooleanResult.Operator = IfcBooleanOperator.INTERSECTION;
                                                    initialBooleanResult.FirstOperand = initialBooleanResult.SecondOperand = ifcExtrudedAreaSolid;
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add(ifcBooleanResult);
                                                ifcStyledItem.Item = ifcBooleanResult;
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
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.X, jsonDoor.location.Y, jsonDoor.location.Z) * 1000;
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
                                                cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                            });
                                            axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                            {
                                                direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                            });
                                            axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                            {
                                                direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
                                            });
                                        });
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
                                                IfcBooleanResult ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(initialBooleanResult =>
                                                {
                                                    initialBooleanResult.Operator = IfcBooleanOperator.INTERSECTION;
                                                    initialBooleanResult.FirstOperand = initialBooleanResult.SecondOperand = ifcExtrudedAreaSolid;
                                                });
                                                foreach (IfcExtrudedAreaSolid ifcColumnsRepresentation in ifcColumnRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcColumnsRepresentation;
                                                    });
                                                }
                                                foreach (IfcExtrudedAreaSolid ifcBeamRepresentation in ifcBeamRepresentations)
                                                {
                                                    ifcBooleanResult = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanResult;
                                                        iterativeBooleanResult.SecondOperand = ifcBeamRepresentation;
                                                    });
                                                }
                                                shapeRepresentation.Items.Add(ifcBooleanResult);
                                                ifcStyledItem.Item = ifcBooleanResult;
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
                                foreach(jsonSlab jsonSlab in js.Slab)
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
                                                                    cartesianPoint.SetXY(jsonXYZ.X * UNIT_CONVERSION, jsonXYZ.Y * UNIT_CONVERSION);
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
                                                            cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                                                                    cartesianPoint.SetXY(jsonXYZ.X * UNIT_CONVERSION, jsonXYZ.Y * UNIT_CONVERSION);
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
                                                            cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z + (js.Wall[0].height + jsonSlab.thickness * 2) * UNIT_CONVERSION);
                                                        });
                                                        axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                        if(type == TypeIFC.Structure) ifcStore.SaveAs(Path.Combine(path, type.ToString() + " " + release.ToString() + ".ifc"), StorageType.Ifc);
                    }
                    if (type == TypeIFC.MEP || type == TypeIFC.Model)
                    {
                        jsonMEP jmep = readJSONMEP();
                        List<IfcExtrudedAreaSolid> ifcPipeRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcEllbowRepresentations = new List<IfcExtrudedAreaSolid>();
                        List<IfcExtrudedAreaSolid> ifcTFittingRepresentations = new List<IfcExtrudedAreaSolid>();

                        List<IfcProduct> ifcProducts = new List<IfcProduct>();
                        using (var txn = ifcStore.BeginTransaction("Create Columns"))
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
                                    double length = jsonPipe.length * 1000;
                                    double radius = jsonPipe.Radius * 1000;
                                    jsonXYZ locationJsonXYZ = new jsonXYZ(jsonPipe.Startpoint.X, jsonPipe.Startpoint.Y, jsonPipe.Startpoint.Z) * 1000;
                                    jsonXYZ axisJsonXYZ = new jsonXYZ((jsonPipe.Endpoint - jsonPipe.Startpoint).X, (jsonPipe.Endpoint - jsonPipe.Startpoint).Y, (jsonPipe.Endpoint - jsonPipe.Startpoint).Z) * 1000;
                                    jsonXYZ refDirJsonXYZ = new jsonXYZ(axisJsonXYZ.Y, -axisJsonXYZ.X, 0) * 1000;
                                    //axis: extrude dir/Z dir; refDirection: width dir/X dir
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
                                                            cartesianPoint.SetXYZ(locationJsonXYZ.X, locationJsonXYZ.Y, locationJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.Axis = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(axisJsonXYZ.X, axisJsonXYZ.Y, axisJsonXYZ.Z);
                                                        });
                                                        axis2Placement3D.RefDirection = ifcStore.Instances.New<IfcDirection>(direction =>
                                                        {
                                                            direction.SetXYZ(refDirJsonXYZ.X, refDirJsonXYZ.Y, refDirJsonXYZ.Z);
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
                        using (var txn = ifcStore.BeginTransaction(""))
                        {
                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
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
            public int ID { get; set; }
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
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public jsonXYZ(double x, double y, double z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
            public double distanceTo(jsonXYZ p)
            {
                double d = Math.Sqrt(Math.Pow(this.X - p.X, 2) + Math.Pow(this.Y - p.Y, 2) + Math.Pow(this.Z - p.Z, 2));
                return d;
            }
            public double dotProduct(jsonXYZ p)
            {
                return this.X * p.X + this.Y * p.Y + this.Z * p.Z;
            }
            public static jsonXYZ operator -(jsonXYZ left, jsonXYZ right)
            {
                return new jsonXYZ(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
            }
            public static jsonXYZ operator -(jsonXYZ s)
            {
                return new jsonXYZ(-s.X, -s.Y, -s.Z);
            }
            public static jsonXYZ operator /(jsonXYZ s, double d)
            {
                return new jsonXYZ(s.X / d, s.Y / d, s.Z / d);
            }
            public static jsonXYZ operator +(jsonXYZ left, jsonXYZ right)
            {
                return new jsonXYZ(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
            }
            public static jsonXYZ operator *(jsonXYZ left, double d)
            {
                return new jsonXYZ(left.X * d, left.Y * d, left.Z * d);
            }
            public static jsonXYZ operator *(double d, jsonXYZ right)
            {
                return new jsonXYZ(right.X * d, right.Y * d, right.Z * d);
            }
            public double distanceTo2Points(jsonXYZ p1, jsonXYZ p2)
            {
                double dis = Math.Sqrt(Math.Pow(distanceTo(p1), 2) - Math.Pow((p2 - p1).dotProduct(this - p1) / (p2.distanceTo(p1)), 2));
                return dis;
            }
            public jsonXYZ normalized()
            {
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
                double vx = axis.normalized().X;
                double vy = axis.normalized().Y;
                double vz = axis.normalized().Z;

                double new_x = (vx * vx * (1 - c) + c) * new_pt.X + (vx * vy * (1 - c) - vz * s) * new_pt.Y + (vx * vz * (1 - c) + vy * s) * new_pt.Z;

                double new_y = (vy * vx * (1 - c) + vz * s) * new_pt.X + (vy * vy * (1 - c) + c) * new_pt.Y + (vy * vz * (1 - c) - vx * s) * new_pt.Z;

                double new_z = (vx * vz * (1 - c) - vy * s) * new_pt.X + (vy * vz * (1 - c) + vx * s) * new_pt.Y + (vz * vz * (1 - c) + c) * new_pt.Z;

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
        }
        public class jsonFitting
        {
            public int Pipe_Index_1 { get; set; }
            public int Pipe_Index_2 { get; set; }
        }
        public class jsonTee
        {
            public int Pipe_Index_1 { get; set; }
            public int Pipe_Index_2 { get; set; }
            public int Pipe_Index_3 { get; set; }
        }
    }
}
