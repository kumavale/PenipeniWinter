using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField]
    private GameObject[] lights = default;
    private Transform[] transforms = default;

    [SerializeField]
    private GameObject press_enter = default;

    private bool fixed_position_now = true;
    private int waiting_count = 0;

    // Start is called before the first frame update
    private void Start() {
        transforms = new Transform[7];

        for (int i = 0; i < 7; ++i) {
            transforms[i] = lights[i].transform;
        }
    }

    // Update is called once per frame
    private void Update() {
        if (Input.GetKey(KeyCode.Return)) {
            SceneManager.LoadScene("main");
        }
    }

    private void FixedUpdate() {
        if (18 < ++waiting_count) {
            waiting_count = 0;

            if (fixed_position_now) {
                // [PRESS Enter] の表示/非表示
                press_enter.SetActive(!press_enter.activeSelf);

                // light0
                Vector3 pos = transforms[0].position;
                pos.x += 0.3f;
                pos.y -= 0.1f;
                transforms[0].position = pos;
                // light1
                pos = transforms[1].position;
                pos.x -= 0.25f;
                pos.y += 0.15f;
                transforms[1].position = pos;
                // light2
                pos = transforms[2].position;
                pos.x += 0.15f;
                pos.y -= 0.2f;
                transforms[2].position = pos;
                // light3
                pos = transforms[3].position;
                pos.y += 0.25f;
                transforms[3].position = pos;
                // light4
                pos = transforms[4].position;
                pos.x -= 0.15f;
                pos.y -= 0.2f;
                transforms[4].position = pos;
                // light5
                pos = transforms[5].position;
                pos.x += 0.2f;
                pos.y += 0.15f;
                transforms[5].position = pos;
                // light6
                pos = transforms[6].position;
                pos.x -= 0.25f;
                pos.y -= 0.1f;
                transforms[6].position = pos;
            } else {
                // light0
                Vector3 pos = transforms[0].position;
                pos.x -= 0.3f;
                pos.y += 0.1f;
                transforms[0].position = pos;
                // light1
                pos = transforms[1].position;
                pos.x += 0.25f;
                pos.y -= 0.15f;
                transforms[1].position = pos;
                // light2
                pos = transforms[2].position;
                pos.x -= 0.15f;
                pos.y += 0.2f;
                transforms[2].position = pos;
                // light3
                pos = transforms[3].position;
                pos.y -= 0.25f;
                transforms[3].position = pos;
                // light4
                pos = transforms[4].position;
                pos.x += 0.15f;
                pos.y += 0.2f;
                transforms[4].position = pos;
                // light5
                pos = transforms[5].position;
                pos.x -= 0.2f;
                pos.y -= 0.15f;
                transforms[5].position = pos;
                // light6
                pos = transforms[6].position;
                pos.x += 0.25f;
                pos.y += 0.1f;
                transforms[6].position = pos;
            }

            fixed_position_now = !fixed_position_now;
        }
    }
}
