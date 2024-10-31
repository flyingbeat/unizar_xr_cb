using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(ObjectSpawner))]
public class SpawnedObjectsManager : MonoBehaviour
{
    [SerializeField]
    TMP_Dropdown m_ObjectSelectorDropdown;

    [SerializeField]
    Button m_DestroyObjectsButton;

    ObjectSpawner m_Spawner;
    FurnitureSpawner m_FurnitureSpawner;

    void OnEnable()
    {
        m_Spawner = GetComponent<ObjectSpawner>();
        m_FurnitureSpawner = GetComponent<FurnitureSpawner>();

        m_Spawner.spawnAsChildren = true;
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
            m_Spawner.RandomizeSpawnOption();
            return;
        }

        m_Spawner.spawnOptionIndex = value - 1;
    }

    void OnDestroyObjectsButtonClicked()
    {
        foreach (Transform child in m_FurnitureSpawner.transform)
        {
            Debug.Log("Destroying " + child.gameObject.ToString());
            Destroy(child.gameObject);
        }
    }
}
