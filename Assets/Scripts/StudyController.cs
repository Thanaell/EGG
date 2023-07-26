
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
    private bool isPreparingLerp;
    private bool isLerping;
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
    private int numberGestureAskedWhileTry;
    private int numberSuccessWhileTry;
    private int numberSuccessWhileRepeat;

    private float nextStaticGestureDetectionTimestamp;
    private float gestureTimeout;
    private float neutralTimeout;
    private float nextAnimPlayTimestamp;
    private float lerpStartTime;

    private List<Vector3> lerpStartPositions;
    private List<Quaternion> lerpStartRotations;
    private List<Vector3> lerpEndPositions;
    private List<Quaternion> lerpEndRotations;

    private GameObject usedHand;
    private Animator usedAnimator;

    private Gesture currentExpectedGesture;

    public HandLogger handLogger;
    public MainDataLogger mainDataLogger;

    // Customisable vars (eg. waiting time between show tech)
    private float delayBetweenAnimations = 3.5f;
    private float delayBetweenStaticDetection = 2f;

    private float neutralTimeoutDelay = 4f;
    private float staticGestureTimeoutDelay = 3f;

    private float lerpDurationAfterShow = 0.2f;


    public UnityEvent<List<StaticGesture>> gestureChanged;

    void Start()
    {
        isTraining = true;
        isAnim = false;
        isPreparingLerp = false;
        isLerping = false;
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

        double timeSinceUnixEpoch = Math.Floor((DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);

        handLogger.CreateStreamWriter("./StudyLogs/Hand_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + "_" + timeSinceUnixEpoch.ToString() + ".csv");
        mainDataLogger.CreateStreamWriter("./StudyLogs/Main_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + "_" + timeSinceUnixEpoch.ToString() + ".csv");

        nextStaticGestureDetectionTimestamp = 
        gestureTimeout = 0f;
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

        if (studyStep == STUDY_STEP.REPETITIONS || studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            if(isExpectingGesture && Time.time > gestureTimeout)
            {
                if(studyStep == STUDY_STEP.REPETITIONS)
                {
                    currentRepetition++;
                }

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
            if(studyStep == STUDY_STEP.FIRST_PERFORM)
            {
                numberSuccessWhileTry++;
            }
            else if(studyStep == STUDY_STEP.REPETITIONS) {
                numberSuccessWhileRepeat++;
            }
        }
        else
        {
            UI.detectionMarker.color = Color.red;
        }
        
        neutralTimeout = Time.time + neutralTimeoutDelay;
    }

    private void LateUpdate()
    {
        if (isPreparingLerp)
        {
            isPreparingLerp = false;

            isLerping = true;
            lerpStartTime = Time.time;

            lerpStartPositions = new List<Vector3>();
            lerpStartRotations = new List<Quaternion>();
            lerpEndPositions = new List<Vector3>();
            lerpEndRotations = new List<Quaternion>();

            OVRSkeleton overrideSkeleton = hands.overrideHand.GetComponent<OVRSkeleton>();
            foreach (OVRBone bone in overrideSkeleton.Bones)
            {
                lerpStartPositions.Add(new Vector3(bone.Transform.localPosition.x, bone.Transform.localPosition.y, bone.Transform.localPosition.z));
                lerpStartRotations.Add(new Quaternion(bone.Transform.localRotation.x, bone.Transform.localRotation.y, bone.Transform.localRotation.z, bone.Transform.localRotation.w));
            }

            OVRSkeleton mainSkeleton = hands.mainHand.GetComponent<OVRSkeleton>();
            foreach (OVRBone bone in mainSkeleton.Bones)
            {
                lerpEndPositions.Add(new Vector3(bone.Transform.localPosition.x, bone.Transform.localPosition.y, bone.Transform.localPosition.z));
                lerpEndRotations.Add(new Quaternion(bone.Transform.localRotation.x, bone.Transform.localRotation.y, bone.Transform.localRotation.z, bone.Transform.localRotation.w));
            }
        }

        if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND && studyStep == STUDY_STEP.SHOW_TECHNIQUE && isLerping)
        {
            if (Time.time > lerpStartTime + lerpDurationAfterShow)
            {
                isLerping = false;
            }
            else
            {
                float lerpProgression = (Time.time - lerpStartTime) / lerpDurationAfterShow;

                int boneIndex = 0;
                OVRSkeleton mainSkeleton = hands.mainHand.GetComponent<OVRSkeleton>();
                foreach (OVRBone bone in mainSkeleton.Bones)
                {
                    bone.Transform.localPosition = Vector3.Lerp(lerpStartPositions[boneIndex], lerpEndPositions[boneIndex], lerpProgression);
                    bone.Transform.localRotation = Quaternion.Lerp(lerpStartRotations[boneIndex], lerpEndRotations[boneIndex], lerpProgression);

                    boneIndex++;
                }
            }
        }
    }

    private void OnNeutralEnd()
    {
        if (studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            numberGestureAskedWhileTry++;
        }

        isExpectingGesture = true;

        UI.instructionsText.text = "Perform the gesture";
        UI.detectionMarker.color = Color.red;

        if (currentExpectedGesture is DynamicGesture)
        {
            gestureTimeout = Time.time + ((DynamicGesture)currentExpectedGesture).execTime;
        }
        else
        {
            gestureTimeout = Time.time + staticGestureTimeoutDelay;
        }
    }

    private void HandLog(string detectedGestureName="n/a")
    {
        handLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, Time.time, isTraining, isLerping, studyStep, isAnim,
             currentRepetition, showGestureRepeats, currentExpectedGesture.name, detectedGestureName); //handlogger knows by itself the hand position
                
    }

    public void StartIdle()
    {
        if (currentGestureIndex != 0 && currentGestureIndex < 5)
        {
            mainDataLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, isTraining, showGestureRepeats, currentExpectedGesture.name, timestampStartFirstPerform - timestampStartNewGesture, timestampStartRepetitions - timestampStartFirstPerform, numberSuccessWhileShow, numberGestureAskedWhileTry, numberSuccessWhileTry, numberSuccessWhileRepeat);
        }

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
            numberGestureAskedWhileTry = 0;
            numberSuccessWhileTry = 0;
            numberSuccessWhileRepeat = 0;

            nextStaticGestureDetectionTimestamp = -1f;

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

            //TODO : send event
            List<StaticGesture> gestureList = new List<StaticGesture>();
            if (currentExpectedGesture is StaticGesture)
            {           
                gestureList.Add((StaticGesture)currentExpectedGesture);
            }
            else
            {
                gestureList = ((DynamicGesture)currentExpectedGesture).orderedKeyFrames;
            }
            gestureChanged.Invoke(gestureList);
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

            // Remove detection square on show
            UI.detectionMarker.enabled = false;

            usedAnimator.enabled = true;
            nextAnimPlayTimestamp = Time.time;
        }
    }

    public void StartFirstPerform()
    {
        if(studyStep == STUDY_STEP.SHOW_TECHNIQUE)
        {
            isLerping = false;

            // Setting the timestamp only the first time the button is pressed
            if (timestampStartFirstPerform < 0f)
            {
                timestampStartFirstPerform = Time.time;
            }

            studyStep = STUDY_STEP.FIRST_PERFORM;
            isAnim = false;

            UI.detectionMarker.enabled = true;

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            if (isFirstPerformDone)
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
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

            OnGestureEnd(false);
        }
    }

    public void StartRepetitions(bool isCalledFromKeypad = false)
    {
        if(studyStep == STUDY_STEP.FIRST_PERFORM && (isFirstPerformDone || isCalledFromKeypad))
        {
            isLerping = false;

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
        if (isLerping)
        {
            return;
        }

        Debug.Log(detectedGesture.name);

        if (detectedGesture.name == currentExpectedGesture.name && currentExpectedGesture is StaticGesture)
        {
            if (Time.time > nextStaticGestureDetectionTimestamp)
            {
                nextStaticGestureDetectionTimestamp = Time.time + delayBetweenStaticDetection;
            }
            else
            {
                return;
            }
        }

        switch (studyStep)
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
                if (isExpectingGesture)
                {
                    HandLog(detectedGesture.name);
                    if (detectedGesture.name == currentExpectedGesture.name)
                    {
                        isFirstPerformDone = true;
                        UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;

                        OnGestureEnd(true);
                    }
                }
                break;
            case STUDY_STEP.REPETITIONS:
                if (isExpectingGesture)
                {
                    HandLog(detectedGesture.name);
                    if (detectedGesture.name == currentExpectedGesture.name)
                    {
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

    private IEnumerator SetNextAnimPlayTimestamp()
    {
        yield return new WaitForEndOfFrame();

        float currentClipLength = usedAnimator.GetCurrentAnimatorStateInfo(0).length;
        nextAnimPlayTimestamp = Time.time + currentClipLength + delayBetweenAnimations;

        yield return new WaitForSeconds(currentClipLength - 0.05f);

        if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
        {
            isPreparingLerp = true;
        }

        yield return new WaitForSeconds(0.05f);

        isAnim = false;
        usedHand.SetActive(false);
        hands.mainHandRenderer.enabled = true;
    }
}
