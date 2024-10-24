using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
public class SpaceVisualizerController : MonoBehaviour
{
    [SerializeField]
    TMP_Dropdown m_SpaceVisualizerSelectorDropdown;

    [SerializeField]
    ARPlaneManager m_ARPlaneManager;

    [SerializeField]
    ARBoundingBoxManager m_ARBoundingBoxManager;

    void OnEnable()
    {
        OnSpaceVisualizerSelectorDropdownValueChanged(m_SpaceVisualizerSelectorDropdown.value);
        m_SpaceVisualizerSelectorDropdown.onValueChanged.AddListener(OnSpaceVisualizerSelectorDropdownValueChanged);
    }

    void OnDisable()
    {
        m_SpaceVisualizerSelectorDropdown.onValueChanged.RemoveListener(OnSpaceVisualizerSelectorDropdownValueChanged);
    }

    void OnSpaceVisualizerSelectorDropdownValueChanged(int value)
    {
        if(value == 0) StartCoroutine(TurnOnBoundingBoxes());
        if(value == 1) StartCoroutine(TurnOnPlanes());
    }

    public IEnumerator TurnOnPlanes()
    {
        yield return new WaitForSeconds(1f);
        m_ARBoundingBoxManager.enabled = false;
        m_ARPlaneManager.enabled = true;
    }

    public IEnumerator TurnOnBoundingBoxes()
    {
        yield return new WaitForSeconds(1f);
        m_ARPlaneManager.enabled = false;
        m_ARBoundingBoxManager.enabled = true;
    }

}
