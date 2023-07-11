
using System.Collections;
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
    private bool isFirstPerformDone;

    private int maxRepetitions = 10;
    private int currentRepetition;
    private int currentGestureIndex;
    private int showGestureRepeats;

    private float timestampStartNewGesture;
    private float timestampStartFirstPerform;
    private float timestampStartRepetitions;
    private int numberSuccessWhileShow;
    private int numberSuccessWhileTry;
    private int numberSuccessWhileRepeat;

    private float repetitionTimeout;
    private float neutralTimeout;
    private float nextAnimPlayTimestamp;

    private GameObject usedHand;
    private Animator usedAnimator;

    private Gesture currentExpectedGesture;

    public HandLogger handLogger;
    public MainDataLogger mainDataLogger;

    // Customisable vars (eg. waiting time between show tech)
    private float delayBetweenAnimations = 2f;

    void Start()
    {
        isTraining = true;
        isAnim = false;
        isExpectingGesture = false;
        isFirstPerformDone = false;

        currentRepetition = 0;
        currentGestureIndex = 0;
        showGestureRepeats = 0;

        if(participantNumber == -1 || modalityNumber == -1)
        {
            Debug.Log("ERROR Participant or modality number not set");
            Application.Quit();
        }

        handLogger.CreateStreamWriter("./Hand_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + ".csv");
        mainDataLogger.CreateStreamWriter("./Main_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + ".csv");

        repetitionTimeout = 0f;
        neutralTimeout = 0f;
        nextAnimPlayTimestamp = 0f;

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
        // Logging each frame
        if (currentGestureIndex <= 4){
            HandLog();
        }

        if (studyStep == STUDY_STEP.SHOW_TECHNIQUE && nextAnimPlayTimestamp < Time.time)
        {
            isAnim = true;

            if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
            {
                hands.mainHandRenderer.enabled = false;
            }
            usedHand.SetActive(true);
            usedAnimator.Play(currentExpectedGesture.name);

            StartCoroutine(SetNextAnimPlayTimestamp());
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
                mainDataLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, isTraining, showGestureRepeats, currentExpectedGesture.name, timestampStartFirstPerform - timestampStartNewGesture, timestampStartRepetitions - timestampStartFirstPerform, numberSuccessWhileShow, numberSuccessWhileTry, numberSuccessWhileRepeat);
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
            numberSuccessWhileRepeat++;
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

    private void HandLog(string detectedGestureName="n/a")
    {
        handLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, Time.time, isTraining, studyStep, isAnim,
             currentRepetition, showGestureRepeats, currentExpectedGesture.name, detectedGestureName); //handlogger knows by itself the hand position
                
    }

    public void StartIdle()
    {
        currentGestureIndex++;

        if (currentGestureIndex >= 5)
        {
            studyStep = STUDY_STEP.IDLE;
            
            UI.instructionsText.text = "Please remove the headset";

            UI.showButton.transform.parent.gameObject.SetActive(false);
            UI.tryButton.transform.parent.gameObject.SetActive(false);
            UI.repeatButton.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            studyStep = STUDY_STEP.IDLE;
            currentRepetition = 0;
            showGestureRepeats = 0;

            timestampStartNewGesture = -1f;
            timestampStartFirstPerform = -1f;
            numberSuccessWhileShow = 0;
            numberSuccessWhileTry = 0;
            numberSuccessWhileRepeat = 0;

            if (currentGestureIndex > 1)
            {
                isTraining = false;
            }

            isFirstPerformDone = false;

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
        
    }

    public void StartShowTechnique()
    {
        if(studyStep != STUDY_STEP.REPETITIONS)
        {
            // Setting the timestamp only the first time the button is pressed
            if(timestampStartNewGesture < 0f)
            {
                timestampStartNewGesture = Time.time;
            }

            studyStep = STUDY_STEP.SHOW_TECHNIQUE;
            showGestureRepeats++;

            UI.instructionsText.text = "When you understand the gesture\npress the second button";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            usedAnimator.enabled = true;
            nextAnimPlayTimestamp = Time.time;
        }
    }

    public void StartFirstPerform()
    {
        if(studyStep == STUDY_STEP.SHOW_TECHNIQUE)
        {
            // Setting the timestamp only the first time the button is pressed
            if (timestampStartFirstPerform < 0f)
            {
                timestampStartFirstPerform = Time.time;
            }

            studyStep = STUDY_STEP.FIRST_PERFORM;
            isAnim = false;

            UI.detectionMarker.enabled = true;

            UI.instructionsText.text = "Perform the gesture\nYou can press the first button to see the gesture again";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            if(isFirstPerformDone)
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
                UI.instructionsText.text = "When you feel confident press the third button";
            }
            else
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            }

            usedAnimator.enabled = false;
            usedHand.SetActive(false);

            if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
            {
                hands.mainHandRenderer.enabled = true;
            }
        }
    }

    public void StartRepetitions()
    {
        if(studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            timestampStartRepetitions = Time.time;

            studyStep = STUDY_STEP.REPETITIONS;

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
                HandLog(detectedGesture.name);
                if (detectedGesture.name == currentExpectedGesture.name)
                {
                    numberSuccessWhileShow++;
                }
                    break;
            case STUDY_STEP.FIRST_PERFORM:
                HandLog(detectedGesture.name);
                if (detectedGesture.name == currentExpectedGesture.name)
                {
                    isFirstPerformDone = true;
                    numberSuccessWhileTry++;

                    UI.instructionsText.text = "When you feel confident press the third button";

                    UI.detectionMarker.color = Color.green;
                    UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;

                    StartCoroutine(WaitThenRed());
                }
                break;
            case STUDY_STEP.REPETITIONS:
                if(isExpectingGesture)
                {
                    HandLog(detectedGesture.name);
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

    public IEnumerator WaitThenRed()
    {
        yield return new WaitForSeconds(2f);
        UI.detectionMarker.color = Color.red;
    }

    private IEnumerator SetNextAnimPlayTimestamp()
    {
        yield return new WaitForEndOfFrame();

        float currentClipLength = usedAnimator.GetCurrentAnimatorStateInfo(0).length;
        nextAnimPlayTimestamp = Time.time + currentClipLength + delayBetweenAnimations;

        yield return new WaitForSeconds(currentClipLength);

        isAnim = false;
        usedHand.SetActive(false);
        hands.mainHandRenderer.enabled = true;
    }
}
