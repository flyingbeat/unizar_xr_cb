using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;
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

    [SerializeField]
    List<GoalTypes> m_trials = new();
    Goal m_CurrentGoal;
    bool m_AllGoalsFinished = false;

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
    public float m_timeToDetectChange = 10.0f;

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

    [SerializeField]
    LogManager m_LogManager;
    private SpaceVisualizationMode _visualizationMode = SpaceVisualizationMode.None;

    [SerializeField]
    int m_nrOfInitialObjects = 5;

    public void SetNrOfInitialObjects(float nrOfInitialObjects)
    {
        m_nrOfInitialObjects = (int)nrOfInitialObjects;
    }

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
        if (m_ObjectPointer != null)
        {
            m_ObjectPointer.ButtonPressed += OnControllerButtonPressed;
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

    void OnDestroy()
    {
        if (m_ObjectPointer != null)
        {
            m_ObjectPointer.ButtonPressed -= OnControllerButtonPressed;
        }
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
        foreach (var trial in m_trials)
        {
            m_Goals.Enqueue(new Goal(trial, 5));
        }

        var experimentEndGoal = new Goal(GoalTypes.EndExperiment, 6); // card 6
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
        Debug.Log("Completing Goal: " + m_CurrentGoal.CurrentGoal);
        m_CurrentGoal.Completed = true;

        // complete current goal (after clicking on step button)
        switch (m_CurrentGoal.CurrentGoal)
        {
            case GoalTypes.EndOnboarding:
                Debug.Log("End Onboarding");
                StartExperiment();
                break;

            case GoalTypes.EndExperiment:
                m_CoachingUIParent.SetActive(false);
                m_AllGoalsFinished = true;
                m_LogManager.SaveToFile();
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
                SelectSpaceVisualizationMode(SpaceVisualizationMode.Both);
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
        m_CoachingUIParent.SetActive(false);
        LogEntry logEntry = new()
        {
            timestamp = DateTime.Now,
            changeType = goal,
        };

        // grey flicker
        m_FadeMaterial.FadeSkybox(false);
        m_ObjectSpawner.SetActive(false);
        GameObject hiddenObject = new("dummy");
        switch (goal)
        {
            case GoalTypes.RemovalTrial:
                hiddenObject = m_SpawnedObjectsManager.HideRandomObject();
                logEntry.objectName = hiddenObject.name;
                logEntry.objectPosition = hiddenObject.transform.position;
                logEntry.associatedPlaneClassification = hiddenObject.tag;
                Debug.Log("Removed " + hiddenObject.name);
                break;
            case GoalTypes.AdditionTrial:
                m_FurnitureSpawner.RandomizeSpawnOption();
                m_FurnitureSpawner.TrySpawnOnPlane();
                GameObject addedObject = m_FurnitureSpawner.spawnedObject;
                logEntry.objectName = addedObject.name;
                logEntry.objectPosition = addedObject.transform.position;
                logEntry.associatedPlaneClassification = addedObject.tag;
                Debug.Log("Added " + addedObject.name);
                break;
            case GoalTypes.RelocationTrial:
                GameObject randomObject = m_SpawnedObjectsManager.DestroyRandomObject();
                m_FurnitureSpawner.TrySpawnOnPlane(prefab: randomObject);
                logEntry.objectName = randomObject.name;
                logEntry.objectPosition = randomObject.transform.position;
                logEntry.associatedPlaneClassification = randomObject.tag;
                Debug.Log("Relocated " + randomObject.name);
                break;
            case GoalTypes.ReplacementTrial:
                GameObject removedObject = m_SpawnedObjectsManager.DestroyRandomObject();
                List<GameObject> furniturePrefabsByTag = m_FurnitureSpawner.furniturePrefabs.Where(prefab => prefab.tag.Equals(removedObject.tag) && !removedObject.name.Contains(prefab.name)).ToList();
                int randomIndex = UnityEngine.Random.Range(0, furniturePrefabsByTag.Count);
                m_FurnitureSpawner.applyRandomAngleAtSpawn = false;
                m_FurnitureSpawner.TrySpawnObject(removedObject.transform.position, Quaternion.identity, furniturePrefabsByTag[randomIndex]);
                Debug.Log("Replaced " + removedObject.name + " with " + furniturePrefabsByTag[randomIndex].name);
                logEntry.objectName = removedObject.name + "->" + furniturePrefabsByTag[randomIndex].name;
                logEntry.objectPosition = removedObject.transform.position;
                logEntry.associatedPlaneClassification = removedObject.tag;
                break;
            default:
                Debug.LogError("Invalid goal type");
                break;
        }

        logEntry.userPosition = m_FurnitureSpawner.cameraToFace.transform.position;
        logEntry.userDirection = m_FurnitureSpawner.cameraToFace.transform.forward;

        yield return new WaitForSeconds(2f);
        m_ObjectSpawner.SetActive(true);
        m_FadeMaterial.FadeSkybox(true);

        // wait 45 seconds or button press
        float elapsedTime = 0f;
        while (elapsedTime <= m_timeToDetectChange && !m_ObjectPointer.buttonHeld)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        logEntry.timeToDetect = elapsedTime;

        if (elapsedTime > m_timeToDetectChange)
        {
            // trial ended by timeout
            logEntry.pointedAtSomething = false;
            Debug.Log("Trial ended by timeout");
        }
        else
        {
            // trial was stopped by buttonpress
            logEntry.pointedAtSomething = true;
            Collider controllerRaycastCollider = m_ObjectPointer.Hit.collider;
            if (controllerRaycastCollider != null)
            {
                logEntry.pointedObjectName = controllerRaycastCollider.gameObject.name;
                logEntry.pointedObjectPosition = controllerRaycastCollider.transform.position;
                Debug.Log("Trial ended by button press");
            }
        }
        Destroy(hiddenObject);
        m_LogManager.Log(logEntry);
        m_CoachingUIParent.SetActive(true);
    }

    // called when step button is pressed
    public void ForceCompleteGoal()
    {
        CompleteGoal();
    }

    public void OnControllerButtonPressed(RaycastHit hit)
    {
        Debug.Log("Primary button is pressed");
        Debug.Log(hit.collider.gameObject.name);
        Debug.Log("tag: " + hit.collider.gameObject.tag);
    }

    // called when skip button is pressed
    public void ForceEndOnboarding()
    {
        Debug.Log("Forcing end of onboarding");
        foreach (var step in m_GoalUICards)
        {
            step.UIObject.SetActive(false);
        }
        m_Goals.Clear();
        InitializeGoals(onlyTrials: true);
        m_CurrentGoal = m_Goals.Dequeue();
        m_GoalUICards[m_CurrentGoal.UICardIndex].UIObject.SetActive(true);
        m_StepButtonTextField.text = m_GoalUICards[m_CurrentGoal.UICardIndex].buttonText;
        m_SkipButton.SetActive(m_GoalUICards[m_CurrentGoal.UICardIndex].includeSkipButton);


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
        m_FurnitureSpawner.SpawnAll(m_nrOfInitialObjects);

        m_LogManager.Init();

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

        if (m_LearnButton != null)
        {
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
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
