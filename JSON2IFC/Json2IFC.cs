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
using Xbim.IO;
using static JSON2IFC.Material;
using static JSON2IFC.SJSONPlugin;
using static JSON2IFC.IFCStructureCreater;
using Xbim.Ifc4.UtilityResource;

namespace JSON2IFC
{
    public static class Json2IfcHelper123
    {
        public static string error_msg;
        public const double UNIT_CONVERSION = 1000;
        public static Result GenerateIFC(XbimSchemaVersion release, string outputIfcFilePath, string structureFilePath, string mepFilePath, string ductFilePath, string beamFilePath, string propertiesPath, string appearancePath, string metaDataFilePath)
        {
            if(!string.IsNullOrEmpty(appearancePath) && File.Exists(appearancePath))
            {
                Dictionary<BuildingComponent, Style> appearance = new DataReader().readAppearance(appearancePath);
                IFCElementCreater.appearance = appearance;
            }
            Result res = new Result(0);
            DataReader dataReader = new DataReader();
            DataWriter dataWriter = new DataWriter();
            TemplateBuilder templateBuilder = new TemplateBuilder();
            using (var ifcStore = createandInitModel("Model"))
            {
                if (ifcStore != null)
                {
                    IfcBuilding ifcBuilding = createBuilding(ifcStore, "Building");
                    IfcBuildingStorey ifcBuildingStorey = createStorey(ifcBuilding);

                    if (!string.IsNullOrEmpty(structureFilePath) && File.Exists(structureFilePath))
                    {
                        jsonStructure js = dataReader.readJSONStructure(structureFilePath);

                        List<IfcColumn> ifcColumns = new List<IfcColumn>();
                        List<IfcBeam> ifcBeams = new List<IfcBeam>();
                        List<IfcWall> ifcWalls = new List<IfcWall>();
                        List<IfcWindow> ifcWindows = new List<IfcWindow>();
                        List<IfcDoor> ifcDoors = new List<IfcDoor>();
                        List<IfcSlab> ifcSlabs = new List<IfcSlab>();
                        List<IfcProduct> ifcProducts = new List<IfcProduct>();

                        IFCStructureCreater iFCStructureCreater = new IFCStructureCreater(ifcStore, ifcBuilding, js);

                        List<IfcRepresentation> excludeReps = new List<IfcRepresentation>();
                        using (var txn = ifcStore.BeginTransaction("Create Columns"))
                        {
                            //create columns
                            if (js.Column != null)
                            {
                                res.noElements += js.Column.Length;
                                ifcColumns.AddRange(iFCStructureCreater.createColumns(null, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Column)));
                            }
                            txn.Commit();
                        }
                        excludeReps.AddRange(ifcColumns.ConvertAll(e => e.Representation.Representations.First()));
                        using (var txn = ifcStore.BeginTransaction("Create Beams"))
                        {
                            //create beams
                            if (js.Beam != null)
                            {
                                res.noElements += js.Beam.Length;
                                ifcBeams.AddRange(iFCStructureCreater.createBeams(excludeReps, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Beam)));
                            }
                            txn.Commit();
                        }

                        excludeReps.AddRange(ifcBeams.ConvertAll(e => e.Representation.Representations.First()));
                        if (!string.IsNullOrEmpty(beamFilePath) && File.Exists(beamFilePath))
                        {
                            //create beams from seperate files
                            jsonBeam[] jb = dataReader.readJSONStructure(beamFilePath).Beam;
                            using (var txn = ifcStore.BeginTransaction("Create Beams"))
                            {
                                //create beams
                                res.noElements += jb.Length;
                                //create material
                                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
                                {
                                    relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                                    {
                                        material.Name = Concrete.ToString();
                                    });
                                });
                                foreach (jsonBeam jsonBeam in jb)
                                {
                                    double length = jsonBeam.length * 1000;
                                    double width = jsonBeam.width * 1000;
                                    double thickness = jsonBeam.startPointInfile.distanceTo(jsonBeam.endPointInfile) * 1000;
                                    if (length == 0 || width == 0 || thickness == 0)
                                    {
                                        error_msg += "Empty Beam: length = " + length + ", width = " + width + ", thickness = " + thickness;
                                        continue;
                                    }
                                    jsonXYZ refDirJsonXYZ = jsonBeam.lengthDirection * 1000;
                                    jsonXYZ locationJsonXYZ = jsonBeam.startPointInfile * 1000;
                                    jsonXYZ axisJsonXYZ = (jsonBeam.endPointInfile - jsonBeam.startPointInfile) * 1000;
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
                                                IfcBooleanOperand ifcBooleanOperand = iFCStructureCreater.createCuboid(thickness, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                                                foreach (IfcRepresentation ifcRepresentation in excludeReps)
                                                {
                                                    ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                                                    {
                                                        iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                                                        iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                                                        iterativeBooleanResult.SecondOperand = (IfcBooleanOperand)ifcRepresentation.Items.First();
                                                    });
                                                }
                                                shapeRepresentation.Items.Add((IfcRepresentationItem)ifcBooleanOperand);
                                            }));
                                        });
                                        beam.ObjectPlacement = ifcBuilding.ObjectPlacement;
                                    });
                                    ifcBeams.Add(ifcBeam);
                                    iFCStructureCreater.setAppearance(ifcBeam.Representation.Representations.First().Items.First(), IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Beam));
                                    ifcRelAssociatesMaterial.RelatedObjects.Add(ifcBeam);
                                }
                                txn.Commit();
                            }
                        }
                        //using (var txn = ifcStore.BeginTransaction("Create Window/Doors Representations"))
                        //{
                        //    //create window/doors representations
                        //    if (js.Window != null) if (js.Door != null)
                        //        {
                        //            foreach (jsonWindow jsonWindow in js.Window)
                        //            {
                        //                double length = jsonWindow.length * 1000;
                        //                double width = jsonWindow.width * 1000;
                        //                double height = jsonWindow.height * 1000;

                        //                jsonXYZ refDirJsonXYZ = (jsonWindow.endPoint - jsonWindow.startPoint) * 1000;
                        //                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonWindow.location.x, jsonWindow.location.y, jsonWindow.location.z) * 1000;
                        //                jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                        //                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                        //                IfcExtrudedAreaSolid ifcExtrudedAreaSolid = iFCElementCreater.createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);

                        //                excludeReps.Add(ifcExtrudedAreaSolid as IfcRepresentation);
                        //            }
                        //            foreach (jsonDoor jsonDoor in js.Door)
                        //            {
                        //                double length = jsonDoor.length * UNIT_CONVERSION;
                        //                double width = jsonDoor.width * UNIT_CONVERSION;
                        //                double height = jsonDoor.height * UNIT_CONVERSION;

                        //                jsonXYZ refDirJsonXYZ = (jsonDoor.endPoint - jsonDoor.startPoint) * 1000;
                        //                jsonXYZ locationJsonXYZ = new jsonXYZ(jsonDoor.location.x, jsonDoor.location.y, jsonDoor.location.z) * 1000;
                        //                jsonXYZ axisJsonXYZ = new jsonXYZ(0, 0, 1) * 1000;
                        //                //axis: extrude dir/Z dir; refDirection: width dir/X dir

                        //                IfcExtrudedAreaSolid ifcExtrudedAreaSolid = iFCElementCreater.createCuboid(height, length, width, locationJsonXYZ, axisJsonXYZ, refDirJsonXYZ);
                        //            }
                        //        }
                        //    txn.Commit();
                        //}
                        Dictionary<string, List<PropertySet>> properties = PropertyAgent.defaultProperties;
                        if (!string.IsNullOrEmpty(propertiesPath) && File.Exists(propertiesPath))
                        {
                            properties = dataReader.readProperties(propertiesPath);
                        }

                        using (var txn = ifcStore.BeginTransaction("Create Walls"))
                        {
                            //create walls
                            if (js.Wall != null)
                            {
                                res.noElements += js.Wall.Length;
                                ifcWalls.AddRange(iFCStructureCreater.createWalls(excludeReps, iFCStructureCreater.createWallType(properties), properties, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Wall)));
                            }
                            txn.Commit();
                        }

                        excludeReps.AddRange(ifcWalls.ConvertAll(e => e.Representation.Representations.First()));

                        using (var txn = ifcStore.BeginTransaction("Create Windows/Doors"))
                        {
                            if (js.Window != null)
                            {
                                res.noElements += js.Window.Length;
                                ifcWindows.AddRange(iFCStructureCreater.createWindows(excludeReps, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Window)));
                            }
                            if (js.Door != null)
                            {
                                res.noElements += js.Door.Length;
                                ifcDoors.AddRange(iFCStructureCreater.createDoors(excludeReps, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Door)));
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create Slabs"))
                        {
                            if (js.Slab != null)
                            {
                                res.noElements += js.Slab.Length;
                                ifcSlabs.AddRange(iFCStructureCreater.createSlabs(null, IFCElementCreater.appearance.First(p => p.Key == BuildingComponent.Slab)));
                            }
                            txn.Commit();
                        }


                        excludeReps.AddRange(ifcSlabs.ConvertAll(e => e.Representation.Representations.First()));

                        ifcProducts.AddRange(ifcColumns);
                        ifcProducts.AddRange(ifcBeams);
                        ifcProducts.AddRange(ifcWalls);
                        ifcProducts.AddRange(ifcDoors);
                        ifcProducts.AddRange(ifcWindows);
                        ifcProducts.AddRange(ifcSlabs);
                        templateBuilder.addObject(ifcProducts);

                        using (var txn = ifcStore.BeginTransaction(""))
                        {

                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
                    }
                    if (!string.IsNullOrEmpty(mepFilePath) && File.Exists(mepFilePath))
                    {
                        jsonMEP jmep = dataReader.readJSONMEP(mepFilePath);
                        res.noElements += jmep.Elbow_Pipe_Junction.Length + jmep.pipe.Length + jmep.T_Pipe_Junction.Length;
                        List<IIfcFlowSegment> ifcFlowSegments = new List<IIfcFlowSegment>();
                        List<IIfcFlowFitting> ifcPipeEllbows = new List<IIfcFlowFitting>();
                        List<IIfcFlowFitting> ifcPipeTFittings = new List<IIfcFlowFitting>();
                        List<IIfcFlowFitting> ifcPipe_S_Traps = new List<IIfcFlowFitting>();
                        List<IIfcFlowFitting> ifcPipe_P_Traps = new List<IIfcFlowFitting>();
                        List<IfcProduct> ifcProducts = new List<IfcProduct>();

                        IFCMEPCreater iFCMEPCreater = new IFCMEPCreater(jmep, ifcStore, ifcBuilding);
                        iFCMEPCreater.MEPAdjuster.adjustPipeElbow();
                        iFCMEPCreater.MEPAdjuster.adjustPipeTee();
                        iFCMEPCreater.MEPAdjuster.adjustPipeSTrap();
                        iFCMEPCreater.MEPAdjuster.adjsutPipeTTrap();

                        List<jsonPipe> pipes = new List<jsonPipe>();
                        if (jmep.pipe != null)
                        {
                            pipes.AddRange(jmep.pipe);
                        }

                        using (var txn = ifcStore.BeginTransaction("Create Pipes"))
                        {
                            if (jmep.pipe != null)
                            {
                                ifcFlowSegments.AddRange(iFCMEPCreater.createPipes());
                            }
                            txn.Commit();
                        }

                        using (var txn = ifcStore.BeginTransaction("Create Pipe Ellbows"))
                        {
                            if (jmep.Elbow_Pipe_Junction != null)
                            {
                                ifcPipeEllbows.AddRange(iFCMEPCreater.createPipeElbows());
                            }
                            txn.Commit();
                        }

                        using (var txn = ifcStore.BeginTransaction("Create Pipe Tees"))
                        {
                            if (jmep.T_Pipe_Junction != null)
                            {
                                ifcPipeTFittings.AddRange(iFCMEPCreater.createPipeTee());
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create S Trap"))
                        {
                            if (jmep.S_Trap != null)
                            {
                                ifcPipe_S_Traps.AddRange(iFCMEPCreater.createPipeSTrap());
                            }
                            txn.Commit();
                        }
                        using (var txn = ifcStore.BeginTransaction("Create P Trap"))
                        {
                            if (jmep.P_Trap != null)
                            {
                                ifcPipe_P_Traps.AddRange(iFCMEPCreater.createPipePTrap());
                            }
                            txn.Commit();
                        }

                        ifcProducts.AddRange(ifcFlowSegments.ConvertAll(e => (IfcProduct)e));
                        ifcProducts.AddRange(ifcPipeEllbows.ConvertAll(e => (IfcProduct)e));
                        ifcProducts.AddRange(ifcPipeTFittings.ConvertAll(e => (IfcProduct)e));
                        ifcProducts.AddRange(ifcPipe_S_Traps.ConvertAll(e => (IfcProduct)e));
                        ifcProducts.AddRange(ifcPipe_P_Traps.ConvertAll(e => (IfcProduct)e));
                        templateBuilder.addObject(ifcProducts);

                        using (var txn = ifcStore.BeginTransaction(""))
                        {
                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
                    }
                    if (!string.IsNullOrEmpty(ductFilePath) && File.Exists(ductFilePath))
                    {
                        jsonM jm = dataReader.readJSONM(ductFilePath);
                        List<IfcDuctSegment> ifcDuctSegments = new List<IfcDuctSegment>();
                        List<IfcDuctFitting> ifcDuctEllbows = new List<IfcDuctFitting>();
                        List<IfcDuctFitting> ifcDuctTFitting = new List<IfcDuctFitting>();
                        List<jsonDuct> ducts = new List<jsonDuct>();

                        IFCMEPCreater iFCMEPCreater = new IFCMEPCreater(jm, ifcStore, ifcBuilding);

                        if (jm.duct != null)
                        {
                            ducts.AddRange(jm.duct);
                        }

                        List<IfcProduct> ifcProducts = new List<IfcProduct>();

                        using (var txn = ifcStore.BeginTransaction("Create Ducts"))
                        {
                            if (jm.duct != null)
                            {
                                ifcDuctSegments.AddRange(iFCMEPCreater.createDucts());
                            }
                            txn.Commit();
                        }

                        ifcProducts.AddRange(ifcDuctSegments);
                        ifcProducts.AddRange(ifcDuctEllbows);
                        ifcProducts.AddRange(ifcDuctTFitting);
                        templateBuilder.addObject(ifcProducts);

                        using (var txn = ifcStore.BeginTransaction(""))
                        {
                            ifcBuilding.AddToSpatialDecomposition(ifcBuildingStorey);
                            foreach (IfcProduct ifcProduct in ifcProducts)
                            {
                                ifcBuildingStorey.AddElement(ifcProduct);
                            }
                            txn.Commit();
                        }
                    }

                    dataWriter.writeIfc(ifcStore, outputIfcFilePath);
                    dataWriter.writeJson(templateBuilder.metaObjects, outputIfcFilePath);
                    if (error_msg != null) dataWriter.WriteError(error_msg, outputIfcFilePath);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadLine();
                    return res;
                }
            }
            return new Result(0);
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
            model.Header.FileName.Name = "Name";
            model.Header.FileName.Organization = new List<String> { "Organizations" };
            model.Header.FileName.AuthorizationMailingAddress = new List<String> { "Authorization Mailing Address" };
            model.Header.FileName.AuthorizationName = "Authorization Name";
            model.Header.FileName.AuthorName = new List<String> { "Author Name" };
            model.Header.FileName.OriginatingSystem = "Originating System";
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
        public class Result
        {
            public int noElements { set; get; }
            public Result(int n)
            {
                this.noElements = n;
            }
        }
        public class Generic<T>
        {
        }
    }
}
