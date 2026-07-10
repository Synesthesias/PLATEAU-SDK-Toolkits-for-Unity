using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Interfaces;
using ProceduralToolkit;
using ProceduralToolkit.Buildings;
using System;
using System.Collections.Generic;
using UnityEngine;
using ConstructorElement = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.ResidenceFacadePlan.ConstructorElement;
using PanelSize = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.ResidenceFacadePlan.PanelSize;
using WallOrWindowElement = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.ResidenceFacadePlan.WallOrWindowElement;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime
{
    [CreateAssetMenu(menuName = "ProceduralToolkit/Buildings/Procedural Facade Planner/House", order = 2)]
    public class ProceduralFacadeResidencePlanner : FacadePlanner
    {
        private const float k_SocleHeight = 1;
        private const float k_FloorHeight = 2.5f;
        private const float k_BufferWidth = 2;
        private const string k_SocleTopTexturedDraftName = "SocleTopTextured";
        private const float k_ShadowWallOffset = 0.1f;

        private readonly Dictionary<PanelType, List<Func<ILayoutElement>>> m_Constructors = new();
        private readonly Dictionary<PanelType, Func<ILayoutElement>> m_CommonConstructors = new();
        private readonly Dictionary<PanelSize, float> m_SizeValues = new()
        {
            {PanelSize.Narrow, 2.5f},
            {PanelSize.Wide, 3},
        };

        public override List<ILayout> Plan(List<Vector2> foundationPolygon, BuildingGenerator.Config config)
        {
            SetupConstructors(config);
            return PlanInternal(foundationPolygon, config, null);
        }

        public ResidenceFacadePlan CreateResidenceFacadePlan(List<Vector2> foundationPolygon, BuildingGenerator.Config config, float buildingWidth, float buildingDepth)
        {
            SetupConstructors(config);

            var residenceFacadePlan = new ResidenceFacadePlan
            {
                buildingWidth = buildingWidth,
                buildingDepth = buildingDepth,
                numFloor = config.residenceParams.numFloor
            };

            for (int i = 0; i < foundationPolygon.Count; i++)
            {
                Vector2 a = foundationPolygon.GetLooped(i + 1);
                Vector2 aNext = foundationPolygon.GetLooped(i + 2);
                Vector2 b = foundationPolygon[i];
                Vector2 bPrevious = foundationPolygon.GetLooped(i - 1);
                float width = (b - a).magnitude;
                bool leftIsConvex = Geometry.GetAngle(b, a, aNext) <= 180;
                bool rightIsConvex = Geometry.GetAngle(bPrevious, b, a) <= 180;
                bool isNormalFacade = i != 2;

                residenceFacadePlan.facades.Add(CreateRandomFacadePlan(width, config, leftIsConvex, rightIsConvex, isNormalFacade));
            }

            return residenceFacadePlan;
        }

        public ResidenceFacadePlan CreateResidenceFacadePlan(List<Vector2> foundationPolygon, BuildingGenerator.Config config, float buildingWidth, float buildingDepth, int randomSeed)
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(randomSeed);
            try
            {
                return CreateResidenceFacadePlan(foundationPolygon, config, buildingWidth, buildingDepth);
            }
            finally
            {
                UnityEngine.Random.state = previousState;
            }
        }

        public List<ILayout> Plan(List<Vector2> foundationPolygon, BuildingGenerator.Config config, ResidenceFacadePlan residenceFacadePlan)
        {
            if (residenceFacadePlan == null)
            {
                throw new ArgumentNullException(nameof(residenceFacadePlan));
            }

            SetupConstructors(config);
            return PlanInternal(foundationPolygon, config, residenceFacadePlan);
        }

        private List<ILayout> PlanInternal(List<Vector2> foundationPolygon, BuildingGenerator.Config config, ResidenceFacadePlan residenceFacadePlan)
        {
            if (residenceFacadePlan != null &&
                (residenceFacadePlan.facades == null || residenceFacadePlan.facades.Count < foundationPolygon.Count))
            {
                throw new ArgumentException("Residence facade plan does not contain enough facades.", nameof(residenceFacadePlan));
            }

            // Supports only rectangular buildings
            var layouts = new List<ILayout>();
            for (int i = 0; i < foundationPolygon.Count; i++)
            {
                Vector2 a = foundationPolygon.GetLooped(i + 1);
                Vector2 aNext = foundationPolygon.GetLooped(i + 2);
                Vector2 b = foundationPolygon[i];
                Vector2 bPrevious = foundationPolygon.GetLooped(i - 1);
                float width = (b - a).magnitude;
                bool leftIsConvex = Geometry.GetAngle(b, a, aNext) <= 180;
                bool rightIsConvex = Geometry.GetAngle(bPrevious, b, a) <= 180;
                ResidenceFacadePlan.FacadePlan facadePlan = residenceFacadePlan?.facades[i];
                int constructorChoiceIndex = 0;

                switch (i)
                {
                    case 0:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Back;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_ShadowWall, width - k_ShadowWallOffset, config.residenceParams.numFloor * k_FloorHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, config, leftIsConvex, rightIsConvex, facadePlan, ref constructorChoiceIndex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 1:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Right;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_ShadowWall, width - k_ShadowWallOffset, config.residenceParams.numFloor * k_FloorHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, config, leftIsConvex, rightIsConvex, facadePlan, ref constructorChoiceIndex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 2:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Front;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_ShadowWall, width - k_ShadowWallOffset, config.residenceParams.numFloor * k_FloorHeight - k_ShadowWallOffset));
                        vertical.Add(PlanEntranceFacade(width, config, leftIsConvex, rightIsConvex, facadePlan, ref constructorChoiceIndex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 3:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Left;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_ShadowWall, width - k_ShadowWallOffset, config.residenceParams.numFloor * k_FloorHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, config, leftIsConvex, rightIsConvex, facadePlan, ref constructorChoiceIndex));
                        layouts.Add(vertical);
                        break;
                    }
                }

                if (facadePlan != null)
                {
                    ValidateConstructorChoiceCount(facadePlan, constructorChoiceIndex);
                }
            }

            return layouts;
        }

        private void SetupConstructors(BuildingGenerator.Config config)
        {
            m_Constructors[PanelType.k_Attic] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
            };
            m_Constructors[PanelType.k_Entrance] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralEntrance(config),
            };
            m_Constructors[PanelType.k_Socle] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralSocle(
                    config,
                    socleColor: config.residenceVertexColorPalette.socleColor,
                    socleMat: config.residenceMaterialPalette.socle
                    )
                {
                    heightScale = k_SocleHeight * 0.15f,
                }
            };
            m_Constructors[PanelType.k_SocleTop] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralSocle(
                    config,
                    socleName: k_SocleTopTexturedDraftName,
                    socleColor: config.residenceVertexColorPalette.socleTopColor,
                    socleMat: config.residenceMaterialPalette.socleTop
                    )
                {
                    heightScale = k_SocleHeight * 0.05f,
                }
            };
            m_Constructors[PanelType.k_Wall] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
            };
            m_Constructors[PanelType.k_ShadowWall] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
                {
                    m_IsShadowWall = true,
                    m_MoveShadowWallDepth = 0.1f,
                    m_ShadowWallWidthOffset = k_ShadowWallOffset,
                    m_ShadowWallHeightOffset = 0
                }
            };
            m_Constructors[PanelType.k_Window] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWindow(config)
                {
                    m_WindowWidthOffset = 0.9f,
                    m_WindowBottomOffset = 0.8f,
                    m_NumCenterRods = 1,
                    m_WindowFrameRodType = ProceduralFacadeElement.WindowFrameRodType.k_Vertical,
                }
            };
            m_Constructors[PanelType.k_WallOrWindow] = new List<Func<ILayoutElement>>
            {
                m_Constructors[PanelType.k_Wall][0],
                m_Constructors[PanelType.k_Window][0]
            };
            m_Constructors[PanelType.k_SeparatedLongCrossWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWindow(config)
                {
                    m_WindowBottomOffset = 0,
                    m_NumCenterRods = 1,
                    m_WindowFrameRodType = ProceduralFacadeElement.WindowFrameRodType.k_Cross,
                    m_HasWindowsill = false
                }
            };
        }

        private ResidenceFacadePlan.FacadePlan CreateRandomFacadePlan(float facadeWidth, BuildingGenerator.Config config, bool leftIsConvex, bool rightIsConvex, bool isNormalFacade)
        {
            List<PanelSize> panelSizes = DivideFacade(facadeWidth, leftIsConvex, rightIsConvex, out float remainder);
            var facadePlan = new ResidenceFacadePlan.FacadePlan
            {
                panelSizes = new List<PanelSize>(panelSizes)
            };

            AddRandomConstructorChoice(facadePlan, PanelType.k_ShadowWall);

            if (isNormalFacade)
            {
                RecordNormalFacadeChoices(facadePlan, panelSizes, config, remainder > Geometry.Epsilon);
            }
            else
            {
                RecordEntranceFacadeChoices(facadePlan, panelSizes, config, remainder > Geometry.Epsilon);
            }

            return facadePlan;
        }

        private void RecordNormalFacadeChoices(ResidenceFacadePlan.FacadePlan facadePlan, List<PanelSize> panelSizes, BuildingGenerator.Config config, bool hasRemainder)
        {
            RecordHorizontalChoices(facadePlan, panelSizes, 0, panelSizes.Count, PanelType.k_Socle);
            RecordHorizontalChoices(facadePlan, panelSizes, 0, panelSizes.Count, PanelType.k_SocleTop);

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex > 0)
                {
                    RecordHorizontalChoices(facadePlan, panelSizes, 0, panelSizes.Count, PanelType.k_SocleTop);
                }

                RecordWallOrWindowChoices(facadePlan, panelSizes, 0, panelSizes.Count);
            }

            if (!hasRemainder)
            {
                return;
            }

            int commonWallConstructorIndex = CreateRandomConstructorIndex(PanelType.k_Wall);
            RecordBufferWallVerticalChoices(facadePlan, config, commonWallConstructorIndex);
            RecordBufferWallVerticalChoices(facadePlan, config, commonWallConstructorIndex);
        }

        private void RecordEntranceFacadeChoices(ResidenceFacadePlan.FacadePlan facadePlan, List<PanelSize> panelSizes, BuildingGenerator.Config config, bool hasRemainder)
        {
            const int entranceCount = 1;
            int entranceIndexInterval = (panelSizes.Count - entranceCount)/(entranceCount + 1);
            const int lastEntranceIndex = -1;
            int commonWallConstructorIndex = CreateRandomConstructorIndex(PanelType.k_Wall);

            if (hasRemainder)
            {
                RecordBufferWallVerticalChoices(facadePlan, config, commonWallConstructorIndex);
            }

            RecordEntranceNormalFacadeChoices(facadePlan, panelSizes, lastEntranceIndex + 1, entranceIndexInterval, config);
            RecordEntranceVerticalChoices(facadePlan, config);
            RecordEntranceNormalFacadeChoices(facadePlan, panelSizes, entranceIndexInterval + 1, panelSizes.Count, config);

            if (hasRemainder)
            {
                RecordBufferWallVerticalChoices(facadePlan, config, commonWallConstructorIndex);
            }
        }

        private void RecordBufferWallVerticalChoices(ResidenceFacadePlan.FacadePlan facadePlan, BuildingGenerator.Config config, int firstFloorWallConstructorIndex)
        {
            AddRandomConstructorChoice(facadePlan, PanelType.k_Socle);
            AddRandomConstructorChoice(facadePlan, PanelType.k_SocleTop);
            AddConstructorChoice(facadePlan, PanelType.k_Wall, firstFloorWallConstructorIndex);

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                AddRandomConstructorChoice(facadePlan, PanelType.k_SocleTop);
                AddRandomConstructorChoice(facadePlan, PanelType.k_Wall);
            }
        }

        private void RecordEntranceNormalFacadeChoices(ResidenceFacadePlan.FacadePlan facadePlan, List<PanelSize> panelSizes, int from, int to, BuildingGenerator.Config config)
        {
            RecordHorizontalChoices(facadePlan, panelSizes, from, to, PanelType.k_Socle);
            RecordHorizontalChoices(facadePlan, panelSizes, from, to, PanelType.k_SocleTop);

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex == 0)
                {
                    RecordHorizontalChoices(facadePlan, panelSizes, from, to, PanelType.k_SeparatedLongCrossWindow);
                }
                else
                {
                    RecordHorizontalChoices(facadePlan, panelSizes, from, to, PanelType.k_SocleTop);
                    RecordHorizontalChoices(facadePlan, panelSizes, from, to, PanelType.k_Window);
                }
            }
        }

        private void RecordEntranceVerticalChoices(ResidenceFacadePlan.FacadePlan facadePlan, BuildingGenerator.Config config)
        {
            AddRandomConstructorChoice(facadePlan, PanelType.k_Socle);
            AddRandomConstructorChoice(facadePlan, PanelType.k_SocleTop);
            AddRandomConstructorChoice(facadePlan, PanelType.k_Entrance);

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                AddRandomConstructorChoice(facadePlan, PanelType.k_SocleTop);
                AddRandomConstructorChoice(facadePlan, PanelType.k_Wall);
            }
        }

        private void RecordHorizontalChoices(ResidenceFacadePlan.FacadePlan facadePlan, List<PanelSize> panelSizes, int from, int to, PanelType panelType)
        {
            for (int i = from; i < to; i++)
            {
                AddRandomConstructorChoice(facadePlan, panelType);
            }
        }

        private void RecordWallOrWindowChoices(ResidenceFacadePlan.FacadePlan facadePlan, List<PanelSize> panelSizes, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                int index = CreateRandomConstructorIndex(PanelType.k_WallOrWindow);
                WallOrWindowElement element = index == 0 ? WallOrWindowElement.Wall : WallOrWindowElement.Window;
                facadePlan.wallOrWindowElements.Add(element);
                AddRandomConstructorChoice(facadePlan, element == WallOrWindowElement.Wall ? PanelType.k_Wall : PanelType.k_Window);
            }
        }

        private ILayout PlanNormalFacade(float facadeWidth, BuildingGenerator.Config config, bool leftIsConvex, bool rightIsConvex, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            List<PanelSize> panelSizes;
            float remainder;
            if (facadePlan == null)
            {
                panelSizes = DivideFacade(facadeWidth, leftIsConvex, rightIsConvex, out remainder);
            }
            else
            {
                panelSizes = GetPanelSizes(facadePlan, facadeWidth, out remainder);
            }

            if (facadePlan == null)
            {
                m_CommonConstructors[PanelType.k_Wall] = m_Constructors[PanelType.k_Wall].GetRandom();
            }

            VerticalLayout vertical;
            if (facadePlan == null)
            {
                vertical = CreateNormalFacadeVertical(panelSizes, 0, panelSizes.Count, config);
            }
            else
            {
                int wallOrWindowIndex = 0;
                vertical = CreateNormalFacadeVertical(panelSizes, 0, panelSizes.Count, config, facadePlan, ref wallOrWindowIndex, ref constructorChoiceIndex);
                ValidateWallOrWindowChoiceCount(facadePlan, wallOrWindowIndex);
            }

            if (remainder > Geometry.Epsilon)
            {
                VerticalLayout leftBuffer = facadePlan == null
                    ? CreateBufferWallVertical(remainder/2, config)
                    : CreateBufferWallVertical(remainder/2, config, facadePlan, ref constructorChoiceIndex);
                VerticalLayout rightBuffer = facadePlan == null
                    ? CreateBufferWallVertical(remainder/2, config)
                    : CreateBufferWallVertical(remainder/2, config, facadePlan, ref constructorChoiceIndex);

                return new HorizontalLayout
                {
                    leftBuffer,
                    vertical,
                    rightBuffer
                };
            }
            return vertical;
        }

        private ILayout PlanEntranceFacade(float facadeWidth, BuildingGenerator.Config config, bool leftIsConvex, bool rightIsConvex, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            List<PanelSize> panelSizes;
            float remainder;
            if (facadePlan == null)
            {
                panelSizes = DivideFacade(facadeWidth, leftIsConvex, rightIsConvex, out remainder);
            }
            else
            {
                panelSizes = GetPanelSizes(facadePlan, facadeWidth, out remainder);
            }

            if (facadePlan == null)
            {
                m_CommonConstructors[PanelType.k_Wall] = m_Constructors[PanelType.k_Wall].GetRandom();
            }

            var horizontal = new HorizontalLayout();
            bool hasRemainder = remainder > Geometry.Epsilon;
            if (hasRemainder)
            {
                horizontal.Add(facadePlan == null
                    ? CreateBufferWallVertical(remainder/2, config)
                    : CreateBufferWallVertical(remainder/2, config, facadePlan, ref constructorChoiceIndex));
            }

            const int entranceCount = 1;
            int entranceIndexInterval = (panelSizes.Count - entranceCount)/(entranceCount + 1);
            const int lastEntranceIndex = -1;
            horizontal.Add(facadePlan == null
                ? CreateEntranceNormalFacadeVertical(panelSizes, lastEntranceIndex + 1, entranceIndexInterval, config)
                : CreateEntranceNormalFacadeVertical(panelSizes, lastEntranceIndex + 1, entranceIndexInterval, config, facadePlan, ref constructorChoiceIndex));
            horizontal.Add(facadePlan == null
                ? CreateEntranceVertical(m_SizeValues[panelSizes[entranceIndexInterval]], config)
                : CreateEntranceVertical(m_SizeValues[panelSizes[entranceIndexInterval]], config, facadePlan, ref constructorChoiceIndex));
            horizontal.Add(facadePlan == null
                ? CreateEntranceNormalFacadeVertical(panelSizes, entranceIndexInterval + 1, panelSizes.Count, config)
                : CreateEntranceNormalFacadeVertical(panelSizes, entranceIndexInterval + 1, panelSizes.Count, config, facadePlan, ref constructorChoiceIndex));

            if (hasRemainder)
            {
                horizontal.Add(facadePlan == null
                    ? CreateBufferWallVertical(remainder/2, config)
                    : CreateBufferWallVertical(remainder/2, config, facadePlan, ref constructorChoiceIndex));
            }
            return horizontal;
        }

        private List<PanelSize> GetPanelSizes(ResidenceFacadePlan.FacadePlan facadePlan, float facadeWidth, out float remainder)
        {
            if (facadePlan == null)
            {
                throw new ArgumentNullException(nameof(facadePlan));
            }
            if (facadePlan.panelSizes == null || facadePlan.panelSizes.Count == 0)
            {
                throw new ArgumentException("Residence facade plan does not contain panel sizes.", nameof(facadePlan));
            }

            var panelSizes = new List<PanelSize>(facadePlan.panelSizes);
            remainder = facadeWidth;
            foreach (PanelSize panelSize in panelSizes)
            {
                remainder -= m_SizeValues[panelSize];
            }

            return panelSizes;
        }

        private static void ValidateWallOrWindowChoiceCount(ResidenceFacadePlan.FacadePlan facadePlan, int usedCount)
        {
            if (facadePlan.wallOrWindowElements != null && usedCount != facadePlan.wallOrWindowElements.Count)
            {
                throw new ArgumentException("Residence facade plan contains an unexpected number of wall/window elements.", nameof(facadePlan));
            }
        }

        private VerticalLayout CreateBufferWallVertical(float width, BuildingGenerator.Config config)
        {
            var vertical = new VerticalLayout
            {
                Construct(m_Constructors[PanelType.k_Socle], width, k_SocleHeight),
                Construct(m_Constructors[PanelType.k_SocleTop], width, k_SocleHeight),
                CreateVertical(width, k_FloorHeight, 1, m_CommonConstructors[PanelType.k_Wall])
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                vertical.Add(Construct(m_Constructors[PanelType.k_SocleTop], width, k_SocleHeight));
                vertical.Add(Construct(m_Constructors[PanelType.k_Wall], width, k_FloorHeight));
            }

            return vertical;
        }

        private VerticalLayout CreateBufferWallVertical(float width, BuildingGenerator.Config config, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            var vertical = new VerticalLayout
            {
                Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_Socle, width, k_SocleHeight),
                Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_SocleTop, width, k_SocleHeight),
                CreateVertical(width, k_FloorHeight, 1, facadePlan, ref constructorChoiceIndex, PanelType.k_Wall)
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                vertical.Add(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_SocleTop, width, k_SocleHeight));
                vertical.Add(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_Wall, width, k_FloorHeight));
            }

            return vertical;
        }

        private VerticalLayout CreateNormalFacadeVertical(List<PanelSize> panelSizes, int from, int to, BuildingGenerator.Config config)
        {
            var vertical = new VerticalLayout
            {
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_Socle]),
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_SocleTop])
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex == 0)
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, m_Constructors[PanelType.k_WallOrWindow]));
                }
                else
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_SocleTop]));
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, m_Constructors[PanelType.k_WallOrWindow]));
                }
            }

            return vertical;
        }

        private VerticalLayout CreateNormalFacadeVertical(List<PanelSize> panelSizes, int from, int to, BuildingGenerator.Config config, ResidenceFacadePlan.FacadePlan facadePlan, ref int wallOrWindowIndex, ref int constructorChoiceIndex)
        {
            var vertical = new VerticalLayout
            {
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_Socle, facadePlan, ref constructorChoiceIndex),
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_SocleTop, facadePlan, ref constructorChoiceIndex)
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex == 0)
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, facadePlan, ref wallOrWindowIndex, ref constructorChoiceIndex));
                }
                else
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_SocleTop, facadePlan, ref constructorChoiceIndex));
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, facadePlan, ref wallOrWindowIndex, ref constructorChoiceIndex));
                }
            }

            return vertical;
        }

        private VerticalLayout CreateEntranceNormalFacadeVertical(List<PanelSize> panelSizes, int from, int to, BuildingGenerator.Config config)
        {
            var vertical = new VerticalLayout
            {
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_Socle]),
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_SocleTop])
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex == 0)
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, m_Constructors[PanelType.k_SeparatedLongCrossWindow]));
                }
                else
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_SocleHeight, m_Constructors[PanelType.k_SocleTop]));
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, m_Constructors[PanelType.k_Window]));
                }
            }

            return vertical;
        }

        private VerticalLayout CreateEntranceNormalFacadeVertical(List<PanelSize> panelSizes, int from, int to, BuildingGenerator.Config config, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            var vertical = new VerticalLayout
            {
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_Socle, facadePlan, ref constructorChoiceIndex),
                CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_SocleTop, facadePlan, ref constructorChoiceIndex)
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 0; floorIndex < floors; floorIndex++)
            {
                if (floorIndex == 0)
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, PanelType.k_SeparatedLongCrossWindow, facadePlan, ref constructorChoiceIndex));
                }
                else
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_SocleHeight, PanelType.k_SocleTop, facadePlan, ref constructorChoiceIndex));
                    vertical.Add(CreateHorizontal(panelSizes, from, to, k_FloorHeight, PanelType.k_Window, facadePlan, ref constructorChoiceIndex));
                }
            }

            return vertical;
        }

        private VerticalLayout CreateEntranceVertical(float width, BuildingGenerator.Config config)
        {
            var vertical = new VerticalLayout
            {
                Construct(m_Constructors[PanelType.k_Socle], width, k_SocleHeight),
                Construct(m_Constructors[PanelType.k_SocleTop], width, k_SocleHeight),
                Construct(m_Constructors[PanelType.k_Entrance], width, k_FloorHeight),
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                vertical.Add(Construct(m_Constructors[PanelType.k_SocleTop], width, k_SocleHeight));
                vertical.Add(Construct(m_Constructors[PanelType.k_Wall], width, k_FloorHeight));
            }

            return vertical;
        }

        private VerticalLayout CreateEntranceVertical(float width, BuildingGenerator.Config config, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            var vertical = new VerticalLayout
            {
                Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_Socle, width, k_SocleHeight),
                Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_SocleTop, width, k_SocleHeight),
                Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_Entrance, width, k_FloorHeight),
            };

            int floors = config.residenceParams.numFloor;
            for (int floorIndex = 1; floorIndex < floors; floorIndex++)
            {
                vertical.Add(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_SocleTop, width, k_SocleHeight));
                vertical.Add(Construct(facadePlan, ref constructorChoiceIndex, PanelType.k_Wall, width, k_FloorHeight));
            }

            return vertical;
        }

        private List<PanelSize> DivideFacade(float facadeWidth, bool leftIsConvex, bool rightIsConvex, out float remainder)
        {
            float availableWidth = facadeWidth;
            if (!leftIsConvex)
            {
                availableWidth -= k_BufferWidth;
            }
            if (!rightIsConvex)
            {
                availableWidth -= k_BufferWidth;
            }

            Dictionary<PanelSize, int> knapsack = PTUtils.Knapsack(m_SizeValues, availableWidth);
            var sizes = new List<PanelSize>();
            remainder = facadeWidth;
            foreach (var pair in knapsack)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    sizes.Add(pair.Key);
                    remainder -= m_SizeValues[pair.Key];
                }
            }
            sizes.Shuffle();
            return sizes;
        }

        private HorizontalLayout CreateHorizontal(List<PanelSize> panelSizes, int from, int to, float height, List<Func<ILayoutElement>> constructors)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to; i++)
            {
                float width = m_SizeValues[panelSizes[i]];
                horizontal.Add(Construct(constructors, width, height));
            }
            return horizontal;
        }

        private HorizontalLayout CreateHorizontal(List<PanelSize> panelSizes, int from, int to, float height, PanelType panelType, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to; i++)
            {
                float width = m_SizeValues[panelSizes[i]];
                horizontal.Add(Construct(facadePlan, ref constructorChoiceIndex, panelType, width, height));
            }
            return horizontal;
        }

        private HorizontalLayout CreateHorizontal(List<PanelSize> panelSizes, int from, int to, float height, ResidenceFacadePlan.FacadePlan facadePlan, ref int wallOrWindowIndex, ref int constructorChoiceIndex)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to; i++)
            {
                float width = m_SizeValues[panelSizes[i]];
                horizontal.Add(Construct(facadePlan, ref constructorChoiceIndex, GetWallOrWindowPanelType(facadePlan, wallOrWindowIndex), width, height));
                wallOrWindowIndex++;
            }
            return horizontal;
        }

        private PanelType GetWallOrWindowPanelType(ResidenceFacadePlan.FacadePlan facadePlan, int wallOrWindowIndex)
        {
            if (facadePlan.wallOrWindowElements == null || wallOrWindowIndex >= facadePlan.wallOrWindowElements.Count)
            {
                throw new ArgumentException("Residence facade plan does not contain enough wall/window elements.", nameof(facadePlan));
            }

            return facadePlan.wallOrWindowElements[wallOrWindowIndex] switch
            {
                WallOrWindowElement.Wall => PanelType.k_Wall,
                WallOrWindowElement.Window => PanelType.k_Window,
                _ => throw new ArgumentOutOfRangeException(nameof(facadePlan))
            };
        }

        private VerticalLayout CreateVertical(float width, float height, int floors, Func<ILayoutElement> constructor)
        {
            var verticalLayout = new VerticalLayout();
            for (int i = 0; i < floors; i++)
            {
                verticalLayout.Add(Construct(constructor, width, height));
            }
            return verticalLayout;
        }

        private VerticalLayout CreateVertical(float width, float height, int floors, ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex, PanelType panelType)
        {
            var verticalLayout = new VerticalLayout();
            for (int i = 0; i < floors; i++)
            {
                verticalLayout.Add(Construct(facadePlan, ref constructorChoiceIndex, panelType, width, height));
            }
            return verticalLayout;
        }

        private ILayoutElement Construct(PanelType panelType, float width, float height)
        {
            return Construct(m_Constructors[panelType], width, height);
        }

        private ILayoutElement Construct(ResidenceFacadePlan.FacadePlan facadePlan, ref int constructorChoiceIndex, PanelType panelType, float width, float height)
        {
            if (facadePlan == null)
            {
                return Construct(m_Constructors[panelType], width, height);
            }
            if (!HasConstructorChoices(facadePlan))
            {
                return Construct(m_Constructors[panelType][0], width, height);
            }

            ResidenceFacadePlan.ConstructorChoice choice = GetConstructorChoice(facadePlan, constructorChoiceIndex);
            ConstructorElement expectedElement = ToConstructorElement(panelType);
            if (choice.element != expectedElement)
            {
                throw new ArgumentException("Residence facade plan contains a constructor choice for an unexpected element.", nameof(facadePlan));
            }
            if (choice.index < 0 || choice.index >= m_Constructors[panelType].Count)
            {
                throw new ArgumentOutOfRangeException(nameof(facadePlan), "Residence facade plan contains a constructor choice index outside the available constructor range.");
            }

            constructorChoiceIndex++;
            return Construct(m_Constructors[panelType][choice.index], width, height);
        }

        private static ILayoutElement Construct(List<Func<ILayoutElement>> constructors, float width, float height)
        {
            return Construct(constructors.GetRandom(), width, height);
        }

        private static ILayoutElement Construct(Func<ILayoutElement> constructor, float width, float height)
        {
            ILayoutElement element = constructor();
            element.width = width * element.widthScale;
            element.height = height * element.heightScale;
            return element;
        }

        private void AddRandomConstructorChoice(ResidenceFacadePlan.FacadePlan facadePlan, PanelType panelType)
        {
            AddConstructorChoice(facadePlan, panelType, CreateRandomConstructorIndex(panelType));
        }

        private void AddConstructorChoice(ResidenceFacadePlan.FacadePlan facadePlan, PanelType panelType, int index)
        {
            if (facadePlan == null)
            {
                throw new ArgumentNullException(nameof(facadePlan));
            }
            if (index < 0 || index >= m_Constructors[panelType].Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            facadePlan.constructorChoices.Add(new ResidenceFacadePlan.ConstructorChoice
            {
                element = ToConstructorElement(panelType),
                index = index
            });
        }

        private int CreateRandomConstructorIndex(PanelType panelType)
        {
            int count = m_Constructors[panelType].Count;
            return count <= 1 ? 0 : UnityEngine.Random.Range(0, count);
        }

        private static bool HasConstructorChoices(ResidenceFacadePlan.FacadePlan facadePlan)
        {
            return facadePlan?.constructorChoices != null && facadePlan.constructorChoices.Count > 0;
        }

        private static ResidenceFacadePlan.ConstructorChoice GetConstructorChoice(ResidenceFacadePlan.FacadePlan facadePlan, int index)
        {
            if (facadePlan.constructorChoices == null || index >= facadePlan.constructorChoices.Count)
            {
                throw new ArgumentException("Residence facade plan does not contain enough constructor choices.", nameof(facadePlan));
            }

            return facadePlan.constructorChoices[index];
        }

        private static void ValidateConstructorChoiceCount(ResidenceFacadePlan.FacadePlan facadePlan, int usedCount)
        {
            if (HasConstructorChoices(facadePlan) && usedCount != facadePlan.constructorChoices.Count)
            {
                throw new ArgumentException("Residence facade plan contains an unexpected number of constructor choices.", nameof(facadePlan));
            }
        }

        private static ConstructorElement ToConstructorElement(PanelType panelType)
        {
            return panelType switch
            {
                PanelType.k_Attic => ConstructorElement.Attic,
                PanelType.k_Entrance => ConstructorElement.Entrance,
                PanelType.k_Socle => ConstructorElement.Socle,
                PanelType.k_SocleTop => ConstructorElement.SocleTop,
                PanelType.k_Wall => ConstructorElement.Wall,
                PanelType.k_ShadowWall => ConstructorElement.ShadowWall,
                PanelType.k_Window => ConstructorElement.Window,
                PanelType.k_SeparatedLongCrossWindow => ConstructorElement.SeparatedLongCrossWindow,
                _ => throw new ArgumentOutOfRangeException(nameof(panelType), panelType, null)
            };
        }

        private enum PanelType : byte
        {
            k_Attic,
            k_Entrance,
            k_Socle,
            k_SocleTop,
            k_Wall,
            k_ShadowWall,
            k_Window,
            k_WallOrWindow,
            k_SeparatedLongCrossWindow,
        }
    }
}
