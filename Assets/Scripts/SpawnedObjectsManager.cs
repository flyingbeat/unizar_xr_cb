using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FurnitureSpawner))]
public class SpawnedObjectsManager : MonoBehaviour
{

    [SerializeField]
    Button m_DestroyObjectsButton;

    FurnitureSpawner m_FurnitureSpawner;

    void OnEnable()
    {
        m_FurnitureSpawner = GetComponent<FurnitureSpawner>();

        m_FurnitureSpawner.spawnAsChildren = true;
        m_DestroyObjectsButton.onClick.AddListener(DestroyAllObjects);
    }

    void OnDisable()
    {
        m_DestroyObjectsButton.onClick.RemoveListener(DestroyAllObjects);
    }

    public GameObject HideRandomObject()
    {
        GameObject randomObject = GetRandomObject();
        return HideObject(randomObject);
    }

    public GameObject HideObject(GameObject gameObject)
    {
        gameObject.SetLayerRecursively(LayerMask.NameToLayer("Hidden"));
        SetRendererRicursively(gameObject, enabled: false);
        return gameObject;
    }

    public GameObject HideObject(int index)
    {
        if (index < 0 || index >= transform.childCount)
        {
            return null;
        }
        Transform furniture = transform.GetChild(index);
        return HideObject(furniture.gameObject);
    }

    public GameObject GetRandomObject()
    {
        return transform.GetChild(GetRandomObjectIndex()).gameObject;
    }

    public int GetRandomObjectIndex()
    {
        return Random.Range(0, transform.childCount);
    }

    public GameObject DestroyObject(GameObject gameObject)
    {
        Destroy(gameObject);
        return gameObject;
    }

    public GameObject DestroyObject(int index)
    {
        if (index < 0 || index >= transform.childCount)
        {
            return null;
        }
        Transform furniture = transform.GetChild(index);
        return DestroyObject(furniture.gameObject);
    }

    public GameObject DestroyRandomObject()
    {
        return DestroyObject(GetRandomObjectIndex());
    }
    void SetRendererRicursively(GameObject gameObject, bool enabled)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = enabled;
        }
        foreach (Transform child in gameObject.transform)
        {
            SetRendererRicursively(child.gameObject, enabled);
        }
    }


    public void DestroyAllObjects()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

}
