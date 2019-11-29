using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayManager : MonoBehaviour
{
    [SerializeField]
    private int FIELD_WIDTH  = 4 + 2;  // 4 + 壁
    [SerializeField]
    private int FIELD_HEIGHT = 9 + 1;  // 9 + 壁

    [SerializeField]
    private GameObject[] penis = default;  // peni_0, peni_1, peni_2

    [SerializeField]
    private Player player = Player.PLAYER_1;

    private Peni[,] field;  // FIELD_HEIGHT x FIELD_WIDTH

    public enum Player {
        PLAYER_1,
        PLAYER_2,
        CPU,
    }

    public enum BlockKind {
        NONE,
        PENI_0,  // purple
        PENI_1,  // green
        PENI_2,  // blue
        WALL,
    }

    public struct Peni {
        public BlockKind kind;  // インスタンスの種類
        public GameObject obj;  // インスタンスの実体

        /// コンストラクタ
        public Peni(BlockKind k, GameObject o) {
            this.kind = k;
            this.obj  = o;
        }
    }

    Peni current_peni;  // 操作中のPeni
    int current_x, current_y;  // 操作中のPeniの x,y 座標

    bool key_lock = false;
    bool fall_lock = false;
    bool fall_end = true;
    bool fall_bottom = false;

    const float fall_spead = -0.01f;  // 落下スピード / 1frame

    Vector2[] directions = {
        new Vector2( 0, -1),  // Up
        new Vector2(-1,  0),  // Left
        new Vector2( 0,  1),  // Down
        new Vector2( 1,  0),  // Right
    };

    Vector2 out_pos = new Vector2(2, 2);

    /// Start is called before the first frame update
    void Start()
    {
        // Initialized field
        field = new Peni[FIELD_HEIGHT, FIELD_WIDTH];
        checked_field = new bool[FIELD_HEIGHT, FIELD_WIDTH];
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                field[y, x] = new Peni(BlockKind.NONE, null);
            }
        }
        // Side wall
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            field[y, 0].kind = BlockKind.WALL;
            field[y, FIELD_WIDTH-1].kind = BlockKind.WALL;
        }
        // Under wall
        for (int x = 0; x < FIELD_WIDTH; ++x) {
            field[FIELD_HEIGHT-1, x].kind = BlockKind.WALL;
        }

        current_peni = spawnNext();
    }

    /// Update is called once per frame
    void Update()
    {
        if (fall_lock) {
            if (fall_end) {
                eval();
                fall_lock = fall();
                if (!fall_lock) {
                    current_peni = spawnNext();
                    if (player == Player.CPU) {
                        switch (Random.Range(0, 4)) {
                            case 0: /* Do nothing */ break;
                            case 1: StartCoroutine(move_x(-1f)); break;
                            case 2: StartCoroutine(move_x(1f));  break;
                            case 3: StartCoroutine(move_x(2f));  break;
                        }
                    }
                }
            }
        } else {
            if (player == Player.PLAYER_1) {
                // A key
                if (Input.GetKeyDown(KeyCode.A)) {
                    if (!key_lock) {
                        key_lock = true;
                        StartCoroutine(move_x(-1f));
                    }
                // D key
                } else if (Input.GetKeyDown(KeyCode.D)) {
                    if (!key_lock) {
                        key_lock = true;
                        StartCoroutine(move_x(1f));
                    }
                // S key
                } else if (Input.GetKey(KeyCode.S)) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                    } else if (!key_lock) {
                        key_lock = true;
                        fix_peni();
                        eval();
                        fall_lock = fall();
                        if (!fall_lock) {
                            current_peni = spawnNext();
                        }
                    }
                }
            } else if (player == Player.PLAYER_2) {
                // LeftArrow key
                if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                    if (!key_lock) {
                        key_lock = true;
                        StartCoroutine(move_x(-1f));
                    }
                // RightArrow key
                } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    if (!key_lock) {
                        key_lock = true;
                        StartCoroutine(move_x(1f));
                    }
                // DownArrow key
                } else if (Input.GetKey(KeyCode.DownArrow)) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                    } else if (!key_lock) {
                        key_lock = true;
                        fix_peni();
                        eval();
                        fall_lock = fall();
                        if (!fall_lock) {
                            current_peni = spawnNext();
                        }
                    }
                }
            } else {
                if (fall_bottom) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                    } else {
                        fix_peni();
                        eval();
                        fall_lock = fall();
                        if (!fall_lock) {
                            current_peni = spawnNext();
                            if (player == Player.CPU) {
                                switch (Random.Range(0, 4)) {
                                    case 0: /* Do nothing */ break;
                                    case 1: StartCoroutine(move_x(-1f)); break;
                                    case 2: StartCoroutine(move_x(1f));  break;
                                    case 3: StartCoroutine(move_x(2f));  break;
                                }
                            }
                        }
                    }
                } else {
                    if (Random.Range(0, 400) == 0) {  // 1/400f
                        fall_bottom = true;
                    }
                }
            }

            // 落下
            if (can_fall(fall_spead)) {
                move_y(fall_spead);
            } else if (!key_lock) {
                key_lock = true;
                fix_peni();
                eval();
                fall_lock = fall();
                if (!fall_lock) {
                    current_peni = spawnNext();
                    if (player == Player.CPU) {
                        switch (Random.Range(0, 4)) {
                            case 0: /* Do nothing */ break;
                            case 1: StartCoroutine(move_x(-1f)); break;
                            case 2: StartCoroutine(move_x(1f));  break;
                            case 3: StartCoroutine(move_x(2f));  break;
                        }
                    }
                }
            }
        }
    }

    /// 繋がっている"ぺに"の数を返す
    private bool[,] checked_field;
    int get_peni_connected_count(int x, int y, BlockKind k, int cnt) {
        if (x < 0 || FIELD_WIDTH <= x || y < 0 || FIELD_HEIGHT <= y
            || checked_field[y, x] || field[y, x].kind != k)
        {
            return cnt;
        }

        ++cnt;
        checked_field[y, x] = true;

        for (int d = 0; d < 4; ++d) {
            int x2 = x + (int)directions[d].x;
            int y2 = y + (int)directions[d].y;
            cnt = get_peni_connected_count(x2, y2, k, cnt);
        }

        return cnt;
    }

    /// 繋がっている"ぺに"を削除する
    void erase_peni(int x, int y, Peni p) {
        if (x < 0 || FIELD_WIDTH <= x || y < 0 || FIELD_HEIGHT <= y
            || field[y, x].kind != p.kind)
        {
            return;
        }

        if (field[y, x].obj != null) {
            Destroy(field[y, x].obj);
        }
        field[y, x].kind = BlockKind.NONE;
        field[y, x].obj  = null;

        for (int d = 0; d < 4; ++d) {
            int x2 = x + (int)directions[d].x;
            int y2 = y + (int)directions[d].y;
            erase_peni(x2, y2, p);
        }
    }

    /// erase_peni()時, 整地する
    bool fall() {
        bool ret = false;
        bool falled = false;

        do {
            falled = false;
            for (int y = FIELD_HEIGHT-2; y > 0; --y) {
                for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                    if (field[y, x].kind == BlockKind.NONE
                        && field[y-1, x].kind != BlockKind.NONE)
                    {
                        ret = true;
                        falled = true;
                        fall_lock = true;
                        fall_end = false;

                        field[y, x] = field[y-1, x];
                        StartCoroutine(smooth_fall(field[y, x], -1f));

                        field[y-1, x].kind = BlockKind.NONE;
                        field[y-1, x].obj  = null;
                    }
                }
            }
        } while(falled);

        return ret;
    }

    /// 落下アニメーション
    IEnumerator smooth_fall(Peni p, float distance) {
        const int speed = 30;

        for (int _i = 0; _i < speed; ++_i) {
            Vector3 pos = p.obj.transform.position;
            pos.y += distance / speed;
            p.obj.transform.position = pos;
            yield return null;
        }

        fall_end = true;
    }

    /// 評価
    /// "ぺに"を削除できるなら削除
    /// ゲームオーバー判定
    void eval() {
        // チェック用配列の初期化
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                checked_field[y, x] = false;
            }
        }

        for (int y = 0; y < FIELD_HEIGHT-1; ++y) {
            for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                if (field[y, x].kind != BlockKind.NONE) {
                    if (get_peni_connected_count(x, y, field[y, x].kind, 0) >= 3) {
                        erase_peni(x, y, field[y, x]);
                    }
                }
            }
        }

        // Check GameOver
        if (field[(int)out_pos.y, (int)out_pos.x].kind != BlockKind.NONE) {
            fall_lock = true;
            Debug.Log("GameOver");
        }
    }

    /// ぺにをfieldに固定
    void fix_peni() {
        int y = (int)Mathf.Abs(Mathf.Floor(current_peni.obj.transform.position.y)) + 1;
        Vector3 pos = current_peni.obj.transform.position;
        pos.y = Mathf.Floor(current_peni.obj.transform.position.y);
        current_peni.obj.transform.position = pos;
        field[y, current_x].kind = current_peni.kind;
        field[y, current_x].obj = current_peni.obj;
    }

    /// 落下可能か否か
    bool can_fall(float y) {
        int y2 = (int)Mathf.Abs(Mathf.Floor(current_peni.obj.transform.position.y + y));
        return field[y2+1, current_x].kind == BlockKind.NONE;
    }

    /// x軸方向に移動可能か否か
    IEnumerator move_x(float distance) {
        int x = distance <= 0.0f ? -1 : 1;
        const int speed = 5;

        if (field[current_y, current_x + x].kind == BlockKind.NONE) {
            for (int _i = 0; _i < speed; ++_i) {
                Vector3 pos = current_peni.obj.transform.position;
                pos.x += distance / speed;
                current_peni.obj.transform.position = pos;
                yield return null;
            }

            current_x += x;
        }

        key_lock = false;
    }

    /// y軸方向に移動可能か否か
    void move_y(float distance) {
        int y = (int)Mathf.Abs(Mathf.Floor(current_peni.obj.transform.position.y + distance));

        Vector3 pos = current_peni.obj.transform.position;
        pos.y += distance;
        current_peni.obj.transform.position = pos;

        current_y = y+1;
        key_lock = false;
    }

    /// 一番下まで落下させる
    //IEnumerator fall_bottom() {
    //    float height = 0.0f;
    //    while (can_fall(height)) {
    //        height += 1.0f;
    //    }
    //        height += -2.0f;

    //    for (int _i = 0; _i < 5 * (int)height; ++_i) {
    //        Vector3 pos = current_peni.obj.transform.position;
    //        pos.y += -0.2f;
    //        current_peni.obj.transform.position = pos;
    //        yield return null;
    //    }
    //    current_y = (int)height;
    //    key_lock = false;
    //}

    /// Nextぺにを生成
    Peni spawnNext() {
        // Random index
        int i = Random.Range(0, penis.Length);

        // Spawn peni at current position
        Vector3 pos = transform.position;
        pos.x += 1.0f;
        GameObject obj = (GameObject)Instantiate(penis[i], pos, Quaternion.identity);

        BlockKind kind;
        switch (i) {
            case 0:  kind = BlockKind.PENI_0; break;
            case 1:  kind = BlockKind.PENI_1; break;
            case 2:  kind = BlockKind.PENI_2; break;
            default: kind = BlockKind.NONE; break;
        }

        current_x = 2;
        current_y = 0;
        fall_bottom = false;

        return new Peni(kind, obj);
    }
}
