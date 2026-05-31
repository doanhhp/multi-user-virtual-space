using UnityEngine;
using UnityEngine.UIElements; // Required for UI Toolkit

/// <summary>
/// This script makes the UI Document showing player tag / name always face the camera.
/// It now also handles the Speaking Indicator!
/// </summary>
public class BillboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument m_uiDocument; // We need this to find the icon
    
    private Camera m_mainCamera; // Store the Camera instead of the Transform
    private VisualElement m_speakerIcon;

    private void Awake()
    {
        // REMOVED Camera.main from here to prevent the NullReference crash on spawn!
        
        // Find the speaking indicator from the UI Document
        if (m_uiDocument != null && m_uiDocument.rootVisualElement != null)
        {
            m_speakerIcon = m_uiDocument.rootVisualElement.Q<VisualElement>("SpeakerIndicator");
            
            // Hide it by default when the player spawns
            if (m_speakerIcon != null)
            {
                m_speakerIcon.style.display = DisplayStyle.None;
            }
        }
    }

    private void LateUpdate()
    {
        // 1. Safely find the camera if we don't have it yet
        if (m_mainCamera == null)
        {
            m_mainCamera = Camera.main;
            
            // If the camera is still off/missing its tag, skip this frame safely so we don't crash
            if (m_mainCamera == null) return; 
        }
        
        // 2. Face the camera
        Vector3 direction = transform.position - m_mainCamera.transform.position;
        direction.y = 0; // Keep the billboard upright
        if(direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // Our Vivox script will call this whenever the player starts/stops talking
    public void ToggleSpeakingIcon(bool isSpeaking)
    {
        if (m_speakerIcon != null)
        {
            m_speakerIcon.style.display = isSpeaking ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}