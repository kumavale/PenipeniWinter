﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// アス比に合わせてUIの位置やサイズを調整するクラス
/// </summary>
[ExecuteInEditMode]
public class UIAdjuster : MonoBehaviour {

    private int resolution_x, resolution_y;
    private const int aspect_width  = 81;
    private const int aspect_height = 58;
    private const float align_for_resolution_x = (float)aspect_width / (float)aspect_height;
    private const float align_for_resolution_y = (float)aspect_height / (float)aspect_width;

    private GameManager gm = default;

    //初期化
    protected virtual void Awake() {
        resolution_x = Screen.width;
        resolution_y = Screen.height;
        StartCoroutine(Check_adjust());
    }

    private void Start() {
        gm = this.GetComponent<GameManager>();
    }

    private IEnumerator Check_adjust() {
        bool changed_x = false,
             changed_y = false;

        while (true) {
            //アス比が変わったら調整
            if (resolution_x != Screen.width) {
                changed_x = true;
            } else if (resolution_y != Screen.height) {
                changed_y = true;
            }

            if (changed_x || changed_y) {
                // タイトル画面の場合はウィンドウサイズの調整のみ
                if (gm != null) {
                    gm.display_pause();
                }
            }

            if (changed_x) {
                // 左クリックが押されていなければ
                if (!Mouse.current.leftButton.isPressed) {
                    changed_x = changed_y = false;
                    resolution_x = Screen.width;
                    resolution_y = (int)((float)resolution_x * align_for_resolution_y);
                    Screen.SetResolution(resolution_x, resolution_y, false, 60);
                }
            } else if (changed_y) {
                // 左クリックが押されていなければ
                if (!Mouse.current.leftButton.isPressed) {
                    changed_x = changed_y = false;
                    resolution_y = Screen.height;
                    resolution_x = (int)((float)resolution_y * align_for_resolution_x);
                    Screen.SetResolution(resolution_x, resolution_y, false, 60);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
