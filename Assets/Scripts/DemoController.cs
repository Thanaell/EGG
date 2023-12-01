using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum DEMO_MODE
{
    IDLE, SHOW_TECHNIQUE, TRAIN
}

public class DemoController : MonoBehaviour
{
    DEMO_MODE currentMode;
    SHOWING_TECHNIQUE currentTechnique;

    public Gesture defaultGesture;

    public HandsAndAnimators hands;
    public GameObject detectionMarker;

    private bool isPreparingLerp;
    private bool isLerping;

    private GameObject usedHand;
    private Animator usedAnimator;
    private Gesture currentExpectedGesture;

    private float nextStaticGestureDetectionTimestamp;
    private float nextAnimPlayTimestamp;
    private float lerpStartTime;

    private List<Vector3> lerpStartPositions;
    private List<Quaternion> lerpStartRotations;
    private List<Vector3> lerpEndPositions;
    private List<Quaternion> lerpEndRotations;

    private float delayBetweenAnimations = 3.5f;
    private float delayBetweenStaticDetection = 2f;

    private float lerpDurationAfterShow = 0.2f;

    public UnityEvent<List<StaticGesture>> gestureChanged;
    public UnityEvent stateChanged;

    private void Start()
    {
        isPreparingLerp = false;
        isLerping = false;

        nextStaticGestureDetectionTimestamp = -1f;
        nextAnimPlayTimestamp = 0f;

        SwitchTechnique(SHOWING_TECHNIQUE.OVERRIDE_HAND);
        SwitchGesture(defaultGesture);
    }

    private void Update()
    {
        // Starting animation if in first phase and wait delay expired
        if (currentMode == DEMO_MODE.SHOW_TECHNIQUE && nextAnimPlayTimestamp < Time.time)
        {
            if (currentTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
            {
                hands.mainHandRenderer.enabled = false;
            }
            usedHand.SetActive(true);
            usedAnimator.Play(currentExpectedGesture.gestureName);
            // Start anim coroutine
            StartCoroutine(SetNextAnimPlayTimestamp());
        }
    }

    // Late Update because we're touching animations
    // Handles lerp between end of animation and tracked position when we show the gesture through Override
    private void LateUpdate()
    {
        // If we need to lerp (only in SHOWING_TECHNIQUE.OVERRIDE)
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
            // Setting both positions and rotations from the end of animation to the currently tracked hand
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

        // If lerp is active, actually lerping in position and rotation until the main hand is back to its actual tracked position
        if (currentTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND && currentMode == DEMO_MODE.SHOW_TECHNIQUE && isLerping)
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

    public void SwitchTechnique(SHOWING_TECHNIQUE technique)
    {
        currentTechnique = technique;

        switch (currentTechnique)
        {
            case SHOWING_TECHNIQUE.OVERRIDE_HAND:
                usedAnimator = hands.overrideAnimator;
                usedHand = hands.overrideHand;
                break;
            case SHOWING_TECHNIQUE.GHOST_HAND:
                usedAnimator = hands.ghostAnimator;
                usedHand = hands.ghostHand;
                break;
            case SHOWING_TECHNIQUE.EXTERNAL_HAND:
                usedAnimator = hands.externalAnimator;
                usedHand = hands.externalHand;
                break;
        }

        SwitchMode(DEMO_MODE.IDLE);
    }

    public void SwitchGesture(Gesture gesture)
    {
        currentExpectedGesture = gesture;

        // Updating StaticGestureDetector from current expected gesture through events
        List<StaticGesture> gestureList = new List<StaticGesture>();
        if (currentExpectedGesture is StaticGesture) // if static, send the gesture directly
        {
            gestureList.Add((StaticGesture)currentExpectedGesture);
        }
        else
        {
            gestureList = ((DynamicGesture)currentExpectedGesture).orderedKeyFrames; // if dynamix, send each frame of the gesture
        }
        gestureChanged.Invoke(gestureList);

        SwitchMode(DEMO_MODE.IDLE);
    }

    public void SwitchMode(DEMO_MODE newMode)
    {
        currentMode = newMode;
        stateChanged.Invoke();

        detectionMarker.GetComponent<Renderer>().material.color = Color.red;

        switch (currentMode)
        {
            case DEMO_MODE.IDLE:
                isPreparingLerp = false;
                isLerping = false;

                nextStaticGestureDetectionTimestamp = -1f;
                break;
            case DEMO_MODE.SHOW_TECHNIQUE:
                // Enabling animator to show the gesture
                usedAnimator.enabled = true;
                nextAnimPlayTimestamp = Time.time;
                break;
            case DEMO_MODE.TRAIN:
                isPreparingLerp = false;
                isLerping = false;

                //Disabling animator and showing hand
                usedAnimator.enabled = false;
                usedHand.SetActive(false);

                // Re-enabling main hand if showing technique is override
                if (currentTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
                {
                    hands.mainHandRenderer.enabled = true;
                }
                break;
        }
    }

    // Called through event by the detector when a gesture (static or dynamic) is detected
    // Detectors only look at the main hand, never the used hand (the one used to show gestures)
    public void OnRecognizeEvent(Gesture detectedGesture)
    {
        // We ignore gestures detected when a lerp is happening
        if (isLerping)
        {
            return;
        }

        //Debug.Log(detectedGesture.gestureName);

        // If we detected the correct gesture, and it's a static one, create a delay before next allowed detection
        // Prevent detection spamming for static gestures
        if (detectedGesture.gestureName == currentExpectedGesture.gestureName && currentExpectedGesture is StaticGesture)
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

        if (detectedGesture.name == currentExpectedGesture.name)
        {
            detectionMarker.GetComponent<Renderer>().material.color = Color.green;
            StartCoroutine(ChangeDetectionMarkerColor());
        }
    }

    // Coroutine handling a pause between animation plays in the show technique phase
    // Also prepares lerp to smoothly go back to tracking at the end of animation
    private IEnumerator SetNextAnimPlayTimestamp()
    {
        yield return new WaitForEndOfFrame();

        float currentClipLength = usedAnimator.GetCurrentAnimatorStateInfo(0).length;
        nextAnimPlayTimestamp = Time.time + currentClipLength + delayBetweenAnimations;

        // Initiating lerp just before the end of animation play
        yield return new WaitForSeconds(currentClipLength - 0.05f);

        if (currentTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND && currentMode == DEMO_MODE.SHOW_TECHNIQUE)
        {
            isPreparingLerp = true;
        }

        yield return new WaitForSeconds(0.05f);

        // Disabling animation play and going back to tracking
        usedHand.SetActive(false);
        hands.mainHandRenderer.enabled = true;
    }

    private IEnumerator ChangeDetectionMarkerColor()
    {
        yield return new WaitForSeconds(1);
        detectionMarker.GetComponent<Renderer>().material.color = Color.red;
    }
}
