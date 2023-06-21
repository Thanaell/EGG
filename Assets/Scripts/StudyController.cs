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

    public GameObject mainHand;
    public SkinnedMeshRenderer mainHandRenderer;

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
    private bool isExpectingGesture = false;

    private int maxRepetitions = 10;
    private int currentRepetition = 0;
    private int currentGestureIndex = 0;
    
    private float repetitionTimeout = 0f;
    private float neutralTimeout = 0f;

    private GameObject usedHand;
    private Animator usedAnimator;

    private DynamicGesture currentExpectedGesture;
    

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

    void Update()
    {
        //TODO : handle log ?

        if(studyStep == STUDY_STEP.REPETITIONS)
        {
            if(isExpectingGesture && Time.time > repetitionTimeout)
            {
                currentRepetition++;
                isExpectingGesture = false;

                neutralTimeout = Time.time + currentExpectedGesture.execTime;

                UI.repetionsCounterText.text = currentRepetition.ToString() + "/10";
                UI.instructionsText.text = "Go back in neutral position";
                UI.detectionMarker.color = Color.red;
            }

            if(!isExpectingGesture && Time.time > neutralTimeout)
            {
                isExpectingGesture = true;

                repetitionTimeout = Time.time + currentExpectedGesture.execTime;

                UI.instructionsText.text = "Perform the gesture";
                UI.detectionMarker.color = Color.red;
            }

            if(currentRepetition >= maxRepetitions)
            {
                StartIdle();
            }
        }
    }
    
    public void StartIdle()
    {
        studyStep = STUDY_STEP.IDLE;
        currentGestureIndex++;

        currentRepetition = 0;

        UI.showGestureButton.enabled = true;
        UI.tryGestureButton.enabled = false;
        UI.repeatGestureButton.enabled = false;

        UI.detectionMarker.enabled = false;
        UI.detectionMarker.color = Color.red;

        UI.repetionsCounterText.enabled = false;
        UI.repetionsCounterText.text = "0/10";

        UI.instructionsText.text = "You're in idle";

        currentExpectedGesture = dynamicGestures[currentGestureIndex - 1];
    }

    public void StartShowTechnique()
    {
        studyStep = STUDY_STEP.SHOW_TECHNIQUE;

        UI.tryGestureButton.enabled = true;

        UI.instructionsText.text = "You're in show tech";

        hands.mainHandRenderer.enabled = false;
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

        hands.mainHandRenderer.enabled = true;
    }

    public void StartRepetitions()
    {
        studyStep = STUDY_STEP.REPETITIONS;

        UI.showGestureButton.enabled = false;
        UI.tryGestureButton.enabled = false;
        UI.repetionsCounterText.enabled = true;
        UI.detectionMarker.color = Color.red;

        UI.instructionsText.text = "Go back in neutral position";

        isExpectingGesture = false;
        neutralTimeout = Time.time + currentExpectedGesture.execTime;
    }

    public void OnRecognizeEvent(DynamicGesture detectedGesture)
    {
        switch(studyStep)
        {
            case STUDY_STEP.IDLE:
                break;
            case STUDY_STEP.SHOW_TECHNIQUE:
                // Log recognize event if correct one
                break;
            case STUDY_STEP.FIRST_PERFORM:
                // Log recognize event if correct one
                if(detectedGesture.name == currentExpectedGesture.name)
                {
                    UI.detectionMarker.color = Color.green;
                }
                break;
            case STUDY_STEP.REPETITIONS:
                if(isExpectingGesture)
                {
                    // Log recognize event if correct one
                    // Change detection marker
                    currentRepetition++;
                    isExpectingGesture = false;

                    neutralTimeout = Time.time + currentExpectedGesture.execTime;

                    UI.repetionsCounterText.text = currentRepetition.ToString() + "/10";
                    UI.instructionsText.text = "Go back in neutral position";
                    UI.detectionMarker.color = Color.green;
                }
                break;
        }
    }
}
