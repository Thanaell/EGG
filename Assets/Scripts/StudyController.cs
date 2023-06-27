
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SHOWING_TECHNIQUE
{
    GHOST_HAND, EXTERNAL_HAND, OVERRIDE_HAND
}

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
    public Image detectionMarker;
    public TMP_Text repetionsCounterText;
    public TMP_Text instructionsText;

    public GameObject showButton;
    public GameObject tryButton;
    public GameObject repeatButton;
}

[System.Serializable]
public struct Mapping
{
    public string refName;
    public Gesture gesture;
}

//two repetitions of the cycle per run. One for training, the other for real
public class StudyController : MonoBehaviour
{
    public int participantNumber = -1;
    public int modalityNumber = -1; //1, 2, 3

    public HandsAndAnimators hands;

    public UI_Elements UI;

    private List<Gesture> gestures;
    public List<Mapping> gestureMapping;
    private StudyStory studyStory;

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

    private Gesture currentExpectedGesture;

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

        logger.CreateStreamWriter("./Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + ".csv");

        repetitionTimeout = 0f;
        neutralTimeout = 0f;

        gestures = new List<Gesture>();

        studyStep = STUDY_STEP.IDLE;

        studyStory = JsonLoader.loadStudyStory("./Study1Story.json");

        Modality currentModality = studyStory.Participants[participantNumber - 1].Modalities[modalityNumber - 1];

        switch(currentModality.ShowTechnique)
        {
            case "OVERRIDE":
                showingTechnique = SHOWING_TECHNIQUE.OVERRIDE_HAND;
                usedAnimator = hands.overrideAnimator;
                usedHand = hands.overrideHand;
                break;
            case "GHOST":
                showingTechnique = SHOWING_TECHNIQUE.GHOST_HAND;
                usedAnimator = hands.ghostAnimator;
                usedHand = hands.ghostHand;
                break;
            case "EXTERNAL":
                showingTechnique = SHOWING_TECHNIQUE.EXTERNAL_HAND;
                usedAnimator = hands.externalAnimator;
                usedHand = hands.externalHand;
                break;
        }

        gestures.Add(FindGesture(currentModality.GestureTraining));
        gestures.Add(FindGesture(currentModality.GestureStatic));
        gestures.Add(FindGesture(currentModality.GestureShort));
        gestures.Add(FindGesture(currentModality.GestureLong));

        
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
                OnGestureEnd(false);
            }

            if(!isExpectingGesture && Time.time > neutralTimeout)
            {
                OnNeutralEnd();
            }

            if(currentRepetition >= maxRepetitions)
            {
                StartIdle();
            } 
        }
    }

    private void OnGestureEnd(bool isGestureRecognized)
    {
        isExpectingGesture = false;

        UI.repetionsCounterText.text = currentRepetition.ToString() + "/10";
        UI.instructionsText.text = "Go to neutral position";

        if(isGestureRecognized)
        {
            UI.detectionMarker.color = Color.green;
        }
        else
        {
            UI.detectionMarker.color = Color.red;
        }
        
        neutralTimeout = Time.time + 4f;
    }

    private void OnNeutralEnd()
    {
        isExpectingGesture = true;

        UI.instructionsText.text = "Perform the gesture you saw";
        UI.detectionMarker.color = Color.red;

        if (currentExpectedGesture is DynamicGesture)
        {
            repetitionTimeout = Time.time + ((DynamicGesture)currentExpectedGesture).execTime;
        }
        else
        {
            repetitionTimeout = Time.time + 3f;
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

        UI.detectionMarker.enabled = false;
        UI.detectionMarker.color = Color.red;

        UI.repetionsCounterText.enabled = false;
        UI.repetionsCounterText.text = "0/10";

        UI.instructionsText.text = "Press the first button to start";

        UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
        UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
        UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

        currentExpectedGesture = gestures[currentGestureIndex - 1];
    }

    public void StartShowTechnique()
    {
        if(studyStep != STUDY_STEP.REPETITIONS)
        {
            studyStep = STUDY_STEP.SHOW_TECHNIQUE;
            isAnim = true;
            showGestureRepeats++;

            UI.instructionsText.text = "When you understand the gesture\npress the second button";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            hands.mainHandRenderer.enabled = false;
            usedHand.SetActive(true);

            usedAnimator.enabled = true;
            usedAnimator.Play(currentExpectedGesture.name);
        }
    }

    public void StartFirstPerform()
    {
        if(studyStep == STUDY_STEP.SHOW_TECHNIQUE)
        {
            studyStep = STUDY_STEP.FIRST_PERFORM;
            isAnim = false;

            UI.detectionMarker.enabled = true;

            UI.instructionsText.text = "Perform the gesture\nYou can press the first button to see the gesture again";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            if(UI.detectionMarker.color == Color.green)
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            }

            usedAnimator.enabled = false;
            usedHand.SetActive(false);

            hands.mainHandRenderer.enabled = true;
        }
    }

    public void StartRepetitions()
    {
        if(studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            studyStep = STUDY_STEP.REPETITIONS;
            isAnim = false;

            UI.repetionsCounterText.enabled = true;

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            OnGestureEnd(false);
        }
    }

    public void OnRecognizeEvent(Gesture detectedGesture)
    {
        Debug.Log(detectedGesture.name);
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
                    UI.instructionsText.text = "When you feel confident press the third button";

                    UI.detectionMarker.color = Color.green;
                    UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
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

                        OnGestureEnd(true);
                    }
                    
                }
                break;
        }
    }

    private Gesture FindGesture(string gestureRef)
    {
        foreach(Mapping mapping in gestureMapping)
        {
            if(mapping.refName == gestureRef)
            {
                return mapping.gesture;
            }
        }

        Debug.Log("Gesture ref was not found, when did you last question your life choices?");

        return null;
    }
}
