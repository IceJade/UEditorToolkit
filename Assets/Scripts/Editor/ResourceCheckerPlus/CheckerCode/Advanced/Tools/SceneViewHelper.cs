using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class SceneViewHelper : Editor
    {
        private void OnSceneGUI()
        {
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}
