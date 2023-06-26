using UnityEngine;

public abstract class Gesture : ScriptableObject
{
    new public string name;
    public float threshold = 0.1f;
}
