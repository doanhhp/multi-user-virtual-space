using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // Listen for unexpected disconnects (e.g., the Host closes their game)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnNetworkDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnNetworkDisconnect;
        }
    }

    private void HandleManualDisconnect()
    {
        Debug.Log("<color=cyan>Disconnect button clicked! Shutting down...</color>");
        StartCoroutine(SafeShutdown());
    }

    private void OnNetworkDisconnect(ulong clientId)
    {
        if (clientId == 0 || clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("<color=orange>Network connection lost. Reloading scene...</color>");
            StartCoroutine(SafeShutdown());
        }
    }

    private IEnumerator SafeShutdown()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        yield return null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- THE CUSTOM UI BUTTON ---
    private GUIStyle m_customBtnStyle;
    private bool m_uiInitialized = false;

    private void InitUI()
    {
        if (m_uiInitialized) return;
        m_uiInitialized = true;

        Color btnBg = new Color(0.61f, 0.55f, 0.52f, 0.3f); // 30% opacity
        Color hoverBg = new Color(0.71f, 0.65f, 0.62f, 0.4f);
        Color border = new Color(1f, 1f, 1f, 0.3f); // 30% white border

        Texture2D btnTex = MakeBorderTex(130, 30, btnBg, border, 2);
        Texture2D hoverTex = MakeBorderTex(130, 30, hoverBg, border, 2);

        m_customBtnStyle = new GUIStyle(GUI.skin.button);
        m_customBtnStyle.normal.background = btnTex;
        m_customBtnStyle.hover.background = hoverTex;
        m_customBtnStyle.active.background = btnTex;
        m_customBtnStyle.normal.textColor = Color.white;
        m_customBtnStyle.hover.textColor = Color.white;
        m_customBtnStyle.active.textColor = Color.white;
        m_customBtnStyle.fontSize = 14;
        m_customBtnStyle.fontStyle = FontStyle.Bold;
        m_customBtnStyle.alignment = TextAnchor.MiddleCenter;
    }

    private Texture2D MakeBorderTex(int width, int height, Color fillCol, Color borderCol, int borderWidth)
    {
        Color[] pix = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth)
                    pix[y * width + x] = borderCol;
                else
                    pix[y * width + x] = fillCol;
            }
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        // Only show this button if we are currently connected to a network!
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            InitUI();
            
            float btnWidth = 130f;
            float btnHeight = 30f;
            float paddingRight = 20f;
            float paddingTop = 20f;

            // Calculates the exact top-right corner of the screen
            float xPos = Screen.width - btnWidth - paddingRight;
            float yPos = paddingTop;

            Rect btnRect = new Rect(xPos, yPos, btnWidth, btnHeight);

            if (GUI.Button(btnRect, "", m_customBtnStyle))
            {
                HandleManualDisconnect();
            }

            GUI.contentColor = new Color(0, 0, 0, 0.5f);
            GUI.Label(new Rect(btnRect.x + 1, btnRect.y + 1, btnRect.width, btnRect.height), "DISCONNECT", m_customBtnStyle);
            GUI.contentColor = new Color(1, 1, 1, 0.8f); // 80% opacity text for cleaner look over 50% bg
            GUI.Label(btnRect, "DISCONNECT", m_customBtnStyle);
        }
    }
}