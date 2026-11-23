using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Padrão arquitetural único de Profundidade, Ordenação Visual (SortingOrder),
    /// Planos de Terreno (Y) e Fatores de Parallax para TODO o Pedal no Bosque.
    ///
    /// Elimina números mágicos espalhados entre scripts e garante coerência estrutural entre:
    /// 1. Renderização (Fundo Distante -> Intermediário -> Terreno -> Objetos Atrás -> Faixa Jogável -> Protagonista -> Primeiro Plano)
    /// 2. Física e Colisões (mesmo piso Y = PlayableGroundY para bicicleta, obstáculos, rampas, pontes e colinas)
    /// 3. Geração Procedural (zonas de exclusão e limites de margem consistentes em todos os 6 biomas).
    /// </summary>
    public static class WorldDepth
    {
        // ─── Planos Verticais Oficiais (Coordenadas de Mundo) ────────
        /// <summary>Piso físico da bicicleta, pneus, obstáculos físicos, rampas, tablado da ponte e base das colinas.</summary>
        public const float PlayableGroundY = -3.45f;

        /// <summary>Margem superior da trilha (atrás da faixa transitável): animais, moradores, bancos, barracas, pedras decorativas e placas.</summary>
        public const float UpperVergeY = -2.42f;

        /// <summary>Base de árvores fixas no mundo (carvalhos e árvores de segmento) atrás da margem superior.</summary>
        public const float WorldTreeY = -2.28f;

        /// <summary>Borda inferior mais próxima da câmera (vegetação ornamental de primeiro plano, abaixo da pista).</summary>
        public const float ForegroundEdgeY = -4.34f;

        // ─── Fatores de Parallax Oficiais (Referencial Único: camX) ──
        public const float ParallaxSkyClouds      = 0.06f; // Céu / Nuvens (6%)
        public const float ParallaxMountains      = 0.12f; // Montanhas distantes (12%)
        public const float ParallaxValleyMist     = 0.18f; // Névoa atmosférica do vale (18%)
        public const float ParallaxDistantForest  = 0.30f; // Floresta distante (30%)
        public const float ParallaxValleyIpes     = 0.42f; // Ipês floridos no vale (42%)
        public const float ParallaxMidgroundTrees = 0.55f; // Árvores intermediárias (55%)
        public const float ParallaxNearTrees      = 0.85f; // Árvores próximas (85%)
        public const float ParallaxWorldLocked    = 1.00f; // Estrada, cercas, pontes, colinas, obstáculos e animais (100% fixos!)
        public const float ParallaxForeground     = 1.18f; // Vegetação de primeiro plano na borda inferior (118%)
        public const float ParallaxFraming        = 1.22f; // Moldura superior de copas/cipós e samambaias tropicais (122%)

        // ─── Convenção Padronizada de SortingOrder ───────────────────
        // 1. FUNDO DISTANTE (-20..-12)
        public const int BgMountains           = -20;
        public const int SkyStars              = -18;
        public const int SkyMoon               = -17;
        public const int SkyClouds             = -16;
        public const int SkySunBloom           = -15;
        public const int WeatherRainbow        = -15;
        public const int ValleyMist            = -14;
        public const int DistantForest         = -13;
        public const int SkyBirds              = -12;

        // 2. FUNDO INTERMEDIÁRIO (-10..-7)
        public const int MidgroundTrees        = -10;
        public const int ValleyFloweringTrees  = -9;

        // 3. TERRENO E ELEMENTOS DE PISTA (-6..-1)
        public const int RoadSurface           = -6;
        public const int TrailCanopyShadows    = -5;
        public const int NearParallaxShadow    = -5;
        public const int NearParallaxTrees     = -4;
        public const int HillAndFenceSurface   = -2; // Encosta da colina + cerca rústica SEMPRE atrás da bicicleta e animais
        public const int WorldBushesBehind     = -1;

        // 4. OBJETOS FIXOS ATRÁS DA PROTAGONISTA (0..7)
        public const int WorldTreesBehindTrail = 0;  // Carvalhos de segmento/colina (acima da encosta -2, atrás de animais/bike)
        public const int RiverWater            = 1;
        public const int RiverShimmer          = 2;
        public const int PropsBehindTrail      = 2;  // Pedras decorativas, troncos musgosos, tocas, placas, flores, chalés
        public const int BridgeDeck            = 3;  // Tablado da ponte (abaixo das rodas da bicicleta!)
        public const int BenchAndFurniture     = 3;
        public const int DistanceSign          = 3;
        public const int JumpRamp              = 4;
        public const int DigSite               = 4;
        public const int InteractiveStations   = 5;  // Barraca da Cozinha, Coluna de Água, Eventos de Aventura, Peixes
        public const int AnimalsAndResidents   = 6;
        public const int ResidentProp          = 7;
        public const int PicnicBlanket         = 7;

        // 5. FAIXA JOGÁVEL: BICICLETA ESTACIONADA, OBSTÁCULOS FÍSICOS E PROTAGONISTA (8..11)
        public const int ParkedBikeShadow      = 8;
        public const int ParkedBike            = 8;
        public const int PlayableObstacle      = 9;  // Pedras/Troncos/Raízes FÍSICOS com colisão na pista
        public const int PlayerSortingGroup    = 10; // SortingGroup único da menina + bike + cesta + cachorrinho!

        // Ordens LOCAIS dentro do SortingGroup da protagonista (nunca intercalam com sprites externos!)
        public const int PlayerLocalShadow     = 0;
        public const int PlayerLocalBikeOrGirl = 1;
        public const int PlayerLocalBasket     = 2;
        public const int PlayerLocalPuppy      = 3;
        public const int PlayerLocalHeadlight  = 4;

        // 6. ÍCONES DE MUNDO, COLECIONÁVEIS E EFEITOS (12..16)
        public const int AnimalOrderBubble     = 12;
        public const int EventSparkle          = 12;
        public const int TrailCollectible      = 13;
        public const int WeatherMist           = 13;
        public const int AerialCollectible     = 14;
        public const int VolumetricGodRays     = 14;
        public const int PuppyMoodBubble       = 15;
        public const int FirefliesAndSunMotes  = 15;
        public const int WeatherRain           = 15;
        public const int FeedbackParticles     = 16;
        public const int FoodProjectile        = 16;

        // 7. PRIMEIRO PLANO ORNAMENTAL (17..18)
        public const int ForegroundBase        = 17; // Vegetação na borda inferior (118%)
        public const int ForegroundFraming     = 18; // Copa superior, cipós e samambaias de moldura (122%)
    }
}
