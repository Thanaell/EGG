using UnityEngine;
using UnityEngine.Events;

public class ModeSelectionButton : MonoBehaviour
{
    public UnityEvent<DEMO_MODE> buttonPressed;
    public DEMO_MODE mode;

    private void OnTriggerEnter(Collider other)
    {
        buttonPressed.Invoke(mode);
    }
}
