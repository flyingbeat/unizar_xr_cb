using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;

public enum SpaceVisualizationMode
{
    None,
    Planes,
    BoundingBoxes,
    Both,
}

public enum ExperimentPhase
{
    Onboarding,
    Trial,
}

public struct Goal
{
    public SceneManager.GoalTypes CurrentGoal;
    public bool Completed;
    public ExperimentPhase Phase;
    public int UICardIndex;

    public Goal(SceneManager.GoalTypes goal, int uiCardIndex, ExperimentPhase phase)
    {
        CurrentGoal = goal;
        Completed = false;
        Phase = phase;
        UICardIndex = uiCardIndex;
    }
}

public class SceneManager : MonoBehaviour
{
    public enum GoalTypes
    {
        Empty,
        FindSurfaces,
        EndOnboarding,
        EndExperiment,
        RemovalTrial,
        AdditionTrial,
        RelocationTrial,
        ReplacementTrial,

    }

    Queue<Goal> m_Goals;
    Goal m_CurrentGoal;
    bool m_AllGoalsFinished = false;
    bool m_OnboardingFinished = false;
    int m_CurrentGoalIndex = 0;

    [Serializable]
    class GoalUICard
    {
        [SerializeField]
        public GameObject UIObject;

        [SerializeField]
        public string buttonText;

        public bool includeSkipButton;
    }

    [SerializeField]
    List<GoalUICard> m_GoalUICards = new();

    [SerializeField]
    public TextMeshProUGUI m_StepButtonTextField;

    [SerializeField]
    public GameObject m_SkipButton;

    [SerializeField]
    GameObject m_LearnButton;

    [SerializeField]
    GameObject m_LearnModal;

    [SerializeField]
    [Tooltip("Time in seconds to detect change in scene during trial")]
    public int m_timeToDetectChange = 10;

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

    [SerializeField]
    SpawnedObjectsManager m_SpawnedObjectsManager;

    [SerializeField]
    GameObject m_ObjectSpawner;
    private SpaceVisualizationMode _visualizationMode = SpaceVisualizationMode.None;

