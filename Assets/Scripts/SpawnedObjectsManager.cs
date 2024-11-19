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

    //ObjectSpawner m_Spawner;
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

    public GameObject RemoveRandomObject()
    {
        if (m_FurnitureSpawner.transform.childCount == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, m_FurnitureSpawner.transform.childCount);
        foreach (Transform child in m_FurnitureSpawner.transform)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("Changeable"))
            {
                GameObject randomObject = child.gameObject;
                List<GameObject> children = new();
                randomObject.GetChildGameObjects(children);
                foreach (GameObject mesh in children)
                {
                    mesh.SetActive(false);
                }
                Debug.Log("Hid object " + randomObject.ToString());
                return randomObject;
            }
        }
        return null;
    }


    public void DestroyAllObjects()
    {
        foreach (Transform child in m_FurnitureSpawner.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void ChangeScene()
    {
        Debug.Log("ChangeScene");
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

    void RandomizeFurniturePosition()
    {
        foreach (var child in m_FurnitureSpawner.transform)
        {
            var childTransform = (Transform)child;
            var oldPosition = childTransform.position;
            childTransform.position = new Vector3(oldPosition.x + Random.Range(-0.5f, 0.5f), oldPosition.y + Random.Range(-0.5f, 0.5f), oldPosition.z);
        }

    }

}
