using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class customerScript : MonoBehaviour
{
    public float timeRemaining = 18f;
    public bool timerIsRunning = false;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI Score;
    public GameObject eyebrow, mouth;
    public GameObject[] MeduimExpression;
    public GameObject[] AngryExpression;
    public GameObject happyMouth;
    public GameObject[] glasses;
    public GameObject[] HeadWear;
    private Color[] color = new Color[6] {Color.blue,Color.green,Color.yellow, Color.magenta, Color.cyan, Color.gray};
    private GameObject targetpos;
    private float moveSpeed = 4.7f; 
    private float IdleSpeed = 1.0f;
    private int Num_Facial_objs=2;
    private bool move = true;    
    private int state = 0;
    private float ToleranceTime;
    private float UpperLimit = 1.64f;
    private float LowerLimit = 1.52f;
    private bool GoUp = true;
    public PepperGameManager PepperGameManager;
    private Slider slider;
    private GameObject SliderFill;

    void Start()
    {
        PepperGameManager = GameObject.Find("PepperGameManager").GetComponent<PepperGameManager>();
        timeRemaining = PepperGameManager.TotalToleranceTime;
        GetComponent<SpriteRenderer>().color = color[UnityEngine.Random.Range(0, color.Length)];
        //spawnPos = transform.position;
        ToleranceTime = timeRemaining;
        slider = FindObjectOfType<Slider>();
        SliderFill = GameObject.Find("Fill");
        targetpos = GameObject.Find("targetpos");
        int rand=UnityEngine.Random.Range(0, 101);
        if (rand > 52 && rand < 85) { glasses[0].SetActive(true); }
        if (rand > 91) { glasses[1].SetActive(true); }
        if (rand % 3 == 0) { HeadWear[0].SetActive(true); }
        else if (rand < 20) { HeadWear[1].SetActive(true);}
        else if (rand > 20 && rand < 30) { HeadWear[1].SetActive(true); }
        else if (rand > 30 && rand < 50) { HeadWear[2].SetActive(true); }
        else if (rand > 50 && rand < 60) { HeadWear[3].SetActive(true); }
        else if (rand > 60 && rand < 80) { HeadWear[4].SetActive(true); }
        else if (rand > 80 && rand < 90) { HeadWear[5].SetActive(true); }
    }

    // Update is called once per frame
    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                slider.value = timeRemaining / ToleranceTime;
            }
            else
            {
                move = true; PepperGameManager.RemainingPplsCount(); PepperGameManager.lifeReduce(2); StartCoroutine(PepperGameManager.ShowChanges());
                cleaerItems(); 
                PepperGameManager.SpawnPlayer();
                //Instantiate(CustomerSpawn, spawnPos, Quaternion.identity);
                timeRemaining = 0;
                timerIsRunning = false;
            }
        }
        if (timeRemaining < 0.6f*ToleranceTime &&state==0)
        {
            SliderFill.GetComponent<Image>().color = Color.yellow;
            medium(); state++;
        }
        if (timeRemaining < 0.25f*ToleranceTime && state == 1)
        {
            SliderFill.GetComponent<Image>().color = Color.red;
            angry(); state++;
        }


        if (move == true)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetpos.transform.position, moveSpeed * Time.deltaTime);
        }



        if (transform.position.y > UpperLimit){ GoUp = false;}

        if (GoUp == true)
        { 
            transform.position = Vector3.MoveTowards(transform.position, new Vector2(transform.position.x, 5.0f), IdleSpeed * Time.deltaTime); 
        }

        if (transform.position.y < LowerLimit) { GoUp = true; }

        if (GoUp == false)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector2(transform.position.x, -5.0f), IdleSpeed * Time.deltaTime);
        }
    }

    public void medium()
    {
        eyebrow.SetActive(false); mouth.SetActive(false);
        for (int i = 0; i < Num_Facial_objs; i++)
        {
            MeduimExpression[i].SetActive(true);
        }
    }

    public void angry()
    {        
        for (int i = 0; i < Num_Facial_objs; i++)
        {
            MeduimExpression[i].SetActive(false);
            AngryExpression[i].SetActive(true);
            IdleSpeed = 1.0f;
        }
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "stoppos")
        { move = false; timerIsRunning = true; IdleSpeed = 0f; gameObject.tag = "Finish";
            SliderFill.GetComponent<Image>().color = Color.green;
            PepperGameManager.Reset(); }
        if (collision.gameObject.name == "targetpos")
        { Destroy(gameObject, 0.2f); timerIsRunning = false; }
    }
    public void satisfied()
    {
        IdleSpeed = 1.0f;
        timerIsRunning = false;
        for (int i = 0; i < Num_Facial_objs; i++)
        {
            MeduimExpression[i].SetActive(false);
            AngryExpression[i].SetActive(false);
        }
        eyebrow.SetActive(true); happyMouth.SetActive(true); mouth.SetActive(false);
        cleaerItems();
        move = true; PepperGameManager.SpawnPlayer(); //Instantiate(CustomerSpawn, spawnPos, Quaternion.identity);
    }
    public void excited()
    {
        IdleSpeed = 1.0f;
        timerIsRunning = false;
        for (int i = 0; i < Num_Facial_objs; i++)
        {
            MeduimExpression[i].SetActive(false);
            AngryExpression[i].SetActive(false);
        }
        eyebrow.SetActive(true); moveSpeed = 15.0f;
        happyMouth.SetActive(true); mouth.SetActive(false);
        PepperGameManager.RemainingPplsCount();
        cleaerItems();
        move = true; PepperGameManager.SpawnPlayer(); //Instantiate(CustomerSpawn, spawnPos, Quaternion.identity);
    }
    public void cleaerItems()
    {
        gameObject.tag = "Respawn";
        GameObject[] DestroyUs = GameObject.FindGameObjectsWithTag("DestroyUs");
        for (int i = 0; i < DestroyUs.Length; i++)
        {
            Destroy(DestroyUs[i].gameObject);
        }
        GameObject[] DestroyUs2 = GameObject.FindGameObjectsWithTag("Timer"); print($"length is {DestroyUs2.Length}");
        for (int i = 0; i < DestroyUs2.Length; i++)
        {
            Destroy(DestroyUs2[i].gameObject);
        }
        PepperGameManager.clear();//clear money offered
        PepperGameManager.cleanText();                          //with text as well
    }
}
