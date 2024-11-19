using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class FurnitureSpawner : BaseObjectSpawner
{

    [SerializeField]
    List<GameObject> m_FixedFurniturePrefabs = new();

    [SerializeField]
    List<GameObject> m_ChangableFurniturePrefabs = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnFurniture(bool changeable, TrackableCollection<ARPlane> planes)
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
            else
            {
                furniturePrefab.layer = LayerMask.NameToLayer("Fixed");
            }
            List<ARPlane> planesWithClassification = GetPlanes(planes, associatedPlaneClassification);

            if (planesWithClassification.Count > 0)
            {
                ARPlane selectedPlane = planesWithClassification[UnityEngine.Random.Range(0, planesWithClassification.Count)];
                objectPrefabs.Add(furniturePrefab);
                TrySpawnObjectOnPlane(selectedPlane);
            }
            else
            {
                Debug.LogWarning($"No planes found for classification {associatedPlaneClassification}");
            }
            objectPrefabs.Clear();
        }

    }

    private List<ARPlane> GetPlanes(TrackableCollection<ARPlane> planes, PlaneClassifications planeClassification)
    {
        List<ARPlane> planesWithClassification = new();
        foreach (ARPlane plane in planes)
        {
            Debug.Log(plane.alignment);
            Debug.Log($"Plane classification: {plane.classifications} subsumedBy: {plane.subsumedBy}");
            if (plane.classifications.HasFlag(planeClassification))
            {
                planesWithClassification.Add(plane);
            }
        }
        Debug.Log($"Found {planesWithClassification.Count} planes with classification {planeClassification}");
        return planesWithClassification;
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {

    }

}