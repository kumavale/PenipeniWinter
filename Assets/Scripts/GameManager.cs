using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    GameObject p1 = default,
               p2 = default;

    [SerializeField]
    GameObject canvas_gameover = default;

    private static DateTime unix_epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

    // Start is called before the first frame update
    void Start() {
        // init seed
        //gen_seed();
    }

    // Update is called once per frame
    //void Update() {
    //}

    // Generate seed
    public void gen_seed() {
        int seed = (int)(DateTime.Now - unix_epoch).TotalSeconds;
        p1.GetComponent<PlayManager>().seed = seed;
        p2.GetComponent<PlayManager>().seed = seed;
    }

    public void GameOver(GameObject player) {
        Debug.Log("GameOver: " + player);
        p1.SetActive(false);
        p2.SetActive(false);

        // Enable gameover canvas
        canvas_gameover.SetActive(true);

        // Set another seed
        gen_seed();
    }

    public void Press_button_retry() {
        // Delete all peni prefabs
        GameObject[] penis = GameObject.FindGameObjectsWithTag("peni");
        foreach (GameObject peni in penis) {
            Destroy(peni);
        }

        // Disable gameover canvas
        canvas_gameover.SetActive(false);

        // p1 and p2 to active
        p1.SetActive(true);
        p2.SetActive(true);
        p1.GetComponent<PlayManager>().init();
        p2.GetComponent<PlayManager>().init();
    }
}
