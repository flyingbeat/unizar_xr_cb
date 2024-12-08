using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FurnitureSpawner : BaseObjectSpawner
{

    public List<GameObject> furniturePrefabs
    {
        get { return objectPrefabs; }
        set { objectPrefabs = value; }
    }

    [SerializeField]
    ARSpaceManager m_ARSpaceManager;

    public bool TrySpawnOnPlane(GameObject prefab = null)
    {
        string tag = (prefab != null) ? prefab.tag : furniturePrefabs[spawnOptionIndex].tag;
        if (!Enum.TryParse(tag, out PlaneClassifications associatedPlaneClassification))
        {
            Debug.LogWarning($"Invalid tag {tag} for furniture prefab.");
            throw new ArgumentException("Prefabs have to be tagged according to their associated plane classification.");
        }
        (Vector3 randomPosition, Quaternion rotation) = m_ARSpaceManager.GetRandomFreePointOnPlane(associatedPlaneClassification);
        return base.TrySpawnObject(randomPosition, rotation, prefab);
    }

    public bool TrySpawnOnPlane(int furnitureIndex)
    {
        spawnOptionIndex = furnitureIndex;
        return TrySpawnOnPlane();
    }

    public bool TrySpawnObject(Vector3 position, Quaternion rotation, int furnitureIndex)
    {
        spawnOptionIndex = furnitureIndex;
        return base.TrySpawnObject(position, rotation);
    }

    public void SpawnAll(int limit = -1)
    {
        if (limit == -1)
        {
            limit = objectPrefabs.Count;
        }
        else if (limit > objectPrefabs.Count)
        {
            Debug.LogWarning("Limit is greater than the number of prefabs. Spawning all prefabs.");
            limit = objectPrefabs.Count;
        }

        for (int i = 0; i < limit; i++)
        {
            spawnOptionIndex = i;
            if (!TrySpawnOnPlane())
            {
                Debug.LogWarning("Failed to spawn object " + objectPrefabs[i].name);
            }
        }
    }

}