    void Start()
    {
        InitializeGoals();

        m_CurrentGoal = m_Goals.Dequeue();

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
            m_FurnitureSpawner = FindAnyObjectByType<FurnitureSpawner>();
            Debug.Log("FurnitureSpawner: " + m_FurnitureSpawner.ToString());
        }
        if (m_SpawnedObjectsManager == null)
        {
            m_SpawnedObjectsManager = FindAnyObjectByType<SpawnedObjectsManager>();
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

    // UI Callbacks
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

    // Onboarding Logic
    void InitializeGoals()
    {
        m_Goals = new Queue<Goal>();

        // onboarding
        var welcomeGoal = new Goal(GoalTypes.Empty, 0, ExperimentPhase.Onboarding); // card 0
        var findSurfaceGoal = new Goal(GoalTypes.FindSurfaces, 1, ExperimentPhase.Onboarding); // card 1
        var explanationGoal = new Goal(GoalTypes.Empty, 2, ExperimentPhase.Onboarding); // card 2
        var endOnboardingGoal = new Goal(GoalTypes.EndOnboarding, 3, ExperimentPhase.Onboarding); // card 3

        m_Goals.Enqueue(welcomeGoal);
        m_Goals.Enqueue(findSurfaceGoal);
        m_Goals.Enqueue(explanationGoal);
        m_Goals.Enqueue(endOnboardingGoal);

        // trials with change in scene
        var removal = new Goal(GoalTypes.RemovalTrial, 4, ExperimentPhase.Trial); // card 5
        // var addition = new Goal(GoalTypes.AdditionTrial, 5); // card 5
        // var relocation = new Goal(GoalTypes.RelocationTrial, 5); // card 5
        // var replacement = new Goal(GoalTypes.ReplacementTrial, 5); // card 5

        m_Goals.Enqueue(removal);
        // m_TrialGoals.Enqueue(addition);
        // m_TrialGoals.Enqueue(relocation);
        // m_TrialGoals.Enqueue(replacement);

        var experimentEndGoal = new Goal(GoalTypes.EndExperiment, 4, ExperimentPhase.Onboarding); // card 6
        m_Goals.Enqueue(experimentEndGoal);

    }

    void ProcessGoals()
    {
        if (!m_CurrentGoal.Completed)
        {
            switch (m_CurrentGoal.CurrentGoal)
            {
                case GoalTypes.Empty:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case GoalTypes.FindSurfaces:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case GoalTypes.RemovalTrial:
                case GoalTypes.AdditionTrial:
                case GoalTypes.RelocationTrial:
                case GoalTypes.ReplacementTrial:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.None;
                    break;
            }
        }
    }

    void CompleteGoal()
    {
        m_CurrentGoal.Completed = true;
        Debug.Log("Completing Goal: " + m_CurrentGoal.CurrentGoal);

        // complete current goal (after clicking on step button)
        switch (m_CurrentGoal.CurrentGoal)
        {
            case GoalTypes.EndOnboarding:
                ForceEndOnboarding();
                m_OnboardingFinished = true;
                break;

            case GoalTypes.EndExperiment:
                m_CoachingUIParent.SetActive(false);
                m_AllGoalsFinished = true;
                break;

            case GoalTypes.FindSurfaces:
                SelectSpaceVisualizationMode(SpaceVisualizationMode.None);
                if (m_LearnButton != null && m_LearnButton.activeSelf) m_LearnButton.SetActive(false);
                break;

            case GoalTypes.RemovalTrial:
            case GoalTypes.AdditionTrial:
            case GoalTypes.RelocationTrial:
            case GoalTypes.ReplacementTrial:
                StartCoroutine(StartTrial(m_CurrentGoal.CurrentGoal));
                break;

            default:
                break;
        }
        m_GoalUICards[m_CurrentGoal.UICardIndex].UIObject.SetActive(false);

        m_CurrentGoalIndex++;
        if (m_Goals.Count > 0)
        {
            m_CurrentGoal = m_Goals.Dequeue();
            m_GoalUICards[m_CurrentGoal.UICardIndex].UIObject.SetActive(true);
            m_StepButtonTextField.text = m_GoalUICards[m_CurrentGoal.UICardIndex].buttonText;
            m_SkipButton.SetActive(m_GoalUICards[m_CurrentGoal.UICardIndex].includeSkipButton);
        }

        // preparing next goal (before pressing on step button)
        switch (m_CurrentGoal.CurrentGoal)
        {
            case GoalTypes.FindSurfaces:
                if (m_FadeMaterial != null) m_FadeMaterial.FadeSkybox(true);
                if (m_PassthroughToggle != null) m_PassthroughToggle.isOn = true;
                if (m_LearnButton != null) m_LearnButton.SetActive(true);
                SelectSpaceVisualizationMode(SpaceVisualizationMode.Planes);
                break;

            default:
                break;
        }
    }

    IEnumerator StartTrial(GoalTypes goal)
    {
        Debug.Log("Starting trial");

        // grey flicker
        m_FadeMaterial.FadeSkybox(false);
        m_ObjectSpawner.SetActive(false);
        yield return new WaitForSeconds(2f);
        m_ObjectSpawner.SetActive(true);

        switch (goal)
        {
            case GoalTypes.RemovalTrial:
                m_SpawnedObjectsManager.RemoveRandomObject();
                break;
            // case GoalTypes.AdditionTrial:
            //     m_FurnitureSpawner.AddRandomObject();
            //     break;
            // case GoalTypes.RelocationTrial:
            //     m_SpawnedObjectsManager.RelocateRandomObject();
            //     break;
            // case GoalTypes.ReplacementTrial:
            //     m_SpawnedObjectsManager.RemoveRandomObject();
            //     m_FurnitureSpawner.SpawnFurniture();
            //     break;
            default:
                Debug.LogError("Invalid goal type");
                break;
        }
        m_FadeMaterial.FadeSkybox(true);

        // wait 45 seconds or button press
        yield return new WaitForSeconds(m_timeToDetectChange);

        //CompleteGoal();
        Debug.Log("Trial ended");
    }

    // called when step button is pressed
    public void ForceCompleteGoal()
    {
        CompleteGoal();
    }

    // called when skip button is pressed
    public void ForceEndOnboarding()
    {
        foreach (var step in m_GoalUICards)
        {
            step.UIObject.SetActive(false);
        }


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

        SelectSpaceVisualizationMode(SpaceVisualizationMode.None);
        StartExperiment();
    }

    public void StartExperiment()
    {
        // Spawn fixed furniture
        Debug.Log("Starting experiment");
        m_FurnitureSpawner.SpawnFurniture(changeable: false, m_ARPlaneManager.trackables);
        // Spawn changeable furniture
        //m_FurnitureSpawner.SpawnFurniture(changeable: true);


        // on button press, change scene

        // wait 45 seconds or until button press

        // show button to change scene

        // repeat
    }

    public void ResetAll()
    {
        m_CoachingUIParent.SetActive(true);

        m_SpawnedObjectsManager.DestroyAllObjects();

        InitializeGoals();

        for (int i = 0; i < m_GoalUICards.Count; i++)
        {
            if (i == 0)
            {
                m_GoalUICards[i].UIObject.SetActive(true);
                m_SkipButton.SetActive(m_GoalUICards[i].includeSkipButton);
                m_StepButtonTextField.text = m_GoalUICards[i].buttonText;
            }
            else
            {
                m_GoalUICards[i].UIObject.SetActive(false);
            }
        }

        m_CurrentGoal = m_Goals.Dequeue();
        m_AllGoalsFinished = false;
        m_OnboardingFinished = false;

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

    // Plane visualization TODO: Move to separate script
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

    private void SelectSpaceVisualizationMode(int mode)
    {
        SelectSpaceVisualizationMode((SpaceVisualizationMode)mode, true);
    }

    private void SelectSpaceVisualizationMode(SpaceVisualizationMode mode, bool fromDropDown = false)
    {
        Debug.Log("SelectSpaceVisualizationMode: " + mode);
        _visualizationMode = mode;
        if (!fromDropDown) m_SpaceVisualizationSelectorDropdown.SetValueWithoutNotify((int)mode);

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

}
