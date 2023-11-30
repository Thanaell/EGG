using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GestureSelectionCollider : MonoBehaviour
{
    public TMP_Text selectionText;
    public Gesture gesture;
    public UnityEvent<Gesture> gestureSelected;

    private void Start()
    {
        if (gesture != null)
        {
            selectionText.text = gesture.name;
        }
        else
        {
            selectionText.text = "No gesture set";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        gestureSelected.Invoke(gesture);
    }
}
