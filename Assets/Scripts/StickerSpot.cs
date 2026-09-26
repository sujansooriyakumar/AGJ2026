using UnityEngine;
using UnityEngine.Events;

public class StickerSpot : MonoBehaviour
{
    public bool stickerPlaced = false;
    public UnityEvent OnStickerPlaced;
    public void StickerPlaced()
    {
        stickerPlaced = true;
        OnStickerPlaced?.Invoke();
    }
}
