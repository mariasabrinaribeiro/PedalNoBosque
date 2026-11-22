using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Projétil de alimento arremessado em arco pela menina na cena lateral até o animal alvo.
    /// </summary>
    public class SideFoodProjectile : MonoBehaviour
    {
        private FoodType foodType;
        private Vector3 startPos;
        private Vector3 targetPos;
        private ForestAnimal targetAnimal;
        private VillageResident targetResident;
        private float duration = 0.52f;
        private float elapsed = 0f;
        private System.Action<ForestAnimal, FoodType> onArrive;
        private System.Action<VillageResident, FoodType> onArriveResident;

        public void Launch(FoodType type, Vector3 from, Vector3 to, ForestAnimal animal, Sprite sp, System.Action<ForestAnimal, FoodType> arriveCb)
        {
            foodType = type;
            startPos = from;
            targetPos = to;
            targetAnimal = animal;
            targetResident = null;
            onArrive = arriveCb;
            onArriveResident = null;

            transform.position = from;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 18;
        }

        public void LaunchToResident(FoodType type, Vector3 from, Vector3 to, VillageResident resident, Sprite sp, System.Action<VillageResident, FoodType> arriveCb)
        {
            foodType = type;
            startPos = from;
            targetPos = to;
            targetAnimal = null;
            targetResident = resident;
            onArrive = null;
            onArriveResident = arriveCb;

            transform.position = from;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 18;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 2.1f; // Arco bonito no ar!
            transform.position = pos;
            transform.Rotate(0f, 0f, -380f * Time.deltaTime);

            if (t >= 1f)
            {
                if (targetAnimal != null)
                {
                    onArrive?.Invoke(targetAnimal, foodType);
                }
                else if (targetResident != null)
                {
                    onArriveResident?.Invoke(targetResident, foodType);
                }
                Destroy(gameObject);
            }
        }
    }
}
