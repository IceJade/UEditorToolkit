﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class ParticleChecker : ObjectChecker
    {
        public class ChildParticle
        {
            public string name;
            public int maxCount;
            public float maxSize;
            public Object psObject;
            public bool active;

            public bool emptyParticle;
        }

        public class ChildTrailRenderer
        {
            public string name;
            public float time;
            public float minVertexDistance;
            public Object trailObject;
            public bool active;
        }

        public class ParticleDetail : ObjectDetail
        {
            public ParticleDetail(Object obj, ParticleChecker checker) : base(obj, checker)
            {
               var dependencys = EditorUtility.CollectDependencies(new Object[] { obj });
                foreach (var o in dependencys)
                {
                    if (o is ParticleSystem)
                    {
                        var ps = o as ParticleSystem;
                        var child = new ChildParticle();
                        child.name = ps.name;
                        child.maxCount = (int)GetParticleEmissionCount(ps);
#if UNITY_5_5_OR_NEWER
                        child.maxSize = ps.emission.enabled ? ps.main.startSize.constantMax : 0;
#else
                        child.maxSize = ps.emission.enabled ? ps.startSize : 0;
#endif
                        child.psObject = o;
                        child.active = CheckIsRefObjectActive(ps.gameObject);
                        child.emptyParticle = IsEmptyParticle(ps);
                        if (child.emptyParticle)
                            emptyParticleCount++;
                        refObjectEnabled &= child.active;
                        childParticles.Add(child);
                    }
                    else if (o is TrailRenderer)
                    {
                        var tr = o as TrailRenderer;
                        var child = new ChildTrailRenderer();
                        child.name = tr.name;
#if UNITY_5_5_OR_NEWER
                        child.minVertexDistance = tr.minVertexDistance;
#else
                        child.minVertexDistance = 0;
#endif
                        child.time = tr.time;
                        child.trailObject = tr;
                        child.active = CheckIsRefObjectActive(tr.gameObject);
                        refObjectEnabled &= child.active;
                        childTrails.Add(child);
                    }
                }
                //根物体最大粒子数等于相加总和
                int totalMaxCount = 0;
                foreach (var p in childParticles)
                {
                    totalMaxCount += p.maxCount;
                }
                AddOrSetCheckValue(checker.particleMaxCount, totalMaxCount);
                //根物体的大小等于子物体中最大的
                float totalMaxSize = 0.0f;
                foreach (var p in childParticles)
                {
                    if (p.maxSize > totalMaxSize)
                        totalMaxSize = p.maxSize;
                }
                AddOrSetCheckValue(checker.particleMaxSize, totalMaxSize);
                AddOrSetCheckValue(checker.particleComponentCount, childParticles.Count);
                AddOrSetCheckValue(checker.trailRendererCount, childTrails.Count);
                AddOrSetCheckValue(checker.activeItem, refObjectEnabled.ToString());
                AddOrSetCheckValue(checker.particleEmptyCount, emptyParticleCount);

                //没有的不显示了
                if (childParticles.Count == 0 && childTrails.Count == 0)
                {
                    checker.CheckList.Remove(this);
                }
            }
            public bool showChildParticleCom = false;
            public bool showChildTrailCom = false;
            public List<ChildParticle> childParticles = new List<ChildParticle>();
            public List<ChildTrailRenderer> childTrails = new List<ChildTrailRenderer>();
            public int emptyParticleCount = 0;

            #region 辅助函数
            private float GetParticleEmissionCount(ParticleSystem ps)
            {
                //关发射器的为0
                if (ps.emission.enabled == false)
                    return 0;
                //取Rate over Time和Rate over Distance定值的最大值
#if UNITY_5_5_OR_NEWER
                float rateCount = Mathf.Max(ps.emission.rateOverTime.constantMax, ps.emission.rateOverDistance.constantMax);
#else
                float rateCount = ps.emission.rate.constantMax;
#endif
                //float curCount = Mathf.Max(ps.emission.rateOverTime.curveMax, ps.emission.rateOverDistance.curveMax);
                //取burst中的最大值
                float burstCount = 0;
                if (ps.emission.burstCount > 0)
                {
                    ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[ps.emission.burstCount];
                    ps.emission.GetBursts(bursts);
                    //从burst中取出最大值
                    foreach (ParticleSystem.Burst burst in bursts)
                    {
                        if (burst.maxCount > burstCount)
                            burstCount = burst.maxCount;
                    }
                }
                //burst值与rate值相加
                float emissionCount = burstCount + rateCount;
                //最后与maxparticle取最小值
#if UNITY_5_5_OR_NEWER
                return Mathf.Min(emissionCount, ps.main.maxParticles);
#else
                return Mathf.Min(emissionCount, ps.maxParticles);
#endif
            }

            private bool IsEmptyParticle(ParticleSystem ps)
            {
               return  ps.emission.enabled == false || ps.gameObject.GetComponent<ParticleSystemRenderer>().enabled == false;
            }
            #endregion
        }

        CheckItem particleMaxCount;
        CheckItem particleMaxSize;
        CheckItem particleComponentCount;
        CheckItem trailRendererCount;
        CheckItem particleEmptyCount;

        public override void InitChecker()
        {
            checkerName = "Particle";
            checkerFilter = "t:Prefab";
            refItem.show = false;
            particleComponentCount = new CheckItem(this, "粒子组件数", CheckType.Int, OnButtonChildParticleComClick);
            particleMaxCount = new CheckItem(this, "粒子数", CheckType.Int);
            particleMaxSize = new CheckItem(this, "粒子大小", CheckType.Float);
            particleEmptyCount = new CheckItem(this, "空粒子发射器", CheckType.Int, OnButtonChildParticleComClick);
            trailRendererCount = new CheckItem(this, "拖尾组件数", CheckType.Int, OnButtonChildTrailComClick);
        }

        public override void AddObjectDetail(Object rootObj)
        {
            ObjectDetail detail = null;
            foreach (var v in CheckList)
            {
                if (v.checkObject == rootObj)
                    detail = v;
            }
            if (detail == null)
            {
                detail = new ParticleDetail(rootObj, this);
            }
        }

        private void OnButtonChildParticleComClick(ObjectDetail detail)
        {
            var pd = detail as ParticleDetail;
            pd.showChildParticleCom = !pd.showChildParticleCom;
        }

        private void OnButtonChildTrailComClick(ObjectDetail detail)
        {
            var pd = detail as ParticleDetail;
            pd.showChildTrailCom = !pd.showChildTrailCom;
        }

        public override void ShowChildDetail(ObjectDetail detail)
        {
            var pDetail = detail as ParticleDetail;
            if (pDetail.showChildParticleCom)
            {
                foreach (var child in pDetail.childParticles)
                {
                    ResourceCheckerHelper.BeginLine(child.emptyParticle);
                    GUILayout.Space(85);
                    if (GUILayout.Button(child.name, GUILayout.Width(245)))
                    {
                        SelectObject(child.psObject);
                    }
                    string maxChildParticles = "" + child.maxCount;
                    GUILayout.Label(maxChildParticles, GUILayout.Width(80));
                    string maxChildSize = "" + child.maxSize;
                    GUILayout.Label(maxChildSize, GUILayout.Width(80));
                    GUILayout.Label("Active: " + child.active.ToString());
                    ResourceCheckerHelper.EndLine();
                }
            }
            if (pDetail.showChildTrailCom)
            {
                foreach (var child in pDetail.childTrails)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(85);
                    if (GUILayout.Button(child.name, GUILayout.Width(245)))
                    {
                        SelectObject(child.trailObject);
                    }
                    var trailTime = "Time:" + child.time;
                    GUILayout.Label(trailTime, GUILayout.Width(80));
                    var trailMinVertexDistance = "MinVertexDistance:" + child.minVertexDistance;
                    GUILayout.Label(trailMinVertexDistance, GUILayout.Width(200));
                    GUILayout.Label("Active: " + child.active.ToString());
                    GUILayout.EndHorizontal();
                }
            }
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            AddEffectDetail<ParticleSystem>(rootObj);
            AddEffectDetail<TrailRenderer>(rootObj);
        }

        public void AddEffectDetail<T>(GameObject rootObj) where T : Component
        {
            var coms = rootObj.GetComponentsInChildren<T>(true);
            foreach (var p in coms)
            {
                AddObjectDetail(p);
            }
        }

    }
}