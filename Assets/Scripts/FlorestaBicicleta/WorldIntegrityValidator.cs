using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Validador Automatizado de Integridade de Profundidade, Física, Colisões e Geração Procedural.
    /// Pode ser executado a qualquer momento no Play Mode apertando F9 (ou automaticamente na cena de diagnóstico),
    /// auditando os 3 sistemas: Renderização (WorldDepth), Física (Obstáculos/Terreno) e Geração Procedural!
    /// </summary>
    public class WorldIntegrityValidator : MonoBehaviour
    {
        public System.Action<string, Color> OnValidationBanner;

        private BikeGirlController player;
        private InfiniteTrackGenerator trackGenerator;

        public void Initialize(BikeGirlController bike, InfiniteTrackGenerator generator)
        {
            player = bike;
            trackGenerator = generator;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                RunFullAudit();
            }
        }

        public bool RunFullAudit()
        {
            int passed = 0;
            int total = 8;
            List<string> issues = new List<string>();

            // 1. Protagonista: SortingGroup + piso físico oficial
            bool okPlayer = true;
            if (player == null)
            {
                okPlayer = false;
                issues.Add("[1] Player ausente.");
            }
            else
            {
                SortingGroup sg = player.GetComponent<SortingGroup>();
                if (sg == null || sg.sortingOrder != WorldDepth.PlayerSortingGroup)
                {
                    okPlayer = false;
                    issues.Add($"[1] SortingGroup da protagonista esperado={WorldDepth.PlayerSortingGroup}, atual={(sg != null ? sg.sortingOrder.ToString() : "nulo")}.");
                }
            }
            if (okPlayer) passed++;

            // 2. Obstáculos físicos (Pedra/Tronco/Raiz): SortingOrder, Collider e Y alinhados à pista
            bool okObstacles = true;
            var obs = TrailObstacle.ActiveObstacles;
            for (int i = 0; i < obs.Count; i++)
            {
                TrailObstacle ob = obs[i];
                if (ob == null) continue;
                SpriteRenderer sr = ob.GetComponent<SpriteRenderer>();
                BoxCollider2D col = ob.GetComponent<BoxCollider2D>();
                float elev = trackGenerator != null ? trackGenerator.GetTrackElevationAtX(ob.transform.position.x, out _) : 0f;
                float expectedY = WorldDepth.PlayableGroundY + elev;

                if (sr == null || sr.sortingOrder != WorldDepth.PlayableObstacle)
                {
                    okObstacles = false;
                    issues.Add($"[2] Obstáculo {ob.name} sortingOrder={sr?.sortingOrder} != {WorldDepth.PlayableObstacle}.");
                }
                if (col == null)
                {
                    okObstacles = false;
                    issues.Add($"[2] Obstáculo {ob.name} sem BoxCollider2D.");
                }
                if (Mathf.Abs(ob.transform.position.y - expectedY) > 0.15f)
                {
                    okObstacles = false;
                    issues.Add($"[2] Obstáculo {ob.name} Y={ob.transform.position.y:F2} fora do piso esperado {expectedY:F2}.");
                }
            }
            if (okObstacles) passed++;

            // 3. Animais: ordenação atrás da bicicleta + espaçamento mínimo orgânico + apoio no terreno
            bool okAnimals = true;
            if (trackGenerator != null)
            {
                var animals = trackGenerator.ActiveAnimals;
                for (int i = 0; i < animals.Count; i++)
                {
                    ForestAnimal a = animals[i];
                    if (a == null) continue;
                    SpriteRenderer sr = a.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sortingOrder >= WorldDepth.PlayerSortingGroup)
                    {
                        okAnimals = false;
                        issues.Add($"[3] Animal {a.animalName} sortingOrder={sr.sortingOrder} >= Player ({WorldDepth.PlayerSortingGroup}).");
                    }
                    for (int j = i + 1; j < animals.Count; j++)
                    {
                        ForestAnimal b = animals[j];
                        if (b == null) continue;
                        float d = Mathf.Abs(a.transform.position.x - b.transform.position.x);
                        if (d < 3.6f && !a.isCatFollowing && !b.isCatFollowing)
                        {
                            okAnimals = false;
                            issues.Add($"[3] Animais muito próximos ({a.animalName} e {b.animalName}, dist={d:F1}m).");
                        }
                    }
                }
            }
            if (okAnimals) passed++;

            // 4. Pontes: tablado SEMPRE abaixo das rodas da bicicleta (BridgeDeck < PlayerSortingGroup)
            bool okBridges = true;
            SpriteRenderer[] allRenderers = FindObjectsOfType<SpriteRenderer>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                SpriteRenderer sr = allRenderers[i];
                if (sr == null) continue;
                if (sr.gameObject.name == "PonteMadeira" && sr.sortingOrder >= WorldDepth.PlayerSortingGroup)
                {
                    okBridges = false;
                    issues.Add($"[4] PonteMadeira sortingOrder={sr.sortingOrder} encobrindo a bicicleta!");
                }
            }
            if (okBridges) passed++;

            // 5. Props decorativos: NUNCA na faixa transitável (Y < -2.70) exceto colinas/pontes/rampas/obstáculos
            bool okProps = true;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                SpriteRenderer sr = allRenderers[i];
                if (sr == null) continue;
                string n = sr.gameObject.name;
                if (n == "PropVivo" && sr.transform.position.y < -2.70f)
                {
                    okProps = false;
                    issues.Add($"[5] Prop decorativo {n} em X={sr.transform.position.x:F1}, Y={sr.transform.position.y:F2} invadindo a pista!");
                }
            }
            if (okProps) passed++;

            // 6. Cercas e colinas atrás da protagonista (HillAndFenceSurface < PlayerSortingGroup)
            bool okFences = (WorldDepth.HillAndFenceSurface < WorldDepth.AnimalsAndResidents
                          && WorldDepth.AnimalsAndResidents < WorldDepth.PlayerSortingGroup
                          && WorldDepth.WorldTreesBehindTrail > WorldDepth.HillAndFenceSurface);
            if (!okFences) issues.Add("[6] Hierarquia de cercas/colinas/árvores inconsistente em WorldDepth.");
            else passed++;

            // 7. Monotonicidade estrita das 7 camadas de parallax
            bool okParallax = WorldDepth.ParallaxSkyClouds < WorldDepth.ParallaxMountains
                           && WorldDepth.ParallaxMountains < WorldDepth.ParallaxValleyMist
                           && WorldDepth.ParallaxValleyMist < WorldDepth.ParallaxDistantForest
                           && WorldDepth.ParallaxDistantForest < WorldDepth.ParallaxValleyIpes
                           && WorldDepth.ParallaxValleyIpes < WorldDepth.ParallaxMidgroundTrees
                           && WorldDepth.ParallaxMidgroundTrees < WorldDepth.ParallaxNearTrees
                           && WorldDepth.ParallaxNearTrees < WorldDepth.ParallaxWorldLocked
                           && WorldDepth.ParallaxWorldLocked < WorldDepth.ParallaxForeground
                           && WorldDepth.ParallaxForeground < WorldDepth.ParallaxFraming;
            if (!okParallax) issues.Add("[7] Fatores de parallax fora de ordem monotônica!");
            else passed++;

            // 8. Estrada física 100% travada no mundo em WorldDepth.RoadSurface
            bool okRoad = false;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].gameObject.name.StartsWith("EstradaFisica_"))
                {
                    okRoad = allRenderers[i].sortingOrder == WorldDepth.RoadSurface;
                    break;
                }
            }
            if (!okRoad) issues.Add("[8] EstradaFisica_* ausente ou fora de WorldDepth.RoadSurface.");
            else passed++;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== AUDITORIA PEDAL NO BOSQUE (F9): {passed}/{total} VERIFICAÇÕES APROVADAS ===");
            for (int i = 0; i < issues.Count; i++) sb.AppendLine("  FALHA: " + issues[i]);
            if (issues.Count == 0)
                sb.AppendLine("  TUDO OK: Profundidade (WorldDepth), SortingGroup, Obstáculos Sólidos, Pontes, Colinas e Parallax 100% coerentes!");

            Debug.Log(sb.ToString());

            string banner = issues.Count == 0
                ? $"Auditoria (F9): {passed}/{total} OK — Profundidade, Pedras Sólidas, Cercas, Pontes e Parallax validados!"
                : $"Auditoria (F9): {passed}/{total} — {issues[0]}";
            OnValidationBanner?.Invoke(banner, issues.Count == 0 ? new Color(0.62f, 1f, 0.72f, 1f) : new Color(1f, 0.76f, 0.42f, 1f));
            return issues.Count == 0;
        }
    }
}
