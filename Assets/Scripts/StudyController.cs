using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum STUDY_STEP
{    
    IDLE, SHOW_TECHNIQUE, FIRST_PERFORM, REPETITIONS
}

[System.Serializable]
public class HandsAndAnimators
{
    public Animator overrideAnimator;
    public Animator externalAnimator;
    public Animator ghostAnimator;

    public GameObject externalHand;
    public GameObject ghostHand;
    public GameObject overrideHand;
}
[System.Serializable]
public class UI_Elements
{
    public Button showGestureButton;
    public Button tryGestureButton;
    public Button repeatGestureButton;

    public Image detectionMarker;
    public TMP_Text repetionsCounterText;
    public TMP_Text instructionsText;
}

//two repetitions of the cycle per run. One for training, the other for real
public class StudyController : MonoBehaviour
{
    public int participantNumber;
    public int modalityNumber; //1, 2, 3

    public HandsAndAnimators hands;

    public UI_Elements UI;

    public List<DynamicGesture> dynamicGestures;

    private SHOWING_TECHNIQUE showingTechnique;
    private STUDY_STEP studyStep;
    private bool isTraining;

    private int maxRepetitions = 10;
    private int currentRepetition;
    private int gestureCount = 0;

    private GameObject usedHand;
    private Animator usedAnimator;

    private DynamicGesture currentExpectedGesture;
    //private Animation currentExpectedGestureAnimation;

    

    // Start is called before the first frame update
    void Start()
    {
        isTraining = true;
        studyStep = STUDY_STEP.IDLE;
        currentRepetition = 0;

        //TODO : Story mode and gesture subset from CSV file (depending on participant number and modality number)

        // temp animator
        usedAnimator = hands.overrideAnimator;

        StartIdle();
    }

    // Update is called once per frame
    void Update()
    {
        //TODO : handle log ?
    }
    
    //Coroutines ?
    public void StartIdle()
    {
        studyStep = STUDY_STEP.IDLE;
        gestureCount++;

        UI.showGestureButton.enabled = true;
        UI.tryGestureButton.enabled = false;
        UI.repeatGestureButton.enabled = false;

        UI.detectionMarker.enabled = false;
        UI.detectionMarker.color = Color.red;

        UI.repetionsCounterText.enabled = false;
        UI.repetionsCounterText.text = "0/10";

        UI.instructionsText.text = "You're in idle";

        currentExpectedGesture = dynamicGestures[gestureCount - 1];

        //string[] animationPath = AssetDatabase.FindAssets(currentExpectedGesture.name, new[] { "Assets/AnimationClips" });
        //currentExpectedGestureAnimation = (Animation)AssetDatabase.LoadAssetAtPath(animationPath[0], typeof(Animation));
    }

    public void StartShowTechnique()
    {
        studyStep = STUDY_STEP.SHOW_TECHNIQUE;

        UI.tryGestureButton.enabled = true;

        UI.instructionsText.text = "You're in show tech";

        hands.overrideHand.SetActive(true);

        usedAnimator.enabled = true;
        usedAnimator.Play(currentExpectedGesture.name);
    }

    public void StartFirstPerform()
    {
        studyStep = STUDY_STEP.FIRST_PERFORM;

        UI.detectionMarker.enabled = true;

        UI.instructionsText.text = "You're in try gesture";

        usedAnimator.enabled = false;
        hands.overrideHand.SetActive(false);
    }

    public void StartRepetitions()
    {
        studyStep = STUDY_STEP.REPETITIONS;

        UI.showGestureButton.enabled = false;
        UI.tryGestureButton.enabled = false;
        UI.repetionsCounterText.enabled = true;

        UI.instructionsText.text = "You're in repetitions";
    }
}
