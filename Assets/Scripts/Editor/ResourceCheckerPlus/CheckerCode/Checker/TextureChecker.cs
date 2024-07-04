using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace ResourceCheckerPlus
{
    public class TextureChecker : ObjectChecker
    {
        public class TextureDetail : ObjectDetail
        {
            public TextureDetail(Object obj, TextureChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                var checker = currentChecker as TextureChecker;
                var tex = obj as Texture;
                if (tex == null)
                    return;
                var format = "Special";
                if (tex is Texture2D)
                {
                    var tex2D = tex as Texture2D;
                    format = tex2D.format + "\n" + tex2D.width + " x " + tex2D.height + " " + tex2D.mipmapCount + "mip ";
                }
                else if (tex is Cubemap)
                {
                    var texCube = tex as Cubemap;
                    format = texCube.format + "\n" + tex.width + " x " + tex.height + " x6 ";
                }

                var texImporter = TextureImporter.GetAtPath(assetPath) as TextureImporter;
                var mip = buildInType;
                var readable = buildInType;
                var type = buildInType;
                var npotScale = buildInType;
                var anisoLevel = 1;
                var texOriSize = 0;

                var androidOverride = buildInType;
                var androidMaxSize = 0;
                var androidFormat = buildInType;
                var androidCompressQuality = buildInType;

                var iosOverride = buildInType;
                var iosMaxSize = 0;
                var iosFormat = buildInType;
                var iosCompressQuality = buildInType;

#if UNITY_5_5_OR_NEWER
                var alpha = buildInType;
                var compression = buildInType;
#else           
                var alphaFromGray = buildInType;
                var alphaIsTran = buildInType;
                var sourceAlpha = buildInType;
                var importType = buildInType;
#endif
                if (texImporter)
                {
                    mip = texImporter.mipmapEnabled.ToString();
                    readable = texImporter.isReadable.ToString();
                    type = texImporter.textureType.ToString();
                    npotScale = texImporter.npotScale.ToString();
                    anisoLevel = texImporter.anisoLevel;
                    texOriSize = GetOriTextureSize(texImporter);
#if UNITY_5_5_OR_NEWER
                    var androidsettings = texImporter.GetPlatformTextureSettings(platformAndroid);
                    androidOverride = androidsettings.overridden.ToString();
                    androidMaxSize = androidsettings.maxTextureSize;
                    androidFormat = androidsettings.format.ToString();
                    androidCompressQuality = GetCompressionQuality(androidsettings.compressionQuality);

                    var iossettings = texImporter.GetPlatformTextureSettings(platformIOS);
                    iosOverride = iossettings.overridden.ToString();
                    iosMaxSize = iossettings.maxTextureSize;
                    iosFormat = iossettings.format.ToString();
                    iosCompressQuality = GetCompressionQuality(iossettings.compressionQuality);

                    alpha = texImporter.alphaSource.ToString();
                    compression = texImporter.textureCompression.ToString();
#else
                    TextureImporterFormat androidImportFormat;
                    int androidImportCompressionQa;
                    androidOverride = texImporter.GetPlatformTextureSettings(platformAndroid, out androidMaxSize, out androidImportFormat, out androidImportCompressionQa).ToString();
                    androidFormat = androidImportFormat.ToString();
                    androidCompressQuality = GetCompressionQuality(androidImportCompressionQa);

                    TextureImporterFormat iosImportFormat;
                    int iosImportCompressionQa;
                    iosOverride = texImporter.GetPlatformTextureSettings(platformIOS, out iosMaxSize, out iosImportFormat, out iosImportCompressionQa).ToString();
                    iosFormat = iosImportFormat.ToString();
                    iosCompressQuality = GetCompressionQuality(iosImportCompressionQa);

                    alphaFromGray = texImporter.grayscaleToAlpha.ToString();
                    alphaIsTran = texImporter.alphaIsTransparency.ToString();
                    //5.5之前可以用
                    sourceAlpha = texImporter.DoesSourceTextureHaveAlpha().ToString();
                    importType = texImporter.normalmap ? "NormalMap" : (texImporter.lightmap ? "LightMap" : "Default");
#endif
                }

                var size = Mathf.Max(tex.width, tex.height);
                var isSquare = tex.width == tex.height;
                var isPoworOfTwo = TextureIsPowerOfTwo(tex);
                var postfix = ResourceCheckerHelper.GetAssetPostfix(assetPath);
                AddOrSetCheckValue(checker.texFormat, format);
                AddOrSetCheckValue(checker.texMipmap, mip);
                AddOrSetCheckValue(checker.texReadable, readable);
                AddOrSetCheckValue(checker.texType, type);
                AddOrSetCheckValue(checker.texSize, size);
                AddOrSetCheckValue(checker.texPostfix, postfix);
                AddOrSetCheckValue(checker.texAnisoLevel, anisoLevel);
                AddOrSetCheckValue(checker.texIsSquareMap, isSquare.ToString());
                AddOrSetCheckValue(checker.texNonPowerOfTwo, isPoworOfTwo.ToString());
                AddOrSetCheckValue(checker.texNpotScale, npotScale);
                AddOrSetCheckValue(checker.texWrapMode, tex.wrapMode.ToString());
                AddOrSetCheckValue(checker.texFilterMode, tex.filterMode.ToString());
                AddOrSetCheckValue(checker.texOriSize, texOriSize);
                //编辑器下获得的贴图2倍大小，覆写默认大小，除以2
                var texMemSize = (int)GetCheckValue(checker.memorySizeItem) / 2;
                AddOrSetCheckValue(checker.memorySizeItem, texMemSize);

                AddOrSetCheckValue(checker.texAndroidOverride, androidOverride);
                AddOrSetCheckValue(checker.texAndroidMaxSize, androidMaxSize);
                AddOrSetCheckValue(checker.texAndroidFormat, androidFormat);
                AddOrSetCheckValue(checker.texAndroidCompressQuality, androidCompressQuality);

                AddOrSetCheckValue(checker.texIOSOverride, iosOverride);
                AddOrSetCheckValue(checker.texIOSMaxSize, iosMaxSize);
                AddOrSetCheckValue(checker.texIOSFormat, iosFormat);
                AddOrSetCheckValue(checker.texIOSCompressQuality, iosCompressQuality);
#if UNITY_5_5_OR_NEWER
                AddOrSetCheckValue(checker.texAlpha, alpha);
                AddOrSetCheckValue(checker.texCompression, compression);
#else
                AddOrSetCheckValue(checker.texAlphaFromGray, alphaFromGray);
                AddOrSetCheckValue(checker.texAlphaIsTransparent, alphaIsTran);
                AddOrSetCheckValue(checker.texSourceAlpha, sourceAlpha);
                AddOrSetCheckValue(checker.texImportType, importType);
#endif
            }

            #region 辅助函数
            int CalculateTextureSizeBytes(Texture tTexture)
            {
                if (tTexture == null)
                    return 0;
                int tWidth = tTexture.width;
                int tHeight = tTexture.height;
                if (tTexture is Texture2D)
                {
                    Texture2D tTex2D = tTexture as Texture2D;
                    int bitsPerPixel = GetBitsPerPixel(tTex2D.format);
                    int mipMapCount = tTex2D.mipmapCount;
                    int mipLevel = 1;
                    int tSize = 0;
                    while (mipLevel <= mipMapCount)
                    {
                        tSize += tWidth * tHeight * bitsPerPixel / 8;
                        tWidth = tWidth / 2;
                        tHeight = tHeight / 2;
                        mipLevel++;
                    }
                    return tSize;
                }

                if (tTexture is Cubemap)
                {
                    Cubemap tCubemap = tTexture as Cubemap;
                    int bitsPerPixel = GetBitsPerPixel(tCubemap.format);
                    return tWidth * tHeight * 6 * bitsPerPixel / 8;
                }
                return 0;
            }

            int GetBitsPerPixel(TextureFormat format)
            {
                switch (format)
                {
                    case TextureFormat.Alpha8: //	 Alpha-only texture format.
                        return 8;
                    case TextureFormat.ARGB4444: //	 A 16 bits/pixel texture format. Texture stores color with an alpha channel.
                        return 16;
                    case TextureFormat.RGBA4444: //	 A 16 bits/pixel texture format.
                        return 16;
                    case TextureFormat.RGB24:   // A color texture format.
                        return 24;
                    case TextureFormat.RGBA32:  //Color with an alpha channel texture format.
                        return 32;
                    case TextureFormat.ARGB32:  //Color with an alpha channel texture format.
                        return 32;
                    case TextureFormat.RGB565:  //	 A 16 bit color texture format.
                        return 16;
                    case TextureFormat.DXT1:    // Compressed color texture format.
                        return 4;
                    case TextureFormat.DXT5:    // Compressed color with alpha channel texture format.
                        return 8;
                    case TextureFormat.PVRTC_RGB2://	 PowerVR (iOS) 2 bits/pixel compressed color texture format.
                        return 2;
                    case TextureFormat.PVRTC_RGBA2://	 PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
                        return 2;
                    case TextureFormat.PVRTC_RGB4://	 PowerVR (iOS) 4 bits/pixel compressed color texture format.
                        return 4;
                    case TextureFormat.PVRTC_RGBA4://	 PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
                        return 4;
                    case TextureFormat.ETC_RGB4://	 ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
                        return 4;
                    case TextureFormat.ETC2_RGBA8://	 ATC (ATITC) 8 bits/pixel compressed RGB texture format.
                        return 8;
                    case TextureFormat.BGRA32://	 Format returned by iPhone camera
                        return 32;
                    case TextureFormat.ETC2_RGBA1:
                        return 4;
                }
                return 0;
            }

            public bool TextureIsPowerOfTwo(Texture tex)
            {
                if (tex == null)
                    return true;
                if (isPowerOfTwo(tex.width) && isPowerOfTwo(tex.height))
                    return true;
                return false;
            }

            private bool isPowerOfTwo(int num)
            {
                return ((num & (num - 1)) == 0);
            }

            private string GetCompressionQuality(int quality)
            {
                if (quality == 50)
                    return "Normal";
                return quality == 0 ? "Fast" : "Best";
            }

            private int GetOriTextureSize(TextureImporter importer)
            {
                object[] args = new object[2] { 0, 0 };
                MethodInfo method = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(importer, args);
                return Mathf.Max((int)args[0], (int)args[1]);
            }

            #endregion
        }

        CheckItem texFormat;
        public CheckItem texMipmap;
        CheckItem texReadable;
        CheckItem texType;
        CheckItem texSize;

        CheckItem texPostfix;
        CheckItem texNonPowerOfTwo;
        CheckItem texNpotScale;
        CheckItem texIsSquareMap;
        CheckItem texFilterMode;
        CheckItem texWrapMode;
        CheckItem texAnisoLevel;
        CheckItem texOriSize;

        CheckItem texAndroidOverride;
        CheckItem texAndroidMaxSize;
        CheckItem texAndroidFormat;
        CheckItem texAndroidCompressQuality;

        CheckItem texIOSOverride;
        CheckItem texIOSMaxSize;
        CheckItem texIOSFormat;
        CheckItem texIOSCompressQuality;
#if UNITY_5_5_OR_NEWER
        CheckItem texAlpha;
        CheckItem texCompression;
#else
        CheckItem texAlphaFromGray;
        CheckItem texAlphaIsTransparent;
         //这个属性，在5.5之后的版本之后不管有没有都是true....暂时只在之前的版本上检查吧
        CheckItem texSourceAlpha;
        CheckItem texImportType;
#endif

        public CheckItem textureSingleSize;
        public CheckItem textureTotalSize;

        public override void InitChecker()
        {
            checkerName = "Texture";
            checkerFilter = "t:Texture";
            enableReloadCheckItem = true;
            texFormat = new CheckItem(this, "格式");
            texMipmap = new CheckItem(this, "Mip");
            texReadable = new CheckItem(this, "Readable");
            texType = new CheckItem(this, "类型");
            texSize = new CheckItem(this, "贴图大小", CheckType.Int, null);
            
            texPostfix = new CheckItem(this, "后缀名");
#if UNITY_5_5_OR_NEWER
            texAlpha = new CheckItem(this, "Alpha");
            texCompression = new CheckItem(this, "纹理压缩");
#else
            texAlphaFromGray = new CheckItem(this, "AlphaFromGray");
            texAlphaIsTransparent = new CheckItem(this, "AlphaIsTran");
            texSourceAlpha = new CheckItem(this, "原始图片Alpha");
            texImportType = new CheckItem(this, "贴图导入类型");
#endif
            texIsSquareMap = new CheckItem(this, "正方形贴图");
            texNonPowerOfTwo = new CheckItem(this, "2次幂贴图");
            texNpotScale = new CheckItem(this, "NonPower Of 2");
            texWrapMode = new CheckItem(this, "WrapMode");
            texFilterMode = new CheckItem(this, "FilterMode");
            texAnisoLevel = new CheckItem(this, "AnisoLevel", CheckType.Int);
            texOriSize = new CheckItem(this, "源图大小", CheckType.Int);
            texAndroidOverride = new CheckItem(this, "安卓开启");
            texAndroidMaxSize = new CheckItem(this, "安卓MaxSize", CheckType.Int);
            texAndroidFormat = new CheckItem(this, "安卓Format");
            texAndroidCompressQuality = new CheckItem(this, "安卓压缩质量");
            texIOSOverride = new CheckItem(this, "IOS开启");
            texIOSMaxSize = new CheckItem(this, "IOSMaxSize", CheckType.Int);
            texIOSFormat = new CheckItem(this, "IOSFormat");
            texIOSCompressQuality = new CheckItem(this, "IOS压缩质量");

            textureSingleSize = new CheckItem(this, "纹理总内存占用", CheckType.FormatSize, null, null, ItemFlag.CheckSummary | ItemFlag.SceneCheckInfo);
            textureTotalSize = new CheckItem(this, "纹理*引用数总内存占用", CheckType.FormatSize, null, null, ItemFlag.CheckSummary);

        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var texture = obj as Texture;
            if (texture == null)
                return null;
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == texture)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new TextureDetail(texture, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void CheckDetailSummary()
        {
            base.CheckDetailSummary();

            int totalTextureSummarySingle = 0;
            foreach (var texDetail in FilterList)
            {
                object obj = texDetail.GetCheckValue(memorySizeItem);
                int value = obj == null ? 0 : (int)obj;
                totalTextureSummarySingle += value;
            }
            int totalTextureSummary = 0;
            foreach (var texDetail in FilterList)
            {
                object obj = texDetail.GetCheckValue(memorySizeItem);
                int value = obj == null ? 0 : (int)obj;
                totalTextureSummary += value * texDetail.referenceObjectList.Count;
            }
            checkResultDic[textureSingleSize] = totalTextureSummarySingle;
            checkResultDic[textureTotalSize] = totalTextureSummary;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            var renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    var obj = EditorUtility.CollectDependencies(new Object[] { mat });
                    foreach (var o in obj)
                    {
                        AddObjectWithRef(o, r.gameObject, rootObj);
                    }
                }
            }
        }

        public override void BatchSetResConfig()
        {
            BatchTextureSettingEditor.Init(GetBatchOptionList());
        }
    }
}
