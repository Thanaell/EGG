using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StaticGesture", menuName = "ScriptableObjects/StaticGesture", order = 1)]
public class StaticGesture : ScriptableObject
{
    public string name;
    [SerializeField]
    public List<Vector3> fingerDatas;
}
