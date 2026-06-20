using UnityEngine;

public class AmbientGlitterManager : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        // Add ParticleSystem component dynamically
        ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop();

        // 1. Configure Main Module
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = 5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        // Set start color to a gradient of gold, pink, and white
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.843f, 0f), 0.0f), // Gold
                new GradientColorKey(new Color(1f, 0.6f, 0.8f), 0.5f),  // Candy Pink
                new GradientColorKey(Color.white, 1.0f)                 // White
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.4f, 0.0f),
                new GradientAlphaKey(0.3f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // 2. Configure Emission Module
        var emission = ps.emission;
        emission.rateOverTime = 25f; // gentle amount of sparkles

        // 3. Configure Shape Module
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // Large box area centered around the camera view (orthographic size is usually ~5, width ~9)
        shape.scale = new Vector3(18f, 12f, 1f);
        shape.position = new Vector3(0f, 0f, 10f); // position offset in front of camera lens

        // 4. Configure Velocity over Lifetime Module
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        // Drift gently to the left/downwards (representing light wind)
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, -0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // 5. Configure Color over Lifetime Module (Fade in / out)
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient fadeGrad = new Gradient();
        fadeGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.4f, 0.1f),
                new GradientAlphaKey(0.4f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorModule.color = fadeGrad;

        // 6. Configure Texture Sheet Animation or Particle material
        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 4; // render above trees but below UI
            // Create a small blank unlit particle material to ensure compatibility
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                renderer.material = new Material(defaultShader);
            }
        }

        // Adjust ParticleSystem offset to follow camera plane
        var trigger = ps.trigger;
        trigger.enabled = false;
        
        ps.Play();
    }
}
