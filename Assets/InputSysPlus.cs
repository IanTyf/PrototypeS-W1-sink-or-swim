using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class InputSysPlus : NetworkBehaviour
{
    public Material mat;
    public float ropeLength;

    [SyncVar]
    public int playerID;

    public float leftForce;
    public float rightForce;

    private string leftKeys = "";
    private string rightKeys = "";

    private string prevLeftKey = "";
    private string prevRightKey = "";
    private Vector2 prevLeftDir = Vector2.zero;
    private Vector2 prevRightDir = Vector2.zero;


    public TugOfWarPlus towP;
    //public GameObject dude;
    private Animator anim;

    public GameObject bloodParticle;
    public float bloodCD;
    private float bloodTimer;


    private bool started;
    public override void OnStartClient()
    {
        base.OnStartClient();

    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (IsOwner)
        {
            Debug.Log("leaving");
            removePlayer(playerID);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        towP = GameObject.Find("TugOfWarPlus").GetComponent<TugOfWarPlus>();

        anim = transform.GetChild(0).GetComponent<Animator>();

        transform.SetParent(towP.transform);
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner) return;

        if (!started)
        {
            /*
            int playerCount = tow.transform.childCount - 2;
            int leftPlayer = (playerCount + 1) / 2;
            int rightPlayer = (playerCount) / 2;
            Debug.Log(leftPlayer + ", " + rightPlayer);

            if (leftPlayer == 3 && rightPlayer == 3) Destroy(this.gameObject);


            if (leftPlayer <= rightPlayer)
            {
                transform.localPosition = new Vector3(-6.791f * ((3f - leftPlayer) / 3f), 0f, -0.17f);
                team = 0;
            }
            else
            {
                transform.localPosition = new Vector3(6.791f * ((3f - rightPlayer) / 3f), 0f, -0.17f);
                transform.localScale = new Vector3(1.8f, 1f, -1.5f);
                team = 1;
            }
            */

            init();
        }
        Debug.Log(transform.eulerAngles.x);
        Debug.Log(transform.localEulerAngles.x);

        if (Input.GetMouseButtonDown(1))
        {
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                if (towP.gameState == 2)
                    resetGame();
                else if (towP.gameState == 0)
                    startGame();
            }
            else
            {
                serverReset();
            }
        }


        float fov = Camera.main.orthographicSize;
        fov -= Input.GetAxis("Mouse ScrollWheel") * 5f;
        fov = Mathf.Clamp(fov, 5, 15);
        Camera.main.orthographicSize = fov;


        speedDown(1f);



        // input stuff
        if (towP.gameState == 1) return;

        string curKeys = "";

        leftKeys = "";
        rightKeys = "";

        foreach (char c in Input.inputString)
        {
            curKeys += c;
            if ("`123456qwertasdfgzxcv~!@#$%^QWERTASDFGZXCV".Contains(c)) leftKeys += c;
            if ("7890-=yuiop[]\\hjkl;'nm,./YUIOPHJKLNM&*()_+}{\":?><".Contains(c)) rightKeys += c;

        }

        if (curKeys.Equals(""))
        {
            return;
        }

        //else
        //Debug.Log(allKeys);
        //Debug.Log("left: " + leftKeys);
        Debug.Log("right: " + rightKeys);

        if (!leftKeys.Equals(""))
        {
            foreach (char c in leftKeys)
            {
                Vector2 newKeyPos = getKeyPos(c.ToString());
                Vector2 prevLeftKeyPos = getKeyPos(prevLeftKey);
                Vector2 newDir = newKeyPos - prevLeftKeyPos;

                float signedAng = Vector2.SignedAngle(prevLeftDir, newDir);

                prevLeftKey = c.ToString();
                prevLeftDir = newDir;

                if (signedAng > 0f && signedAng <= 100f)
                {
                    // success
                    leftForce += Time.deltaTime * 10f;

                    speedUp(50f);

                    //if (team == 0) addToLeft(Time.deltaTime * 10f);
                    //else if (team == 1) addToRight(Time.deltaTime * 10f);
                    addToTowP(Time.deltaTime * 10f, -Time.deltaTime * 50f, playerID);

                    break;
                }
                else
                {
                    // failure, do nothing
                }
            }
        }

        if (!rightKeys.Equals(""))
        {
            foreach (char c in rightKeys)
            {
                Vector2 newKeyPos = getKeyPos(c.ToString());
                Vector2 prevLeftKeyPos = getKeyPos(prevRightKey);
                Vector2 newDir = newKeyPos - prevLeftKeyPos;

                float signedAng = Vector2.SignedAngle(prevRightDir, newDir);

                prevRightKey = c.ToString();
                prevRightDir = newDir;

                if (signedAng < 0f && signedAng >= -100f)
                {
                    // success
                    rightForce += Time.deltaTime * 10f;

                    speedUp(50f);

                    //if (team == 0) addToLeft(Time.deltaTime * 10f);
                    //else if (team == 1) addToRight(Time.deltaTime * 10f);
                    addToTowP(Time.deltaTime * 10f, Time.deltaTime * 50f, playerID);

                    break;
                }
                else
                {
                    // failure, do nothing
                }
            }
        }

        if (rightKeys.Equals("") && !leftKeys.Equals(""))
        {
            //addToTowP(0f, transform.position, )
        }

    }

    private void init()
    {
        // initialize: find the right position to spawn from, set up collider and material, add itself to TOWP
        //GetComponent<BoxCollider>().enabled = true;
        transform.GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>().material = mat;

        addPlayer(this);
        started = true;
    }

    private void speedUp(float spd)
    {
        float curSpd = anim.GetFloat("speedMult");
        float newSpd = curSpd + Time.deltaTime * spd;
        if (newSpd > 1) newSpd = 1;
        anim.SetFloat("speedMult", newSpd);
    }

    private void speedDown(float spd)
    {
        float curSpd = anim.GetFloat("speedMult");
        float newSpd = curSpd - Time.deltaTime * spd;
        if (newSpd < 0) newSpd = 0;
        anim.SetFloat("speedMult", newSpd);
    }

    /*
    [ServerRpc]
    private void addToLeft(float val)
    {
        tow.leftSideForce += val;
    }

    [ServerRpc]
    private void addToRight(float val)
    {
        tow.rightSideForce += val;
    }
    */

    public void UpdateRot(Vector3 pos, float ang)
    {
        transform.RotateAround(pos, Vector3.forward, ang);
    }

    public void UpdatePos(Vector3 pos)
    {
        transform.position = pos;
    }

    [ServerRpc]
    private void addToTowP(float force, float steer, int id)
    {
        towP.UpdatePlayerInfo(force, steer, id);
    }

    [ServerRpc]
    private void addPlayer(InputSysPlus sys)
    {
        towP.AddPlayer(sys);
    }

    [ServerRpc]
    private void removePlayer(int playerID)
    {
        Debug.Log("leaving, sending rpc");
        towP.RemovePlayer(playerID);
    }

    [ServerRpc]
    private void resetGame()
    {
        towP.ResetGame();
    }

    [ServerRpc]
    private void startGame()
    {
        towP.StartGame();
    }

    [ServerRpc]
    private void serverReset()
    {
        towP.ServerReset();
    }

    private Vector2 getKeyPos(string key)
    {
        Vector2 pos = Vector2.zero;
        switch (key)
        {
            case "`":
                pos.x = 0;
                pos.y = 4;
                break;
            case "~":
                pos.x = 0;
                pos.y = 4;
                break;
            case "1":
                pos.x = 1;
                pos.y = 4;
                break;
            case "!":
                pos.x = 1;
                pos.y = 4;
                break;
            case "2":
                pos.x = 2;
                pos.y = 4;
                break;
            case "@":
                pos.x = 2;
                pos.y = 4;
                break;
            case "3":
                pos.x = 3;
                pos.y = 4;
                break;
            case "#":
                pos.x = 3;
                pos.y = 4;
                break;
            case "4":
                pos.x = 4;
                pos.y = 4;
                break;
            case "$":
                pos.x = 4;
                pos.y = 4;
                break;
            case "5":
                pos.x = 5;
                pos.y = 4;
                break;
            case "%":
                pos.x = 5;
                pos.y = 4;
                break;
            case "6":
                pos.x = 6;
                pos.y = 4;
                break;
            case "^":
                pos.x = 6;
                pos.y = 4;
                break;
            case "7":
                pos.x = 7;
                pos.y = 4;
                break;
            case "&":
                pos.x = 7;
                pos.y = 4;
                break;
            case "8":
                pos.x = 8;
                pos.y = 4;
                break;
            case "*":
                pos.x = 8;
                pos.y = 4;
                break;
            case "9":
                pos.x = 9;
                pos.y = 4;
                break;
            case "(":
                pos.x = 9;
                pos.y = 4;
                break;
            case "0":
                pos.x = 10;
                pos.y = 4;
                break;
            case ")":
                pos.x = 10;
                pos.y = 4;
                break;
            case "-":
                pos.x = 11;
                pos.y = 4;
                break;
            case "_":
                pos.x = 11;
                pos.y = 4;
                break;
            case "=":
                pos.x = 12;
                pos.y = 4;
                break;
            case "+":
                pos.x = 12;
                pos.y = 4;
                break;
            case "q":
                pos.x = 1.4f;
                pos.y = 3;
                break;
            case "Q":
                pos.x = 1.4f;
                pos.y = 3;
                break;
            case "w":
                pos.x = 2.4f;
                pos.y = 3;
                break;
            case "W":
                pos.x = 2.4f;
                pos.y = 3;
                break;
            case "e":
                pos.x = 3.4f;
                pos.y = 3;
                break;
            case "E":
                pos.x = 3.4f;
                pos.y = 3;
                break;
            case "r":
                pos.x = 4.4f;
                pos.y = 3;
                break;
            case "R":
                pos.x = 4.4f;
                pos.y = 3;
                break;
            case "t":
                pos.x = 5.4f;
                pos.y = 3;
                break;
            case "T":
                pos.x = 5.4f;
                pos.y = 3;
                break;
            case "y":
                pos.x = 6.4f;
                pos.y = 3;
                break;
            case "Y":
                pos.x = 6.4f;
                pos.y = 3;
                break;
            case "u":
                pos.x = 7.4f;
                pos.y = 3;
                break;
            case "U":
                pos.x = 7.4f;
                pos.y = 3;
                break;
            case "i":
                pos.x = 8.4f;
                pos.y = 3;
                break;
            case "I":
                pos.x = 8.4f;
                pos.y = 3;
                break;
            case "o":
                pos.x = 9.4f;
                pos.y = 3;
                break;
            case "O":
                pos.x = 9.4f;
                pos.y = 3;
                break;
            case "p":
                pos.x = 10.4f;
                pos.y = 3;
                break;
            case "P":
                pos.x = 10.4f;
                pos.y = 3;
                break;
            case "[":
                pos.x = 11.4f;
                pos.y = 3;
                break;
            case "{":
                pos.x = 11.4f;
                pos.y = 3;
                break;
            case "]":
                pos.x = 12.4f;
                pos.y = 3;
                break;
            case "}":
                pos.x = 12.4f;
                pos.y = 3;
                break;
            case "\\":
                pos.x = 13.4f;
                pos.y = 3;
                break;
            case "|":
                pos.x = 13.4f;
                pos.y = 3;
                break;
            case "a":
                pos.x = 1.8f;
                pos.y = 2;
                break;
            case "A":
                pos.x = 1.8f;
                pos.y = 2;
                break;
            case "s":
                pos.x = 2.8f;
                pos.y = 2;
                break;
            case "S":
                pos.x = 2.8f;
                pos.y = 2;
                break;
            case "d":
                pos.x = 3.8f;
                pos.y = 2;
                break;
            case "D":
                pos.x = 3.8f;
                pos.y = 2;
                break;
            case "f":
                pos.x = 4.8f;
                pos.y = 2;
                break;
            case "F":
                pos.x = 4.8f;
                pos.y = 2;
                break;
            case "g":
                pos.x = 5.8f;
                pos.y = 2;
                break;
            case "G":
                pos.x = 5.8f;
                pos.y = 2;
                break;
            case "h":
                pos.x = 6.8f;
                pos.y = 2;
                break;
            case "H":
                pos.x = 6.8f;
                pos.y = 2;
                break;
            case "j":
                pos.x = 7.8f;
                pos.y = 2;
                break;
            case "J":
                pos.x = 7.8f;
                pos.y = 2;
                break;
            case "k":
                pos.x = 8.8f;
                pos.y = 2;
                break;
            case "K":
                pos.x = 8.8f;
                pos.y = 2;
                break;
            case "l":
                pos.x = 9.8f;
                pos.y = 2;
                break;
            case "L":
                pos.x = 9.8f;
                pos.y = 2;
                break;
            case ";":
                pos.x = 10.8f;
                pos.y = 2;
                break;
            case ":":
                pos.x = 10.8f;
                pos.y = 2;
                break;
            case "'":
                pos.x = 11.8f;
                pos.y = 2;
                break;
            case "\"":
                pos.x = 11.8f;
                pos.y = 2;
                break;
            case "z":
                pos.x = 2.2f;
                pos.y = 1;
                break;
            case "Z":
                pos.x = 2.2f;
                pos.y = 1;
                break;
            case "x":
                pos.x = 3.2f;
                pos.y = 1;
                break;
            case "X":
                pos.x = 3.2f;
                pos.y = 1;
                break;
            case "c":
                pos.x = 4.2f;
                pos.y = 1;
                break;
            case "C":
                pos.x = 4.2f;
                pos.y = 1;
                break;
            case "v":
                pos.x = 5.2f;
                pos.y = 1;
                break;
            case "V":
                pos.x = 5.2f;
                pos.y = 1;
                break;
            case "b":
                pos.x = 6.2f;
                pos.y = 1;
                break;
            case "B":
                pos.x = 6.2f;
                pos.y = 1;
                break;
            case "n":
                pos.x = 7.2f;
                pos.y = 1;
                break;
            case "N":
                pos.x = 7.2f;
                pos.y = 1;
                break;
            case "m":
                pos.x = 8.2f;
                pos.y = 1;
                break;
            case "M":
                pos.x = 8.2f;
                pos.y = 1;
                break;
            case ",":
                pos.x = 9.2f;
                pos.y = 1;
                break;
            case "<":
                pos.x = 9.2f;
                pos.y = 1;
                break;
            case ".":
                pos.x = 10.2f;
                pos.y = 1;
                break;
            case ">":
                pos.x = 10.2f;
                pos.y = 1;
                break;
            case "/":
                pos.x = 11.2f;
                pos.y = 1;
                break;
            case "?":
                pos.x = 11.2f;
                pos.y = 1;
                break;
            default:
                pos.x = 6f;
                pos.y = 0;
                break;
        }

        return pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "lava")
        {
            Instantiate(bloodParticle, transform.position + new Vector3(Mathf.Sign(transform.localScale.z) * 0.5f, 0f, 0f), Quaternion.identity);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "lava")
        {
            if (bloodTimer > bloodCD)
            {
                bloodTimer = 0f;
                Instantiate(bloodParticle, transform.position + new Vector3(Mathf.Sign(transform.localScale.z) * 0.5f, 0f, 0f), Quaternion.identity);
            }
            else
            {
                bloodTimer += Time.deltaTime;
            }
        }
    }
}
