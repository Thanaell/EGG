using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public int participantNumber = -1;
    public int modalityNumber = -1; //1, 2, 3

    public HandsAndAnimators hands;

    public UI_Elements UI;

    public List<DynamicGesture> dynamicGestures;

    private SHOWING_TECHNIQUE showingTechnique;
    private STUDY_STEP studyStep;

    private bool isTraining;
    private bool isAnim;
    private bool isExpectingGesture;

    private int maxRepetitions = 10;
    private int currentRepetition;
    private int currentGestureIndex;
    private int showGestureRepeats;

    private float repetitionTimeout;
    private float neutralTimeout;

    private GameObject usedHand;
    private Animator usedAnimator;

    private DynamicGesture currentExpectedGesture;

    public HandLogger logger;
    

    void Start()
    {
        isTraining = true;
        isAnim = false;
        isExpectingGesture = false;

        currentRepetition = 0;
        currentGestureIndex = 0;
        showGestureRepeats = 0;

        if(participantNumber == -1 || modalityNumber == -1)
        {
            Debug.Log("ERROR Participant or modality number not set");
            Application.Quit();
        }

        repetitionTimeout = 0f;
        neutralTimeout = 0f;

        studyStep = STUDY_STEP.IDLE;

        //TODO : Story mode and gesture subset from CSV file (depending on participant number and modality number)

        // temp animator
        usedAnimator = hands.overrideAnimator;
        usedHand = hands.overrideHand;

        StartIdle();
    }

    void Update()
    {
        //Log when
            //Show technique, during animation
            //Repetitions, when a gesture is expected
            //First perform, before the first correct gesture
        if (isAnim ||
            isExpectingGesture||
            (studyStep == STUDY_STEP.FIRST_PERFORM && UI.detectionMarker.color == Color.red))
        {
            Log();
        }

        if (studyStep == STUDY_STEP.REPETITIONS)
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
    private void Log(string detectedGestureName="n/a")
    {
        logger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, Time.time, isTraining, studyStep, isAnim,
             currentRepetition, showGestureRepeats, currentExpectedGesture.name, detectedGestureName); //handlogger knows by itself the hand position
                
    }
    public void StartIdle()
    {
        studyStep = STUDY_STEP.IDLE;
        currentGestureIndex++;
        currentRepetition = 0;
        showGestureRepeats = 0;

        if (currentGestureIndex > 1)
        {
            isTraining = false;
        }

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
        isAnim = true;
        showGestureRepeats++;

        UI.tryGestureButton.enabled = true;

        UI.instructionsText.text = "You're in show tech";

        hands.mainHandRenderer.enabled = false;
        usedHand.SetActive(true);

        usedAnimator.enabled = true;
        usedAnimator.Play(currentExpectedGesture.name);
    }

    public void StartFirstPerform()
    {
        studyStep = STUDY_STEP.FIRST_PERFORM;
        isAnim = false;

        UI.detectionMarker.enabled = true;

        UI.instructionsText.text = "You're in try gesture";

        usedAnimator.enabled = false;
        usedHand.SetActive(false);

        hands.mainHandRenderer.enabled = true;
    }

    public void StartRepetitions()
    {
        studyStep = STUDY_STEP.REPETITIONS;
        isAnim = false;

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
                Log(detectedGesture.name);
                break;
            case STUDY_STEP.FIRST_PERFORM:
                Log(detectedGesture.name);
                if (detectedGesture.name == currentExpectedGesture.name)
                {
                    UI.detectionMarker.color = Color.green;
                }
                break;
            case STUDY_STEP.REPETITIONS:
                if(isExpectingGesture)
                {
                    Log(detectedGesture.name);
                    if (detectedGesture.name == currentExpectedGesture.name)
                    {
                        // Change detection marker
                        currentRepetition++;
                        isExpectingGesture = false;

                        neutralTimeout = Time.time + currentExpectedGesture.execTime;

                        UI.repetionsCounterText.text = currentRepetition.ToString() + "/10";
                        UI.instructionsText.text = "Go back in neutral position";
                        UI.detectionMarker.color = Color.green;
                    }
                    
                }
                break;
        }
    }
}
