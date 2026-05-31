using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Bozo.ModularCharacters; 

public class MainMenuNetworker : MonoBehaviour
{
    [Header("References")]
    public OutfitSystem m_dummyCharacter;
    public GameObject m_customizationMenuUI; 
    public MultiplayerUI m_multiplayerUI; 

    [Header("Cameras")]
    public GameObject m_customizationCamera;
    public GameObject m_mainGameplayCamera; 

    private string m_joinCodeInput = "";
    private string m_hostJoinCodeDisplay = "";
    private bool m_isRelayConnecting = false;
    private bool m_showJoinPrompt = false;

    private GUIStyle m_customBtnStyle;
    private GUIStyle m_customInputStyle;
    private bool m_uiInitialized = false;

    private void InitUI()
    {
        if (m_uiInitialized) return;
        m_uiInitialized = true;

        Color btnBg = new Color(0.61f, 0.55f, 0.52f, 1f); // Warm taupe/grey from the image
        Color border = Color.white;

        Texture2D btnTex = MakeBorderTex(200, 60, btnBg, border, 2);
        Texture2D hoverTex = MakeBorderTex(200, 60, new Color(0.71f, 0.65f, 0.62f, 1f), border, 2);

        m_customBtnStyle = new GUIStyle(GUI.skin.button);
        m_customBtnStyle.normal.background = btnTex;
        m_customBtnStyle.hover.background = hoverTex;
        m_customBtnStyle.active.background = btnTex;
        m_customBtnStyle.normal.textColor = Color.white;
        m_customBtnStyle.hover.textColor = Color.white;
        m_customBtnStyle.active.textColor = Color.white;
        m_customBtnStyle.fontSize = 32;
        m_customBtnStyle.fontStyle = FontStyle.Bold;
        m_customBtnStyle.alignment = TextAnchor.MiddleCenter;
        
        m_customInputStyle = new GUIStyle(GUI.skin.textField);
        m_customInputStyle.fontSize = 40;
        m_customInputStyle.alignment = TextAnchor.MiddleCenter;
        m_customInputStyle.normal.textColor = Color.white;
        m_customInputStyle.normal.background = MakeBorderTex(400, 60, new Color(0.1f, 0.1f, 0.1f, 0.8f), Color.white, 2);
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

    private async void Start()
    {
        try
        {
            // Give each instance a unique profile so Unity Multiplayer Play Mode clones don't clash and overwrite each other's Relay tokens
            InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
            options.SetProfile("Player_" + System.Guid.NewGuid().ToString().Substring(0, 8));
#endif
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"<color=cyan>[Relay Auth] Initialized! Project ID: {UnityServices.ExternalUserId} | Environment: {UnityServices.State}</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError("UGS Init Failed: " + e);
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only transition to the game screen once OUR local player has actually connected and spawned!
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            TransitionToGame();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // If we (the local client) disconnect, clean up and reload the main menu scene
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    public async void OnClick_StartHost()
    {
        if (m_isRelayConnecting) return;
        m_isRelayConnecting = true;

        SaveOutfitData();

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string rawCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // Do not force uppercase or lowercase. Display EXACTLY what Relay generates.
            m_hostJoinCodeDisplay = rawCode;
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                false);

            // Fix: Increase MaxPacketQueueSize. 
            // The JSON outfit string fragments into many small packets. If it exceeds 128 packets, the queue overflows.
            NetworkManager.Singleton.GetComponent<UnityTransport>().MaxPacketQueueSize = 2048;

            if (NetworkManager.Singleton.StartHost())
            {
                // Attach our persistent UI overlay to the NetworkManager so it survives the menu getting disabled
                RelayUIOverlay overlay = NetworkManager.Singleton.gameObject.AddComponent<RelayUIOverlay>();
                overlay.JoinCodeDisplay = m_hostJoinCodeDisplay;
            }
            else
            {
                Debug.LogWarning("Port is already in use! A Host already exists. Click Join instead.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Host Error: " + e);
        }

        m_isRelayConnecting = false;
    }

    public void OnClick_StartClient()
    {
        // Instead of connecting immediately, show the center prompt
        m_showJoinPrompt = true;
    }

    private async void ExecuteJoinClient()
    {
        if (m_isRelayConnecting || string.IsNullOrEmpty(m_joinCodeInput)) return;
        m_isRelayConnecting = true;

        SaveOutfitData();

        try
        {
            // Strip out invisible/invalid characters, but preserve EXACT case. 
            // Do NOT force ToLower() or ToUpper() as Relay codes can be mixed-case.
            string cleanedCode = System.Text.RegularExpressions.Regex.Replace(m_joinCodeInput, @"[^a-zA-Z0-9]", "");
            
            Debug.Log($"<color=orange>Attempting to join Relay Code: '{cleanedCode}' | Env: {UnityServices.State} | PlayerId: {AuthenticationService.Instance.PlayerId}</color>");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(cleanedCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                false);

            // Fix: Increase MaxPacketQueueSize.
            NetworkManager.Singleton.GetComponent<UnityTransport>().MaxPacketQueueSize = 2048;

            if (NetworkManager.Singleton.StartClient())
            {
                // Attach a disconnect button overlay for clients too
                RelayUIOverlay overlay = NetworkManager.Singleton.gameObject.AddComponent<RelayUIOverlay>();
                overlay.IsClient = true;
            }
            else
            {
                Debug.LogWarning("Failed to start client.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Join Error: " + e);
        }

        m_isRelayConnecting = false;
    }

    private void OnGUI()
    {
        if (m_showJoinPrompt && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            InitUI();
            float rx = Screen.width / 1920f;
            float ry = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));

            float x = 1920 / 2f;
            float y = 1080 / 2f;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 26;
            labelStyle.normal.textColor = Color.white;
            
            // Drop shadow for label
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(x - 200 + 2, y - 60 + 2, 400, 50), "JOIN CODE", labelStyle);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(x - 200, y - 60, 400, 50), "JOIN CODE", labelStyle);

            m_joinCodeInput = GUI.TextField(new Rect(x - 200, y, 400, 60), m_joinCodeInput, 6, m_customInputStyle);

            // JOIN Button
            Rect btnRect = new Rect(x - 100, y + 80, 200, 60);
            
            // Draw slight black drop shadow manually for text inside the button
            if (GUI.Button(btnRect, "", m_customBtnStyle))
            {
                m_showJoinPrompt = false;
                ExecuteJoinClient();
            }
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(btnRect.x + 2, btnRect.y + 2, btnRect.width, btnRect.height), "JOIN", m_customBtnStyle);
            GUI.contentColor = Color.white;
            GUI.Label(btnRect, "JOIN", m_customBtnStyle);

