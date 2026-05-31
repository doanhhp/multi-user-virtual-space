using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(TextMeshProUGUI), typeof(LayoutElement))]
public class BubbleFitter : MonoBehaviour
{
    [Tooltip("The maximum width the bubble can get before the text wraps downwards")]
    public float maxWidth = 4500f;
    
    private TextMeshProUGUI m_text;
    private LayoutElement m_layoutElement;
    private RectTransform m_backgroundRect;

    private void Start()
    {
        m_text = GetComponent<TextMeshProUGUI>();
        m_layoutElement = GetComponent<LayoutElement>();
        
        // Grab the background object (which should be the parent of this text)
        if (transform.parent != null)
        {
            m_backgroundRect = transform.parent.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (m_text == null || m_layoutElement == null) return;

        // 1. Calculate exactly how wide the text wants to be
        float trueWidth = m_text.GetPreferredValues(m_text.text).x;

        // 2. Cap the text width at your max width
        m_layoutElement.preferredWidth = Mathf.Min(trueWidth, maxWidth);

        // 3. THE SILVER BULLET: Force Unity to recalculate the background padding instantly!
        if (m_backgroundRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_backgroundRect);
        }
    }
}