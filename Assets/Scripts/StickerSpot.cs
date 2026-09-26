using UnityEngine;
using UnityEngine.Events;
using StarterAssets;

public class StickerSpot : MonoBehaviour
{
    public bool stickerPlaced = false;
    public UnityEvent OnStickerPlaced;
    public void StickerPlaced()
    {
        stickerPlaced = true;
        OnStickerPlaced?.Invoke();
        ActionPointManager.instance.RegenAP();
    }
}
