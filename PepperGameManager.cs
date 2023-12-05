using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Globalization;
using UnityEngine.SceneManagement;

public class PepperGameManager : MonoBehaviour
{
    // References and variables
    public GameObject ScoreManager;
    private HighScore_Script HighScore_Script;
    private PauseButton PauseButton;

    AudioSource[] Sounds;

    // Gameplay variables
    public float TotalToleranceTime = 25f;
    private int difficulty = 1;
    public GameObject skin_angry;
    public GameObject skin_norm;
    public bool medium = false;
    public bool angry = false;
    public int LayerOrder = 10;
    public float AmassedValue;
    private float compare = 0.0f;
    public GameObject CustomerSpawn;
    private GameObject spawnPos;
    public GameObject[] numpos;
    public GameObject[] Textnums;
    public GameObject[] ShoppingItems;
    public GameObject[] OfferedMoneyArray;
    private GameObject ItemPos, OfferedMoneyPos; // Spawn positions of Gameobjects
    public GameObject ItemPosTxt, OfferedMoneyPosTxt; // Positions for text (used only once in void start)
    private GameObject OfferedAmount, ProductAmount, GiveTxt; // The buttons/text items themselves
    private float value, AmountToPay, OfferedMoney;
    private GameObject ShowChange, ShowChangePos;
    private int indexMoney = -1;
    private float[] MoneyNotes = new float[7] { 0.5f, 1f, 2f, 5f, 10f, 20f, 50f };
    private int life = 4;
    public TextMeshProUGUI Scores, Decoy, customersRemaining, highscore_txt;
    public TextMeshProUGUI GuideBoxTxt;
    private int GameScore = 0;
    private int customerCount = 10;
    private int Highscore = 0;
    private customerScript customerScript;
    private GameObject MrPepper;
    public GameObject GameOverObjs;
    private GameObject happy, unhappy, fired;
    private int PepState = 0;
    private float PepSpeed = 40.0f;
    public GameObject Target, Home;
    private bool runAgain = true;
    public GameObject rawPepper, rawPepper2;
    private bool gameover = false;

    void Start()
    {
        // Check the currency and adjust the available money notes accordingly
        if (CurrencySelect_Pepper.currency == "euro") { MoneyNotes = new float[9] { 0.5f, 1f, 2f, 5f, 10f, 20f, 50f, 100f, 200f }; }

        // Get audio sources and set the difficulty level
        Sounds = GetComponents<AudioSource>();
        difficulty = TutorialPepper.level;

        // Find and initialize UI elements
        happy = GameObject.Find("happy"); unhappy = GameObject.Find("unhappy"); fired = GameObject.Find("fired");
        unhappy.SetActive(false); fired.SetActive(false);
        for (int i = 0; i < numpos.Length; i++)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(numpos[i].transform.position);
            Textnums[i].transform.position = pos;
        }

        HighScore_Script = GameObject.Find("ScoreManager").GetComponent<HighScore_Script>();
        PauseButton = GameObject.Find("Pause Button").GetComponent<PauseButton>();

        ItemPos = GameObject.Find("ItemPos");
        spawnPos = GameObject.Find("spawnPos");
        OfferedMoneyPos = GameObject.Find("OfferedMoneyPos");
        OfferedAmount = GameObject.Find("OfferedTxt");
        ProductAmount = GameObject.Find("ItemTxt");
        GiveTxt = GameObject.Find("GiveTxt");
        MrPepper = GameObject.Find("MrPepper");
        ShowChangePos = GameObject.Find("ShowChangePos");
        ShowChange = GameObject.Find("ShowChange");

        // Set initial positions of UI elements
        Vector2 pos2 = Camera.main.WorldToScreenPoint(OfferedMoneyPosTxt.transform.position);
        OfferedAmount.transform.position = pos2;
        pos2 = Camera.main.WorldToScreenPoint(ItemPosTxt.transform.position);
        ProductAmount.transform.position = pos2;
        Vector2 givepos2 = Camera.main.WorldToScreenPoint(GameObject.Find("GiveBoxPosTxt").transform.position);
        GiveTxt.transform.position = givepos2;
        Vector2 post2 = Camera.main.WorldToScreenPoint(GameObject.Find("GuideBox").transform.position);
        GuideBoxTxt.transform.position = post2;
        Vector2 post22 = Camera.main.WorldToScreenPoint(ShowChangePos.transform.position);
        ShowChange.transform.position = post22;

