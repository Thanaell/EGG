using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum STORY_MODE{
    GHOST_HAND_ROOT, GHOST_HAND_EXTERNAL, OVERRIDE_TRACKING
}
public enum HAND_STATE{
    ANIM, TRACKING
}
[System.Serializable]
public class HandStateEvent : UnityEvent<HAND_STATE>
{
}
public class StoryController : MonoBehaviour
{
    public Animator overrideAnimator;
    public Animator externalAnimator;
    public Animator rootAnimator;

    public GameObject ghostHandExternal;
    public GameObject ghostHandRoot;

    public HandStateEvent handStateEvent;
    
    private float timeCount;
    private HAND_STATE state;

    public STORY_MODE mode;
    // Start is called before the first frame update
    void Start()
    {
        timeCount=0;
        state= HAND_STATE.TRACKING;
        switchMode();
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
        switch(mode){
            case STORY_MODE.OVERRIDE_TRACKING:{
                ControlState(overrideAnimator);
                break;
            }
            case STORY_MODE.GHOST_HAND_EXTERNAL:{
                ControlState(externalAnimator);
                break;
            }
            case STORY_MODE.GHOST_HAND_ROOT:{
                ControlState(rootAnimator);
                break;
            }
            default:break;
        }
            
        timeCount+=Time.deltaTime;

    }

    void ControlState(Animator animator){
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

    void switchMode()  {
        switch(mode){
            case STORY_MODE.OVERRIDE_TRACKING:{
                ghostHandExternal.SetActive(false);
                ghostHandRoot.SetActive(false);
                break;
            }
            case STORY_MODE.GHOST_HAND_EXTERNAL:{
                ghostHandExternal.SetActive(true);
                ghostHandRoot.SetActive(false);
                break;
            }
            case STORY_MODE.GHOST_HAND_ROOT:{
                ghostHandExternal.SetActive(false);
                ghostHandRoot.SetActive(true);
                break;
            }
            default:break;
        }
    }
}
