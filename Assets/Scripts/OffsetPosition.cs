using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetPosition : MonoBehaviour
{
    public GameObject userHand;
    public GameObject cameraRig;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position= userHand.transform.position+ 0.5f*cameraRig.transform.forward;
        this.transform.rotation=userHand.transform.rotation;
    }
}
