using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetPosition : MonoBehaviour
{
    public GameObject userHand;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position= userHand.transform.position + new Vector3(0f,0f,0.6f);
    }
}