        // Initialize UI text and values
        customersRemaining.text = customerCount.ToString();
        Highscore = PlayerPrefs.GetInt($"PepperGame{difficulty}", 0);
        highscore_txt.text = "/" + Highscore.ToString();
    }

    void Update()
    {
        // Update game logic
        if (PepState == 1) { MrPepper.transform.position = Vector2.MoveTowards(MrPepper.transform.position, Target.transform.position, PepSpeed * Time.deltaTime); if (runAgain == true) { runAgain = false; StartCoroutine(returnChest()); } }
        if (PepState == 2) { MrPepper.transform.position = Vector2.MoveTowards(MrPepper.transform.position, Home.transform.position, PepSpeed * Time.deltaTime); runAgain = true; }
        if (PepState == 4)
        {
            float a = ItemPos.transform.position.x + (Mathf.Sin(Time.time * 50.0f) * 0.1f);
            float b = ItemPos.transform.position.y + (Mathf.Sin(Time.time * 21.0f) * 0.1f);
            Vector2 temp = new Vector2(a, b);
            rawPepper.transform.position = Camera.main.WorldToScreenPoint(temp);
        }
    }

    public void Reset()
    {
        // Reset game values and prepare for the next customer
        compare = 0.0f;
        if (life > 0)
        {
            if (difficulty == 1) { AmountToPay = UnityEngine.Random.Range(3, 20); }
            // Adjust difficulty settings for different scenarios
            if (difficulty == 2)
            {
                int w = UnityEngine.Random.Range(1, 5);
                if (w == 1) { AmountToPay = UnityEngine.Random.Range(150.0f, 200.0f); AmountToPay = Mathf.Round(AmountToPay * 2) / 2; }
                if (w == 2 || w == 3) { AmountToPay = UnityEngine.Random.Range(2, 101); }
                if (w == 4) { AmountToPay = UnityEngine.Random.Range(8, 20) * 10; }
            }
            if (difficulty == 3) { AmountToPay = UnityEngine.Random.Range(0.15f, 50.00f); AmountToPay = (float)Math.Round(AmountToPay, 2); }

            // Find appropriate note to spawn
            indexMoney = -1;
            for (int i = 0; i < MoneyNotes.Length; i++)
            {
                indexMoney++;
                if (AmountToPay < MoneyNotes[i]) { break; }
            }
            int randMoneyIndex;
            if (difficulty == 1 && CurrencySelect_Pepper.currency == "ghc") { randMoneyIndex = UnityEngine.Random.Range(indexMoney, MoneyNotes.Length - 1); } // minus 1 for easy mode...no 50 ghc used
            else if (difficulty == 1 && CurrencySelect_Pepper.currency == "euro") { randMoneyIndex = UnityEngine.Random.Range(indexMoney, MoneyNotes.Length - 3); } // minus 3 for easy mode... no 20 used
            else { randMoneyIndex = UnityEngine.Random.Range(indexMoney, MoneyNotes.Length); }

            OfferedMoney = MoneyNotes[randMoneyIndex];
            value = OfferedMoney - AmountToPay;

            // Instantiate objects based on randomly generated values
            Instantiate(OfferedMoneyArray[randMoneyIndex], OfferedMoneyPos.transform.position, Quaternion.identity);
            Instantiate(ShoppingItems[UnityEngine.Random.Range(0, ShoppingItems.Length)], ItemPos.transform.position, Quaternion.identity);

            // Set UI text values
            OfferedAmount.GetComponentInChildren<TextMeshProUGUI>().text = OfferedMoney.ToString("0.00");
            ProductAmount.GetComponentInChildren<TextMeshProUGUI>().text = AmountToPay.ToString("0.00");
            GuideBoxTxt.text = OfferedMoney.ToString("0.00") + System.Environment.NewLine + "-" + AmountToPay.ToString("0.00") + System.Environment.NewLine + "______" + System.Environment.NewLine + "???";
        }
        else if (life == 0) { customerScript.timerIsRunning = false; }
    }

    IEnumerator returnChest()
    {
        // Coroutine to return the chest
        yield return new WaitForSeconds(2.5f);
        PepState = 2;
    }

    public void wrong()
    {
        // Handle wrong answer scenario
        if (customerScript.timeRemaining > 0.25f * customerScript.timeRemaining)
        {
            customerScript.timeRemaining = 0.25f * customerScript.timeRemaining;
        }
        // TODO: Add sound effect for wrong answer
    }

    public void lifeReduce(int state)
    {
        // Reduce life and trigger appropriate actions
        life--;
        if (life == 0) { GameOver(); }
        else if (state == 1 && life > 0) { StartCoroutine(givingMoney()); StartCoroutine(PepperCompliant()); }
        else if (state == 2 && life > 0) { Sounds[4].Play(); StartCoroutine(PepperCompliant()); }
    }

    public void correct()
    {
        // Handle correct answer scenario
        GameScore += (int)(100 * (customerScript.timeRemaining / TotalToleranceTime));
        Scores.text = GameScore.ToString();
        customerScript.satisfied();
        customerScript.cleaerItems();
        Sounds[1].Play();
        RemainingPplsCount();
    }

    public void RemainingPplsCount()
    {
        // Update remaining customers count
        if (life != 0) { customerCount--; }
        customersRemaining.text = customerCount.ToString();
        if (customerCount == 0) { GameOver(); }
    }

    public void SpawnPlayer()
    {
        // Spawn a new customer
        Instantiate(CustomerSpawn, spawnPos.transform.position, Quaternion.identity);
    }

    public void Check()
    {
        // Check the answer
        compare = 0.0f;
        GameObject[] Balls = GameObject.FindGameObjectsWithTag("Ball");
        for (int i = 0; i < Balls.Length; i++)
        {
            float comp = float.Parse(Balls[i].name, CultureInfo.InvariantCulture.NumberFormat);
            compare += comp;
        }

        customerScript = GameObject.FindGameObjectWithTag("Finish").GetComponent<customerScript>();
        if (Mathf.Approximately(value, compare)) { correct(); }
        else if (compare > value) { StartCoroutine(ShowChanges()); customerScript.excited(); Sounds[2].Play(); lifeReduce(1); }
        else if (compare < value) { wrong(); }
    }

    IEnumerator givingMoney()
    {
        // Coroutine for giving money
        yield return new WaitForSeconds(0.35f);
        Sounds[3].Play();
    }

    public void clear()
    {
        // Clear the displayed items
        GameObject[] Destroy = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject kill in Destroy) { GameObject.Destroy(kill); }
        AmassedValue = 0;
        Hint();
    }

    public void cleanText()
    {
        // Clear displayed text
        OfferedAmount.GetComponentInChildren<TextMeshProUGUI>().text = null;
        ProductAmount.GetComponentInChildren<TextMeshProUGUI>().text = null;
        GiveTxt.GetComponentInChildren<TextMeshProUGUI>().text = null;
        GuideBoxTxt.text = null;
    }

    public void Hint()
    {
        // Display a hint
        float print = (float)Math.Round(AmassedValue, 2);
        GiveTxt.GetComponentInChildren<TextMeshProUGUI>().text = print.ToString("0.00");
        GuideBoxTxt.text = OfferedMoney.ToString("0.00") + System.Environment.NewLine + "-" + AmountToPay.ToString("0.00") + System.Environment.NewLine + "______" + System.Environment.NewLine + print.ToString("0.00");
    }

    public void GameOver()
    {
        // Handle game over scenario
        Destroy(GameObject.FindGameObjectWithTag("Player"));
        if (GameScore > Highscore) { PlayerPrefs.SetInt($"PepperGame{difficulty}", GameScore); }
        if (life == 0 && gameover == false)
        {
            Sounds[5].Play();
            gameover = true;
            happy.SetActive(false); unhappy.SetActive(false); fired.SetActive(true);
            PepState = 4; rawPepper.SetActive(true); customerCount = 100;

            GameOverObjs.SetActive(true);
            GameObject[] Destroy = GameObject.FindGameObjectsWithTag("GameController");
            foreach (GameObject kill in Destroy) { GameObject.Destroy(kill); }
            ScoreManager.SetActive(true);
            HighScore_Script.StartHighScoreCheck();
        }
        else if (customerCount == 0 && PepState != 4)
        {
            life = 0; customerScript.timerIsRunning = false; gameover = true;
            GameOverObjs.SetActive(true);
            happy.SetActive(true); unhappy.SetActive(false); fired.SetActive(false);

            PepState = 0;
            Sounds[0].Stop();
            rawPepper2.SetActive(true);
            rawPepper2.transform.position = Camera.main.WorldToScreenPoint(ItemPos.transform.position);
            GameObject Conclusion = GameObject.Find("Conclusion");
            Conclusion.GetComponentInChildren<TextMeshProUGUI>().text = "Well Done";
            ScoreManager.SetActive(true);
            HighScore_Script.StartHighScoreCheck();
        }
    }

    public IEnumerator PepperCompliant()
    {
        // Coroutine for Pepper's compliant behavior
        PepState = 1;
        GuideBoxTxt.text = null;
        happy.SetActive(false); unhappy.SetActive(true); fired.SetActive(false);
        yield return new WaitForSeconds(0.01f);
    }

  




   public IEnumerator ShowChanges()
{
    ShowChange.GetComponentInChildren<TextMeshProUGUI>().text = value.ToString("0.00");

    for (int i = 0; i < 4; i++)
    {
        ShowChange.SetActive(!ShowChange.activeSelf);
        yield return new WaitForSeconds(0.2f);
    }

    // Make sure it's set to false after the loop
    ShowChange.SetActive(false);
}

    // Restart the game
    public void Restart()
    {
        SceneManager.LoadScene("PepperTutorial");
    }
   
    // Quit the game
    public void Quit()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }
}