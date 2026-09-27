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
        if (activeStickerSpot != null)
        {
            StickerSpot stickerSpot = activeStickerSpot.GetComponent<StickerSpot>();
            if (stickerSpot.stickerPlaced == false)
            {
                StartCoroutine(StickerCoroutine(stickerSpot.targetPosition.transform));

                stickerSpot.StickerPlaced();
            }
        }


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

    private IEnumerator StickerCoroutine(Transform target)
    {
        GameObject sticker = Instantiate(stickerPrefab, stickerSpawnLocation.position, target.rotation);
        Transform t = sticker.transform;

        while (t.position != target.position)
        {
            t.position = Vector3.MoveTowards(t.position, target.position, 30f * Time.deltaTime);
            t.rotation = target.rotation; // follow the target's current world rotation
            yield return null;
        }

        // Snap exactly on arrival
        t.SetPositionAndRotation(target.position, target.rotation);
    }
}
