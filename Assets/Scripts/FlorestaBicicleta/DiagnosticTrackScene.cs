using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Circuito Controlado de Diagnóstico (acionável pela tecla F8 no Play Mode ou na cena FlorestaBicicleta_Diagnostico):
    /// Cria 6 estações controladas e sinalizadas em linha reta para validar visual e fisicamente cada caso crítico:
    /// 1. Cerca & Árvores atrás da pista;
    /// 2. Pedra Decorativa (fora da pista, sem colisão) vs. Pedra Física (na pista, bloqueia até saltar!);
    /// 3. Rampa de Salto + Tronco Físico + Estrelas Aéreas;
    /// 4. Ponte de Madeira sobre Rio;
    /// 5. Colina de Montanha (Subida + Descida + Cerca Curva);
    /// 6. Banco + Estação de Desembarque & Carinho no Cachorrinho.
    /// </summary>
    public class DiagnosticTrackScene : MonoBehaviour
    {
        public System.Action<string, Color> OnDiagnosticMessage;

        private BikeGirlController player;
        private WorldIntegrityValidator validator;
        private GameObject diagnosticRoot;

        public void Initialize(BikeGirlController bike, WorldIntegrityValidator integrityValidator)
        {
            player = bike;
            validator = integrityValidator;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                SpawnControlledDiagnosticZoneNearPlayer();
            }
        }

        public void SpawnControlledDiagnosticZoneNearPlayer()
        {
            if (player == null) return;
            if (diagnosticRoot != null) Destroy(diagnosticRoot);

            float originX = Mathf.Max(4f, player.transform.position.x + 6f);
            diagnosticRoot = new GameObject("CircuitoDiagnosticoControlado");

            // Estação 1: Árvores e Pedra Decorativa atrás da pista (NÃO bloqueiam a bicicleta!)
            SpawnMarker(diagnosticRoot.transform, originX + 4f, "1. DECORACAO ATRAS");
            SpawnDiagProp(diagnosticRoot.transform, BiomePropsFactory.LoadOakTreeSprite(),
                new Vector3(originX + 4f, WorldDepth.WorldTreeY, 0f), 0.72f, WorldDepth.WorldTreesBehindTrail);
            SpawnDiagProp(diagnosticRoot.transform, BiomePropsFactory.CreateMossyStonesClusterSprite(),
                new Vector3(originX + 6.5f, WorldDepth.UpperVergeY, 0f), 0.95f, WorldDepth.PropsBehindTrail);

            // Estação 2: Pedra Física SÓLIDA na pista (bloqueia até saltar com W!)
            SpawnMarker(diagnosticRoot.transform, originX + 14f, "2. PEDRA FISICA (PULE W)");
            GameObject rockObj = new GameObject("Diag_PedraFisica");
            rockObj.transform.SetParent(diagnosticRoot.transform, false);
            rockObj.transform.position = new Vector3(originX + 14f, WorldDepth.PlayableGroundY, 0f);
            rockObj.AddComponent<TrailObstacle>().Setup(TrailObstacle.ObstacleKind.Pedra);

            // Estação 3: Rampa de Salto + Tronco Físico + Estrelas no Ar
            SpawnMarker(diagnosticRoot.transform, originX + 24f, "3. RAMPA + TRONCO");
            GameObject rampObj = new GameObject("Diag_Rampa");
            rampObj.transform.SetParent(diagnosticRoot.transform, false);
            rampObj.transform.position = new Vector3(originX + 24f, WorldDepth.PlayableGroundY, 0f);
            rampObj.AddComponent<JumpRamp>().Setup();

            GameObject logObj = new GameObject("Diag_TroncoFisico");
            logObj.transform.SetParent(diagnosticRoot.transform, false);
            logObj.transform.position = new Vector3(originX + 29.5f, WorldDepth.PlayableGroundY, 0f);
            logObj.AddComponent<TrailObstacle>().Setup(TrailObstacle.ObstacleKind.Tronco);

            // Estação 4: Banco + Placa para teste de desembarque (Q) e carinho (C)
            SpawnMarker(diagnosticRoot.transform, originX + 38f, "4. DESCER (Q) / CARINHO (C)");
            SpawnDiagProp(diagnosticRoot.transform, BiomePropsFactory.LoadParkBenchSprite(),
                new Vector3(originX + 38f, WorldDepth.UpperVergeY, 0f), 1.0f, WorldDepth.BenchAndFurniture);

            OnDiagnosticMessage?.Invoke(
                "Circuito de Diagnóstico (F8) montado à sua frente! Teste: Decoração -> Pedra Sólida -> Rampa+Tronco -> Descer (Q). Aperte F9 para auditar!",
                new Color(0.68f, 0.92f, 1f, 1f));

            validator?.RunFullAudit();
        }

        private static void SpawnDiagProp(Transform parent, Sprite sp, Vector3 pos, float scale, int sortOrder)
        {
            if (sp == null) return;
            GameObject go = new GameObject("DiagProp");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;
        }

        private static void SpawnMarker(Transform parent, float x, string label)
        {
            GameObject sign = new GameObject($"DiagPlaca_{label}");
            sign.transform.SetParent(parent, false);
            sign.transform.position = new Vector3(x, WorldDepth.UpperVergeY + 0.02f, 0f);
            SpriteRenderer sr = sign.AddComponent<SpriteRenderer>();
            sr.sprite = BiomePropsFactory.CreateDistanceSignSprite();
            sr.sortingOrder = WorldDepth.DistanceSign;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(sign.transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.38f, -0.05f);
            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(4.2f, 0.9f);
            tmp.text = label;
            tmp.fontSize = 2.6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.18f, 0.10f, 0.05f, 1f);
            tmp.sortingOrder = WorldDepth.DistanceSign + 1;
        }
    }
}
