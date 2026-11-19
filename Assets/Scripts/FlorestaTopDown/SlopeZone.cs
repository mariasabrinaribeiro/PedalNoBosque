using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Zona de Subida ou Descida na pista do parque.
    /// Subida deixa a pedalada mais pesada; Descida embala e acelera a bicicleta.
    /// </summary>
    public class SlopeZone : MonoBehaviour
    {
        public enum SlopeType { Subida, Descida }
        public SlopeType slopeType = SlopeType.Subida;

        public float SpeedMultiplier => slopeType == SlopeType.Subida ? 0.55f : 1.55f;
        public string DisplayLabel => slopeType == SlopeType.Subida ? "SUBIDA! Pedalando forte" : "DESCIDA! Embalou!";

        public void Setup(SlopeType type, Vector2 size, float angleDeg)
        {
            slopeType = type;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ParkSpriteFactory.CreateSlopeChevronSprite(type == SlopeType.Subida);
            sr.sortingOrder = 1;
            transform.localScale = new Vector3(size.x / 3f, size.y / 3f, 1f);

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 3f);
        }
    }
}
