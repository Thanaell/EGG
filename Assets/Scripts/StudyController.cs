using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
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

[System.Serializable]
public class Modality
{
    public string ShowTechnique;
    public string GestureTraining;
    public string GestureStatic;
    public string GestureShort;
    public string GestureLong;
}

[System.Serializable]
public class Participant
{
    public List<Modality> Modalities;
}


[System.Serializable]
public class StudyStory
{
    public List<Participant> Participants;
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

        repetitionTimeout = 0f;
        neutralTimeout = 0f;

        gestures = new List<Gesture>();

        studyStep = STUDY_STEP.IDLE;

        StreamReader reader = new StreamReader("./Study1Story.json");
        string jsonStudyStory = reader.ReadToEnd();
        studyStory = JsonUtility.FromJson<StudyStory>(jsonStudyStory);

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
        UI.instructionsText.text = "Go back in neutral position";

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

        UI.instructionsText.text = "Perform the gesture";
        UI.detectionMarker.color = Color.red;

        // FIXME fix unchecked downcast
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

        UI.showGestureButton.enabled = true;
        UI.tryGestureButton.enabled = false;
        UI.repeatGestureButton.enabled = false;

        UI.detectionMarker.enabled = false;
        UI.detectionMarker.color = Color.red;

        UI.repetionsCounterText.enabled = false;
        UI.repetionsCounterText.text = "0/10";

        UI.instructionsText.text = "You're in idle";

        currentExpectedGesture = gestures[currentGestureIndex - 1];
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

        OnGestureEnd(false);
    }

    // TODO map static gesture detector event
    public void OnRecognizeEvent(Gesture detectedGesture)
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

                        OnGestureEnd(true);
                    }
                    
                }
                break;
        }
    }

    private Gesture FindGesture(string gestureRef)
    {
        Debug.Log("Looking for gesture called: " + gestureRef);

        foreach(Mapping mapping in gestureMapping)
        {
            if(mapping.refName == gestureRef)
            {
                Debug.Log("Found gesture");
                Debug.Log(mapping.gesture);
                Debug.Log(mapping.gesture.name);

                return mapping.gesture;
            }
        }

        Debug.Log("Gesture ref was not found, when did you last question your life choices?");

        return null;
    }
}
