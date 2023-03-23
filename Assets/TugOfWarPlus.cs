using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class TugOfWarPlus : NetworkBehaviour
{
    [SyncVar]
    public int gameState; // 0 means practice, 1 means starting, 2 means started

    public float speed;

    public float leftSideForce;
    public float rightSideForce;

    [System.Serializable]
    public class playerInfo
    {
        public Vector2 pos;
        public Vector2 dir;
        public float force;
        public float steer;
        public bool active;
        public InputSysPlus inputSys;

        public playerInfo(InputSysPlus inputSys)
        {
            this.active = true;
            this.inputSys = inputSys;
        }
    }

    public List<playerInfo> players = new List<playerInfo>();

    [SyncVar]
    public int leftPlayer;
    [SyncVar]
    public int rightPlayer;

    public GameObject center;
    public Vector2 targetPos;

    [Space(10f)]
    public GameObject lava;
    private float growTimer;

    [Space(10f)]
    public GameObject countdown;

    // Start is called before the first frame update
    void Start()
    {
        //center = transform.GetChild(0).gameObject;
        //center = GameObject.Find("center").gameObject;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsServer)
        {
            // should lerp the center towards the position calculated from three forces
            float netX = 0f;
            float netY = 0f;
            foreach (playerInfo player in players)
            {
                if (!player.active) continue;

                netX += player.dir.normalized.x * player.force * speed;
                netY += player.dir.normalized.y * player.force * speed;
            }

            targetPos += new Vector2(netX, netY);

            center.transform.position = Vector3.Lerp(center.transform.position, new Vector3(targetPos.x, targetPos.y, center.transform.position.z), Time.fixedDeltaTime * 10f);
            //SyncCenterPos(center.transform.position);


            // update the player steer, direction, position, rope position
            foreach (playerInfo player in players)
            {
                if (!player.active || player.inputSys == null) continue;

                // calculate steer
                Debug.Log("steer: " + player.steer);
                player.inputSys.transform.RotateAround(center.transform.position, -Vector3.forward, player.steer);
                player.pos = player.inputSys.transform.position;

                // update direction
                Vector2 newDir = (player.pos - (Vector2)center.transform.position).normalized;
                //float ang = Vector2.SignedAngle(player.dir, newDir);
                //player.inputSys.transform.RotateAround(player.pos, Vector3.forward, ang);
                player.inputSys.transform.rotation = Quaternion.LookRotation(newDir, -Vector3.forward);
                //SyncRot(player.conn, player.inputSys, player.pos, ang);
                if (player.inputSys.playerID == 0)
                {
                    Debug.Log(newDir.y);
                    Debug.Log(player.dir);
                    //Debug.Log(ang);
                    Debug.Log(player.inputSys.transform.eulerAngles.x);
                }
                player.dir = newDir;

                // player pos
                Vector2 newPos = player.inputSys.ropeLength * player.dir;
                newPos = (Vector2)center.transform.position + new Vector2(newPos.x, newPos.y);
                player.inputSys.transform.position = newPos;
                player.pos = newPos;
                //SyncPos(player.conn, player.inputSys, newPos);

                // rope visual
                // no need to update since it's being childed to the player object


            }


            // reset the playerInfo force
            foreach (playerInfo player in players)
            {
                player.force = 0f;
                player.steer = 0f;
            }



            // lava increase

            if (gameState == 2)
            {
                growTimer += Time.fixedDeltaTime;
                lava.transform.localScale = Vector3.one * (6 + growTimer * 0.5f);
            }


            // center z shake for synchronization
            if (center.transform.position.z <= 0f) center.transform.position = new Vector3(center.transform.position.x, center.transform.position.y, 0.001f);
            else center.transform.position = new Vector3(center.transform.position.x, center.transform.position.y, -0.001f);
        }
    }

    [ObserversRpc]
    public void SyncCenterPos(Vector3 pos)
    {
        center.transform.position = pos;
    }

    [TargetRpc]
    public void SyncRot(NetworkConnection conn, InputSysPlus sys, Vector3 pos, float ang)
    {
        sys.UpdateRot(pos, ang);
    }

    [TargetRpc]
    public void SyncPos(NetworkConnection conn, InputSysPlus sys, Vector3 pos)
    {
        sys.UpdatePos(pos);
    }

    public void UpdatePlayerInfo(float force, float steer, int playerID)
    {
        players[playerID].force += force;
        players[playerID].steer += steer;
    }


    public void AddPlayer(InputSysPlus inputSys)
    {
        // try to fill up leftover spots first
        for (int i=0; i<players.Count; i++)
        {
            playerInfo player = players[i];
            if (player.active == false || player.inputSys == null)
            {
                playerInfo newPlayer = InitPlayer(inputSys, i);
                players[i] = newPlayer;
                inputSys.playerID = i;
                resetPlayersAndSettings();
                return;
            }
        }

        // try to add more
        if (players.Count < 3)
        {
            playerInfo newPlayer = InitPlayer(inputSys, players.Count);
            players.Add(newPlayer);
            inputSys.playerID = players.Count - 1;
            resetPlayersAndSettings();
            return;
        }
        // no more empty space
        else
        {
            inputSys.playerID = -1;
            return;
        }
    }

    public playerInfo InitPlayer(InputSysPlus inputSys, int pos)
    {
        playerInfo newPlayer = new playerInfo(inputSys);
        //newPlayer.pos = pos;
        //newPlayer.dir = dir;
        int activePlayerCount = 0;
        foreach (playerInfo player in players)
        {
            if (player.active && player.inputSys != null) activePlayerCount++;
        }

        if (activePlayerCount == 0)
        {
            switch (pos)
            {
                case 0:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(-1.5f * Mathf.PI / 3), inputSys.ropeLength * Mathf.Cos(-1.5f * Mathf.PI / 3), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (-1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
            }
        }
        else if (activePlayerCount == 1)
        {
            switch (pos)
            {
                case 0:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(-1.5f * Mathf.PI / 3), inputSys.ropeLength * Mathf.Cos(-1.5f * Mathf.PI / 3), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (-1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
                case 1:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(1.5f * Mathf.PI / 3), inputSys.ropeLength * Mathf.Cos(1.5f * Mathf.PI / 3), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
            }
        }
        else if (activePlayerCount == 2)
        {
            switch (pos)
            {
                case 0:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(-2f * Mathf.PI / 3), inputSys.ropeLength * Mathf.Cos(-2f * Mathf.PI / 3), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (-2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
                case 1:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(2f * Mathf.PI / 3), inputSys.ropeLength * Mathf.Cos(2f * Mathf.PI / 3), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
                case 2:
                    inputSys.transform.localPosition = new Vector3(inputSys.ropeLength * Mathf.Sin(0f), inputSys.ropeLength * Mathf.Cos(0f), -0.17f);
                    inputSys.transform.RotateAround(inputSys.transform.position, inputSys.transform.up, Mathf.Rad2Deg * (0f - Mathf.PI * 1.5f));
                    break;
            }
        }
        newPlayer.pos = inputSys.transform.position;
        newPlayer.dir = inputSys.transform.forward;

        return newPlayer;
    }

    public void RemovePlayer(int playerID)
    {
        Debug.Log("player " + playerID + " removed");
        // don't know what need to be done here yet, probably removing from list?
        players[playerID].active = false;
        reorganizePlayerID();
    }

    public void StartGame()
    {
        if (gameState != 0) return;

        gameState = 1;
        resetPlayersAndSettings();
        float delay = Random.Range(1.5f, 3f);
        showCountDown(delay);
        Invoke("RealStartGame", delay);
    }

    [ObserversRpc]
    private void showCountDown(float delay)
    {
        GameObject cd = Instantiate(countdown);
        cd.GetComponent<Countdown>().Init(delay);
    }

    private void RealStartGame()
    {
        gameState = 2;
    }

    public void ResetGame()
    {
        gameState = 0;
        resetPlayersAndSettings();
    }

    public void ServerReset()
    {
        resetPlayersAndSettings();
    }

    private void reorganizePlayerID()
    {
        for (int i=0; i<players.Count - 1; i++)
        {
            if (players[i].inputSys == null)
            {
                // find the next available one and replace this one
                for (int j=1; j<players.Count - i; j++)
                {
                    if (players[i+j].inputSys != null)
                    {
                        players[i].force = players[i + j].force;
                        players[i].steer = players[i + j].steer;
                        players[i].pos = players[i + j].pos;
                        players[i].dir = players[i + j].dir;
                        players[i].inputSys = players[i + j].inputSys;
                        players[i].inputSys.playerID = i;

                        players[i + j].inputSys = null;
                        break;
                    }
                }
            }
        }
    }

    private void resetPlayersAndSettings()
    {
        reorganizePlayerID();

        growTimer = 0f;
        center.transform.position = Vector3.zero;
        lava.transform.localScale = Vector3.one * 6f;
        targetPos = Vector2.zero;
        Debug.Log("resetting pos...");

        int activePlayerCount = 0;
        foreach (playerInfo player in players)
        {
            if (player.active && player.inputSys != null) activePlayerCount++;
        }

        foreach (playerInfo player in players)
        {
            if (player.active == false || player.inputSys == null) continue;

            player.force = 0f;
            player.steer = 0f;

            /*
            switch (player.inputSys.playerID)
            {
                case 0:
                    player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(-2f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(-2f * Mathf.PI / 3), -0.17f);
                    player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (-2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
                case 1:
                    player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(2f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(2f * Mathf.PI / 3), -0.17f);
                    player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                    break;
                case 2:
                    player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(0f), player.inputSys.ropeLength * Mathf.Cos(0f), -0.17f);
                    player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (0f - Mathf.PI * 1.5f));
                    break;
            }
            */
            

            if (activePlayerCount == 1)
            {
                switch (player.inputSys.playerID)
                {
                    case 0:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(-1.5f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(-1.5f * Mathf.PI / 3), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (-1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                        break;
                }
            }
            else if (activePlayerCount == 2)
            {
                switch (player.inputSys.playerID)
                {
                    case 0:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(-1.5f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(-1.5f * Mathf.PI / 3), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (-1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                        break;
                    case 1:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(1.5f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(1.5f * Mathf.PI / 3), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (1.5f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                        break;
                }
            }
            else if (activePlayerCount == 3)
            {
                switch (player.inputSys.playerID)
                {
                    case 0:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(-2f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(-2f * Mathf.PI / 3), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (-2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                        break;
                    case 1:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(2f * Mathf.PI / 3), player.inputSys.ropeLength * Mathf.Cos(2f * Mathf.PI / 3), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (2f * Mathf.PI / 3 - Mathf.PI * 1.5f));
                        break;
                    case 2:
                        player.inputSys.transform.localPosition = new Vector3(player.inputSys.ropeLength * Mathf.Sin(0f), player.inputSys.ropeLength * Mathf.Cos(0f), -0.17f);
                        player.inputSys.transform.RotateAround(player.inputSys.transform.position, player.inputSys.transform.up, Mathf.Rad2Deg * (0f - Mathf.PI * 1.5f));
                        break;
                }
            }

            player.pos = player.inputSys.transform.position;
            player.dir = player.inputSys.transform.forward;

        }
    }
}
