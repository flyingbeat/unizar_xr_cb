using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Sirenix.Utilities;
using System.Linq;

public enum SpaceVisualizationMode
{
    None,
    Planes,
    BoundingBoxes,
    Both,
}

public struct Goal
{
    public SceneManager.GoalTypes CurrentGoal;
    public bool Completed;
    public int UICardIndex;

    public Goal(SceneManager.GoalTypes goal, int uiCardIndex)
    {
        CurrentGoal = goal;
        Completed = false;
        UICardIndex = uiCardIndex;
    }
}

public class SceneManager : MonoBehaviour
{
    public enum GoalTypes
    {
        Empty,
        FindSurfaces,
        UseControllers,
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
    TextMeshProUGUI m_StepButtonTextField;

    [SerializeField]
    GameObject m_SkipButton;

    [SerializeField]
    Button m_ContinueButton;

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
    ARSpaceManager m_ARSpaceManager;

    [SerializeField]
    TMP_Dropdown m_SpaceVisualizationSelectorDropdown;

    [SerializeField]
    FurnitureSpawner m_FurnitureSpawner;

    [SerializeField]
    SpawnedObjectsManager m_SpawnedObjectsManager;

    [SerializeField]
    GameObject m_ObjectSpawner;

    [SerializeField]
    ObjectPointer m_ObjectPointer;
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
    void InitializeGoals(bool onlyTrials = false)
    {

        m_Goals = new Queue<Goal>();
        if (!onlyTrials)
        {

            // onboarding
            var welcomeGoal = new Goal(GoalTypes.Empty, 0); // card 0
            var findSurfaceGoal = new Goal(GoalTypes.FindSurfaces, 1); // card 1
            var useControllersGoal = new Goal(GoalTypes.UseControllers, 2); // card 2
            var explanationGoal = new Goal(GoalTypes.Empty, 3); // card 3
            var endOnboardingGoal = new Goal(GoalTypes.EndOnboarding, 4); // card 4

            m_Goals.Enqueue(welcomeGoal);
            m_Goals.Enqueue(findSurfaceGoal);
            m_Goals.Enqueue(useControllersGoal);
            m_Goals.Enqueue(explanationGoal);
            m_Goals.Enqueue(endOnboardingGoal);
        }

        // trials with change in scene
        var removal = new Goal(GoalTypes.RemovalTrial, 5); // card 5
        var addition = new Goal(GoalTypes.AdditionTrial, 5); // card 5
        var relocation = new Goal(GoalTypes.RelocationTrial, 5); // card 5
        var replacement = new Goal(GoalTypes.ReplacementTrial, 5); // card 5

        m_Goals.Enqueue(removal);
        m_Goals.Enqueue(addition);
        m_Goals.Enqueue(relocation);
        m_Goals.Enqueue(replacement);

        var experimentEndGoal = new Goal(GoalTypes.EndExperiment, 4); // card 6
        m_Goals.Enqueue(experimentEndGoal);

    }

    void ProcessGoals()
    {
        if (!m_CurrentGoal.Completed)
        {
            switch (m_CurrentGoal.CurrentGoal)
            {
                case GoalTypes.Empty:
                case GoalTypes.FindSurfaces:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case GoalTypes.UseControllers:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    if (m_ObjectPointer.ControllersInHand())
                    {
                        m_ContinueButton.enabled = true;
                        m_StepButtonTextField.color = Color.white;
                    }
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
            case GoalTypes.UseControllers:
                m_ContinueButton.enabled = false;
                m_StepButtonTextField.color = Color.black;
                break;

            default:
                break;
        }
    }

    IEnumerator StartTrial(GoalTypes goal)
    {
        Debug.Log("Starting trial" + goal);

        // grey flicker
        m_FadeMaterial.FadeSkybox(false);
        m_ObjectSpawner.SetActive(false);

        switch (goal)
        {
            case GoalTypes.RemovalTrial:
                GameObject hiddenObject = m_SpawnedObjectsManager.HideRandomObject();
                Debug.Log("Removed " + hiddenObject.name);
                break;
            case GoalTypes.AdditionTrial:
                m_FurnitureSpawner.RandomizeSpawnOption();
                m_FurnitureSpawner.TrySpawnOnPlane();
                Debug.Log("Added " + m_FurnitureSpawner.furniturePrefabs[m_FurnitureSpawner.spawnOptionIndex].name);
                break;
            case GoalTypes.RelocationTrial:
                GameObject randomObject = m_SpawnedObjectsManager.DestroyRandomObject();
                int prefabIndex = m_FurnitureSpawner.furniturePrefabs.IndexOf(randomObject);
                m_FurnitureSpawner.TrySpawnOnPlane(prefabIndex);
                Debug.Log("Relocated " + randomObject.name);
                break;
            case GoalTypes.ReplacementTrial:
                GameObject removedObject = m_SpawnedObjectsManager.DestroyRandomObject();
                List<GameObject> furniturePrefabsByTag = m_FurnitureSpawner.furniturePrefabs.Where(prefab => prefab.tag.Equals(removedObject.tag) && !prefab.Equals(removedObject)).ToList();
                int randomIndex = UnityEngine.Random.Range(0, furniturePrefabsByTag.Count);
                int furnitureIndex = m_FurnitureSpawner.furniturePrefabs.IndexOf(furniturePrefabsByTag[randomIndex]);
                m_FurnitureSpawner.applyRandomAngleAtSpawn = false;
                m_FurnitureSpawner.TrySpawnObject(removedObject.transform.position, Quaternion.identity, furnitureIndex);
                Debug.Log("Replaced " + removedObject.name + " with " + m_FurnitureSpawner.furniturePrefabs[furnitureIndex].name);
                break;
            default:
                Debug.LogError("Invalid goal type");
                break;
        }

        yield return new WaitForSeconds(2f);
        m_ObjectSpawner.SetActive(true);
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
        m_Goals.Clear();
        InitializeGoals(onlyTrials: true);
        m_CurrentGoal = m_Goals.Dequeue();
        m_GoalUICards[m_CurrentGoal.UICardIndex].UIObject.SetActive(true);


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
        // Spawn furniture
        Debug.Log("Starting experiment");
        m_FurnitureSpawner.SpawnAll();

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
                m_ARSpaceManager.boundingBoxesVisualized = false;
                m_ARSpaceManager.planesVisualized = true;
                break;

            case SpaceVisualizationMode.BoundingBoxes:
                m_ARSpaceManager.boundingBoxesVisualized = true;
                m_ARSpaceManager.planesVisualized = false;
                break;

            case SpaceVisualizationMode.Both:
                m_ARSpaceManager.boundingBoxesVisualized = true;
                m_ARSpaceManager.planesVisualized = true;
                break;

            case SpaceVisualizationMode.None:
                m_ARSpaceManager.boundingBoxesVisualized = false;
                m_ARSpaceManager.planesVisualized = false;
                break;

            default:
                break;
        }
    }

}
