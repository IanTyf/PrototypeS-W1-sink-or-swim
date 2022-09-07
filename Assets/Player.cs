using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class Player : NetworkBehaviour
{
    public GameObject dude;

    private TugOfWar tow;

    private bool spawned;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!spawned)
        {
            if (base.IsOwner)
            {

                tow = GameObject.Find("TugOfWar").GetComponent<TugOfWar>();
                int r = tow.AddPlayer();
                if (r == -1)
                {
                    // instantiate a player on the left
                    spawnDude(-1, Owner);
                }
                else if (r == 1)
                {
                    // instantiate a player on the right
                    spawnDude(1, Owner);
                }

                spawned = true;
            }
        }
    }

    [ServerRpc]
    public void spawnDude(int dir, FishNet.Connection.NetworkConnection conn)
    {
        GameObject d = Instantiate(dude);
        if (dir == -1)
            d.transform.position = new Vector3(-6.791f * ((4 - tow.leftPlayer) / 3), 0f, -0.17f);
        if (dir == 1)
            d.transform.position = new Vector3(6.791f * ((4 - tow.rightPlayer) / 3), 0f, -0.17f);

        d.transform.SetParent(tow.transform, true);

        base.Spawn(d, conn);
    }
}
