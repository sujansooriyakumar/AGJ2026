using UnityEngine;
using UnityEngine.InputSystem;

public class StickerController : MonoBehaviour
{
    public InputAction placeStickerInput;
    public float collisionRange = 0.5f;
    public LayerMask stickerSpotLayer;
    public bool inRangeOfSticker;
    public GameObject activeStickerSpot;

    private void Awake()
    {
        placeStickerInput.Enable();
        placeStickerInput.performed += PlaceSticker;
    }

    private void PlaceSticker(InputAction.CallbackContext callbackContext)
    {
        if (!inRangeOfSticker) return;
        if(activeStickerSpot != null) activeStickerSpot.GetComponent<StickerSpot>().StickerPlaced();

    }

    private void Update()
    {
        // check if in range of stickerspot 
        inRangeOfSticker =  Physics.CheckSphere(transform.position, collisionRange, stickerSpotLayer);
        Collider[] results = Physics.OverlapSphere(transform.position, collisionRange, stickerSpotLayer);
        if (inRangeOfSticker)
        {
            activeStickerSpot = results[0].gameObject;
        }
        else
        {
            activeStickerSpot = null;
        }
    }
}
