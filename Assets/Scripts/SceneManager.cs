using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;

public enum SpaceVisualizationMode
{
    None,
    Planes,
    BoundingBoxes,
    Both,
}

public struct Goal
{
    public SceneManager.OnboardingGoals CurrentGoal;
    public bool Completed;

    public Goal(SceneManager.OnboardingGoals goal)
    {
        CurrentGoal = goal;
        Completed = false;
    }
}

public class SceneManager : MonoBehaviour
{
    public enum OnboardingGoals
    {
        Empty,
        FindSurfaces,
        TapSurface,
    }

    Queue<Goal> m_OnboardingGoals;
    Goal m_CurrentGoal;
    bool m_AllGoalsFinished;
    int m_SurfacesTapped;
    int m_CurrentGoalIndex = 0;

    [Serializable]
    class Step
    {
        [SerializeField]
        public GameObject stepObject;

        [SerializeField]
        public string buttonText;

        public bool includeSkipButton;
    }

    [SerializeField]
    List<Step> m_StepList = new List<Step>();

    [SerializeField]
    public TextMeshProUGUI m_StepButtonTextField;

    [SerializeField]
    public GameObject m_SkipButton;

    [SerializeField]
    GameObject m_LearnButton;

    [SerializeField]
    GameObject m_LearnModal;

    [SerializeField]
    Button m_LearnModalButton;

    [SerializeField]
    GameObject m_CoachingUIParent;

    [SerializeField]
    FadeMaterial m_FadeMaterial;

    [SerializeField]
    Toggle m_PassthroughToggle;

    [SerializeField]
    LazyFollow m_GoalPanelLazyFollow;

    [SerializeField]
    ARPlaneManager m_ARPlaneManager;

    [SerializeField]
    ARBoundingBoxManager m_ARBoundingBoxManager;

    [SerializeField]
    TMP_Dropdown m_SpaceVisualizationSelectorDropdown;

    [SerializeField]
    FurnitureSpawner m_FurnitureSpawner;

    const int k_NumberOfSurfacesTappedToCompleteGoal = 1;

    private SpaceVisualizationMode _visualizationMode = SpaceVisualizationMode.None;

    void Start()
    {
        m_OnboardingGoals = new Queue<Goal>();
        var welcomeGoal = new Goal(OnboardingGoals.Empty);
        var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(endGoal);

        m_CurrentGoal = m_OnboardingGoals.Dequeue();

        if (m_SpaceVisualizationSelectorDropdown != null)
        {
            m_SpaceVisualizationSelectorDropdown.onValueChanged.AddListener(SelectSpaceVisualizationMode);
            SelectSpaceVisualizationMode(_visualizationMode);
        }

        if (m_FadeMaterial != null)
        {
            m_FadeMaterial.FadeSkybox(false);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = false;
        }

        if (m_LearnButton != null)
        {
            m_LearnButton.GetComponent<Button>().onClick.AddListener(OpenModal); ;
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        if (m_LearnModalButton != null)
        {
            m_LearnModalButton.onClick.AddListener(CloseModal);
        }

        if (m_FurnitureSpawner == null)
        {
#if UNITY_2023_1_OR_NEWER
            m_FurnitureSpawner = FindAnyObjectByType<FurnitureSpawner>();
#else
            m_FurnitureSpawner = FindObjectOfType<FurnitureSpawner>();
#endif
        }
    }

    void OpenModal()
    {
        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.one;
        }
    }

    void CloseModal()
    {
        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }
    }



    void Update()
    {
        if (!m_AllGoalsFinished)
        {
            ProcessGoals();
        }

        // Debug Input
#if UNITY_EDITOR
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CompleteGoal();
        }
