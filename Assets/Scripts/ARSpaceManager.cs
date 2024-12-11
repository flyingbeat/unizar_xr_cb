using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSpaceManager : MonoBehaviour
{

    [SerializeField]
    private GameObject m_boundingBoxPrefab;

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

    public (Vector3, Quaternion) GetRandomFreePointOnPlane(PlaneClassifications classification, Vector3 size)
    {
        List<ARPlane> planes = GetPlanesByClassification(classification);
        if (planes.Count == 0)
        {
            throw new System.Exception($"No planes found for classification {classification}");
        }
        ARPlane randomPlane = planes[Random.Range(0, planes.Count)];
        return GetRandomFreePointOnPlane(randomPlane, size);
    }

    public (Vector3, Quaternion) GetRandomFreePointOnPlane(ARPlane plane, Vector3 size)
    {
        Collider[] lastColliders;
        (Vector3, Quaternion) randomPointOnPlane;
        int tries = 0;
        do
        {
            randomPointOnPlane = GetRandomPointOnPlane(plane);
            tries++;
        }
        while (HasCollision(randomPointOnPlane, plane, size, out lastColliders) && tries < 100);
        Debug.Log("tries : " + tries); // check for colision with other objects
        if (lastColliders.Length > 0) Debug.Log("Failed to get a free position: " + lastColliders[0].gameObject.name + ", " + "total nr: " + lastColliders.Length);
        return randomPointOnPlane;
    }

    private void VisualizeOverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
    {
        // Create a cube to represent the OverlapBox
        Debug.Log("Visualizing OverlapBox at");
        // Set the position and size to match OverlapBox parameters
        GameObject visualization = Instantiate(m_boundingBoxPrefab);
        visualization.transform.position = center;
        visualization.transform.localScale = new Vector3(halfExtents.z, halfExtents.z, halfExtents.z);

        Destroy(visualization, 5.0f);
    }

    private bool HasCollision((Vector3, Quaternion) position, ARPlane plane, Vector3 size, out Collider[] lastColliders)
    {
        int layerMask = 1 << LayerMask.NameToLayer("Objects");
        layerMask |= 1 << LayerMask.NameToLayer("Planes");
        layerMask |= 1 << LayerMask.NameToLayer("BoundingBoxes");
        bool isSpawningOnTable = plane.classifications.HasFlag(PlaneClassifications.Table);

        Vector3 halfExtents = size / 2;
        // move center of the box up by half the height of the box in the direction of the quaternion
        if (plane.alignment == PlaneAlignment.HorizontalDown)
        {
            halfExtents.y = -halfExtents.y;
        }
        Vector3 center = position.Item1 + (position.Item2 * new Vector3(0, halfExtents.y, 0));

        //VisualizeOverlapBox(center, halfExtents, position.Item2);
        Collider[] colliders = new Collider[10];
        int numColliders = Physics.OverlapSphereNonAlloc(center, halfExtents.z, colliders);
        Collider[] filtered = colliders.Take(numColliders).Where(collider =>
        {
            ARBoundingBox collidedBoundingBox = collider.gameObject.GetComponent<ARBoundingBox>();
            ARPlane collidedPlane = collider.gameObject.GetComponent<ARPlane>();
            bool isPlane = collidedPlane != null;
            bool isBoundingBox = collidedBoundingBox != null;
            bool hasNoTag = collider.gameObject.tag.Equals("Untagged");
            Debug.Log("tag: " + collider.gameObject.tag);
            if (hasNoTag && !isBoundingBox && !isPlane) return false; // ignore weird objects that are not tagged
            if (isSpawningOnTable && isBoundingBox && collidedBoundingBox.classifications.HasFlag(BoundingBoxClassifications.Table))
            {
                return false; // exclude table boundingbox from collision if the plane is a table
            }
            if (isPlane && (collidedPlane.trackableId == plane.trackableId || collidedPlane.classifications.HasFlag(PlaneClassifications.Other)))
            {
                return false; // exclude the plane itself from collision and the boundry which is classified as other
            }
            return true; // include all other colliders
        }).ToArray();
        lastColliders = filtered;
        return filtered.Length > 0;
    }

    private static (Vector3, Quaternion) GetRandomPointOnPlane(ARPlane plane, float marginPercentage = 25f)
    {
        // Convert the plane's normal to a rotation (local Z-axis is normal direction)
        Quaternion rotation =
            plane.alignment == PlaneAlignment.Vertical ?
                Quaternion.LookRotation(plane.normal, Vector3.up) :
                    Quaternion.identity;

        // Special case: WindowFrame or DoorFrame classifications
        if (plane.classifications.HasFlag(PlaneClassifications.WindowFrame) ||
            plane.classifications.HasFlag(PlaneClassifications.DoorFrame))
        {
            return (plane.center, rotation);
        }

        // Calculate the margin based on the percentage of the plane's extents
        float marginX = plane.extents.x * Mathf.Clamp01(marginPercentage / 100f); // Clamp to [0, 100%]
        float marginY = plane.extents.y * Mathf.Clamp01(marginPercentage / 100f);

        // Randomly pick a point within the plane's extents minus the margin
        float randomHeight = Random.Range(-plane.extents.y + marginY, plane.extents.y - marginY); // Vertical direction
        float randomWidth = Random.Range(-plane.extents.x + marginX, plane.extents.x - marginX); // Horizontal direction

        // Construct a random local point within the plane's bounds
        Vector3 randomLocalPoint = new Vector3(randomWidth, 0, randomHeight); // XZ plane for vertical planes

        // Transform the random point to world space
        Vector3 randomWorldPoint = plane.transform.TransformPoint(randomLocalPoint);

        // Return the point and the plane's rotation
        return (randomWorldPoint, rotation);
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