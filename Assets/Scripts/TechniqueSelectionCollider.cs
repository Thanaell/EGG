using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TechniqueSelectionCollider : MonoBehaviour
{
    public TMP_Text selectionText;
    public SHOWING_TECHNIQUE showTechnique;
    public UnityEvent<SHOWING_TECHNIQUE> techniqueSelected;

    private void OnTriggerEnter(Collider other)
    {
        techniqueSelected.Invoke(showTechnique);
    }
}
