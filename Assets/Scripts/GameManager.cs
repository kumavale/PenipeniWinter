using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void GameOver(GameObject player) {
        Debug.Log("GameOver: " + player);
        GameObject p1 = GameObject.Find("1P");
        GameObject p2 = GameObject.Find("2P");
        p1.SetActive(false);
        p2.SetActive(false);
    }
}
