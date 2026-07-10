using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime
{
    [Serializable]
    public class ResidenceFacadePlan
    {
        public float buildingWidth;
        public float buildingDepth;
        public int numFloor;
        public List<FacadePlan> facades = new();

        public bool Matches(float width, float depth, int floor)
        {
            return Mathf.Approximately(buildingWidth, width) &&
                Mathf.Approximately(buildingDepth, depth) &&
                numFloor == floor;
        }

        public ResidenceFacadePlan Clone()
        {
            var copy = new ResidenceFacadePlan
            {
                buildingWidth = buildingWidth,
                buildingDepth = buildingDepth,
                numFloor = numFloor
            };

            if (facades == null)
            {
                return copy;
            }

            foreach (FacadePlan facade in facades)
            {
                if (facade != null)
                {
                    copy.facades.Add(facade.Clone());
                }
            }

            return copy;
        }

        [Serializable]
        public class FacadePlan
        {
            public List<PanelSize> panelSizes = new();
            public List<WallOrWindowElement> wallOrWindowElements = new();
            public List<ConstructorChoice> constructorChoices = new();

            public FacadePlan Clone()
            {
                return new FacadePlan
                {
                    panelSizes = panelSizes == null ? new List<PanelSize>() : new List<PanelSize>(panelSizes),
                    wallOrWindowElements = wallOrWindowElements == null ? new List<WallOrWindowElement>() : new List<WallOrWindowElement>(wallOrWindowElements),
                    constructorChoices = CloneConstructorChoices()
                };
            }

            private List<ConstructorChoice> CloneConstructorChoices()
            {
                var choices = new List<ConstructorChoice>();
                if (constructorChoices == null)
                {
                    return choices;
                }

                foreach (ConstructorChoice choice in constructorChoices)
                {
                    choices.Add(choice.Clone());
                }

                return choices;
            }
        }

        [Serializable]
        public class ConstructorChoice
        {
            public ConstructorElement element;
            public int index;

            public ConstructorChoice Clone()
            {
                return new ConstructorChoice
                {
                    element = element,
                    index = index
                };
            }
        }

        public enum PanelSize : byte
        {
            Narrow,
            Wide,
        }

        public enum WallOrWindowElement : byte
        {
            Wall,
            Window,
        }

        public enum ConstructorElement : byte
        {
            Attic,
            Entrance,
            Socle,
            SocleTop,
            Wall,
            ShadowWall,
            Window,
            SeparatedLongCrossWindow,
        }
    }
}
