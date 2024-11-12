using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FurnitureSpawner : BaseObjectSpawner
{

    [SerializeField]
    ARPlaneManager m_ARPlaneManager;

    [SerializeField]
    List<GameObject> m_FixedFurniturePrefabs = new List<GameObject>();

    [SerializeField]
    List<GameObject> m_ChangableFurniturePrefabs = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_ARPlaneManager != null)
        {
            //Debug.Log("Spawn furniture is called");
            //SpawnFurniture(changeable: true);
            //SpawnFurniture(changeable: false);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnFurniture(bool changeable)
    {
        objectPrefabs.Clear();
        List<GameObject> furniturePrefabs = changeable ? m_ChangableFurniturePrefabs : m_FixedFurniturePrefabs;
        foreach (GameObject furniturePrefab in furniturePrefabs)
        {
            if (!Enum.TryParse(furniturePrefab.tag, out PlaneClassifications associatedPlaneClassification))
            {
                Debug.LogWarning($"Invalid tag {furniturePrefab.tag} for furniture prefab.");
                throw new ArgumentException("Prefabs have to be tagged according to their associated plane classification.");
            }
            if (changeable)
            {
                furniturePrefab.layer = LayerMask.NameToLayer("Changeable");
            }
            objectPrefabs.Add(furniturePrefab);
            List<ARPlane> planes = GetPlanes(associatedPlaneClassification);

            if (planes.Count > 0)
            {
                ARPlane selectedPlane = planes[UnityEngine.Random.Range(0, planes.Count)];
                Quaternion rotation = Quaternion.identity;
                if (selectedPlane.classifications.HasFlag(PlaneClassifications.WallFace))
                {
                    rotation = Quaternion.LookRotation(selectedPlane.normal);
                    applyRandomAngleAtSpawn = false;
                }
                else
                {
                    applyRandomAngleAtSpawn = true;
                }
                TrySpawnObject(selectedPlane.center, rotation);
            }
            else
            {
                Debug.LogWarning($"No planes found for classification {associatedPlaneClassification}");
            }
            objectPrefabs.Clear();
        }

    }

    private List<ARPlane> GetPlanes(PlaneClassifications planeClassification)
    {
        List<ARPlane> planes = new();
        foreach (ARPlane plane in m_ARPlaneManager.trackables)
        {
            Debug.Log($"Plane classification: {plane.classifications}");
            if (plane.classifications.HasFlag(planeClassification))
            {
                planes.Add(plane);
            }
        }
        Debug.Log($"Found {planes.Count} planes with classification {planeClassification}");
        return planes;
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {

    }

}