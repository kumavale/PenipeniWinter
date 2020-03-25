using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PlayManager : MonoBehaviour
{
    private Chain chain;
    private int chain_count = 0;

    [SerializeField]
    private GameObject opponent = default;  // 対戦相手
    private PlayManager opponent_pm;

    [SerializeField]
    private const int FIELD_WIDTH  = 4 + 2;  // 4 + 壁
    [SerializeField]
    private const int FIELD_HEIGHT = 2 + 9 + 1;  // 番兵2 + 9 + 壁

    [SerializeField]
    private GameObject[] penis = default;  // peni_0, peni_1, peni_2
    [SerializeField]
    private GameObject[] disturbs = default;  // disturb_0, disturb_1
    public enum DisturbKind {
        L,
        Four,
    }
    private Queue<DisturbKind> disturb_queue = new Queue<DisturbKind>();
    private Queue<DisturbKind> disturb_queue_for_send = new Queue<DisturbKind>();
    public void push_disturbs(Queue<DisturbKind> q) {
        foreach (DisturbKind dk in q) {
            this.disturb_queue.Enqueue(dk);
        }
    }

    [SerializeField]
    private Player player = Player.PLAYER_1;

    [SerializeField]
    private GameObject score_object = default;
    private Text score_text = default;
    private int score = 0;

    private Peni[,] field;  // FIELD_HEIGHT x FIELD_WIDTH
    private bool[,] can_erase_field;  // get_can_erase_field用field

    public enum Player {
        PLAYER_1,
        PLAYER_2,
        CPU,
    }

    public enum BlockKind {
        NONE,
        PENI_0,     // purple
        PENI_1,     // green
        PENI_2,     // blue
        DISTURB_0,  // L
        DISTURB_1,  // _
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

    private Peni current_peni;  // 操作中のPeni
    private int current_x, current_y;  // 操作中のPeniの x,y 座標

    private bool key_lock = false;
    private bool fall_lock = false;
    private bool fall_bottom = false;

    private const float fall_spead = -0.01f;  // 落下スピード / 1frame

    private Vector2[] directions = {
        new Vector2( 0, -1),  // Up
        new Vector2(-1,  0),  // Left
        new Vector2( 0,  1),  // Down
        new Vector2( 1,  0),  // Right
    };

    public int seed = 0;
    private PeniRandom peni_random;

    // 落下中のぺにの有無
    private bool falling_peni = false;
    // after_falling関数が呼ばれた時に真となる
    // 全てのコルーチンが終了していたら偽になる
    private bool after_falling_invoked = false;

    // コルーチンの復帰用キュー
    private Queue<IEnumerator> coroutines = new Queue<IEnumerator>();
    private IEnumerator coroutine_move_x = default;

    // アニメーション
    [SerializeField]
    private Animator peni_jump = default;
    private bool is_chain = false;

    /// Start is called before the first frame update
    void Start() {
        // Initialized field
        field = new Peni[FIELD_HEIGHT, FIELD_WIDTH];
        checked_field = new bool[FIELD_HEIGHT, FIELD_WIDTH];
        can_erase_field = new bool[FIELD_HEIGHT, FIELD_WIDTH];
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                field[y, x] = new Peni(BlockKind.NONE, null);
            }
        }

        // checked_fieldの番兵
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            checked_field[y, 0] = checked_field[y, FIELD_WIDTH-1] = true;
        }
        for (int x = 0; x < FIELD_WIDTH; ++x) {
            checked_field[0, x] = checked_field[FIELD_HEIGHT-1, x] = true;
        }

        chain = this.GetComponent<Chain>();
        score_text = score_object.GetComponent<Text>();

        opponent_pm = opponent.GetComponent<PlayManager>();

        // UNIX時間で乱数の初期化
        System.DateTime unix_epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
        seed = (int)(System.DateTime.Now - unix_epoch).TotalSeconds;
        peni_random = new PeniRandom(seed);

        init();
    }

    private void OnEnable() {
        // コルーチンの再開
        foreach (var coroutine in coroutines) {
            StartCoroutine(coroutine);
        }
        if (coroutine_move_x != null) {
            StartCoroutine(coroutine_move_x);
        }
    }

    public void init() {
        peni_random.set_seed(seed);

        key_lock              = false;
        fall_lock             = false;
        fall_bottom           = false;
        falling_peni          = false;
        after_falling_invoked = false;
        is_chain              = false;

        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                field[y, x].kind = BlockKind.NONE;
                field[y, x].obj  = null;
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

        // Score
        score = 0;
        score_text.text = "0";
        chain_count = 0;

        disturb_queue.Clear();
        disturb_queue_for_send.Clear();
        coroutines.Clear();

        current_peni = spawnNext();
    }

    /// Update is called once per frame
    private void Update() {
        if (fall_lock) {
            if (coroutines.Count == 0) {
                if (after_falling_invoked) {
                    after_falling_invoked = false;
                    after_falling_disturb();
                } else {
                    ++chain_count;
                    eval();
                    fall();
                    if (!fall_lock) {
                        after_falling();
                    }
                }
            }
        } else if (!key_lock) {
            if (player == Player.PLAYER_1) {
                // A key
                if (Input.GetKeyDown(KeyCode.A)) {
                    key_lock = true;
                    coroutine_move_x = move_x(-1);
                    StartCoroutine(coroutine_move_x);
                // D key
                } else if (Input.GetKeyDown(KeyCode.D)) {
                    key_lock = true;
                    coroutine_move_x = move_x(1);
                    StartCoroutine(coroutine_move_x);
                // S key
                } else if (Input.GetKey(KeyCode.S)) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                        score_add(1);
                    } else {
                        fix();
                    }
                }

            } else if (player == Player.PLAYER_2) {
                // LeftArrow key
                if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                    key_lock = true;
                    coroutine_move_x = move_x(-1);
                    StartCoroutine(coroutine_move_x);
                // RightArrow key
                } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                    key_lock = true;
                    coroutine_move_x = move_x(1);
                    StartCoroutine(coroutine_move_x);
                // DownArrow key
                } else if (Input.GetKey(KeyCode.DownArrow)) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                        score_add(1);
                    } else {
                        fix();
                    }
                }
            } else {
                // CPU
                if (fall_bottom) {
                    if (can_fall(-0.2f)) {
                        move_y(-0.2f);
                        score_add(1);
                    } else {
                        fix();
                    }
                } else {
                    if (Random.Range(0, 100) == 0) {  // 1/100f
                        fall_bottom = true;
                    }
                }
            }

            // 落下
            if (!key_lock) {
                if (can_fall(fall_spead)) {
                    move_y(fall_spead);
                } else {
                    fix();
                }
            }
        } else if (current_peni.obj == null) {
            fall_lock = true;
        }
    }

    // ぺにが落下しきった時の処理
    private void fix() {
        key_lock  = true;
        fall_lock = true;
        chain_count = 0;
        disturb_queue_for_send.Clear();
        fix_peni();
    }

    /// 繋がっている"ぺに"の数を返す
    private bool[,] checked_field;
    int get_peni_connected_count(int x, int y, BlockKind k, int cnt) {
        if (checked_field[y, x] || field[y, x].kind != k) {
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

    // 削除出来るぺにのみtrueのフィールドを作成する
    private void get_can_erase_field(int x, int y, Peni p) {
        if (x <= 0 || FIELD_WIDTH-1 <= x || y <= 0 || FIELD_HEIGHT-1 <= y
            || field[y, x].kind != p.kind || can_erase_field[y, x])
        {
            return;
        }

        can_erase_field[y, x] = true;

        for (int d = 0; d < 4; ++d) {
            int x2 = x + (int)directions[d].x;
            int y2 = y + (int)directions[d].y;
            get_can_erase_field(x2, y2, p);
        }
    }

    /// L字に繋がっているか
    private bool is_L_connected(int _x, int _y, Peni p) {
        // can_erase_fieldの初期化
        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                can_erase_field[y, x] = false;
            }
        }

        get_can_erase_field(_x, _y, p);

        for (int y = 1; y < FIELD_HEIGHT-1; ++y) {
            for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                if (can_erase_field[y, x]) {
                    int up_x    = x + (int)directions[0].x;
                    int up_y    = y + (int)directions[0].y;
                    int left_x  = x + (int)directions[1].x;
                    int left_y  = y + (int)directions[1].y;
                    int down_x  = x + (int)directions[2].x;
                    int down_y  = y + (int)directions[2].y;
                    int right_x = x + (int)directions[3].x;
                    int right_y = y + (int)directions[3].y;

                    if (can_erase_field[up_y, up_x] && can_erase_field[right_y, right_x])
                        return true;
                    if (can_erase_field[right_y, right_x] && can_erase_field[down_y, down_x])
                        return true;
                    if (can_erase_field[down_y, down_x] && can_erase_field[left_y, left_x])
                        return true;
                    if (can_erase_field[left_y, left_x] && can_erase_field[up_y, up_x])
                        return true;
                }
            }
        }

        return false;
    }
    // 水平方向に4つ繋がっているか
    // can_erase_fieldが正しく処理されている必要がある
    private bool is_4_horizontal_connected(int _y) {
        for (int y = _y; y < FIELD_HEIGHT-1; ++y) {
            if (can_erase_field[y, 1] && can_erase_field[y, 2]   
             && can_erase_field[y, 3] && can_erase_field[y, 4]) {
                return true;
            }
        }
        return false;
    }

    /// 繋がっている"ぺに"を削除する
    private void erase_peni(int x, int y, Peni p) {
        if (x <= 0 || FIELD_WIDTH-1 <= x || y <= 0 || FIELD_HEIGHT-1 <= y
            || field[y, x].kind != p.kind)
        {
            // お邪魔が隣接していたら, お邪魔も消す
            // DisturbKind.L
            if (field[y, x].kind == BlockKind.DISTURB_0) {
                // L字の 角 の部分
                if (field[y, x].obj != null) {
                    Destroy(field[y, x].obj);
                    field[y, x].obj = null;
                    field[y, x].kind   = BlockKind.NONE;  // 角
                    field[y-1, x].kind = BlockKind.NONE;  // 上
                    field[y, x+1].kind = BlockKind.NONE;  // 右
                }
                // L字の 上 の部分
                else if (field[y+1, x].kind == BlockKind.DISTURB_0 && field[y+1, x].obj != null) {
                    Destroy(field[y+1, x].obj);
                    field[y+1, x].obj = null;
                    field[y, x].kind     = BlockKind.NONE;  // 上
                    field[y+1, x].kind   = BlockKind.NONE;  // 角
                    field[y+1, x+1].kind = BlockKind.NONE;  // 右
                }
                // L字の 右 の部分
                else if (field[y, x-1].kind == BlockKind.DISTURB_0 && field[y, x-1].obj != null) {
                    Destroy(field[y, x-1].obj);
                    field[y, x-1].obj = null;
                    field[y, x].kind     = BlockKind.NONE;  // 右
                    field[y, x-1].kind   = BlockKind.NONE;  // 角
                    field[y-1, x-1].kind = BlockKind.NONE;  // 上

                }
            }
            // DisturbKind.Four
            else if (field[y, x].kind == BlockKind.DISTURB_1) {
                Destroy(field[y, 1].obj);
                field[y, 1].obj  = null;
                field[y, 1].kind = BlockKind.NONE;
                field[y, 2].kind = BlockKind.NONE;
                field[y, 3].kind = BlockKind.NONE;
                field[y, 4].kind = BlockKind.NONE;
            }

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

    /// ぺに,お邪魔を落下させる
    private void fall() {
        bool fall_more_than_1 = false;
        bool falled = false;

        do {
            falled = false;
            for (int y = FIELD_HEIGHT-2; 0 < y; --y) {
                for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                    // ぺにの落下
                    if (field[y, x].kind == BlockKind.NONE
                        && (field[y-1, x].kind == BlockKind.PENI_0
                         || field[y-1, x].kind == BlockKind.PENI_1
                         || field[y-1, x].kind == BlockKind.PENI_2))
                    {
                        falled           = true;
                        fall_lock        = true;
                        fall_more_than_1 = true;

                        field[y, x] = field[y-1, x];
                        IEnumerator coroutine = smooth_fall(field[y, x], -1f);
                        coroutines.Enqueue(coroutine);
                        StartCoroutine(coroutine);

                        field[y-1, x].kind = BlockKind.NONE;
                        field[y-1, x].obj  = null;

                    // お邪魔Lの落下
                    } else if (field[y-1, x].kind == BlockKind.DISTURB_0
                    && field[y-1, x].obj != null
                    && field[y, x].kind   == BlockKind.NONE
                    && field[y, x+1].kind == BlockKind.NONE) {
                        fall_more_than_1 = true;
                        falled = true;
                        fall_lock = true;

                        field[y, x]   = field[y-1, x];
                        field[y, x+1] = field[y-1, x+1];
                        field[y-1, x] = field[y-2, x];
                        IEnumerator coroutine = smooth_fall(field[y, x], -1f);
                        coroutines.Enqueue(coroutine);
                        StartCoroutine(coroutine);

                        field[y-2, x].kind   = BlockKind.NONE;
                        field[y-1, x+1].kind = BlockKind.NONE;
                        field[y-1, x].obj    = null;

                    // お邪魔Fourの落下
                    } else if (x == 1 && field[y-1, x].kind == BlockKind.DISTURB_1
                    && field[y, 1].kind == BlockKind.NONE
                    && field[y, 2].kind == BlockKind.NONE
                    && field[y, 3].kind == BlockKind.NONE
                    && field[y, 4].kind == BlockKind.NONE) {
                        fall_more_than_1 = true;
                        falled = true;
                        fall_lock = true;

                        field[y, 1] = field[y-1, 1];
                        field[y, 2] = field[y-1, 2];
                        field[y, 3] = field[y-1, 3];
                        field[y, 4] = field[y-1, 4];
                        IEnumerator coroutine = smooth_fall(field[y, x], -1f);
                        coroutines.Enqueue(coroutine);
                        StartCoroutine(coroutine);

                        field[y-1, 1].kind = BlockKind.NONE;
                        field[y-1, 2].kind = BlockKind.NONE;
                        field[y-1, 3].kind = BlockKind.NONE;
                        field[y-1, 4].kind = BlockKind.NONE;
                        field[y-1, 1].obj  = null;
                    }
                }
            }
        } while (falled);

        fall_lock =  fall_more_than_1;
    }

    /// 落下アニメーション
    private IEnumerator smooth_fall(Peni p, float distance) {
        const int speed = 30;

        for (int _i = 0; _i < speed; ++_i) {
            Vector3 pos = p.obj.transform.position;
            pos.y += distance / speed;
            p.obj.transform.position = pos;
            yield return null;
        }

        coroutines.Dequeue();
    }

    /// 評価
    /// "ぺに"を削除できるなら削除
    private void eval() {
        // チェック用配列の初期化
        for (int y = 1; y < FIELD_HEIGHT-1; ++y) {
            for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                checked_field[y, x] = false;
            }
        }
        int peni_count = 0;
        int link_count = 0;

        for (int y = 0; y < FIELD_HEIGHT-1; ++y) {
            for (int x = 1; x < FIELD_WIDTH-1; ++x) {
                if (field[y, x].kind != BlockKind.NONE) {
                    if (field[y, x].kind == BlockKind.DISTURB_0 || field[y, x].kind == BlockKind.DISTURB_1) {
                        continue;
                    }
                    int peni_connected_count = get_peni_connected_count(x, y, field[y, x].kind, 0);
                    if (3 <= peni_connected_count) {
                        bool is_L = is_L_connected(x, y, field[y, x]);
                        bool is_4 = is_4_horizontal_connected(y);

                        if (is_L && is_4) {
                            if (7 <= peni_connected_count) {
                                // Send DisturbKind.L and DisturbKind.Four
                                disturb_queue_for_send.Enqueue(DisturbKind.L);
                                disturb_queue_for_send.Enqueue(DisturbKind.Four);
                            } else {
                                // Send DisturbKind.Four
                                disturb_queue_for_send.Enqueue(DisturbKind.Four);
                            }
                        } else {
                            if (is_L) {
                                // Send DisturbKind.L
                                disturb_queue_for_send.Enqueue(DisturbKind.L);
                            } else if (is_4) {
                                // Send DisturbKind.Four
                                disturb_queue_for_send.Enqueue(DisturbKind.Four);
                            }
                        }
                        is_chain = true;
                        peni_count += peni_connected_count;
                        if (link_count < peni_connected_count) {
                            link_count = peni_connected_count;
                        }
                        erase_peni(x, y, field[y, x]);
                    }
                }
            }
        }

        // Score calculate
        score_add(score_calc(peni_count, chain_count, link_count));
    }

    /// ぺにをfieldに固定
    private void fix_peni() {
        int y = (int)Mathf.Floor(-current_peni.obj.transform.position.y) + 2;
        Vector3 pos = current_peni.obj.transform.position;
        pos.y = Mathf.Floor(current_peni.obj.transform.position.y);
        if (player == Player.PLAYER_1) {
            pos.x = -5.5f + current_x;
        } else {
            pos.x = 3.5f + current_x;
        }
        current_peni.obj.transform.position = pos;
        field[y, current_x].kind = current_peni.kind;
        field[y, current_x].obj = current_peni.obj;
        falling_peni = false;
        current_peni.obj = null;
    }

    /// 落下可能か否か
    private bool can_fall(float distance) {
        int y = (int)Mathf.Floor(-(current_peni.obj.transform.position.y + distance)+1);
        return (y < FIELD_HEIGHT-1) && field[y+1, current_x].kind == BlockKind.NONE;
    }

    /// x軸方向に移動可能ならする
    private IEnumerator move_x(int distance) {
        const int speed = 4;

        int dir = distance < 0 ? -1 : 1;

        if (distance != 0) {
            for (int _x = 1; _x <= Mathf.Abs(distance); ++_x) {
                if (field[current_y+1, current_x + dir].kind == BlockKind.NONE) {
                    current_x += dir;
                    for (int _i = 0; _i < speed; ++_i) {
                        Vector3 pos = current_peni.obj.transform.position;
                        pos.x += (float)dir / speed;
                        current_peni.obj.transform.position = pos;
                        yield return null;
                    }
                } else {
                    break;
                }
            }
        }

        coroutine_move_x = null;
        key_lock = false;
    }

    /// y軸方向に移動可能か否か
    private void move_y(float distance) {
        int y = (int)Mathf.Floor(-(current_peni.obj.transform.position.y + distance));

        Vector3 pos = current_peni.obj.transform.position;
        pos.y += distance;
        current_peni.obj.transform.position = pos;

        current_y = y + 1;
        key_lock = false;
    }

    /// Nextぺにを生成
    private Peni spawnNext() {
        // Random index
        int i = peni_random.Range(0, penis.Length);

        // Spawn peni at current position
        Vector3 pos = transform.position;
        pos.x += 1.0f;
        GameObject obj = (GameObject)Instantiate(penis[i], pos, Quaternion.identity);

        BlockKind kind;
        switch (i) {
            case 0:  kind = BlockKind.PENI_0; break;
            case 1:  kind = BlockKind.PENI_1; break;
            case 2:  kind = BlockKind.PENI_2; break;
            default: kind = BlockKind.NONE;   break;
        }

        current_x = 2;
        current_y = 1;
        fall_bottom = false;
        falling_peni = true;

        return new Peni(kind, obj);
    }

    // スコアを加算し、描画する
    private void score_add(int s) {
        this.score += s;
        score_text.text = this.score.ToString();
    }

    // 得点計算
    // 消したぺにの個数 * (連鎖ボーナス + 連結ボーナス) * 10
    private int score_calc(int peni_count, int chain_count, int link_count) {
        link_count -= 3;
        if (link_count < 0) {
            link_count = 0;
        } else if(7 < link_count) {
            link_count = 7;
        }
        return peni_count * (chain.Chain_bonus[chain_count] + chain.Link_bonus[link_count]) * 10;
    }

    // fall()で全てのぺにが落下が終わった後の処理
    private void after_falling() {
        // 固定されていないぺにがある場合, 何もしない
        if (falling_peni) {
            return;
        }

        after_falling_invoked = true;
        fall_lock = true;

        // 1連鎖以上で中央のぺにをジャンプさせる
        if (is_chain) {
            is_chain = false;
            peni_jump.Play("peni_jump");
        }
        // 相殺の計算
        neutralizing();
        // disturb_queueにお邪魔が溜まっているなら自フィールドにお邪魔を降らす
        descend_disturb();
        // 相手にお邪魔を送る
        opponent_pm.push_disturbs(disturb_queue_for_send);
    }

    // fall()で全てのお邪魔が落下が終わった後の処理
    private void after_falling_disturb() {
        // ゲームオーバーのチェック
        bool is_gameover = check_gameover();

        // ゲームオーバーでなければ, 次のぺにを生成
        // CPUは落下予定の列へ移動する
        if (!is_gameover) {
            key_lock = false;
            current_peni = spawnNext();
            if (player == Player.CPU) {
                switch (Random.Range(0, 4)) {
                    case 0: /* Do nothing */ break;
                    case 1: StartCoroutine(move_x(-1)); break;
                    case 2: StartCoroutine(move_x(1));  break;
                    case 3: StartCoroutine(move_x(2));  break;
                }
            }
        }
    }

    // disturb_queueにお邪魔が溜まっているなら自フィールドにお邪魔を降らす
    private void descend_disturb() {
        foreach (DisturbKind dk in disturb_queue) {
            if (dk == DisturbKind.L) {
                // 1/2の確率で左右に振り分ける
                int x = Random.Range(0, 2)==0 ? 1 : 3;
                // もしフィールドが上まで埋まっているなら逆側に置く
                if (field[2, x].kind != BlockKind.NONE || field[2, x+1].kind != BlockKind.NONE) {
                    x = x==1 ? 3 : 1;
                }
                field[0, x].kind   = BlockKind.DISTURB_0;
                field[1, x].kind   = BlockKind.DISTURB_0;
                field[1, x+1].kind = BlockKind.DISTURB_0;
                Vector3 pos = transform.position;
                pos.x += x - 0.5f;
                pos.y += 0.5f;
                GameObject obj = (GameObject)Instantiate(disturbs[0], pos, Quaternion.identity);
                field[1, x].obj = obj;
            } else {
                field[1, 1].kind = BlockKind.DISTURB_1;
                field[1, 2].kind = BlockKind.DISTURB_1;
                field[1, 3].kind = BlockKind.DISTURB_1;
                field[1, 4].kind = BlockKind.DISTURB_1;
                Vector3 pos = transform.position;
                pos.x += 1.5f;
                GameObject obj = (GameObject)Instantiate(disturbs[1], pos, Quaternion.identity);
                field[1, 1].obj = obj;
            }

            fall();
        }

        disturb_queue.Clear();
    }

    // 相殺の計算
    // お邪魔の種類は関係なく, 数で中和する
    private void neutralizing() {
        for (int _q = disturb_queue_for_send.Count; 0 < _q; --_q) {
            if (disturb_queue.Count == 0) { return; }
            disturb_queue.Dequeue();
            disturb_queue_for_send.Dequeue();
        }
    }

    // ゲームオーバーのチェック
    private bool check_gameover() {
        // 2列目一番上のX印部分かその上が埋まっている場合ゲームオーバー
        if (field[0, 2].kind == BlockKind.NONE
         && field[1, 2].kind == BlockKind.NONE
         && field[2, 2].kind == BlockKind.NONE) {
             return false;
        } else {
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            gm.GameOver(this.gameObject);
            return true;
        }
    }


    // デバッグ用関数群
    [Conditional("UNITY_EDITOR")]
    public void show_field(Player p) {
        if (p != player) { return; }

        string _field = null;

        for (int y = 0; y < FIELD_HEIGHT; ++y) {
            for (int x = 0; x < FIELD_WIDTH; ++x) {
                switch (field[y, x].kind) {
                    case BlockKind.NONE:      _field += "_"; break;
                    case BlockKind.WALL:      _field += "#"; break;
                    case BlockKind.DISTURB_0:
                    case BlockKind.DISTURB_1: _field += "*"; break;
                    case BlockKind.PENI_0:
                    case BlockKind.PENI_1:
                    case BlockKind.PENI_2:    _field += "P"; break;
                }
            }
            _field += "\n";
        }
        Debug.Log(_field);
    }
}
