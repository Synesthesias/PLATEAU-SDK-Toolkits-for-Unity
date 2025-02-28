using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PlateauToolkit.Rendering.Editor
{

    public static class TextureScaler
    {
        public static void ResizeTextures(GameObject[] selectedGameObjects, float scale, string thresholdInput = "2048")
        {
            if (selectedGameObjects.Length == 0)
            {
                Debug.LogError("No GameObjects selected.");
                return;
            }

            int resizeThreshold;
            if (!int.TryParse(thresholdInput, out resizeThreshold))
            {
                Debug.LogError("Invalid threshold value entered.");
                return;
            }

            List<Texture2D> allTextures = new List<Texture2D>();
            foreach (GameObject selectedGameObject in selectedGameObjects)
            {
                MeshRenderer meshRenderer = selectedGameObject.GetComponent<MeshRenderer>();

                if (meshRenderer == null)
                {
                    Debug.LogError("The selected GameObject does not have a MeshRenderer: " + selectedGameObject.name);
                    continue;
                }

                Material[] materials = meshRenderer.sharedMaterials;

                foreach (Material material in materials)
                {
                    if (material.mainTexture != null)
                    {
                        allTextures.Add(material.mainTexture as Texture2D);
                    }
                }
            }

            List<Texture2D> uniqueTextures = new List<Texture2D>();
            foreach (Texture2D texture in allTextures)
            {
                if (!uniqueTextures.Contains(texture))
                {
                    uniqueTextures.Add(texture);
                }
            }

            Dictionary<Texture2D, Texture2D> resizedTextures = new Dictionary<Texture2D, Texture2D>();
            foreach (Texture2D uniqueTexture in uniqueTextures)
            {
                int resizedWidth = (int)(uniqueTexture.width * scale);
                int resizedHeight = (int)(uniqueTexture.height * scale);

                if (resizedWidth < resizeThreshold && resizedHeight < resizeThreshold)
                {
                    continue;
                }

                Texture2D resizedTexture = Resize(uniqueTexture, resizedWidth, resizedHeight);
                resizedTextures.Add(uniqueTexture, resizedTexture);
            }

            foreach (GameObject selectedGameObject in selectedGameObjects)
            {
                MeshRenderer meshRenderer = selectedGameObject.GetComponent<MeshRenderer>();

                if (meshRenderer == null)
                {
                    continue;
                }

                Material[] materials = meshRenderer.sharedMaterials;

                foreach (Material material in materials)
                {
                    if (material.mainTexture != null)
                    {
                        Texture2D originalTexture = material.mainTexture as Texture2D;
                        if (originalTexture != null)
                        {
                            Texture2D resizedTexture;
                            if (resizedTextures.TryGetValue(originalTexture, out resizedTexture))
                            {

                                if (selectedGameObject.name.Contains("auto_textured"))
                                {
                                    material.SetTexture("_BaseColorMap", resizedTexture);
                                }
                                else
                                {
                                    material.mainTexture = resizedTexture;
                                }
                            }
                        }
                    }
                }

                Debug.Log("Textures resized successfully for: " + selectedGameObject.name);
            }
        }

        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture current = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = current;
            Graphics.Blit(texture2D, current);
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.ARGB32, true);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Compress(true);
            result.Apply(false);

            RenderTexture.active = previous;
            current.Release();

            return result;
        }
    }
}