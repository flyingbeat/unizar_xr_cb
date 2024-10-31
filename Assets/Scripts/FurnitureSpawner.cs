using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FurnitureSpawner : MonoBehaviour
{

    [SerializeField]
    [Tooltip("The list of prefabs available to spawn.")]
    List<GameObject> m_FurniturePrefabs = new List<GameObject>();

    [SerializeField]
    [Tooltip("Whether to spawn each object as a child of this object.")]
    bool m_SpawnAsChildren;

    /// <summary>
    /// Whether to spawn each object as a child of this object.
    /// </summary>
    public bool spawnAsChildren
    {
        get => m_SpawnAsChildren;
        set => m_SpawnAsChildren = value;
    }

    /// <summary>
    /// Event invoked after an object is spawned.
    /// </summary>
    /// <seealso cref="TrySpawnObject"/>
    public event Action<GameObject> objectSpawned;


    [SerializeField]
    ARPlaneManager m_ARPlaneManager;

    [SerializeField]
    ARBoundingBoxManager m_ARBoundingBoxManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_ARPlaneManager != null)
        {
            m_ARPlaneManager.trackablesChanged.AddListener(OnPlanesChanged);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {   
        // planes are added at the start of the app
        if (eventArgs.added.Count > 0)
        {
            Debug.Log("Planes added" + eventArgs.ToString());
            // for each plane that is added and is a wall, spawn furniture
            foreach (ARPlane addedPlane in eventArgs.added)
            {
                if (addedPlane.classifications.HasFlag(PlaneClassifications.WallFace))
                {
                    SpawnFurniture(addedPlane.center, addedPlane.normal);
                }
            }
        }

    }


    private void SpawnFurniture(Vector3 spawnPoint, Vector3 spawnNormal)
    {
        // iterate through the list of furniture prefabs
        foreach (GameObject furniturePrefab in m_FurniturePrefabs)
        {
            // spawn the furniture prefab
            var newObject = Instantiate(furniturePrefab);
            if (m_SpawnAsChildren)
                newObject.transform.parent = transform;

            // set the rotation to the same as direction of spawnNormal
            newObject.transform.SetPositionAndRotation(spawnPoint, Quaternion.LookRotation(spawnNormal));
            objectSpawned?.Invoke(newObject);
        }
    }


    private Vector3 getWalls()
    {
        foreach (ARPlane plane in m_ARPlaneManager.trackables)
        {
            Debug.Log("Plane: " + plane.classifications);
            if (plane.classifications.HasFlag(PlaneClassifications.WallFace))
            {
                Debug.Log("Wall found" + plane.center);
                return plane.center;
            }
        }
        Debug.LogError("No walls found");
        return Vector3.zero;
    }
}
