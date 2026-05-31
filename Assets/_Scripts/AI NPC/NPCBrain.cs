using System.Collections;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class NpcBrain : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI m_speechBubbleText;
    
    [Header("API Settings")]
    [Tooltip("Paste your API Key here")]
    [SerializeField] private string m_apiKey = "PASTE_YOUR_GEMINI_KEY_HERE";
    
    private string m_apiUrl => $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key={m_apiKey}";
    
    [Header("AI Personality & Course Context")]
    [TextArea(5, 10)]
    [SerializeField] private string m_systemPrompt = "You are a helpful teaching assistant for a Game Development course. Keep your answers under 2 sentences so the text bubble does not overflow.";
    
    private Coroutine m_typewriter;
    
    // --- RATE LIMIT PROTECTION FLAG ---
    // This stops the player from spamming the API and crashing your quota!
    private bool m_isThinking = false;

    public void AskQuestion(string message) => SubmitQuestionRpc(message);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitQuestionRpc(string text)
    {
        // 1. THE GATEKEEPER: If already thinking, ignore new button clicks entirely.
        if (m_isThinking) return; 
        
        // 2. LOCK THE GATE
        m_isThinking = true;
        
        BroadcastResponseRpc("Thinking...");
        StartCoroutine(GetGeminiResponse(text));
    }

    private IEnumerator GetGeminiResponse(string userText)
    {
        // Clean the text so quotation marks don't break the JSON format
        string safeText = userText.Replace("\"", "\\\"");
        
        string jsonData = $"{{\"contents\": [{{\"parts\":[{{\"text\":\"{m_systemPrompt} User asks: {safeText}\"}}]}}]}}";
        
        using (UnityWebRequest req = new UnityWebRequest(m_apiUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseText = ExtractText(req.downloadHandler.text);
                BroadcastResponseRpc(responseText);
            }
            else 
            { 
                Debug.LogError($"--- AI NETWORK ERROR ---");
                Debug.LogError($"Error Code: {req.responseCode} {req.error}");
                Debug.LogError($"Google's Response: {req.downloadHandler.text}");
                
                // Smart Error Handling for the UI
                if (req.responseCode == 429)
                {
                    BroadcastResponseRpc("I am thinking too hard! Please wait a minute before asking another question.");
                }
                else
                {
                    BroadcastResponseRpc("My brain is foggy... check the console."); 
                }
            }

            // 3. UNLOCK THE GATE
            // Whether it succeeded or failed, the API call is done. The player can ask again.
            m_isThinking = false;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastResponseRpc(string text)
    {
        if (m_typewriter != null) StopCoroutine(m_typewriter);
        m_typewriter = StartCoroutine(Typewriter(text));
    }

    private string ExtractText(string json)
    {
        try 
        {
            int start = json.IndexOf("\"text\": \"") + 9;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start).Replace("\\n", "\n");
        } 
        catch 
        { 
            return "I got confused reading the data."; 
        }
    }

    private IEnumerator Typewriter(string text)
    {
        m_speechBubbleText.text = "";
        foreach (char c in text) 
        { 
            m_speechBubbleText.text += c; 
            yield return new WaitForSeconds(0.04f); 
        }
    }
}