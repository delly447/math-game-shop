using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class MoneyScript_Pepper : MonoBehaviour
{
    public PepperGameManager PepperGameManager;
    public GameObject SpawnMoney;
    private Vector3 _dragOffset;
    private Camera _cam;
    private bool follow = true;
    private Vector3 root;
    private bool ItsOver = false;
    private bool AddAmount = true;
    private bool forSpawn = true;

    [SerializeField] private float _speed = 800;

    private void Start()
    {
        //GetComponent<SpriteRenderer>().sortingOrder = 10;
        PepperGameManager = GameObject.Find("PepperGameManager").GetComponent<PepperGameManager>();
        root = transform.position;
        gameObject.name = gameObject.name.Replace("(Clone)", "").Trim();
        gameObject.tag = "fruit";
    }


    void Awake()
    {
        _cam = Camera.main;
    }

    private void OnMouseUp()
    {
        if (gameObject.tag == "fruit")
        {
            if (transform.position != root && follow == true) { transform.position = root; }
        }
        else
        {
            //Instantiate(SpawnMoney, root, Quaternion.identity);
        }

        if (AddAmount == true && gameObject.tag == "Ball")
        {
            PepperGameManager.AmassedValue += float.Parse(gameObject.name); PepperGameManager.Hint();
            AddAmount = false; 
        }

        if (AddAmount == false && gameObject.tag == "fruit")
        {
            PepperGameManager.AmassedValue -= float.Parse(gameObject.name); PepperGameManager.Hint();
            AddAmount = true;
        }

        /*float compare=0.0f;
        GameObject[] Balls = GameObject.FindGameObjectsWithTag("Ball");
        for (int i = 0; i < Balls.Length; i++)
        {

            float comp = float.Parse(Balls[i].name, CultureInfo.InvariantCulture.NumberFormat);
            compare += comp;
        }
        print(compare);*/
    }
    void OnMouseDown()
    {
        if (ItsOver == false)
        {
            follow = true;
            _dragOffset = transform.position - GetMousePos();
            GetComponent<SpriteRenderer>().sortingOrder = PepperGameManager.LayerOrder + 1;
            PepperGameManager.LayerOrder++;
        }
    }

    void OnMouseDrag()
    {
        if (forSpawn == true) { Instantiate(SpawnMoney, root, Quaternion.identity); forSpawn = false; }
        if (follow == true) { transform.position = Vector3.MoveTowards(transform.position, GetMousePos() + _dragOffset, _speed * Time.deltaTime); }
    }

    Vector3 GetMousePos()
    {
        var mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        return mousePos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "GiveBox")
        {
            // print("entered"); follow = false; ItsOver = true;
            gameObject.tag = "Ball";
        }
        /* else
         {
             follow = false; transform.position = root;
         }*/

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "GiveBox")
        {
            gameObject.tag = "fruit";
        }
    }


}
