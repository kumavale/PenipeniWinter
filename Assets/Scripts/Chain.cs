using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chain : MonoBehaviour {

    // 連鎖ボーナス
    private static readonly int[] chain_bonus = {
        0,    // 1連鎖
        8,    // 2連鎖
        16,   // 3連鎖
        32,   // 4連鎖
        64,   // 5連鎖
        96,   // 6連鎖
        128,  // 7連鎖
        160,  // 8連鎖
        192,  // 9連鎖
        224,  // 10連鎖
        256,  // 11連鎖
        288,  // 12連鎖
    };

    public int[] Chain_bonus {
        get { return chain_bonus; }
    }

    // 連結ボーナス
    private static readonly int[] link_bonus = {
        0,   // 3個
        2,   // 4個
        3,   // 5個
        4,   // 6個
        5,   // 7個
        6,   // 8個
        7,   // 9個
        10,  // 10個～
    };

    public int[] Link_bonus {
        get { return link_bonus; }
    }
}
