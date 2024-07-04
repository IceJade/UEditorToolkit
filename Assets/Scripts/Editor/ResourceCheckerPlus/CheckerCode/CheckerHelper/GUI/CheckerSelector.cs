using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class CheckerSelector
    {
        private List<ObjectChecker> checkerList = null;
        private int currentActiveChecker = 0;
        private string[] checkerListNames = null;

        public void RefreshChecker(List<ObjectChecker> checkers)
        {
            checkerList = new List<ObjectChecker>(checkers);
            checkerListNames = checkerList.Select(x => x.checkerName).ToArray();
        }

        public void DrawCheckerSelector(float width)
        {
            if (checkerListNames == null)
                return;
            currentActiveChecker = GUILayout.Toolbar(currentActiveChecker, checkerListNames, GUILayout.Width(width));
        }

        public void SetActiveChecker(ObjectChecker checker)
        {
            if (!checkerList.Contains(checker))
                return;
            currentActiveChecker = checkerList.IndexOf(checker);
        }

        public ObjectChecker GetCurrentActiveChecker()
        {
            if (checkerList == null || currentActiveChecker >= checkerList.Count)
                return null;
            return checkerList[currentActiveChecker];
        }
    }

}

