using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject p1 = default,
                       p2 = default;

    [SerializeField]
    private GameObject canvas_gameover = default;
    [SerializeField]
    private GameObject canvas_pause = default;

    private readonly DateTime unix_epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

    // Start is called before the first frame update
    //void Start() {
    //    // init seed
    //    //gen_seed();
    //}

    // Update is called once per frame
    private void Update() {
        // ESCキーで Pause menu
        if (Input.GetKeyDown(KeyCode.Escape)) {
            display_pause();
        }
    }

    // ポーズメニューを表示
    public void display_pause() {
        // ゲームオーバー画面, ポーズ画面共に表示されていないなら表示する
        if (!canvas_gameover.activeSelf && !canvas_pause.activeSelf) {
            p1.SetActive(false);
            p2.SetActive(false);
            canvas_pause.SetActive(true);
        }
    }

    // フォーカスがない時、ポーズ画面を表示
    private void OnApplicationFocus(bool has_focus) {
        if (!has_focus && !canvas_gameover.activeSelf) {
            p1.SetActive(false);
            p2.SetActive(false);
            canvas_pause.SetActive(true);
        }
    }

    // Generate seed
    public void gen_seed() {
        int seed = (int)(DateTime.Now - unix_epoch).TotalSeconds;
        p1.GetComponent<PlayManager>().seed = seed;
        p2.GetComponent<PlayManager>().seed = seed;
    }

    public void GameOver(GameObject player) {
        p1.SetActive(false);
        p2.SetActive(false);

        // Enable gameover canvas
        canvas_gameover.SetActive(true);
    }

    public void Press_button_startover() {
        // Delete all peni prefabs
        GameObject[] penis = GameObject.FindGameObjectsWithTag("peni");
        foreach (GameObject peni in penis) {
            Destroy(peni);
        }

        // Disable gameover canvas
        canvas_pause.SetActive(false);
        canvas_gameover.SetActive(false);

        // p1 and p2 to active
        p1.SetActive(true);
        p2.SetActive(true);

        // Set another seed
        gen_seed();

        p1.GetComponent<PlayManager>().init();
        p2.GetComponent<PlayManager>().init();
    }

    public void Press_button_resume() {
        // Disable pause canvas
        canvas_pause.SetActive(false);

        // p1 and p2 to active
        p1.SetActive(true);
        p2.SetActive(true);
    }

    public void Press_button_title() {
        SceneManager.LoadScene("title");
    }
}
