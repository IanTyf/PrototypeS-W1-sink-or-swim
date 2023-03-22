using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Countdown : MonoBehaviour
{
    public float delay;

    private bool initialized;
    private float timer;

    // Update is called once per frame
    void Update()
    {
        if (initialized)
        {
            timer += Time.deltaTime;

            int ite = Mathf.FloorToInt(timer / 0.6f);
            if (ite % 2 == 0)
            {
                GetComponent<TMP_Text>().text = "ready..";
            }
            else
            {
                GetComponent<TMP_Text>().text = "ready...";
            }


            if (timer > delay)
            {
                GetComponent<TMP_Text>().text = "SWIM!";
                RectTransform rt = GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 10f);
                GetComponent<TMP_Text>().fontSize = 32;
                GetComponent<TMP_Text>().horizontalAlignment = HorizontalAlignmentOptions.Center;
                initialized = false;
                Destroy(this.gameObject, 1f);
            }
        }
    }

    public void Init(float _delay)
    {
        delay = _delay;
        initialized = true;
        timer = 0f;
    }
}