#endif
    }

    void ProcessGoals()
    {
        if (!m_CurrentGoal.Completed)
        {
            switch (m_CurrentGoal.CurrentGoal)
            {
                case OnboardingGoals.Empty:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case OnboardingGoals.FindSurfaces:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case OnboardingGoals.TapSurface:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.None;
                    break;
            }
        }
    }

    void CompleteGoal()
    {
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            m_FurnitureSpawner.objectSpawned -= OnObjectSpawned;

        m_CurrentGoal.Completed = true;
        m_CurrentGoalIndex++;
        if (m_OnboardingGoals.Count > 0)
        {
            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_StepList[m_CurrentGoalIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentGoalIndex].buttonText;
            m_SkipButton.SetActive(m_StepList[m_CurrentGoalIndex].includeSkipButton);
        }
        else
        {
            m_AllGoalsFinished = true;
            ForceEndAllGoals();
        }

        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
        {
            if (m_FadeMaterial != null)
                m_FadeMaterial.FadeSkybox(true);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = true;

            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(true);
            }

            SelectSpaceVisualizationMode(SpaceVisualizationMode.Planes);
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
        {
            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(false);
            }
            m_SurfacesTapped = 0;
            m_FurnitureSpawner.objectSpawned += OnObjectSpawned;
        }
    }

    private IEnumerator SetARManagerState(ARPlaneManager manager, bool enabled)
    {
        yield return new WaitForSeconds(1.0f);
        manager.enabled = enabled;
        SetTrackableAlpha(manager.trackables, enabled ? 0.3f : 0.0f, enabled ? 1.0f : 0.0f);
    }
    private IEnumerator SetARManagerState(ARBoundingBoxManager manager, bool enabled)
    {
        yield return new WaitForSeconds(1.0f);
        manager.enabled = enabled;
        SetTrackableAlpha(manager.trackables, enabled ? 0.3f : 0.0f, enabled ? 1.0f : 0.0f);
    }

    private void SetTrackableAlpha<T>(TrackableCollection<T> trackables, float fillAlpha = 0.3f, float lineAlpha = 1.0f) where T : ARTrackable
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

    private void OnBoundingBoxesChanged(ARTrackablesChangedEventArgs<ARBoundingBox> args)
    {
        SelectSpaceVisualizationMode(_visualizationMode);
    }
    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARBoundingBox> args)
    {
        SelectSpaceVisualizationMode(_visualizationMode);
    }

    private void SelectSpaceVisualizationMode(int mode)
    {
        SelectSpaceVisualizationMode((SpaceVisualizationMode)mode, true);
    }
    private void SelectSpaceVisualizationMode(SpaceVisualizationMode mode, bool fromDropDown = false)
    {
        Debug.Log("SelectSpaceVisualizationMode: " + mode);
        _visualizationMode = mode;
        if (fromDropDown) m_SpaceVisualizationSelectorDropdown.SetValueWithoutNotify((int)mode);

        switch (mode)
        {
            case SpaceVisualizationMode.Planes:
                StartCoroutine(SetARManagerState(m_ARPlaneManager, true));
                StartCoroutine(SetARManagerState(m_ARBoundingBoxManager, false));
                break;

            case SpaceVisualizationMode.BoundingBoxes:
                StartCoroutine(SetARManagerState(m_ARPlaneManager, false));
                StartCoroutine(SetARManagerState(m_ARBoundingBoxManager, true));
                break;

            case SpaceVisualizationMode.Both:
                StartCoroutine(SetARManagerState(m_ARPlaneManager, true));
                StartCoroutine(SetARManagerState(m_ARBoundingBoxManager, true));
                break;

            case SpaceVisualizationMode.None:
                StartCoroutine(SetARManagerState(m_ARPlaneManager, false));
                StartCoroutine(SetARManagerState(m_ARBoundingBoxManager, false));
                break;

            default:
                break;
        }
    }

    public void ForceCompleteGoal()
    {
        CompleteGoal();
    }

    public void ForceEndAllGoals()
    {
        m_CoachingUIParent.transform.localScale = Vector3.zero;


        if (m_FadeMaterial != null)
        {
            m_FadeMaterial.FadeSkybox(true);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = true;
        }

        if (m_LearnButton != null)
        {
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        SelectSpaceVisualizationMode(SpaceVisualizationMode.Planes);
        m_FurnitureSpawner.SpawnFurniture(changeable: true);
        m_FurnitureSpawner.SpawnFurniture(changeable: false);
    }

    public void ResetCoaching()
    {
        m_CoachingUIParent.transform.localScale = Vector3.one;

        m_OnboardingGoals.Clear();
        m_OnboardingGoals = new Queue<Goal>();
        var welcomeGoal = new Goal(OnboardingGoals.Empty);
        var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(endGoal);

        for (int i = 0; i < m_StepList.Count; i++)
        {
            if (i == 0)
            {
                m_StepList[i].stepObject.SetActive(true);
                m_SkipButton.SetActive(m_StepList[i].includeSkipButton);
                m_StepButtonTextField.text = m_StepList[i].buttonText;
            }
            else
            {
                m_StepList[i].stepObject.SetActive(false);
            }
        }

        m_CurrentGoal = m_OnboardingGoals.Dequeue();
        m_AllGoalsFinished = false;

        if (m_LearnButton != null)
        {
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        m_CurrentGoalIndex = 0;
    }

    void OnObjectSpawned(GameObject spawnedObject)
    {
        m_SurfacesTapped++;
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface && m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
        {
            CompleteGoal();
            m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
        }
    }
}
