// UGUI资源检查
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ResourceCheckerPlus
{
    public class UGUIChecker : ObjectChecker
    {
        public class UGUIDetail : ObjectDetail
        {
           
            public const string buildInResStr = "Build In Res";
            public const string nonAtalasStr = "None Atlas";
            public UGUIDetail(Object obj, UGUIChecker checker) : base(obj, checker)
            {
                var tag = GetAtlasTag(assetPath);
                AddOrSetCheckValue(checker.spriteAtlasName, tag);
                if (checker.currentMod == UGUICheckerMod.Atlas)
                {
                    assetName = tag != buildInResStr && tag != nonAtalasStr ? tag : assetName;
                    AddOrSetCheckValue(checker.nameItem, assetName);
                }
                    
                AddOrSetCheckValue(checker.uiComType, "");
                AddOrSetCheckValue(checker.selectChildTextComponent, "选中子节点中文本");
                AddOrSetCheckValue(checker.isMissingSprite, "None");
                AddOrSetCheckValue(checker.atlasReferenceSpriteCount, 0);
            }

            public static string GetAtlasTag(string assetPath)
            {
                var texImporter = TextureImporter.GetAtPath(assetPath) as TextureImporter;
                var tag = texImporter == null ? buildInResStr : texImporter.spritePackingTag;
                tag = string.IsNullOrEmpty(tag) ? nonAtalasStr : tag;
                return tag;
            }

            public void AddSubDetail(Object obj, Object refObj, Object detailRefObj)
            {
                UGUISubDetail subDetail = null;
                foreach(var sub in subDetailList)
                {
                    if (sub.detailObject == obj)
                    {
                        subDetail = sub;
                    }
                }
                if (subDetail == null)
                {
                    subDetail = new UGUISubDetail();
                    subDetail.detailObject = obj;
                    subDetailList.Add(subDetail);
                }
                if (refObj != null && !subDetail.referenceList.Contains(refObj))
                {
                    subDetail.referenceList.Add(refObj);
                }
                if (detailRefObj != null && !subDetail.detailReferenceList.Contains(detailRefObj))
                {
                    subDetail.detailReferenceList.Add(detailRefObj);
                }
                
            }

            public Texture uiTexture;
            public List<UGUISubDetail> subDetailList = new List<UGUISubDetail>();
            public bool showChildren = false;
        }

        public class UGUISubDetail
        {
            public Object detailObject;
            public List<Object> referenceList = new List<Object>();
            public List<Object> detailReferenceList = new List<Object>();
        }

        public enum UGUICheckerMod
        {
            Sprite,
            Atlas,
        }

        public string[] checkModStr = new string[] { "散图模式", "图集模式" };

        CheckItem uiComType;
        CheckItem spriteAtlasName;
        CheckItem isMissingSprite;
        CheckItem atlasReferenceSpriteCount;

        CheckItem selectChildTextComponent;

        public UGUICheckerMod currentMod = UGUICheckerMod.Atlas;

        public override void InitChecker()
        {
            checkerName = "UGUIChecker";
            checkerFilter = "t:Prefab";
            isSpecialChecker = true;
           
            spriteAtlasName = new CheckItem(this, "图集名称");
            
            atlasReferenceSpriteCount = new CheckItem(this, "图集引用Sprite数量", CheckType.Int, OnSpriteCountButtonClick);
            isMissingSprite = new CheckItem(this, "空或Missing状态");
            uiComType = new CheckItem(this, "类型");
            selectChildTextComponent = new CheckItem(this, "选中子节点中文本", CheckType.String, OnSelectChildTextButtonClick);

            //atlasReferenceSpriteCount.itemFlag |= ItemFlag.NoCustomShow;
            RefreshUGUIShowItemConfig();
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var unityObject = obj as Object;
            if (currentMod == UGUICheckerMod.Atlas)
                return AddDetailAtlasCheckMod(unityObject, refObj, detailRefObj);
            else // (currentMod == UGUICheckerMod.Sprite)
                return AddDetailSpriteCheckMod(unityObject, refObj, detailRefObj);
        }

        private ObjectDetail AddDetailSpriteCheckMod(Object obj, Object refObj, Object detailRefObj)
        {
            if (obj is Texture || obj is Sprite)
            {
                UGUIDetail detail = null;
                foreach (var v in CheckList)
                {
                    if (v.checkObject == obj)
                        detail = v as UGUIDetail;
                }
                if (detail == null)
                    detail = new UGUIDetail(obj, this);
                detail.AddObjectReference(refObj, detailRefObj);
                return detail;
            }
            return null;
        }

        private ObjectDetail AddDetailAtlasCheckMod(Object obj, Object refObj, Object detailRefObj)
        {
            if (obj is Texture || obj is Sprite)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                var tag = UGUIDetail.GetAtlasTag(assetPath);
                UGUIDetail detail = null;
                foreach (var v in CheckList)
                {
                    if (v.checkObject != null)
                    {
                        if ((tag == UGUIDetail.buildInType || tag == UGUIDetail.nonAtalasStr) && v.checkObject == obj)
                            detail = v as UGUIDetail;
                        else if (v.GetCheckValue(spriteAtlasName).ToString() == tag && (tag != UGUIDetail.buildInType && tag != UGUIDetail.nonAtalasStr))
                            detail = v as UGUIDetail;
                    }
                }
                if (detail == null)
                    detail = new UGUIDetail(obj, this);
                detail.AddObjectReference(refObj, detailRefObj);
                detail.AddSubDetail(obj, refObj, detailRefObj);
                detail.AddOrSetCheckValue(atlasReferenceSpriteCount, detail.subDetailList.Count);
                return detail;
            }
            return null;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            AddRawImageReferenceRes(rootObj);
            AddImageReferenceRes(rootObj);
        }

        private void AddRawImageReferenceRes(GameObject rootObj)
        {
            var coms = rootObj.GetComponentsInChildren<RawImage>(true);
            foreach (var v in coms)
            {
                if (v.texture != null)
                {
                    var detail = AddObjectWithRef(v.texture, v.gameObject, rootObj);
                    SetUIResType(detail, "Raw-Image");
                }
                else
                {
                    //TODO:
                    var state = ResourceCheckerHelper.GetInvalidPropertyState(v, "");
                    var detail = AddNullOrMissingSprite(state, rootObj, v.gameObject);
                    SetUIResType(detail, "Raw-Image");
                }
                
                if (v.material != null)
                {
                    var refTextures = GetMaterialReferenceTextures(v.material);
                    foreach (var tex in refTextures)
                    {
                        if (tex != null)
                        {
                            var detail = AddObjectWithRef(tex, v.gameObject, rootObj);
                            SetUIResType(detail, "Raw-Material");
                        }
                    }
                }
            }
        }

        private void OnSpriteCountButtonClick(ObjectDetail detail)
        {
            var ud = detail as UGUIDetail;
            ud.showChildren = !ud.showChildren;
        }

        private void OnSelectChildTextButtonClick(ObjectDetail detail)
        {
            var ud = detail as UGUIDetail;
            var targetObjects = checkModule is SceneResCheckModule ? ud.referenceObjectList : ud.detailReferenceList;
            var list = new List<Object>();
            foreach(var obj in targetObjects)
            {
                var go = obj as GameObject;
                if (go == null)
                    continue;
                var texts = go.GetComponentsInChildren<Text>(true);
                foreach(var text in texts)
                {
                    if (list.Contains(text.gameObject))
                        continue;
                    list.Add(text.gameObject);
                }
            }
            SelectObjects(list);
        }

        public override void ShowChildDetail(ObjectDetail detail)
        {
            var ud = detail as UGUIDetail;
            if (ud.showChildren == true)
            {
                foreach(var child in ud.subDetailList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(65);
                    if (GUILayout.Button(child.detailObject.name, GUILayout.Width(245)))
                    {
                        SelectObject(child.detailObject);
                    }
                    if (GUILayout.Button(child.referenceList.Count.ToString(), GUILayout.Width(50)))
                    {
                        SelectObjects(child.referenceList);
                        checkModule.AddObjectToSideBarList(child.referenceList);
                    }
                    if (checkModule is SceneResCheckModule == false)
                    {
                        if (GUILayout.Button(child.detailReferenceList.Count.ToString(), GUILayout.Width(50)))
                        {
                            SelectObjects(child.detailReferenceList);
                            checkModule.AddObjectToSideBarList(child.detailReferenceList);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            EditorGUI.BeginChangeCheck();
            currentMod = (UGUICheckerMod)GUILayout.Toolbar((int)currentMod, checkModStr, GUILayout.Width(300));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshUGUIShowItemConfig();
            }
        }

        private void RefreshUGUIShowItemConfig()
        {
            atlasReferenceSpriteCount.show = currentMod != UGUICheckerMod.Sprite;
            selectChildTextComponent.show = currentMod == UGUICheckerMod.Sprite;
        }

        private void AddImageReferenceRes(GameObject rootObj)
        {
            var coms = rootObj.GetComponentsInChildren<Image>(true);
            foreach (var v in coms)
            {
                if (v.sprite != null)
                {
                    var detail = AddObjectWithRef(v.sprite.texture, v.gameObject, rootObj);
                    SetUIResType(detail, "Image-Image");
                }
                else
                {
                    var state = ResourceCheckerHelper.GetInvalidPropertyState(v, "m_Sprite");
                    var detail = AddNullOrMissingSprite(state, rootObj, v.gameObject);
                    SetUIResType(detail, "Image-Image");
                }
                if (v.material != null)
                {
                    var refTextures = GetMaterialReferenceTextures(v.material);
                    foreach (var tex in refTextures)
                    {
                        if (tex != null)
                        {
                            var detail = AddObjectWithRef(tex, v.gameObject, rootObj);
                            SetUIResType(detail, "Image-Material");
                        }
                    }
                }
            }
        }

        private Object[] GetMaterialReferenceTextures(Material material)
        {
            return EditorUtility.CollectDependencies(new Object[] { material }).Where(x => x is Texture2D).ToArray();
        }

        public override void RefreshCheckResult()
        {
            base.RefreshCheckResult();
            CheckDetailSort(spriteAtlasName, true);
        }

        private void CustomTextureCheckerDetailRefDelegate(GameObject rootObj, ObjectChecker checker)
        {
            var coms = rootObj.GetComponentsInChildren<RawImage>(true);
            foreach (var v in coms)
            {
                if (v.texture != null)
                {
                    checker.AddObjectWithRef(v.texture, v.gameObject, rootObj);
                }
                if (v.material != null)
                {
                    var refTextures = GetMaterialReferenceTextures(v.material);
                    foreach (var tex in refTextures)
                    {
                        if (tex != null)
                        {
                            checker.AddObjectWithRef(tex, v.gameObject, rootObj);
                        }
                    }
                }
            }

            var imgcoms = rootObj.GetComponentsInChildren<Image>(true);
            foreach (var v in imgcoms)
            {
                if (v.sprite != null && v.sprite.texture != null)
                {
                    checker.AddObjectWithRef(v.sprite.texture, v.gameObject, rootObj);
                }
                if (v.material != null)
                {
                    var refTextures = GetMaterialReferenceTextures(v.material);
                    foreach (var tex in refTextures)
                    {
                        if (tex != null)
                        {
                            checker.AddObjectWithRef(tex, v.gameObject, rootObj);
                        }
                    }
                }
            }
        }

        public override void PostInitChecker()
        {
            base.PostInitChecker();
            ResourceCheckerPlus.instance.RegistCustomCheckObjectDetailRefDelegate<TextureChecker>(CustomTextureCheckerDetailRefDelegate);
        }

        private void SetUIResType(ObjectDetail detail, string type)
        {
            var uguiDetail = detail as UGUIDetail;
            if (uguiDetail == null)
                return;
            var value = uguiDetail.GetCheckValue(uiComType).ToString();
            if (!value.Contains(type))
            {
                value += " " + type;
                uguiDetail.AddOrSetCheckValue(uiComType, value);
            }

        }

        private UGUIDetail AddNullOrMissingSprite(PropertyState state, GameObject rootGo, GameObject refGo)
        {
            UGUIDetail detail = null;
            foreach(var v in CheckList)
            {
                if (v.checkObject == null && v.GetCheckValue(isMissingSprite).ToString() == state.ToString())
                    detail = v as UGUIDetail;
            }
            if (detail == null)
            {
                detail = new UGUIDetail(null, this);
                detail.GetCheckValueItem(isMissingSprite).warningLevel = state == PropertyState.Missing ? ResourceWarningLevel.FatalError : ResourceWarningLevel.Warning;
                detail.warningLevel = state == PropertyState.Missing ? ResourceWarningLevel.FatalError : ResourceWarningLevel.Warning;
                detail.resourceWarningTips = state == PropertyState.Missing ? "Missing资源" : "空资源引用";
                detail.flag |= ObjectDetailFlag.Warning;
                detail.AddOrSetCheckValue(isMissingSprite, state.ToString());
                detail.AddOrSetCheckValue(spriteAtlasName, UGUIDetail.nonAtalasStr);
            }
            if (checkModule is SceneResCheckModule)
            {
                detail.AddObjectReference(refGo, null);
            }
            else
            {
                detail.AddObjectReference(rootGo, refGo);
            }
            
            return detail;
        }
    }
}