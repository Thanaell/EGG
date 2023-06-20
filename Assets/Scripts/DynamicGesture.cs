using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DynamicGesture", menuName = "ScriptableObjects/DynamicGesture", order = 2)]
public class DynamicGesture : ScriptableObject
{
    new public string name;
    [SerializeField]
    public List<StaticGesture> orderedKeyFrames;
    public float execTime;
}