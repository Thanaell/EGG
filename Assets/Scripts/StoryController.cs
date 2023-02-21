using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



public enum HAND_STATE{
    ANIM, TRACKING
}
[System.Serializable]
public class HandStateEvent : UnityEvent<HAND_STATE>
{
}
public class StoryController : MonoBehaviour
{
 
    public HandStateEvent handStateEvent;
    public Animator animator;
    private float timeCount;
    private HAND_STATE state;
    // Start is called before the first frame update
    void Start()
    {
        timeCount=0;
        state= HAND_STATE.TRACKING;
    }

    // Update is called once per frame
    void Update()
    {
        //0-10 : Tracking
        //10-20 : Anim
        if (timeCount>10 && state==HAND_STATE.TRACKING){
            handStateEvent.Invoke(HAND_STATE.ANIM);
            state=HAND_STATE.ANIM;
            
        }
        if (timeCount>20 && state==HAND_STATE.ANIM){
            handStateEvent.Invoke(HAND_STATE.TRACKING);
            state=HAND_STATE.TRACKING;
            timeCount=0;
        }
        ControlState();
        timeCount+=Time.deltaTime;

    }

    void ControlState(){
        if (state==HAND_STATE.TRACKING && animator.enabled==true){
            animator.enabled=false;
        }
        if (state==HAND_STATE.ANIM && animator.enabled==false){
            animator.enabled=true;
        }
    }

        HAND_STATE getHandState(){
        return state;
    }
}
