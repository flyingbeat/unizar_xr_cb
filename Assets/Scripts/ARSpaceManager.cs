using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSpaceManager : MonoBehaviour
{

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

    public (Vector3, Quaternion) GetRandomPointOnPlane(PlaneClassifications classification)
    {
        List<ARPlane> planes = GetPlanesByClassification(classification);
        if (planes.Count == 0)
        {
            throw new System.Exception($"No planes found for classification {classification}");
        }
        ARPlane randomPlane = planes[Random.Range(0, planes.Count)];
        return GetRandomPointOnPlane(randomPlane);
    }

    public (Vector3, Quaternion) GetRandomPointOnPlane(ARPlane plane)
    {
        Quaternion rotation = Quaternion.identity;
        float borderThreshold = 0.5f;

        if (plane.alignment == PlaneAlignment.Vertical)
        // Vector2 -> X is width, Y is height
        {
            // determine direction of the normal with respect to the axes
            Debug.Log("Normal: " + plane.normal);
            rotation = Quaternion.LookRotation(plane.normal);
            if (Mathf.Abs(plane.normal.x) > 0.9f) // Vector3 -> Y is height, Z is width, X is forward (normal)
            {
                float randomZ = Random.Range(plane.center.z - plane.extents.x + borderThreshold, plane.center.z + plane.extents.x - borderThreshold);
                float randomY = Random.Range(plane.center.y - plane.extents.y + borderThreshold, plane.center.y + plane.extents.y - borderThreshold);
                return (new Vector3(plane.center.x, randomY, randomZ), rotation);

            }
            else // Vector3 -> Y is height, X is width, Z is forward (normal)
            {
                float randomX = Random.Range(plane.center.x - plane.extents.x + borderThreshold, plane.center.x + plane.extents.x - borderThreshold);
                float randomY = Random.Range(plane.center.y - plane.extents.y + borderThreshold, plane.center.y + plane.extents.y - borderThreshold);
                return (new Vector3(randomX, randomY, plane.center.z), rotation);
            }


        }
        else  // Horizontal 
        //Vector3 -> Y is up (normal), Z is height, X is width
        // Vector2 -> X is width, Y is height
        {
            float randomX = Random.Range(plane.center.x - plane.extents.x + borderThreshold, plane.center.x + plane.extents.x - borderThreshold);
            float randomZ = Random.Range(plane.center.z - plane.extents.y + borderThreshold, plane.center.z + plane.extents.y - borderThreshold);
            return (new Vector3(randomX, plane.center.y, randomZ), rotation);
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