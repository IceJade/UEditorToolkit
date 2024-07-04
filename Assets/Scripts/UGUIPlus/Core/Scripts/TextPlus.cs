using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("UGUI Plus/Text Plus")]
public class TextPlus : Text
{
    [SerializeField]
    FontSpacingHandler m_FontSpacingHandler = new FontSpacingHandler();
    [SerializeField]
    VertexColorHandler m_VertexColorHandler = new VertexColorHandler();
    [SerializeField]
    TextShadowHandler m_TextShadowHandler = new TextShadowHandler();
    [SerializeField]
    TextOutlineHandler m_TextOutlineHandler = new TextOutlineHandler();

    [SerializeField] public bool m_ignoreStatistic;

    public FontSpacingHandler FontSpacingHandler
    {
        get
        {
            return m_FontSpacingHandler;
        }
    }

    public VertexColorHandler VertexColorHandler
    {
        get
        {
            return m_VertexColorHandler;
        }
    }

    public TextShadowHandler TextShadowHandler
    {
        get
        {
            return m_TextShadowHandler;
        }
    }

    public TextOutlineHandler TextOutlineHandler
    {
        get
        {
            return m_TextOutlineHandler;
        }
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);
        m_FontSpacingHandler.PopulateMesh(toFill);
        m_VertexColorHandler.PopulateMesh(toFill, rectTransform, color);
        m_TextShadowHandler.PopulateMesh(toFill, rectTransform, color);
        m_TextOutlineHandler.PopulateMesh(toFill);        
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
    }
#endif

}
