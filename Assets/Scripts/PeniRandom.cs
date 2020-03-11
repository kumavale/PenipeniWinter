using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeniRandom : MonoBehaviour {

    private Random.State state;

    public PeniRandom(int seed = 42) {
        set_seed(seed);
    }

    public void set_seed(int seed) {
        Random.State prev_state = Random.state;
        Random.InitState(seed);
        state = Random.state;
        Random.state = prev_state;
    }

    public int Range(int min, int max) {
        Random.State prev_state = Random.state;
        Random.state = state;
        int result = Random.Range(min, max);
        state = Random.state;
        Random.state = prev_state;

        return result;
    }
}
