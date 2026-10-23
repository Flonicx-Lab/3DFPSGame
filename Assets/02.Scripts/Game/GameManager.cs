using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;
    private const string ReadyText = "준비";
    private const string PlayText = "시작!";
    private const string GameOverText = "게임오버";

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _state = GameState.Ready;
        _stateText.text = ReadyText;
        _stateText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        _state = GameState.Playing;
        _stateText.text = PlayText;

        yield return new WaitForSeconds(0.5f);

        _stateText.gameObject.SetActive(false);
    }

    public void GameOver()
    {
        _state = GameState.GameOver;
        _stateText.color = Color.red;
        _stateText.text = GameOverText;
        _stateText.gameObject.SetActive(true);
    }
}
