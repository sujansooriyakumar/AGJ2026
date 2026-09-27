using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject leftBuildingPrefab;
    [SerializeField] private GameObject rightBuildingPrefab;

    [Header("Spawn / Despawn Distances")]
    [SerializeField] private float spawnAheadDistance = 150f;
    [SerializeField] private float despawnBehindDistance = 30f;

    [Header("Z Spacing")]
    [SerializeField] private float minGap = 2f;
    [SerializeField] private float maxGap = 6f;

    [Header("Side Placement")]
    [SerializeField] private float laneHalfWidth = 5f;
    [SerializeField] private float xJitter = 0.5f;
    [SerializeField] private bool startOnRight = false;

    [Header("Start")]
    [SerializeField] private float startZ = 0f;
    [SerializeField] private int easyStartBuildings = 3;

    private class ActiveBuilding
    {
        public GameObject instance;
        public bool isRight;
        public float endZ;
    }

    private readonly Queue<ActiveBuilding> activeBuildings = new Queue<ActiveBuilding>();
    private readonly Queue<GameObject> leftPool = new Queue<GameObject>();
    private readonly Queue<GameObject> rightPool = new Queue<GameObject>();

    private float nextSpawnZ;
    private int spawnedCount;
    private bool nextOnRight;

    private void Start()
    {
        nextSpawnZ = startZ;
        nextOnRight = startOnRight;

        if (CanSpawn())
            UpdateBuildings();
    }

    private void Update()
    {
        if (!CanSpawn()) return;
        UpdateBuildings();
    }

    private bool CanSpawn()
    {
        return player != null && leftBuildingPrefab != null && rightBuildingPrefab != null;
    }

    private void UpdateBuildings()
    {
        float playerZ = player.position.z;

        while (nextSpawnZ < playerZ + spawnAheadDistance)
            SpawnNext();

        while (activeBuildings.Count > 0 &&
               activeBuildings.Peek().endZ < playerZ - despawnBehindDistance)
        {
            Despawn(activeBuildings.Dequeue());
        }
    }

    private void SpawnNext()
    {
        bool easy = spawnedCount < easyStartBuildings;
        bool isRight = nextOnRight;

        GameObject building = GetFromPool(isRight);

        building.transform.SetPositionAndRotation(
            new Vector3(transform.position.x, transform.position.y, nextSpawnZ),
            Quaternion.identity);
        building.SetActive(true);

        Bounds b = GetBounds(building);
        float jitter = easy ? 0f : Random.Range(-xJitter, xJitter);
        float centreX = transform.position.x;

        float xCorrection = isRight
            ? (centreX + laneHalfWidth + jitter) - b.min.x
            : (centreX - laneHalfWidth + jitter) - b.max.x;

        Vector3 correction = new Vector3(
            xCorrection,
            transform.position.y - b.min.y,
            nextSpawnZ - b.min.z);
        building.transform.position += correction;

        float endZ = b.max.z + correction.z;
        activeBuildings.Enqueue(new ActiveBuilding
        {
            instance = building,
            isRight = isRight,
            endZ = endZ
        });

        float gap = easy ? minGap : Random.Range(minGap, maxGap);
        nextSpawnZ = endZ + gap;
        nextOnRight = !nextOnRight;
        spawnedCount++;
    }

    private GameObject GetFromPool(bool isRight)
    {
        Queue<GameObject> pool = isRight ? rightPool : leftPool;
        if (pool.Count > 0) return pool.Dequeue();

        GameObject prefab = isRight ? rightBuildingPrefab : leftBuildingPrefab;
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    private void Despawn(ActiveBuilding building)
    {
        building.instance.SetActive(false);
        (building.isRight ? rightPool : leftPool).Enqueue(building.instance);
    }

    private static Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}