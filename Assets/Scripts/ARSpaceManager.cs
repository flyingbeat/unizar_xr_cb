using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSpaceManager : MonoBehaviour
{

    void Start()
    {
        Random.InitState(System.DateTime.Now.Millisecond);

    }

    // ARPlanes
    [SerializeField]
    ARPlaneManager m_ARPlaneManager;
    public bool planesEnabled
    {
        get { return m_ARPlaneManager.enabled; }
        set { m_ARPlaneManager.enabled = value; }
    }

    private bool m_planesVisualized = false;
    public bool planesVisualized
    {
        get { return m_planesVisualized; }
        set
        {
            if (m_planesVisualized == value)
                return;
            if (!planesEnabled)
                throw new System.Exception("Planes must be enabled to visualize them");

            m_planesVisualized = value;
            float fillAlpha = value ? 0.3f : 0.0f;
            float lineAlpha = value ? 1.0f : 0.0f;
            SetTrackablesAlpha(m_ARPlaneManager.trackables, fillAlpha, lineAlpha);
        }
    }

    public TrackableCollection<ARPlane> planes
    {
        get { return m_ARPlaneManager.trackables; }
    }

    public List<ARPlane> GetPlanesByClassification(PlaneClassifications classification)
    {
        List<ARPlane> filteredPlanes = new();
        foreach (ARPlane plane in planes)
        {
            if (plane.classifications.HasFlag(classification))
            {
                filteredPlanes.Add(plane);
            }
        }
        return filteredPlanes;
    }

    public (Vector3, Quaternion) GetRandomFreePointOnPlane(PlaneClassifications classification)
    {
        List<ARPlane> planes = GetPlanesByClassification(classification);
        if (planes.Count == 0)
        {
            throw new System.Exception($"No planes found for classification {classification}");
        }
        ARPlane randomPlane = planes[Random.Range(0, planes.Count)];
        return GetRandomFreePointOnPlane(randomPlane);
    }

    public (Vector3, Quaternion) GetRandomFreePointOnPlane(ARPlane plane)
    {
        (Vector3, Quaternion) randomPointOnPlane;
        int tries = 0;
        do
        {
            randomPointOnPlane = GetRandomPointOnPlane(plane);
            tries++;
        }
        while (tries < 150 && HasCollision(randomPointOnPlane.Item1, plane));
        Debug.Log("tries: " + tries); // check for colision with other objects
        if (tries == 150) Debug.Log("Failed to get a free position: " + randomPointOnPlane.Item1);
        return randomPointOnPlane;
    }

    private bool HasCollision(Vector3 position, ARPlane plane, float collisionRadius = 0.2f)
    {
        Collider[] colliders = new Collider[10];
        int numColliders = Physics.OverlapSphereNonAlloc(position, collisionRadius, colliders);
        Collider[] filtered = colliders.Take(numColliders).Where(collider =>
        {
            ARBoundingBox collidedBoundingBox = collider.gameObject.GetComponent<ARBoundingBox>();
            ARPlane collidedPlane = collider.gameObject.GetComponent<ARPlane>();
            bool hasTag = collider.gameObject.tag.Equals("Untagged");
            if (!hasTag) return false; // ignore weird objects that are not tagged
            bool isTable = plane.classifications.HasFlag(PlaneClassifications.Table);
            if (isTable && collidedBoundingBox != null && collidedBoundingBox.classifications.HasFlag(BoundingBoxClassifications.Table))
            {
                return false; // exclude table boundingbox from collision if the plane is a table
            }
            if (collidedPlane != null && (collidedPlane.trackableId == plane.trackableId || collidedPlane.classifications.HasFlag(PlaneClassifications.Other)))
            {
                return false; // exclude the plane itself from collision and the boundry which is classified as other
            }
            return true; // include all other colliders
        }).ToArray();
        return filtered.Length > 0;
    }

    private (Vector3, Quaternion) GetRandomPointOnPlane(ARPlane plane)
    {
        if (plane.alignment == PlaneAlignment.Vertical)
        {
            return GetRandomPointOnVerticalPlane(plane);
        }
        else  // Horizontal 
        {
            return GetRandomPointOnHorizontalPlane(plane);
        }
    }

    private static (Vector3, Quaternion) GetRandomPointOnHorizontalPlane(ARPlane plane)
    {
        //Vector3 -> Y is up (normal), Z is height, X is width
        // Vector2 -> X is width, Y is height
        Debug.Log("plane.center: " + plane.center);
        Debug.Log("plane.extents: " + plane.extents);
        Debug.Log("Range x:" + (plane.center.x - plane.extents.x) + "range Z: " + (plane.center.x + plane.extents.x));
        float randomX = Random.Range(plane.center.x - plane.extents.x, plane.center.x + plane.extents.x);
        float randomZ = Random.Range(plane.center.z - plane.extents.y, plane.center.z + plane.extents.y);
        return (new Vector3(randomX, plane.center.y, randomZ), Quaternion.identity);
    }

    private static (Vector3, Quaternion) GetRandomPointOnVerticalPlane(ARPlane plane)
    {
        // Vector2 -> X is width, Y is height
        Quaternion rotation = Quaternion.LookRotation(plane.normal);
        if (plane.classifications.HasFlag(PlaneClassifications.WindowFrame) || plane.classifications.HasFlag(PlaneClassifications.DoorFrame))
        {
            return (plane.center, rotation);
        }

        // determine direction of the normal with respect to the axes
        if (Mathf.Abs(plane.normal.x) > 0.9f) // Vector3 -> Y is height, Z is width, X is forward (normal)
        {
            float randomZ = Random.Range(plane.center.z - plane.extents.x, plane.center.z + plane.extents.x);
            float randomY = Random.Range(plane.center.y - plane.extents.y, plane.center.y + plane.extents.y);
            return (new Vector3(plane.center.x, randomY, randomZ), rotation);

        }
        else // Vector3 -> Y is height, X is width, Z is forward (normal)
        {
            float randomX = Random.Range(plane.center.x - plane.extents.x, plane.center.x + plane.extents.x);
            float randomY = Random.Range(plane.center.y - plane.extents.y, plane.center.y + plane.extents.y);
            return (new Vector3(randomX, randomY, plane.center.z), rotation);
        }
    }

    // ARBoundingBoxes
    [SerializeField]
    private ARBoundingBoxManager m_ARBoundingBoxManager;

    public bool boundingBoxesEnabled
    {
        get { return m_ARBoundingBoxManager.enabled; }
        set { m_ARBoundingBoxManager.enabled = value; }
    }

    private bool m_boundingBoxesVisualized = false;
    public bool boundingBoxesVisualized
    {
        get { return m_boundingBoxesVisualized; }
        set
        {
            if (m_boundingBoxesVisualized == value)
                return;
            if (!boundingBoxesEnabled)
                throw new System.Exception("Bounding boxes must be enabled to visualize them");

            m_boundingBoxesVisualized = value;
            float fillAlpha = value ? 0.3f : 0.0f;
            float lineAlpha = value ? 1.0f : 0.0f;
            SetTrackablesAlpha(m_ARBoundingBoxManager.trackables, fillAlpha, lineAlpha);
        }
    }

    // Common methods
    private void SetTrackablesAlpha<T>(TrackableCollection<T> trackables, float fillAlpha, float lineAlpha) where T : ARTrackable
    {
        foreach (ARTrackable trackable in trackables)
        {
            MeshRenderer meshRenderer = trackable.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color color = meshRenderer.material.color;
                color.a = fillAlpha;
                meshRenderer.material.color = color;
            }
            LineRenderer lineRenderer = trackable.GetComponentInChildren<LineRenderer>();
            if (lineRenderer != null)
            {
                Color startColor = lineRenderer.startColor;
                Color endColor = lineRenderer.endColor;

                startColor.a = lineAlpha;
                endColor.a = lineAlpha;
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }
        }

    }

}