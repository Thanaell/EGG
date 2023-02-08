using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateCube : MonoBehaviour
{
    public GameObject cube;
    public int delay = 10;
    bool isCreated = false;
    // Start is called before the first frame update
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        if (!isCreated)
        {
            if (Time.time > delay)
            {
                Instantiate(cube);
                isCreated = true;
            }
        }
    }
}
