using UnityEngine;
using UnityEngine.Events;
using StarterAssets;

public class StickerSpot : MonoBehaviour
{
    public bool stickerPlaced = false;
    public UnityEvent OnStickerPlaced;
    public GameObject targetPosition;
    public GameObject FX;
    public void StickerPlaced()
    {
        stickerPlaced = true;
        OnStickerPlaced?.Invoke();
        ActionPointManager.instance.RegenAP();
    }

    private void Start()
    {
        targetPosition.GetComponent<MeshRenderer>().enabled = false;    
    }

    public void DisplayFX()
    {
        FX.SetActive(true);
    }
}
