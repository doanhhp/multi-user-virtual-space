using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

public class VivoxVoiceManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCamera m_playerCamera;
    [Tooltip("Reference to the CameraFollowTarget or the Avatar's Head transform for precise voice emission.")]
    [SerializeField] private Transform m_avatarHeadTransform;
    [SerializeField] private BillboardUI m_billboardUI; 

    [Header("3D Audio Settings")]
    [Tooltip("The maximum distance from the listener that a speaker can be heard.")]
    [SerializeField] private int m_audibleDistance = 85;
    [Tooltip("The distance from the listener within which a speaker’s voice is heard at its original volume.")]
    [SerializeField] private int m_conversationalDistance = 2;
    [Tooltip("The strength of the audio fade effect as the speaker moves away from the listener.")]
    [SerializeField] private float m_audioFadeIntensity = 1.0f;
    [Tooltip("The model that determines how loud a voice is at different distances. InverseByDistance mimics real-life acoustics.")]
    [SerializeField] private AudioFadeModel m_audioFadeModel = AudioFadeModel.InverseByDistance;

    [Header("Audio Occlusion Settings")]
    [Tooltip("Layers that block audio")]
    [SerializeField] private LayerMask m_occlusionLayers = 1; // 1 = Default layer mask
    [Tooltip("Radius of the occlusion check ray. Larger is more forgiving.")]
    [SerializeField] private float m_occlusionRadius = 0.5f;
    [Tooltip("How much volume to reduce when blocked (-50 is completely silent).")]
    [SerializeField] private int m_occludedVolume = -30;
    
    private float m_occlusionCheckTimer = 0f;
    private const float OCCLUSION_CHECK_INTERVAL = 0.15f; // Check ~6.6 times per second

    private string m_channelName = "ProximityLobby";

    public NetworkVariable<bool> IsSpeaking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> VivoxPlayerId = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public static List<VivoxVoiceManager> AllManagers = new List<VivoxVoiceManager>();

    private Dictionary<string, float> m_targetVolumes = new Dictionary<string, float>();
    private Dictionary<string, float> m_currentVolumes = new Dictionary<string, float>();
    private Dictionary<string, int> m_lastAppliedVolumes = new Dictionary<string, int>();

    public override async void OnNetworkSpawn()
    {
        AllManagers.Add(this);

        if (!IsOwner) return;

        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            // Set our Vivox ID for others to map us
            VivoxPlayerId.Value = AuthenticationService.Instance.PlayerId;

            await VivoxService.Instance.InitializeAsync();

            // --- THE FIX: Check if we are already in the channel from a previous host session! ---
            if (VivoxService.Instance.ActiveChannels.ContainsKey(m_channelName))
            {
                Debug.Log("Vivox: Restored previous 3D proximity channel connection.");
            }
            else
            {
                Channel3DProperties spatialProps = new Channel3DProperties(m_audibleDistance, m_conversationalDistance, m_audioFadeIntensity, m_audioFadeModel);
                await VivoxService.Instance.JoinPositionalChannelAsync(m_channelName, ChatCapability.AudioOnly, spatialProps);
                Debug.Log("Vivox: Successfully joined 3D proximity channel!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox failed to connect: {e}");
        }
    }

    private void Update()
    {
        // 1. UPDATE UI FOR EVERYONE
        if (m_billboardUI != null)
        {
            m_billboardUI.ToggleSpeakingIcon(IsSpeaking.Value);
        }

        // 2. ONLY THE OWNER CHECKS THEIR OWN MIC
        if (!IsOwner) return;

        bool amISpeaking = false;

        if (VivoxService.Instance != null && 
            VivoxService.Instance.IsLoggedIn && 
            VivoxService.Instance.ActiveChannels.ContainsKey(m_channelName))
        {
            var activeChannel = VivoxService.Instance.ActiveChannels[m_channelName];

            try
            {
                // Sync our 3D position
                if (m_avatarHeadTransform != null)
                {
                    VivoxService.Instance.Set3DPosition(m_avatarHeadTransform.gameObject, m_channelName);
                }
                else if (m_playerCamera != null && m_playerCamera.CameraTransform != null)
                {
                    VivoxService.Instance.Set3DPosition(m_playerCamera.CameraTransform.gameObject, m_channelName);
                }
            }
            catch { /* Vivox throws if channel is not fully connected yet, ignore until ready */ }

            // Check physical microphone
            try
            {
                foreach (var participant in activeChannel)
                {
                    if (participant.IsSelf)
                    {
                        amISpeaking = participant.SpeechDetected;
                    }
                    else
                    {
                        string pid = participant.PlayerId;
                        if (!m_targetVolumes.ContainsKey(pid)) m_targetVolumes[pid] = 0f;
                        if (!m_currentVolumes.ContainsKey(pid)) m_currentVolumes[pid] = 0f;
                        
                        m_currentVolumes[pid] = Mathf.Lerp(m_currentVolumes[pid], m_targetVolumes[pid], Time.deltaTime * 6f);
                        
                        // Prevent micro-jitters by snapping to target if very close
                        if (Mathf.Abs(m_currentVolumes[pid] - m_targetVolumes[pid]) < 0.5f)
                        {
                            m_currentVolumes[pid] = m_targetVolumes[pid];
                        }
                        
                        int applied = Mathf.RoundToInt(m_currentVolumes[pid]);
                        if (!m_lastAppliedVolumes.ContainsKey(pid) || m_lastAppliedVolumes[pid] != applied)
                        {
                            participant.SetLocalVolume(applied);
                            m_lastAppliedVolumes[pid] = applied;
                        }
                    }
                }
            }
            catch { /* Ignore loading errors */ }

                // 4. AUDIO OCCLUSION CHECKS
                m_occlusionCheckTimer += Time.deltaTime;
                if (m_occlusionCheckTimer >= OCCLUSION_CHECK_INTERVAL && m_avatarHeadTransform != null)
                {
                    m_occlusionCheckTimer = 0f;
                    
                    foreach (var manager in AllManagers)
                    {
                        if (manager == this || manager.m_avatarHeadTransform == null) continue;
                        
                        string targetPlayerId = manager.VivoxPlayerId.Value.ToString();
                        if (string.IsNullOrEmpty(targetPlayerId)) continue;
                        
                        // Find participant
                        VivoxParticipant targetParticipant = null;
                        foreach(var p in activeChannel)
                        {
                            if (p.PlayerId == targetPlayerId)
                            {
                                targetParticipant = p;
                                break;
                            }
                        }
                        
                        if (targetParticipant != null && !targetParticipant.IsSelf)
                        {
                            Vector3 start = m_avatarHeadTransform.position;
                            Vector3 end = manager.m_avatarHeadTransform.position;
                            Vector3 dir = end - start;
                            float dist = dir.magnitude;
                            
                            // Prevent Unity physics crash if players are literally inside each other
                            if (dist < 0.01f)
                            {
                                m_targetVolumes[targetPlayerId] = 0;
                                continue;
                            }
                            
                            // Volumetric Multi-Raycast (Cylinder)
                            // We shoot 5 parallel rays to simulate a realistic volume of sound.
                            // If a tiny pole blocks the center ray, it only muffles the sound by 20%.
                            // If a wall blocks all 5 rays, it muffles by 100%.
                            Vector3 fwd = dir.normalized;
                            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
                            Vector3 up = Vector3.Cross(fwd, right).normalized;

                            float spread = m_occlusionRadius;
                            Vector3[] offsets = new Vector3[] {
                                Vector3.zero,           // Center
                                right * spread,         // Right
                                -right * spread,        // Left
                                up * spread,            // Top
                                -up * spread            // Bottom
                            };

                            int blockedRays = 0;
                            foreach (var offset in offsets)
                            {
                                if (Physics.Linecast(start + offset, end + offset, m_occlusionLayers))
                                {
                                    blockedRays++;
                                }
                            }

                            // Calculate final volume drop based on how much of the cylinder was blocked
                            float blockRatio = (float)blockedRays / offsets.Length;
                            m_targetVolumes[targetPlayerId] = m_occludedVolume * blockRatio;
                        }
                    }
                }
        }

        // 3. Sync our status over the network
        IsSpeaking.Value = amISpeaking;
    }

    public override void OnNetworkDespawn()
    {
        AllManagers.Remove(this);

        if (IsOwner && VivoxService.Instance != null)
        {
            // --- THE FIX: Only try to leave if we are actually currently in it! ---
            if (VivoxService.Instance.ActiveChannels.ContainsKey(m_channelName))
            {
                VivoxService.Instance.LeaveChannelAsync(m_channelName);
            }
        }
    }

    public override void OnDestroy()
    {
        AllManagers.Remove(this);
        base.OnDestroy();
    }
}