using UnityEngine;

public class NpcBillboard : MonoBehaviour
{
    private Camera m_mainCamera;

    private void LateUpdate()
    {
        // 1. If we don't have a camera, OR the camera we grabbed is turned off, find the active one!
        if (m_mainCamera == null || !m_mainCamera.isActiveAndEnabled)
        {
            m_mainCamera = Camera.main;
            
            // If it is still null (no active cameras), safely skip this frame
            if (m_mainCamera == null) return;
        }

        transform.forward = m_mainCamera.transform.forward;
    }
}