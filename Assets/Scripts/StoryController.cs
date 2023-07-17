using UnityEngine;

public enum HAND_STATE
{
    ANIM, TRACKING
}

public class StoryController : MonoBehaviour
{
    public string animationName;
    public Animator overrideAnimator;

    public GameObject overrideHand;

    void Start()
    {
        overrideAnimator.enabled = true;
        overrideAnimator.Play(animationName);
    }
}
