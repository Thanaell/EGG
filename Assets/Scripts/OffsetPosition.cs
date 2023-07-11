using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetPosition : MonoBehaviour
{
    public GameObject userHand;
    public GameObject cameraRig;
    public float forwardOffset = 0.15f;
    public float rightOffset = 0f;
    public float upOffset = 0.15f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = userHand.transform.position + forwardOffset * cameraRig.transform.forward + rightOffset * cameraRig.transform.right + upOffset * cameraRig.transform.up;
        this.transform.rotation=userHand.transform.rotation;
    }
}
