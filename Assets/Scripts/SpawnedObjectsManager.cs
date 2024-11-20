using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FurnitureSpawner))]
public class SpawnedObjectsManager : MonoBehaviour
{
    [SerializeField]
    TMP_Dropdown m_ObjectSelectorDropdown;

    [SerializeField]
    Button m_DestroyObjectsButton;

    FurnitureSpawner m_FurnitureSpawner;

    void OnEnable()
    {
        //m_Spawner = GetComponent<ObjectSpawner>();
        m_FurnitureSpawner = GetComponent<FurnitureSpawner>();

        //m_Spawner.spawnAsChildren = true;
        m_FurnitureSpawner.spawnAsChildren = true;
        OnObjectSelectorDropdownValueChanged(m_ObjectSelectorDropdown.value);
        m_ObjectSelectorDropdown.onValueChanged.AddListener(OnObjectSelectorDropdownValueChanged);
        m_DestroyObjectsButton.onClick.AddListener(OnDestroyObjectsButtonClicked);
    }

    void OnDisable()
    {
        m_ObjectSelectorDropdown.onValueChanged.RemoveListener(OnObjectSelectorDropdownValueChanged);
        m_DestroyObjectsButton.onClick.RemoveListener(OnDestroyObjectsButtonClicked);
    }

    void OnObjectSelectorDropdownValueChanged(int value)
    {
        if (value == 0)
        {
            //m_Spawner.RandomizeSpawnOption();
            return;
        }

        //m_Spawner.spawnOptionIndex = value - 1;
    }

    public GameObject HideRandomObject()
    {
        GameObject randomObject = GetRandomObject();
        SetRendererRicursively(randomObject, enabled: false);
        return randomObject;
    }

    public GameObject HideObject(GameObject gameObject)
    {
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

    void OnDestroyObjectsButtonClicked()
    {
        Debug.Log(m_FurnitureSpawner.transform.ToString());
        foreach (Transform child in m_FurnitureSpawner.transform)
        {
            Debug.Log("child " + child.gameObject.ToString());
            Debug.Log(child.gameObject.transform.rotation);
            //Destroy(child.gameObject);
        }
    }

}
