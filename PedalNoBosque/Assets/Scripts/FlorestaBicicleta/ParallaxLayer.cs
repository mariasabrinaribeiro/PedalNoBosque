using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Aplica efeito Parallax horizontal suave em camadas do cenário (montanhas, árvores, nuvens).
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public Transform cameraTransform;
        [Range(0f, 1f)]
        public float parallaxFactor = 0.5f;
        public float autoDriftSpeed = 0f;

        private Vector3 startPos;
        private float cameraStartX;
        private float driftOffset = 0f;

        public void Initialize(Transform cam, float factor, float drift = 0f)
        {
            cameraTransform = cam;
            parallaxFactor = factor;
            autoDriftSpeed = drift;
            startPos = transform.position;
            cameraStartX = cam != null ? cam.position.x : 0f;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            driftOffset += autoDriftSpeed * Time.deltaTime;
            float camDeltaX = cameraTransform.position.x - cameraStartX;
            transform.position = new Vector3(
                startPos.x + camDeltaX * parallaxFactor + driftOffset,
                startPos.y,
                startPos.z
            );
        }
    }
}
