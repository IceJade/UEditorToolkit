using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FontSpacingHandler
{
    [SerializeField]
    private bool m_UseTextSpacing;
    public bool UseTextSpacing
    {
        get
        {
            return m_UseTextSpacing;
        }
        set
        {
            m_UseTextSpacing = value;
        }
    }

    [SerializeField]
    [Range(-10, 10)]
    private float m_TextSpacing = 1f;
    public float _textSpacing
    {
        get
        {
            return m_TextSpacing;
        }
        set
        {
            m_TextSpacing = value;
        }
    }


    public void PopulateMesh(VertexHelper toFill)
    {
        if (UseTextSpacing)
        {
            if (toFill.currentVertCount == 0)
            {
                return;
            }
            List<UIVertex> vertexs = new List<UIVertex>();
            toFill.GetUIVertexStream(vertexs);
            int indexCount = toFill.currentIndexCount;
            UIVertex vt;
            for (int i = 6; i < indexCount; i++)
            {
                //第一个字不用改变位置
                vt = vertexs[i];
                vt.position += new Vector3(m_TextSpacing * (i / 6), 0, 0);
                vertexs[i] = vt;
                //以下注意点与索引的对应关系
                if (i % 6 <= 2)
                {
                    toFill.SetUIVertex(vt, (i / 6) * 4 + i % 6);
                }
                if (i % 6 == 4)
                {
                    toFill.SetUIVertex(vt, (i / 6) * 4 + i % 6 - 1);
                }
            }
        }
    }

}
