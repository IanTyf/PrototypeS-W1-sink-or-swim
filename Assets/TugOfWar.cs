using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class TugOfWar : NetworkBehaviour
{

    public float speed;

    public float leftSideForce;
    public float rightSideForce;

    [SyncVar]
    public int leftPlayer;
    [SyncVar]
    public int rightPlayer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // should lerp towards the current position based on left and right side force
        if (IsServer)
        {
            float targetXPos = (rightSideForce - leftSideForce) * speed;
            float x = Mathf.Lerp(transform.position.x, targetXPos, Time.fixedDeltaTime * 10f);

            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
        //rightSideForce += Time.fixedDeltaTime * 0.5f;
    }


    public int AddPlayer()
    {
        // update player count based on how many players there are
        int playerCount = transform.childCount - 1;
        leftPlayer = (playerCount + 1) / 2;
        rightPlayer = (playerCount) / 2;


        if (leftPlayer == 3 && rightPlayer == 3) return 0;

        if (leftPlayer == rightPlayer)
        {
            leftPlayer++;
            return -1;
        }
        else if (leftPlayer > rightPlayer)
        {
            rightPlayer++;
            return 1;
        }
        else
        {
            leftPlayer++;
            return -1;
        }
    }
    
    public void RemovePlayer(int team)
    {
        Debug.Log("player " + team + " removed");
        if (team == 0)
        {
            leftPlayer--;
        }
        if (team == 1)
        {
            rightPlayer--;
        }
    }

    public void ResetGame()
    {
        Debug.Log("resetting pos...");
        transform.position = Vector3.zero;
        leftSideForce = 0;
        rightSideForce = 0;
    }
    
}
