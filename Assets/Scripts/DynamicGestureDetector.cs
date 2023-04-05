using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public struct DynamicGesture
{
    public string name;
    public List<Gesture> orderedKeyFrames; //not sorted so that it can show in the editor. Careful !
    public float execTime; //time to execute in seconds
}


public class DynamicGestureDetector : MonoBehaviour
{
    public List<DynamicGesture> dynamicGestures;
    public Dictionary<DynamicGesture, float> runningTimers;
    public Dictionary<DynamicGesture, int> reachedKeyFrame;

    public UnityEvent<DynamicGesture> onRecognized;
    // Start is called before the first frame update
    void Start()
    {
        runningTimers = new Dictionary<DynamicGesture, float>();
        reachedKeyFrame= new Dictionary<DynamicGesture, int>();
        foreach (var dynamicGesture in dynamicGestures)
        {
            reachedKeyFrame[dynamicGesture] = -1;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        //removing expired timers and making the present ones advance
        foreach (var timer in runningTimers)
        {
            if (timer.Value > timer.Key.execTime)
            {
                runningTimers.Remove(timer.Key);
                Debug.Log("stopping timer");
            }
            else
            {
                runningTimers[timer.Key] += Time.deltaTime;
            }
        }
    }

    public void keyFrameRecognized(Gesture gesture)
    {
        foreach (var dynamicGesture in dynamicGestures)
        {
            Debug.Log(gesture.name + " keyframe recognized");
            if (dynamicGesture.orderedKeyFrames[0].name == gesture.name){ //first keyframe reached or rereached (resets gesture) => no gesture with several times the same keyframe
                //start timer
                runningTimers[dynamicGesture] = 0f;
                // advance keyFrameNumber
                reachedKeyFrame[dynamicGesture] = 0; //reached first keyframe
            }
            else
            {
                int nextKeyFrame = reachedKeyFrame[dynamicGesture] + 1; //keyframe needed to progress this particular gesture

                if (dynamicGesture.orderedKeyFrames[nextKeyFrame].name == gesture.name)
                {
                    if (nextKeyFrame == dynamicGesture.orderedKeyFrames.Count - 1) //last keyframe reached
                    {
                        //Gesture recognition event
                        onRecognized?.Invoke(dynamicGesture);
                        //Remove timer
                        runningTimers.Remove(dynamicGesture);
                        //remove reachedKeyFrame
                        reachedKeyFrame[dynamicGesture] = -1;
                    }
                    else //intermediate keyframe reached
                    {
                        reachedKeyFrame[dynamicGesture]++;
                    }
                }
            }
            
        }
    }

    public void DebugDynamicGesture(DynamicGesture dynamicGesture)
    {
        Debug.Log(dynamicGesture.name);
    }
}
