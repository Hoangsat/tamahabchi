using UnityEngine;

public class RoomVisualController : MonoBehaviour
{
    [SerializeField] private GameObject roomState0;
    [SerializeField] private GameObject roomState1;
    [SerializeField] private GameObject roomState2;

    public void ApplyRoom(RoomData roomData)
    {
        int level = roomData != null ? roomData.roomLevel : 0;

        if (roomState0 != null) roomState0.SetActive(level == 0);
        if (roomState1 != null) roomState1.SetActive(level == 1);
        if (roomState2 != null) roomState2.SetActive(level == 2);
    }
}
