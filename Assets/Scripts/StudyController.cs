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
}

//two repetitions of the cycle per run. One for training, the other for real
public class StudyController : MonoBehaviour
{
    public int participantNumber;
    public int modalityNumber; //1, 2, 3

    public HandsAndAnimators hands;

    public UI_Elements UI;

    private STORY_MODE showingTechnique;
    private STUDY_STEP studyStep;
    private bool isTraining;

    private int maxRepetitions = 10;
    private int currentRepetition;

    private GameObject usedHand;
    private Animator usedAnimator;

    

    // Start is called before the first frame update
    void Start()
    {
        isTraining = true;
        studyStep = STUDY_STEP.IDLE;
        currentRepetition = 0;

        UI.repeatGestureButton.enabled = false;
        UI.tryGestureButton.enabled = false;
        UI.repetionsCounterText.text = "hello world";
        UI.detectionMarker.color = Color.green;


    //TODO : Story mode and gesture subset from CSV file (depending on participant number and modality number)
}

// Update is called once per frame
void Update()
    {
        //TODO : handle log ?
    }
    
    //Coroutines ?
    public void StartShowTechnique()
    {

    }

    public void StartFirstPerform()
    {

    }

    public void StartRepetitions()
    {

    }

    public void OnButtonPressed(STUDY_STEP stepToGo)
    {
        switch (stepToGo)
        {
            case STUDY_STEP.SHOW_TECHNIQUE:
                StartShowTechnique();
                break;
            case STUDY_STEP.FIRST_PERFORM:
                StartFirstPerform();
                break;
            case STUDY_STEP.REPETITIONS:
                StartRepetitions();
                break;
            default:
                Debug.Log("did not switch to an active mode. Should we really be here ?");
                studyStep = STUDY_STEP.IDLE;
                break;
        }
    }
}
