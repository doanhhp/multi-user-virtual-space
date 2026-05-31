using System.Collections.Generic;
using UnityEngine;

public class SeatPrefabReplacer : MonoBehaviour
{
    [Header("Parent Containing OLD Seats")]
    public Transform seatParent;

    [Header("New Seat Prefab")]
    public GameObject newSeatPrefab;

    [ContextMenu("Replace All Seats")]
    public void ReplaceAllSeats()
    {
        if (seatParent == null || newSeatPrefab == null)
        {
            Debug.LogWarning("Missing references!");
            return;
        }

        // Lưu ghế cũ trước
        List<Transform> oldSeats = new List<Transform>();

        foreach (Transform child in seatParent)
        {
            oldSeats.Add(child);
        }

        // Replace
        foreach (Transform oldSeat in oldSeats)
        {
            Vector3 pos = oldSeat.position;
            Quaternion rot = oldSeat.rotation;
            Vector3 scale = oldSeat.localScale;

            GameObject newSeat = Instantiate(
                newSeatPrefab,
                pos,
                rot,
                seatParent
            );

            newSeat.transform.localScale = scale;

            // Xóa ghế cũ
            DestroyImmediate(oldSeat.gameObject);
        }

        Debug.Log("Seat replacement completed!");
    }
}