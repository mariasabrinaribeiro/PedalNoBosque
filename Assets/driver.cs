using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class driverpro : MonoBehaviour
{
    [SerializeField]float currentSpeed = 5f;
    [SerializeField] float steerSpeed = 200f;
    [SerializeField] float boostSpeed = 10f;
    [SerializeField] float regularSpeed = 5f;

    [SerializeField] TMP_Text boostText;
   void Start()
     {
         boostText.gameObject.SetActive(false);
    } 

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boost item"))
        {

            currentSpeed = boostSpeed; Destroy(collision.gameObject);
            boostText.gameObject.SetActive(true);
            //Destroy(collision.gameObject);       
        }
        
        {
          //void Update()   
        }
          
    }
    void OnCollisionEnter2D (Collision2D collision)
    {
        currentSpeed = regularSpeed;
    }
    // Start is called before the first frame update
    
    
    // renato
    //bool correndo = false;
    int correndo = 0;

    // Mobile inputs set by UI touch buttons
    private float mobileMoveInput = 0f;
    private float mobileSteerInput = 0f;

    public void SetMobileMove(float val)
    {
        mobileMoveInput = val;
    }

    public void SetMobileSteer(float val)
    {
        mobileSteerInput = val;
    }

    // Update is called once per frame
    void Update()
    {
        float steer = 0f;
        float move = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
            {
                move = 1f;
            }
            else if (Keyboard.current.sKey.isPressed)
            {
                move = -1f;
            }

            if (Keyboard.current.aKey.isPressed)
            {
                steer = 1f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                steer = -1f;
            }
        }

        // Combine inputs and clamp them
        move = Mathf.Clamp(move + mobileMoveInput, -1f, 1f);
        steer = Mathf.Clamp(steer + mobileSteerInput, -1f, 1f);

        float moveAmount = move * currentSpeed * Time.deltaTime;
        float steerAmount = steer * steerSpeed * Time.deltaTime;

        // Translate moves the object, rotate rotates it
        transform.Translate(0, moveAmount, 0);
        transform.Rotate(0, 0, steerAmount);
    }
}
