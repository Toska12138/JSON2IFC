﻿using System;
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

namespace JSON2IFC
{
    class IFCElementCreater
    {
        public static Dictionary<BuildingComponent, Style> appearance = new Dictionary<BuildingComponent, Style>()
        {
            { BuildingComponent.Column, new Style { RGB = new double[] { 0.250980, 0.250980, 0.250980 }, transparency = 0 } },
            { BuildingComponent.Beam, new Style { RGB = new double[] { 0.250980, 0.250980, 0.250980 }, transparency = 0 } },
            { BuildingComponent.Wall, new Style { RGB = new double[] { 0.666666, 0.392156, 0.411764 }, transparency = 0 } },
            { BuildingComponent.Window, new Style { RGB = new double[] { 0.137255, 0.403922, 0.870588 }, transparency = 0.4 } },
            { BuildingComponent.Door, new Style { RGB = new double[] { 0.637255, 0.603922, 0.670588 }, transparency = 0 } },
            { BuildingComponent.Slab, new Style { RGB = new double[] { 0.837255, 0.603922, 0.670588 }, transparency = 0.4 } },
            { BuildingComponent.pipe, new Style { RGB = new double[] { 0.1, 1, 0.1 }, transparency = 0 } },
            { BuildingComponent.pipeElbow, new Style { RGB = new double[] { 0.5, 0.5, 0.5 }, transparency = 0 } },
            { BuildingComponent.pipeTee, new Style { RGB = new double[] { 0.5, 0.5, 0.5 }, transparency = 0 } },
            { BuildingComponent.S_trap, new Style { RGB = new double[] { 1, 0, 0 }, transparency = 0 } },
            { BuildingComponent.P_trap, new Style { RGB = new double[] { 0, 0, 1 }, transparency = 0 } },
            { BuildingComponent.Duct, new Style { RGB = new double[] { 1, 0, 0 }, transparency = 0 } }
        };
        public const double UNIT_CONVERSION = 1000;
        public IfcStore ifcStore { get; }
        public IfcBuilding ifcBuilding { get; }
        public IFCElementCreater(IfcStore ifcStore, IfcBuilding ifcBuilding)
        {
            this.ifcStore = ifcStore;
            this.ifcBuilding = ifcBuilding;
        }
        public IfcBooleanOperand exclude(List<IfcRepresentation> excludeReps, IfcBooleanOperand ifcBooleanOperand)
        {
            foreach (IfcRepresentation ifcRepresentation in excludeReps)
            {
                ifcBooleanOperand = ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                {
                    iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                    iterativeBooleanResult.FirstOperand = ifcBooleanOperand;
                    iterativeBooleanResult.SecondOperand = (IfcBooleanOperand)ifcRepresentation.Items.First();
                });
            }
            return ifcBooleanOperand;
        }
        public void cutReps(List<IfcRepresentation> reps, IfcBooleanOperand ifcBooleanOperand)
        {
            foreach (IfcRepresentation ifcRepresentation in reps)
            {
                //Console.WriteLine(ifcRepresentation);
                IfcBooleanOperand FirstOperand = (IfcBooleanOperand)ifcRepresentation.Items.First();
                //Console.WriteLine(ifcRepresentation.Items.First());
                IfcStyledItem ifcStyledItem = ifcRepresentation.Items.First().StyledByItem.First();
                ifcRepresentation.Items.Add(ifcStore.Instances.New<IfcBooleanResult>(iterativeBooleanResult =>
                {
                    iterativeBooleanResult.Operator = IfcBooleanOperator.DIFFERENCE;
                    iterativeBooleanResult.FirstOperand = FirstOperand;
                    iterativeBooleanResult.SecondOperand = ifcBooleanOperand;
                }));
                ifcRepresentation.Items.RemoveAt(0);
                ifcStyledItem.Item = ifcRepresentation.Items.First();
            }
        }
        public void attachMaterial(IfcProduct ifcProduct, Material m)
        {
            IfcRelAssociatesMaterial ifcRelAssociatesMaterial = ifcStore.Instances.New<IfcRelAssociatesMaterial>(relAssociatesMaterial =>
            {
                relAssociatesMaterial.RelatingMaterial = ifcStore.Instances.New<IfcMaterial>(material =>
                {
                    material.Name = m.ToString();
                });
            });
            ifcRelAssociatesMaterial.RelatedObjects.Add(ifcProduct);
        }
        public void setAppearance(IfcRepresentationItem ifcRepresentationItem, KeyValuePair<BuildingComponent, Style> pair)
        {
            IfcStyledItem ifcStyledItem = ifcStore.Instances.New<IfcStyledItem>(styledItem =>
            {
                styledItem.Styles.Add(ifcStore.Instances.New<IfcPresentationStyleAssignment>(presentationStyleAssignment =>
                {
                    presentationStyleAssignment.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyle>(surfaceStyle =>
                    {
                        surfaceStyle.Name = pair.Key.ToString();
                        surfaceStyle.Side = IfcSurfaceSide.BOTH;
                        surfaceStyle.Styles.Add(ifcStore.Instances.New<IfcSurfaceStyleRendering>(surfaceStyleRendering =>
                        {
                            surfaceStyleRendering.SurfaceColour = ifcStore.Instances.New<IfcColourRgb>(colorRGB =>
                            {
                                colorRGB.Red = pair.Value.RGB[0];
                                colorRGB.Green = pair.Value.RGB[1];
                                colorRGB.Blue = pair.Value.RGB[2];
                            });
                            surfaceStyleRendering.Transparency = pair.Value.transparency;
                            surfaceStyleRendering.SpecularColour = new IfcNormalisedRatioMeasure(0.5);
                            surfaceStyleRendering.SpecularHighlight = new IfcSpecularExponent(128);
                            surfaceStyleRendering.ReflectanceMethod = IfcReflectanceMethodEnum.NOTDEFINED;
                        }));
                    }));
                }));
            });
            ifcStyledItem.Item = ifcRepresentationItem;
        }
        
    }

}