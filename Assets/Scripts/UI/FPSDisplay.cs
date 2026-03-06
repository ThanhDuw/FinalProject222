using UnityEngine;

// Simple FPS display. Attach to any GameObject (e.g. a UI manager or the Main Camera).
// Shows a small FPS label in the top-right corner using OnGUI.
public class FPSDisplay : MonoBehaviour
{
    [Tooltip("How often (seconds) the FPS counter updates")]
    public float updateInterval = 0.5f;

    [Tooltip("Margin from the top-right corner")]
    public Vector2 margin = new Vector2(10f, 10f);

    [Tooltip("Text color for the FPS label")]
    public Color textColor = Color.white;

    [Tooltip("Font size for the FPS label")]
    public int fontSize = 14;

    float m_TimeLeft;
    float m_Accum;
    int m_Frames;
    float m_Fps;
    GUIStyle m_Style;

    void Start()
    {
        m_TimeLeft = updateInterval;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            m_TimeLeft -= dt;
            m_Accum += 1f / dt;
            m_Frames++;

            if (m_TimeLeft <= 0f)
            {
                m_Fps = m_Accum / m_Frames;
                m_TimeLeft = updateInterval;
                m_Accum = 0f;
                m_Frames = 0;
            }
        }
    }

    void OnGUI()
    {
        if (m_Style == null)
        {
            m_Style = new GUIStyle(GUI.skin.label);
            m_Style.alignment = TextAnchor.UpperRight;
            m_Style.fontSize = fontSize;
            m_Style.normal.textColor = textColor;
        }

        string text = $"FPS: {m_Fps:F1}";

        // Reserve some width for the label
        float width = 100f;
        float height = Mathf.Max(20f, fontSize + 6f);
        Rect rect = new Rect(Screen.width - margin.x - width, margin.y, width, height);
        GUI.Label(rect, text, m_Style);
    }
}
