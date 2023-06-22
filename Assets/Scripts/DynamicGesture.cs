using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DynamicGesture", menuName = "ScriptableObjects/DynamicGesture", order = 2)]
public class DynamicGesture : Gesture
{
    [SerializeField]
    public List<StaticGesture> orderedKeyFrames;
    public float execTime;
}
