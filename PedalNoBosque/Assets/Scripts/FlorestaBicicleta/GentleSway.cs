using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Movimento delicado de vento para flores, juncos, galhos e samambaias.
    /// Usa a posição X no mundo como fase para criar ondas suaves de brisa sem alocar memória.
    /// </summary>
    public class GentleSway : MonoBehaviour
    {
        public float maxAngleDeg = 4.5f;
        public float speed = 1.8f;
        private float phaseOffset;

        private void Awake()
        {
            phaseOffset = transform.position.x * 0.35f + Random.Range(0f, 1.5f);
        }

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * speed + phaseOffset) * maxAngleDeg;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
