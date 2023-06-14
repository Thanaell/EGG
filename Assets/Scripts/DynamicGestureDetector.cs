using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



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
        List<DynamicGesture> timersToRemove = new List<DynamicGesture>();
        List<DynamicGesture> timersToAdvance = new List<DynamicGesture>();
        foreach (var timer in runningTimers)
        {
            if (timer.Value > timer.Key.execTime)
            {
                timersToRemove.Add(timer.Key); //marking expired timers (not removing them during the iteration)
            }
            else
            {
                timersToAdvance.Add(timer.Key); //marking other timers (not doing it during the iteration)
            }
        }
        foreach (var dynamicGesture in timersToRemove) //removing expired timers
        {
            runningTimers.Remove(dynamicGesture);
        }
        foreach (var dynamicGesture in timersToAdvance) //advancing other timers
        {
            runningTimers[dynamicGesture] += Time.deltaTime;

        }
    }

    public void keyFrameRecognized(StaticGesture gesture)
    {
        foreach (var dynamicGesture in dynamicGestures)
        {
            //Debug.Log(gesture.name + " keyframe recognized");
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
                        DebugDynamicGesture(dynamicGesture);
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
