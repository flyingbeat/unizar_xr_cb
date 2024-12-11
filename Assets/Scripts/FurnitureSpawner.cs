using System;
using System.Collections.Generic;
using UnityEngine;
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
        GameObject spawningObject = (prefab != null) ? prefab : furniturePrefabs[spawnOptionIndex];
        BoxCollider objectBoundingBox = spawningObject.GetComponent<BoxCollider>();
        if (objectBoundingBox == null)
        {
            objectBoundingBox = spawningObject.GetComponentInChildren<BoxCollider>();
        }
        Debug.Log($"Spawning: {spawningObject.name}");
        if (!Enum.TryParse(spawningObject.tag, out PlaneClassifications associatedPlaneClassification) || objectBoundingBox == null)
        {
            Debug.LogWarning($"Invalid tag {spawningObject.tag} for furniture prefab.");
            throw new ArgumentException("Prefabs have to be tagged according to their associated plane classification and have a BoxCollider component.");
        }
        (Vector3 randomPosition, Quaternion rotation) = m_ARSpaceManager.GetRandomFreePointOnPlane(associatedPlaneClassification, objectBoundingBox.size * spawningObject.transform.localScale.x);
        return base.TrySpawnObject(randomPosition, rotation, spawningObject);
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
        int maxObjects = 20;
        if (limit == -1)
        {
            limit = objectPrefabs.Count;
        }
        else if (limit > objectPrefabs.Count)
        {
            Debug.LogWarning("Limit is greater than the number of prefabs. Spawning all prefabs.");
            limit = objectPrefabs.Count;
        }

        if (limit > maxObjects) limit = maxObjects;
        for (int i = 0; i < limit; i++)
        {
            spawnOptionIndex = i;
            if (!TrySpawnOnPlane())
            {
                Debug.LogWarning("Failed to spawn object " + objectPrefabs[spawnOptionIndex].name);
            }
        }
    }

}