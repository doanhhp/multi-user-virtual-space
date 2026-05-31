using UnityEngine;

public class SeatReplacer : MonoBehaviour
{
    [Header("=== Prefab ghế mới ===")]
    public GameObject seatPrefab;
    public Vector3 seatPrefabScale  = new Vector3(0.3f, 0.3f, 0.3f);
    public Vector3 seatPrefabOffset = new Vector3(0, 0, 0);
    public bool    autoAddCollider  = true;

    public void ReplaceSeats()
    {
        if (seatPrefab == null)
        {
            Debug.LogError("[SeatReplacer] Chưa assign Seat Prefab!");
            return;
        }

        // Tự dùng chính transform này làm parent (vì script gắn vào Seats)
        Transform seatsParent = transform;
        int count = 0;

        for (int i = seatsParent.childCount - 1; i >= 0; i--)
        {
            Transform oldSeat = seatsParent.GetChild(i);

            // Bỏ qua object không phải ghế (Step, Aisle...)
            if (!oldSeat.name.StartsWith("Seat_")) continue;

            Vector3    pos  = oldSeat.localPosition;
            Quaternion rot  = oldSeat.localRotation;
            string     name = oldSeat.name;

            DestroyImmediate(oldSeat.gameObject);

            GameObject newSeat = Instantiate(seatPrefab, Vector3.zero, rot);
            newSeat.name = name;
            newSeat.transform.SetParent(seatsParent);
            newSeat.transform.localPosition = pos + seatPrefabOffset;
            newSeat.transform.localScale    = seatPrefabScale;

            if (autoAddCollider && newSeat.GetComponentInChildren<Collider>() == null)
                newSeat.AddComponent<BoxCollider>();

            count++;
        }

        Debug.Log($"[SeatReplacer] Đã thay {count} ghế thành công!");
    }

    public void RestoreDefaultSeats()
    {
        Transform seatsParent = transform;

        for (int i = seatsParent.childCount - 1; i >= 0; i--)
        {
            Transform child = seatsParent.GetChild(i);
            if (child.name.StartsWith("Seat_"))
                DestroyImmediate(child.gameObject);
        }

        Debug.Log("[SeatReplacer] Đã xóa hết ghế. Generate Room lại để về primitive.");
    }
}