            // CANCEL Button
            Rect cancelRect = new Rect(x - 100, y + 160, 200, 60);
            if (GUI.Button(cancelRect, "", m_customBtnStyle))
            {
                m_showJoinPrompt = false;
            }
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(cancelRect.x + 2, cancelRect.y + 2, cancelRect.width, cancelRect.height), "CANCEL", m_customBtnStyle);
            GUI.contentColor = Color.white;
            GUI.Label(cancelRect, "CANCEL", m_customBtnStyle);
        }
    }

    private void SaveOutfitData()
    {
        if (m_dummyCharacter != null)
        {
            CharacterData myData = BMAC_SaveSystem.GetCharacterData(m_dummyCharacter);
            PlayerSessionLook.CharacterJSON = JsonUtility.ToJson(myData);
        }
    }

    private void TransitionToGame()
    {
        if (m_customizationMenuUI != null) m_customizationMenuUI.SetActive(false);
        if (m_customizationCamera != null) m_customizationCamera.SetActive(false);
        if (m_mainGameplayCamera != null) m_mainGameplayCamera.SetActive(true);
        if (m_multiplayerUI != null) m_multiplayerUI.DisableButtons();
        if (m_dummyCharacter != null) Destroy(m_dummyCharacter.gameObject);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

public class RelayUIOverlay : MonoBehaviour
{
    public string JoinCodeDisplay = "";
    public bool IsClient = false;
    private GUIStyle m_codeStyle;
    private bool m_styleInitialized = false;

    private void Update()
    {
        // Automatically clean up the overlay if the server shuts down or we disconnect
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            Destroy(gameObject.GetComponent<RelayUIOverlay>());
        }
    }

    private void OnGUI()
    {
        float rx = Screen.width / 1920f;
        float ry = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));

        if (!m_styleInitialized)
        {
            m_codeStyle = new GUIStyle(GUI.skin.label);
            m_codeStyle.fontSize = 40; // Scales cleanly down/up based on matrix
            m_codeStyle.alignment = TextAnchor.LowerRight;
            m_codeStyle.normal.textColor = Color.white;
            m_styleInitialized = true;
        }

        // Show Join Code for host
        if (!string.IsNullOrEmpty(JoinCodeDisplay))
        {
            GUI.Label(new Rect(1920 - 450, 1080 - 100, 430, 80), JoinCodeDisplay, m_codeStyle);
        }
    }
}