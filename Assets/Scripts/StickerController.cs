using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StickerController : MonoBehaviour
{
    public InputAction placeStickerInput;
    public float collisionRange = 0.5f;
    public LayerMask stickerSpotLayer;
    public bool inRangeOfSticker;
    public GameObject activeStickerSpot;
    public GameObject stickerPrefab;
    public Transform stickerSpawnLocation;

    private void Awake()
    {
        placeStickerInput.Enable();
        placeStickerInput.performed += PlaceSticker;
    }

    private void PlaceSticker(InputAction.CallbackContext callbackContext)
    {
        if (!inRangeOfSticker) return;
        if(activeStickerSpot != null) activeStickerSpot.GetComponent<StickerSpot>().StickerPlaced();
        StartCoroutine(StickerCoroutine(activeStickerSpot.GetComponent<StickerSpot>().targetPosition.transform.position));

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

    private IEnumerator StickerCoroutine(Vector3 target)
    {
        GameObject sticker = Instantiate(stickerPrefab, stickerSpawnLocation.position, Quaternion.identity);
        while (true)
        {
            sticker.transform.position = Vector3.MoveTowards(sticker.transform.position, target, 30f*Time.fixedDeltaTime);
            if(sticker.transform.position == target)
            {
                break;
            }
            yield return null;
        }
    }
}
