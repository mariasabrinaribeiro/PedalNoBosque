using UnityEngine;

public class Delivery : MonoBehaviour
{
    bool hasPackage;
    [SerializeField] float delay = 1f;

    void OnTriggerEnter2D(Collider2D collision)
    {

        // if (the tag is package)
        //then ("print picked up package")
        if (collision.CompareTag("package") && !hasPackage)

        {

            Debug.Log("Pickep up package");
            hasPackage = true;
            GetComponent<ParticleSystem>().Play();
            Destroy(collision.gameObject, delay);
        }
        if (collision.CompareTag("Customer") && hasPackage)
            {
            Debug.Log("Deliveredpackage");
            hasPackage = false;
            GetComponent<ParticleSystem>().Stop();
            Destroy(collision.gameObject);
        }

        
    }
   
}
