using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : SingletonMonobehaviour<GameManager>
{
    [Header ("Game Settings:")]
    [SerializeField] private float stageDuration;
    [SerializeField] private int matchScoreValue = 10;
    [SerializeField] private int matchComboScoreMultiplier = 2;
    [SerializeField] private int initialScoreTarget = 100;
    [SerializeField][Range (0.001f, 1f)] private float scoreIncreaseRate = 0.3f;
    [Header ("UI References:")]
    [SerializeField] private Slider scoreSliderLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private TMP_Text stageLabel;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private GameObject gameOverUI;

    private int stageScore, totalScore,
    currentStage,
    currentScoreTarget;
    private float timer;

    public static bool GameIsRunning { get; private set; }

    private void Start ()
    {
        StartGame ();
    }

    private void Update ()
    {
        if (!GameIsRunning) return;

        UpdateScoreSlider();
        UpdateTimer ();
    }

    public void ResetGame()
    {
        GemsManager.Instance.ResetGemsBoard();
        StartGame();
    }
    
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public static void StartGame ()
    {
        Instance.timer = Instance.stageDuration;
        Instance.stageScore = 0;
        Instance.totalScore = 0;
        Instance.scoreLabel.SetText (Instance.totalScore.ToString ());
        Instance.currentScoreTarget = Instance.initialScoreTarget;
        Instance.currentStage = 1;
        Instance.stageLabel.SetText (Instance.currentStage.ToString ());
        Instance.scoreSliderLabel.value = Instance.stageScore / (float)Instance.currentScoreTarget;

        GemsController.Instance.ResetController ();
        GemsManager.Instance.SpawnInitialGems ();

        GameIsRunning = true;
        Instance.gameOverUI.SetActive(false);
    }

    public static void AddScore (int comboMultiplier = 0)
    {
        var comboScore = comboMultiplier * Instance.matchComboScoreMultiplier;
        Instance.stageScore += Instance.matchScoreValue * (comboScore > 0 ? comboScore : 1);
        Instance.totalScore += Instance.stageScore;
        Instance.scoreLabel.SetText (Instance.totalScore.ToString ());
        Instance.UpdateScoreTarget ();
    }

    private void UpdateScoreTarget ()
    {
        if (stageScore >= currentScoreTarget)
        {
            scoreSliderLabel.value = 0;
            stageScore = 0;
            currentScoreTarget += (int)(initialScoreTarget + initialScoreTarget * scoreIncreaseRate);
            currentStage++;
            stageLabel.SetText (currentStage.ToString ());

            timer = stageDuration;
        }
    }

    private void UpdateTimer ()
    {
        if (!GameIsRunning) return;

        timer -= Time.deltaTime;
        timerLabel.SetText (Mathf.RoundToInt (timer).ToString () + "s");

        if (timer <= 0)
        {
            timer = 0f;
            GameIsRunning = false;
            gameOverUI.SetActive(true);
        }
    }

    private void UpdateScoreSlider ()
    {
        scoreSliderLabel.value = Mathf.Lerp(scoreSliderLabel.value, stageScore / (float)currentScoreTarget, Time.deltaTime * 4f);
    }
}