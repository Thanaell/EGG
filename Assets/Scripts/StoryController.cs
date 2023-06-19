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

    public GameObject externalHand;
    public GameObject ghostHandRoot;
    public GameObject overrideHand;

    public HandStateEvent handStateEvent;
    public UnityEvent logEvent;
    
    private float timeCount;
    private HAND_STATE state;

    public STORY_MODE mode;

    public SkinnedMeshRenderer overrideHandRenderer;
    public SkinnedMeshRenderer mainHandRenderer;
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
         //0-3 : Tracking only
        //3-4 : Anim
        // >4 Tracking
        if (timeCount>3 && state==HAND_STATE.TRACKING){
            handStateEvent.Invoke(HAND_STATE.ANIM);
            state=HAND_STATE.ANIM;         
        }

        if (timeCount>6)
        {
            state = HAND_STATE.TRACKING;
        }

        if (state == HAND_STATE.ANIM)
        {
            logEvent.Invoke();
        }
       
        switch(mode){
            case STORY_MODE.OVERRIDE_TRACKING:{
                ControlState(overrideAnimator);
                break;
            }
            case STORY_MODE.GHOST_HAND_EXTERNAL:{
                ControlState(externalAnimator, externalHand);
                break;
            }
            case STORY_MODE.GHOST_HAND_ROOT:{
                ControlState(rootAnimator, ghostHandRoot);
                break;
            }
            default:break;
        }
            
        timeCount+=Time.deltaTime;

    }

    void ControlState(Animator animator, GameObject ghostHand=null){
        if (state==HAND_STATE.TRACKING && animator.enabled==true){
            animator.enabled=false;
            if (ghostHand!=null){
                ghostHand.SetActive(false);
            }
            if (mode == STORY_MODE.OVERRIDE_TRACKING)
            {
                mainHandRenderer.enabled = true;
                overrideHandRenderer.enabled = false;
            }
        }
        if (state==HAND_STATE.ANIM && animator.enabled==false){
            if (ghostHand!=null){
                ghostHand.SetActive(true);
            }
            if(mode == STORY_MODE.OVERRIDE_TRACKING)
            {
                mainHandRenderer.enabled=false;
                overrideHandRenderer.enabled = true;
            }
            animator.enabled=true;            
        }
    }

    HAND_STATE getHandState(){
        return state;
    }

    void switchMode()  {
        switch(mode){
            case STORY_MODE.OVERRIDE_TRACKING:{
                externalHand.SetActive(false);
                ghostHandRoot.SetActive(false);
                break;
            }
            case STORY_MODE.GHOST_HAND_EXTERNAL:{
                externalHand.SetActive(true);
                ghostHandRoot.SetActive(false);
                break;
            }
            case STORY_MODE.GHOST_HAND_ROOT:{
                externalHand.SetActive(false);
                ghostHandRoot.SetActive(true);
                break;
            }
            default:break;
        }
    }
}
