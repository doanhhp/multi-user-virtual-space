using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class NetworkedMediaBoard : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer m_boardRenderer;
    [SerializeField] private VideoPlayer m_videoPlayer; 

    private NetworkList<FixedString4096Bytes> m_mediaUrls = new NetworkList<FixedString4096Bytes>();
    private NetworkVariable<int> m_currentIndex = new NetworkVariable<int>(
        -1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> m_isVideoPaused = new NetworkVariable<bool>(false);
    private NetworkVariable<double> m_videoTime = new NetworkVariable<double>(0.0);
    private NetworkVariable<float> m_videoVolume = new NetworkVariable<float>(1f);

    private Texture2D m_downloadedTexture;
    private RenderTexture m_videoRenderTexture;

    private void Awake()
    {
        if (m_boardRenderer == null) m_boardRenderer = GetComponent<Renderer>();
        if (m_videoPlayer == null) m_videoPlayer = GetComponent<VideoPlayer>();

        if (m_videoPlayer != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 50f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            m_videoPlayer.SetTargetAudioSource(0, audioSource);
            
            // Render video into a 16:9 RenderTexture to guarantee perfect letterboxing with FitInside
            m_videoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            m_videoPlayer.targetTexture = m_videoRenderTexture;
            m_videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }
    }

    private void Update()
    {
        if (m_videoPlayer == null) return;

        if (IsServer && m_videoPlayer.enabled)
        {
            if (Time.frameCount % 60 == 0)
            {
                m_videoTime.Value = m_videoPlayer.time;
            }
        }

        if (IsClient && !IsServer && m_videoPlayer.enabled)
        {
            // Sync play/pause state
            if (m_isVideoPaused.Value && m_videoPlayer.isPlaying) m_videoPlayer.Pause();
            else if (!m_isVideoPaused.Value && !m_videoPlayer.isPlaying) m_videoPlayer.Play();

            // Sync time if drifted > 2.5 seconds
            if (!m_isVideoPaused.Value && Mathf.Abs((float)(m_videoPlayer.time - m_videoTime.Value)) > 2.5f)
            {
                m_videoPlayer.time = m_videoTime.Value;
            }
        }

        // Apply synced volume to the audio source on both server and client
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = m_videoVolume.Value;
        }
    }

    public override void OnNetworkSpawn()
    {
        m_currentIndex.OnValueChanged += OnIndexChanged;
        m_mediaUrls.OnListChanged += OnListChanged;

        RefreshBoard();
    }

    public override void OnNetworkDespawn()
    {
        m_currentIndex.OnValueChanged -= OnIndexChanged;
        m_mediaUrls.OnListChanged -= OnListChanged;
        CleanupMedia();
    }

    private void OnIndexChanged(int oldVal, int newVal)
    {
        RefreshBoard();
    }

    private void OnListChanged(NetworkListEvent<FixedString4096Bytes> changeEvent)
    {
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        if (m_currentIndex.Value >= 0 && m_currentIndex.Value < m_mediaUrls.Count)
        {
            ProcessMedia(m_mediaUrls[m_currentIndex.Value].ToString());
        }
        else
        {
            CleanupMedia();
        }
    }

    public void RequestAddMedia(string newUrl)
    {
        if (string.IsNullOrEmpty(newUrl)) return;
        AddMediaServerRpc(newUrl);
    }

    public void RequestNextSlide() => NextSlideServerRpc();
    public void RequestPrevSlide() => PrevSlideServerRpc();
    public void RequestDeleteCurrent() => DeleteCurrentServerRpc();

    public void RequestTogglePause() => TogglePauseServerRpc();
    public void RequestSeekToPercent(float percent) => SeekToPercentServerRpc(percent);

    public int GetCurrentIndex() => m_currentIndex.Value;
    public int GetTotalMediaCount() => m_mediaUrls.Count;
    public bool IsVideoActive() => m_videoPlayer != null && m_videoPlayer.enabled;
    public bool IsVideoPaused() => m_isVideoPaused.Value;
    public float GetVideoProgress() 
    {
        if (m_videoPlayer != null && m_videoPlayer.length > 0)
            return (float)(m_videoPlayer.time / m_videoPlayer.length);
        return 0f;
    }

    public void RequestSetVolume(float volume) => SetVolumeServerRpc(volume);
    public float GetVolume() => m_videoVolume.Value;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AddMediaServerRpc(string newUrl)
    {
        m_mediaUrls.Add(newUrl);
        if (m_currentIndex.Value == -1)
        {
            m_currentIndex.Value = 0;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NextSlideServerRpc()
    {
        if (m_mediaUrls.Count > 0)
        {
            m_currentIndex.Value = (m_currentIndex.Value + 1) % m_mediaUrls.Count;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PrevSlideServerRpc()
    {
        if (m_mediaUrls.Count > 0)
        {
            m_currentIndex.Value = m_currentIndex.Value - 1;
            if (m_currentIndex.Value < 0) m_currentIndex.Value = m_mediaUrls.Count - 1;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DeleteCurrentServerRpc()
    {
        if (m_currentIndex.Value >= 0 && m_currentIndex.Value < m_mediaUrls.Count)
        {
            int idx = m_currentIndex.Value;
            m_mediaUrls.RemoveAt(idx);

            if (m_mediaUrls.Count == 0)
            {
                m_currentIndex.Value = -1;
            }
            else if (idx >= m_mediaUrls.Count)
            {
                m_currentIndex.Value = m_mediaUrls.Count - 1;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TogglePauseServerRpc()
    {
        if (m_videoPlayer != null && m_videoPlayer.enabled)
        {
            m_isVideoPaused.Value = !m_isVideoPaused.Value;
            if (m_isVideoPaused.Value) m_videoPlayer.Pause();
            else m_videoPlayer.Play();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SeekToPercentServerRpc(float percent)
    {
        if (m_videoPlayer != null && m_videoPlayer.enabled && m_videoPlayer.length > 0)
        {
            double newTime = m_videoPlayer.length * percent;
            m_videoPlayer.time = newTime;
            m_videoTime.Value = newTime;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetVolumeServerRpc(float volume)
    {
        m_videoVolume.Value = Mathf.Clamp01(volume);
    }

    // --- THE UPGRADED SMART MEDIA SORTER ---
    private void ProcessMedia(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        string cleanUrl = url.Trim();
        CleanupMedia(); 

        // THE FIX: Create a temporary version of the URL that chops off everything after the '?'
        string urlWithoutQuery = cleanUrl.Split('?')[0];

        // Now we check this clean version. Added IgnoreCase just in case someone types .MP4
        bool isVideo = urlWithoutQuery.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase) || 
                       urlWithoutQuery.EndsWith(".webm", System.StringComparison.OrdinalIgnoreCase) || 
                       urlWithoutQuery.EndsWith(".mov", System.StringComparison.OrdinalIgnoreCase);

        if (isVideo)
        {
            if (m_videoPlayer != null)
            {
                m_videoPlayer.enabled = true; 
                
                string videoUrl = cleanUrl;
                
                // Videos MUST be downloaded from the raw CDN, not the media proxy, otherwise Discord corrupts the stream
                videoUrl = videoUrl.Replace("media.discordapp.net", "cdn.discordapp.com");
                
                // Strip discord's webp format enforcement and thumbnail resizing parameters
                videoUrl = videoUrl.Replace("&format=webp", "");
                videoUrl = videoUrl.Replace("?format=webp", "?");
                videoUrl = System.Text.RegularExpressions.Regex.Replace(videoUrl, @"&width=\d+", "");
                videoUrl = System.Text.RegularExpressions.Regex.Replace(videoUrl, @"&height=\d+", "");
                
                if (m_boardRenderer != null)
                {
                    m_boardRenderer.material.mainTextureScale = Vector2.one;
                    m_boardRenderer.material.mainTextureOffset = Vector2.zero;
                    m_boardRenderer.material.mainTexture = m_videoRenderTexture;
                }

                m_videoPlayer.url = videoUrl; 
                m_videoPlayer.Play();
                
                if (IsServer) m_isVideoPaused.Value = false;
            }
        }
        else 
        {
            if (m_videoPlayer != null)
            {
                m_videoPlayer.Stop();
                m_videoPlayer.enabled = false; 
            }
            StartCoroutine(DownloadAndApplyTexture(cleanUrl));
        }
    }

    private IEnumerator DownloadAndApplyTexture(string url)
    {
        string finalUrl = url;

        // --- THE DISCORD FIXER ---
        // If someone pastes a Discord link that asks for a WebP, we intercept it 
        // and force Discord to give us a Unity-friendly PNG instead!
        if (finalUrl.Contains("discordapp") && finalUrl.Contains("format=webp"))
        {
            finalUrl = finalUrl.Replace("format=webp", "format=png");
        }

        // Use the fixed URL for the request
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            // Fake ID to bypass bot protection
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            yield return request.SendWebRequest();

            // FALLBACK LOGIC
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Board failed to download image. URL: '{finalUrl}' | Error: {request.error}");
                
                if (m_boardRenderer != null)
                {
                    m_boardRenderer.material.mainTexture = null;
                }
            }
            else // SUCCESS
            {
                    Texture2D rawTex = DownloadHandlerTexture.GetContent(request);
                    m_downloadedTexture = PadTexture(rawTex, 16f / 9f);
                    
                    if (m_downloadedTexture != rawTex) 
                    {
                        Destroy(rawTex);
                    }

                    if (m_boardRenderer != null)
                    {
                        // Reset scale to 1:1 since the texture is perfectly padded now
                        m_boardRenderer.material.mainTextureScale = Vector2.one;
                        m_boardRenderer.material.mainTextureOffset = Vector2.zero;
                        m_boardRenderer.material.mainTexture = m_downloadedTexture;
                    }
            }
        }
    }

    private Texture2D PadTexture(Texture2D original, float targetAspect)
    {
        float imageAspect = (float)original.width / original.height;
        if (Mathf.Abs(imageAspect - targetAspect) < 0.05f) return original; // Already ~16:9

        int targetWidth = original.width;
        int targetHeight = original.height;

        if (imageAspect > targetAspect) 
            targetHeight = Mathf.RoundToInt(original.width / targetAspect);
        else 
            targetWidth = Mathf.RoundToInt(original.height * targetAspect);

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        
        GL.Clear(true, true, Color.black);
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetWidth, 0, targetHeight);
        
        float destX = (targetWidth - original.width) / 2f;
        float destY = (targetHeight - original.height) / 2f;
        Graphics.DrawTexture(new Rect(destX, destY, original.width, original.height), original);
        
        GL.PopMatrix();

        Texture2D padded = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        padded.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        
        // ReadPixels in Unity flips the image upside down because of OpenGL coordinate mismatches.
        // We can instantly fix it by flipping the pixel array vertically!
        Color32[] pixels = padded.GetPixels32();
        Color32[] flipped = new Color32[pixels.Length];
        for (int y = 0; y < targetHeight; y++)
        {
            int sourceY = targetHeight - 1 - y;
            System.Array.Copy(pixels, sourceY * targetWidth, flipped, y * targetWidth, targetWidth);
        }
        padded.SetPixels32(flipped);
        padded.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return padded;
    }

    private void CleanupMedia()
    {
        if (m_videoPlayer != null)
        {
            m_videoPlayer.Stop();
            m_videoPlayer.enabled = false;
        }

        // Safely destroy the old image from RAM to prevent memory leaks
        if (m_downloadedTexture != null)
        {
            Destroy(m_downloadedTexture);
            m_downloadedTexture = null;
        }

        // Wipe the material clean
        if (m_boardRenderer != null && m_boardRenderer.material != null)
        {
            m_boardRenderer.material.mainTexture = null;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (m_videoRenderTexture != null)
        {
            m_videoRenderTexture.Release();
            Destroy(m_videoRenderTexture);
        }
    }
}