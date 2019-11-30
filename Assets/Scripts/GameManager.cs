using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    GameObject p1 = default,
               p2 = default;

    [SerializeField]
    GameObject canvas_gameover = default;

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
        p1.SetActive(false);
        p2.SetActive(false);

        // Enable gameover canvas
        canvas_gameover.SetActive(true);
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
