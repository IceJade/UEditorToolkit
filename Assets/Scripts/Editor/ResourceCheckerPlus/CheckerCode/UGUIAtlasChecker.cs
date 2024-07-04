using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using SystemObject = System.Object;

namespace ResourceCheckerPlus
{
    public class SpriteAtlasChecker : ObjectChecker
    {
        public class SpriteAtlasDetail : ObjectDetail
        {
            public SpriteAtlasDetail(Object obj, SpriteAtlasChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as SpriteAtlasChecker;
            }
        }

        public override void InitChecker()
        {
            checkerName = "UGUIAtlas";
            InitReflectionMethod();
            enableReloadCheckItem = false;
            nameItem.clickOption = OnObjectButtonClick;
        }

        System.Type spritePacker = null;
        PropertyInfo atlasNames = null;
        MethodInfo getTexturesForAtlas = null;
        MethodInfo rebuildAtlas = null;
        private bool showCustomPreview = true;
       // private BuildTarget atlasBuildTarget = EditorUserBuildSettings.activeBuildTarget;

        private void InitReflectionMethod()
        {
            spritePacker = ResourceCheckerAssemblyHelper.GetTypeWithinLoadedAssemblies("UnityEditor.Sprites.Packer");
            if (spritePacker != null)
            {
                atlasNames = spritePacker.GetProperty("atlasNames", BindingFlags.Static | BindingFlags.Public);
                getTexturesForAtlas = spritePacker.GetMethod("GetTexturesForAtlas", BindingFlags.Static | BindingFlags.Public);
                var paramType = new System.Type[2];
                paramType[0] = typeof(BuildTarget);
                paramType[1] = typeof(System.Boolean);
                rebuildAtlas = spritePacker.GetMethod("RebuildAtlasCacheIfNeeded",  paramType);
            }
        }

        public string[] GetAtlasNames()
        {
            return (string[])atlasNames.GetValue(null, null);
        }

        public Texture2D[] GetTexturesForAtlas(string name)
        {
            return (Texture2D[])getTexturesForAtlas.Invoke(null, new string[] { name });
        }

        public void RebuildAtlas()
        {
            rebuildAtlas.Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget, true});
        }

        public void AddObjectDetail(string atlasName, Texture2D[] textures)
        {
            var tex = textures == null || textures.Length < 1 ? null : textures[0];
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == tex)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new SpriteAtlasDetail(tex, this);
                detail.assetName = atlasName;
            }
        }

        public override void DirectResCheck(Object[] selection)
        {
            RebuildAtlas();
            var atlasNames = GetAtlasNames();
            foreach(var atlas in atlasNames)
            {
                var textures = GetTexturesForAtlas(atlas);
                AddObjectDetail(atlas, textures);
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            showCustomPreview = GUILayout.Toggle(showCustomPreview, "显示预览");
           // atlasBuildTarget = (BuildTarget)EditorGUILayout.EnumFlagsField("图集平台",atlasBuildTarget);
        }

        private void OnObjectButtonClick(ObjectDetail detail)
        {
            var texture = detail.checkObject as Texture2D;
            if (showCustomPreview == true)
            {
                CustomPreviewWindow.SetPreviewTexture(texture);
            }
        }

        public static string[] Export(SpriteAtlas atlas, string dirPath)
        {
            string platformName = "Standalone";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                platformName = "Android";
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                platformName = "iPhone";
            }

            TextureImporterPlatformSettings tips = atlas.GetPlatformSettings(platformName);
            TextureImporterPlatformSettings cachedTips = new TextureImporterPlatformSettings();
            tips.CopyTo(cachedTips);

            tips.overridden = true;
            tips.format = TextureImporterFormat.RGBA32;
            atlas.SetPlatformSettings(tips);

            List<string> texturePathList = new List<string>();

            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            MethodInfo getPreviewTextureMI = typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures", BindingFlags.Static | BindingFlags.NonPublic);
            Texture2D[] atlasTextures = (Texture2D[])getPreviewTextureMI.Invoke(null, new SystemObject[] { atlas });
            if (atlasTextures != null && atlasTextures.Length > 0)
            {
                for (int i = 0; i < atlasTextures.Length; i++)
                {
                    Texture2D packTexture = atlasTextures[i];
                    byte[] rawBytes = packTexture.GetRawTextureData();

                    Texture2D nTexture = new Texture2D(packTexture.width, packTexture.height, packTexture.format, false, false);
                    nTexture.LoadRawTextureData(rawBytes);
                    nTexture.Apply();
                    string textPath = string.Format("{0}/{1}_{2}.png", dirPath, atlas.name, i);
                    File.WriteAllBytes(textPath, nTexture.EncodeToPNG());

                    texturePathList.Add(textPath);
                }
            }

            atlas.SetPlatformSettings(cachedTips);

            return texturePathList.ToArray();
        }


        [MenuItem("Game/UI/Atlas/SpriteAtlas Exporter")]
        private static void ExportAltas()
        {
            List<SpriteAtlas> atlasList = new List<SpriteAtlas>();
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj.GetType() == typeof(SpriteAtlas))
                    {
                        atlasList.Add(obj as SpriteAtlas);
                    }
                }
            }

            if (atlasList.Count == 0)
            {
                EditorUtility.DisplayDialog("Tips", "Please Selected SpriteAtlas", "OK");
                return;
            }

            string dirPath = EditorUtility.OpenFolderPanel("Save Dir", "D:/", "");
            if (string.IsNullOrEmpty(dirPath))
            {
                EditorUtility.DisplayDialog("Tips", "Please Selected a folder", "OK");
                return;
            }

            foreach (var atlas in atlasList)
            {
                Export(atlas, dirPath);
            }

            //ExplorerUtil.OpenExplorerFolder(dirPath);
        }
    }
}


