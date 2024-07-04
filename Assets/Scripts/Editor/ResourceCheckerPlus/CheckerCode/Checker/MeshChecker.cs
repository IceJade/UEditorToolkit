using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace ResourceCheckerPlus
{
    public class MeshChecker : ObjectChecker
    {
        public class SubMeshData
        {
            public string name;
            public string format;
            public int vertexCount;
            public int tranCount;
            public Object meshObject;
            public List<Object> refCount = new List<Object>();
        }

        public class MeshDetail : ObjectDetail
        {
            public MeshDetail(Object obj, MeshChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                MeshChecker checker = currentChecker as MeshChecker;
                Mesh mesh = obj as Mesh;
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                //Mesh的object直接指向FBX根物体
                checkObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                AddOrSetCheckValue(checker.previewItem, AssetPreview.GetMiniThumbnail(checkObject));
                string readable = buildInType;
                string compression = buildInType;
                string tangent = buildInType;
                string optimize = buildInType;
                string normal = buildInType;
                string blendshape = buildInType;
                string animation = buildInType;
                string genCollider = buildInType;
                string keepQuads = buildInType;
                string swapUVs = buildInType;
                string generateLightMapUVs = buildInType;
                float scale = 1.0f;
                if (importer != null)
                {
                    readable = importer.isReadable.ToString();
                    optimize = importer.optimizeMesh.ToString();
                    blendshape = importer.importBlendShapes.ToString();
                    animation = importer.animationType.ToString();
                    normal = importer.importNormals.ToString();
                    tangent = importer.importTangents.ToString();
                    compression = importer.meshCompression.ToString();
                    genCollider = importer.addCollider.ToString();
                    swapUVs = importer.swapUVChannels.ToString();
                    generateLightMapUVs = importer.generateSecondaryUV.ToString();
                    scale = importer.globalScale;
                }
                AddOrSetCheckValue(checker.meshSubMeshCount, 0);
                AddOrSetCheckValue(checker.meshVertexCount, 0);
                AddOrSetCheckValue(checker.meshTrangleCount, 0);

                if (mesh == null && checker.isReloadCheckItem)
                {
                    List<string> oriSubMeshList = subMeshList.Select(x => x.name).ToList();
                    subMeshList.Clear();
                    foreach (var v in EditorUtility.CollectDependencies(new Object[] { obj }))
                    {
                        if (v is Mesh)
                        {
                            mesh = v as Mesh;
                            if (oriSubMeshList.Contains(mesh.name))
                            {
                                AddSubMesh(mesh, checker, null);
                            }
                        }
                    }
                }

                AddOrSetCheckValue(checker.meshFormat, GetMeshFormat(mesh));
                AddOrSetCheckValue(checker.meshReadable, readable);
                AddOrSetCheckValue(checker.meshImortBlendShaps, blendshape);
                AddOrSetCheckValue(checker.meshGenCollider, genCollider);
                AddOrSetCheckValue(checker.meshSwapUVs, swapUVs);
                AddOrSetCheckValue(checker.meshGenLightMapUVs, generateLightMapUVs);
                AddOrSetCheckValue(checker.meshKeepQuads, keepQuads);
                AddOrSetCheckValue(checker.meshOptimized, optimize);
                AddOrSetCheckValue(checker.meshAnimSetting, animation);
                AddOrSetCheckValue(checker.meshCompression, compression);
                AddOrSetCheckValue(checker.meshTanSetting, tangent);
                AddOrSetCheckValue(checker.meshNormalSetting, normal);
                AddOrSetCheckValue(checker.meshScaleFactor, scale);
            }

            public List<SubMeshData> subMeshList = new List<SubMeshData>(1);
            public bool showSubMesh = false;
            public bool isParticleEmitMesh = false;

            public virtual void AddSubMesh(Mesh subMesh, MeshChecker checker, Object refObject)
            {
                foreach (var v in subMeshList)
                {
                    if (v.meshObject == subMesh)
                    {
                        if (refObject != null && !v.refCount.Contains(refObject))
                        {
                            v.refCount.Add(refObject);
                        }
                        return;
                    }

                }
                SubMeshData data = new SubMeshData();
                data.meshObject = subMesh;
                data.format = GetMeshFormat(subMesh);
                data.name = subMesh.name;
                data.vertexCount = subMesh.vertexCount;
                data.tranCount = subMesh.triangles.Length / 3;
                data.refCount.Add(refObject);
                subMeshList.Add(data);
                //重算总体顶点以及面数
                AddOrSetCheckValue(checker.meshSubMeshCount, subMeshList.Count);
                AddOrSetCheckValue(checker.meshVertexCount, subMeshList.Sum(x => x.vertexCount));
                AddOrSetCheckValue(checker.meshTrangleCount, subMeshList.Sum(x => x.tranCount));
            }

            #region 辅助函数

            public string GetMeshFormat(Mesh mesh)
            {
                string haveNormal = mesh.normals.Length > 0 ? "Normal" : "";
                string haveUV = mesh.uv.Length > 0 ? "UV" : "";
                string haveTan = mesh.tangents.Length > 0 ? "Tan" : "";
                string haveColor = mesh.colors.Length > 0 ? "Color" : "";
                string haveUV2 = mesh.uv2.Length > 0 ? "UV2" : "";
                string haveUV3 = mesh.uv3.Length > 0 ? "UV3" : "";
                string haveUV4 = mesh.uv4.Length > 0 ? "UV4" : "";
                string haveColor32 = mesh.colors32.Length > 0 ? "Color32" : "";

                string format = haveNormal + haveTan + haveColor + haveUV + haveUV2 + haveUV3 + haveUV4 + haveColor32;
                return format;
            }

            public virtual int GetMeshVertexCount()
            {
                int count = 0;
                foreach (var v in subMeshList)
                {
                    count += v.vertexCount * v.refCount.Count;
                }
                return count;
            }

            public virtual int GetMeshTrangleCount()
            {
                int count = 0;
                foreach (var v in subMeshList)
                {
                    count += v.tranCount * v.refCount.Count;
                }
                return count;
            }

            #endregion
        }

        public CheckItem meshSubMeshCount;
        public CheckItem meshVertexCount;
        public CheckItem meshTrangleCount;
        public CheckItem meshFormat;
        public CheckItem meshReadable;
        public CheckItem meshOptimized;
        public CheckItem meshGenCollider;
        public CheckItem meshKeepQuads;
        public CheckItem meshSwapUVs;
        public CheckItem meshGenLightMapUVs;
        public CheckItem meshNormalSetting;
        public CheckItem meshTanSetting;
        public CheckItem meshCompression;
        public CheckItem meshAnimSetting;
        public CheckItem meshScaleFactor;
        public CheckItem meshImortBlendShaps;

        public CheckItem meshIsParticleEmitMesh;

        public CheckItem singleVertexCount;
        public CheckItem singleTrangleCount;
        public CheckItem totalVertexCount;
        public CheckItem totalTrangleCount;

        public bool checkMeshFilter = true;
        public bool checkSkinnedMeshRenderer = true;
        public bool checkMeshCollider = true;
        public bool checkParticleMesh = true;

        private GUIContent checkMeshFilterContent = new GUIContent("MeshFilter", "检查MeshFliter上引用的Mesh");
        private GUIContent checkSkinnedMeshRendererContent = new GUIContent("SkinnedMeshRenderer", "检查SkinnedMeshRenderer上引用的Mesh");
        private GUIContent checkMeshColliderContent = new GUIContent("MeshCollider", "检查MeshCollider上引用的Mesh");
        private GUIContent checkParticleSystemContent = new GUIContent("ParticleSystem", "检查Particle上引用的Mesh");

        public override void InitChecker()
        {
            checkerName = "Model";
            checkerFilter = "t:Model";
            enableReloadCheckItem = true;
            meshSubMeshCount = new CheckItem(this, "子网格数", CheckType.Int, OnButtonSubMeshCountClick);
            meshVertexCount = new CheckItem(this, "顶点数", CheckType.Int);
            meshTrangleCount = new CheckItem(this, "面数", CheckType.Int);
            meshFormat = new CheckItem(this, "格式");
            meshReadable = new CheckItem(this, "Readable");
            meshOptimized = new CheckItem(this, "Optimize");
            meshNormalSetting = new CheckItem(this, "Normals");
            meshTanSetting = new CheckItem(this, "Tangents");
            meshCompression = new CheckItem(this, "Compression");
            meshScaleFactor = new CheckItem(this, "ScaleFactor", CheckType.Float);
            meshImortBlendShaps = new CheckItem(this, "BlendShape");
            meshGenCollider = new CheckItem(this, "GenCollider");
            meshKeepQuads = new CheckItem(this, "KeepQuads");
            meshSwapUVs = new CheckItem(this, "SwapUVs");
            meshGenLightMapUVs = new CheckItem(this, "GenLightMapUV");
            meshAnimSetting = new CheckItem(this, "Anim");
            meshIsParticleEmitMesh = new CheckItem(this, "粒子组件引用Mesh");

            singleVertexCount = new CheckItem(this, "顶点数", CheckType.Int, null, null, ItemFlag.CheckSummary);
            singleTrangleCount = new CheckItem(this, "面数", CheckType.Int, null, null, ItemFlag.CheckSummary);
            totalVertexCount = new CheckItem(this, "总顶点数 * 引用", CheckType.Int, null, null, ItemFlag.CheckSummary | ItemFlag.SceneCheckInfo);
            totalTrangleCount = new CheckItem(this, "总面数 * 引用", CheckType.Int, null, null, ItemFlag.CheckSummary | ItemFlag.SceneCheckInfo);
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            var mesh = obj as Mesh;
            if (mesh == null)
                return null;
            MeshDetail detail = null;
            foreach (var checker in CheckList)
            {
                if (checker.assetPath == AssetDatabase.GetAssetPath(mesh))
                    detail = checker as MeshDetail;
            }
            if (detail == null)
            {
                detail = new MeshDetail(mesh, this);
            }
            detail.AddSubMesh(mesh, this, refObj);
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            if (checkMeshFilter)
                AddMeshInternal<MeshFilter>(rootObj);
            if (checkSkinnedMeshRenderer)
                AddMeshInternal<SkinnedMeshRenderer>(rootObj);
            if (checkMeshCollider)
                AddMeshInternal<MeshCollider>(rootObj);
            if (checkParticleMesh)
                AddParticleSystemMeshInternal(rootObj);
        }

        private void AddMeshInternal<T>(GameObject rootObj) where T : Component
        {
            var coms = rootObj.GetComponentsInChildren<T>(true);
            if (coms == null || coms.Length == 0)
                return;
            var info = coms[0].GetType().GetProperty("sharedMesh");
            foreach (var v in coms)
            {
                var mesh = info.GetValue(v, null) as Mesh;
                AddMeshDetailRef(mesh, rootObj, v.gameObject, false);
            }
        }

        private void AddParticleSystemMeshInternal(GameObject rootObj)
        {
            //Mesh形状发射
            var psComs = rootObj.GetComponentsInChildren<ParticleSystem>(true);
            if (psComs == null || psComs.Length == 0)
                return;//木有Ps组件Renderer貌似也不用判断了
            foreach (var ps in psComs)
            {
                var mesh = ps.shape.mesh;
                AddMeshDetailRef(mesh, rootObj, ps.gameObject, true);
            }
            //发射的Mesh
            var coms = rootObj.GetComponentsInChildren<ParticleSystemRenderer>(true);
            if (coms == null || coms.Length == 0)
                return;
            foreach (var psRenderer in coms)
            {
#if UNITY_5_5_OR_NEWER
                var meshArray = new Mesh[4];
                int count = psRenderer.GetMeshes(meshArray);
                for (int i = 0; i < count; i++)
                {
                    AddMeshDetailRef(meshArray[i], rootObj, psRenderer.gameObject, true);
                }
#else
                //5.3木有GetMesh方法...反射貌似也取不到，so...只取一个Mesh吧，一般Particle也只发射一个
                AddMeshDetailRef(psRenderer.mesh, rootObj, psRenderer.gameObject, true);
#endif
            }
        }

        private void AddMeshDetailRef(Object checkObj, Object rootObj, Object refObj, bool isParticleEmitMesh)
        {
            var detail = AddObjectWithRef(checkObj, refObj, rootObj);
            var meshDetail = detail as MeshDetail;
            if (meshDetail == null)
                return;
            meshDetail.isParticleEmitMesh |= isParticleEmitMesh;
            meshDetail.AddOrSetCheckValue(meshIsParticleEmitMesh, meshDetail.isParticleEmitMesh.ToString());
        }

        public override void CheckDetailSummary()
        {
            base.CheckDetailSummary();
            int singleVertices = 0;
            int singleTrangles = 0;
            int totalVertices = 0;
            int totalTrangles = 0;
            foreach (var detail in FilterList)
            {
                var md = detail as MeshDetail;
                totalVertices += md.GetMeshVertexCount();
                totalTrangles += md.GetMeshTrangleCount();
                var vertexCount = md.GetCheckValue(meshVertexCount);
                int vertexCountValue = vertexCount == null ? 0 : (int)vertexCount;
                var trangleCount = md.GetCheckValue(meshTrangleCount);
                int trangleCountValue = trangleCount == null ? 0 : (int)trangleCount;
                singleVertices += vertexCountValue;
                singleTrangles += trangleCountValue;
            }
            checkResultDic[totalVertexCount] = totalVertices;
            checkResultDic[totalTrangleCount] = totalTrangles;
            checkResultDic[singleVertexCount] = singleVertices;
            checkResultDic[singleTrangleCount] = singleTrangles;
        }

        public override void ShowChildDetail(ObjectDetail detail)
        {
            var md = detail as MeshDetail;
            if (md.showSubMesh)
            {
                foreach (var child in md.subMeshList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(170);
                    if (GUILayout.Button(child.name, GUILayout.Width(245)))
                    {
                        SelectObject(child.meshObject);
                    }
                    string vertexCount = "" + child.vertexCount;
                    GUILayout.Label(vertexCount, GUILayout.Width(80));
                    string tranCount = "" + child.tranCount;
                    GUILayout.Label(tranCount, GUILayout.Width(80));
                    GUILayout.Label(child.format, GUILayout.Width(200));
                    GUILayout.EndHorizontal();
                }
            }
        }

        public override void BatchSetResConfig()
        {
            BatchMeshSettingEditor.Init(GetBatchOptionList());
        }

        private void OnButtonSubMeshCountClick(ObjectDetail detail)
        {
            var md = detail as MeshDetail;
            md.showSubMesh = !md.showSubMesh;
        }

        //直接查找资源采用查Model然后找Model依赖的方式检查目录下的Mesh
        //用AssetDatabase.FindAssets("t:Mesh"， path)方式查找时，如果一个FBX下挂有n个Mesh，
        //会返回n个相同的guid指向同一个子mesh！！！目测是bug....(Unity版本5.5.2)
        public override void DirectResCheck(Object[] selection)
        {
            var objects = GetAllDirectCheckObjectFromInput(selection, "t:Model");
            if (objects != null && objects.Count > 0)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var o = objects[i];
                    EditorUtility.DisplayProgressBar("正在检查" + checkerName + "类型资源", "已完成：" + i + "/" + objects.Count, (float)i / objects.Count);
                    AddMeshDetailFromFBX(o);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        public void AddMeshDetailFromFBX(Object fbx)
        {
            var dependency = EditorUtility.CollectDependencies(new Object[] { fbx });
            foreach (var dep in dependency)
            {
                if (dep is Mesh)
                {
                    AddObjectDetail(dep, null, null);
                }
            }
        }

        public override void ShowOptionButton()
        {
            base.ShowOptionButton();
            if (ShowCustomOpeion())
            {
                checkMeshFilter = GUILayout.Toggle(checkMeshFilter, checkMeshFilterContent);
                checkSkinnedMeshRenderer = GUILayout.Toggle(checkSkinnedMeshRenderer, checkSkinnedMeshRendererContent);
                checkMeshCollider = GUILayout.Toggle(checkMeshCollider, checkMeshColliderContent);
                checkParticleMesh = GUILayout.Toggle(checkParticleMesh, checkParticleSystemContent);

            }
        }

        private bool ShowCustomOpeion()
        {
            if (checkModule is SceneResCheckModule)
            {
                return !(checkModule as SceneResCheckModule).completeRefCheck;
            }
            else if (checkModule is ReferenceResCheckModule)
            {
                return (checkModule as ReferenceResCheckModule).checkPrefabDetailRef;
            }
            else
            {
                return false;
            }
        }
    }
}