using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TugOfWar : MonoBehaviour
{
    public float speed;

    public float leftSideForce;
    public float rightSideForce;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // should lerp towards the current position based on left and right side force
        float targetXPos = (rightSideForce - leftSideForce) * speed;
        float x = Mathf.Lerp(transform.position.x, targetXPos, Time.fixedDeltaTime * 10f);

        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        rightSideForce += Time.deltaTime * 0.5f;
    }
}
