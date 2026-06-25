using System.IO;
using UnityEditor;
using UnityEngine;

namespace RaftProto.Editor
{
    public static class ItemIconRendererUtility
    {
        public static Texture2D RenderPrefabIcon(
            GameObject prefab,
            int resolution,
            Vector3 rotationEuler,
            float padding,
            Color backgroundColor)
        {
            if (prefab == null)
            {
                return null;
            }

            PreviewRenderUtility preview = new PreviewRenderUtility();
            GameObject instance = null;

            try
            {
                instance = preview.InstantiatePrefabInScene(prefab);
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.Euler(rotationEuler);

                Bounds bounds = CalculateRenderBounds(instance);
                instance.transform.position = -bounds.center;
                bounds = CalculateRenderBounds(instance);

                float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float orthoSize = Mathf.Max(maxSize * 0.5f * padding, 0.01f);

                Camera camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backgroundColor;
                camera.orthographic = true;
                camera.orthographicSize = orthoSize;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = maxSize * 10f + 10f;
                camera.aspect = 1f;
                camera.transform.position = bounds.center + new Vector3(0.35f, 0.25f, -1f).normalized * (maxSize * 2f + 1f);
                camera.transform.LookAt(bounds.center);

                if (preview.lights != null && preview.lights.Length > 0)
                {
                    preview.lights[0].intensity = 1.35f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);

                    if (preview.lights.Length > 1)
                    {
                        preview.lights[1].intensity = 0.85f;
                        preview.lights[1].transform.rotation = Quaternion.Euler(315f, 215f, 0f);
                    }
                }

                RenderTexture renderTarget = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTarget;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTarget;

                Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                texture.Apply();

                RenderTexture.active = previous;
                renderTarget.Release();
                Object.DestroyImmediate(renderTarget);

                return texture;
            }
            finally
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                preview.Cleanup();
            }
        }

        public static string SaveIconTexture(Texture2D texture, string outputFolder, string fileName)
        {
            if (texture == null)
            {
                return null;
            }

            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                CreateFolderRecursive(outputFolder);
            }

            string assetPath = Path.Combine(outputFolder, fileName).Replace('\\', '/');
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImport(assetPath);
            return assetPath;
        }

        private static void ConfigureSpriteImport(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void CreateFolderRecursive(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolderRecursive(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Bounds CalculateRenderBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one * 0.5f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}
