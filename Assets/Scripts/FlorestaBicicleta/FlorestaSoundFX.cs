using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Sistema central de áudio do PEDAL NO BOSQUE:
    /// - Reproduz as 9 trilhas musicais produzidas (Tela Inicial, 6 Biomas e variações/Noite)
    ///   com crossfade suave de potência constante em dois decks (musicDeckA / musicDeckB),
    ///   ducking contextual (Pausa, Cozinha, Missões) e loop sem emendas.
    /// - Reproduz o sino real da bicicleta (Vintage Bicycle Bell) com controle anti-sobreposição.
    /// - Mantém todos os efeitos sonoros sintetizados de animais, natureza e eventos como cobertura
    ///   imediata e fallback seguro caso algum arquivo não esteja presente.
    /// </summary>
    public class FlorestaSoundFX : MonoBehaviour
    {
        public static FlorestaSoundFX Instance { get; private set; }

        private AudioSource source;
        private AudioSource bellSource;
        private AudioSource musicDeckA;
        private AudioSource musicDeckB;

        public float MusicVolume { get; private set; } = 0.80f;
        public float SfxVolume { get; private set; } = 0.85f;
        public int LoadedRealClipsCount { get; private set; } = 0;
        public string CurrentTrackName { get; private set; } = "Fallback";

        // Ganho mestre calibrado para faixas MP3 masterizadas (Suno) vs. síntese
        private const float RealMusicMasterGain = 0.42f;
        private const float SynthMusicMasterGain = 0.16f;
        private const float SfxMasterGain = 0.50f;
        private const float RealBellMasterGain = 0.62f;

        // Estado contextual para seleção inteligente de trilha
        private GameFlowState currentFlowState = GameFlowState.TitleScreen;
        private BiomeType currentBiome = BiomeType.CampoFlorido;
        private DayPeriod currentPeriod = DayPeriod.Manha;
        private bool kitchenFocused = false;
        private int titleVisitCount = 0;
        private int flowersVisitCount = 0;
        private int villageVisitCount = 0;

        // Estado do motor de crossfade (usa Time.unscaledDeltaTime para funcionar mesmo em Pausa)
        private bool activeIsA = true;
        private AudioClip activeMusicClip;
        private AudioClip outgoingMusicClip;
        private float crossfadeElapsed = 1f;
        private float crossfadeDuration = 1f;
        private bool isCrossfading = false;
        private float currentDuck = 1f;
        private bool musicMutedPaused = false;
        private float lastBellUnscaledTime = -10f;
        private float lastTailTriggerTime = -10f;
        private bool usingRealBell = false;

        // 9 Trilhas Musicais reais em Resources/Floresta/Audio/Music/
        private AudioClip bgmMenuPrimary;    // The Forest Whimsy.mp3
        private AudioClip bgmMenuAlt;        // The Forest Whimsy (1).mp3
        private AudioClip bgmFlowers;        // vale das flores.mp3
        private AudioClip bgmWind;           // montanha dos ventos-gostei.mp3
        private AudioClip bgmWaters;         // Valle de Agua.mp3
        private AudioClip bgmSecrets;        // Forest Adventure.mp3
        private AudioClip bgmVillage;        // Woodland Market.mp3
        private AudioClip bgmVillageAlt;     // Woodland Market (1).mp3
        private AudioClip bgmMoon;           // Moonlit Forest.mp3
        private AudioClip bgmSynthFallback;

        // Sons dos animais (sintetizados)
        private AudioClip clipDogBark;
        private AudioClip clipThirstPant;
        private AudioClip clipCatMeow;
        private AudioClip clipWolfHowl;
        private AudioClip clipSquirrelChirp;
        private AudioClip clipRabbitSniff;
        private AudioClip clipCoatiChitter;
        private AudioClip clipFoxYip;
        private AudioClip clipDeerCall;
        private AudioClip clipHedgehogSqueak;
        private AudioClip clipRaccoonTrill;
        private AudioClip clipBearCub;
        private AudioClip clipBirdTweet;
        private AudioClip clipBeaverSlap;
        private AudioClip clipFrogRibbit;
        private AudioClip clipOwlHoot;

        // Efeitos de aventura e interface
        private AudioClip clipBell;
        private AudioClip clipThrow;
        private AudioClip clipHappy;
        private AudioClip clipWrong;
        private AudioClip clipCry;
        private AudioClip clipWater;
        private AudioClip clipDig;
        private AudioClip clipDiamond;
        private AudioClip clipJump;
        private AudioClip clipBump;
        private AudioClip clipStar;
        private AudioClip clipMusicianTune;
        private AudioClip clipVillagerHappy;

        // Assinaturas sutis de entrada nas 6 regiões
        private AudioClip clipBiomeFlowers;
        private AudioClip clipBiomeWind;
        private AudioClip clipBiomeWaters;
        private AudioClip clipBiomeSecrets;
        private AudioClip clipBiomeVillage;
        private AudioClip clipBiomeMoon;

        // Instrumentos naturais da Música da Floresta
        private readonly AudioClip[] natureNotes = new AudioClip[4];

        private void Awake()
        {
            Instance = this;

            source = CreateAudioSource(loop: false, priority: 110);
            source.volume = SfxMasterGain * SfxVolume;

            bellSource = CreateAudioSource(loop: false, priority: 64);
            bellSource.volume = RealBellMasterGain * SfxVolume;

            musicDeckA = CreateAudioSource(loop: true, priority: 24);
            musicDeckB = CreateAudioSource(loop: true, priority: 25);

            LoadRealAudioAssets();
            BuildSynthesizedClips();

            // Inicia já com o tema adequado à Tela Inicial (ou fallback se não houver arquivos)
            SelectAndCrossfadeTrack(immediate: true, fadeSeconds: 0.6f);
            Debug.Log(GetAudioStatusReport());
        }

        private AudioSource CreateAudioSource(bool loop, int priority)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f; // 2D puro para áudio limpo e consistente
            s.priority = priority;
            s.volume = 0f;
            return s;
        }

        private void LoadRealAudioAssets()
        {
            LoadedRealClipsCount = 0;

            bgmMenuPrimary = LoadClipTracked("Floresta/Audio/Music/bgm_menu_forest_whimsy");
            bgmMenuAlt     = LoadClipTracked("Floresta/Audio/Music/bgm_menu_forest_whimsy_alt");
            bgmFlowers     = LoadClipTracked("Floresta/Audio/Music/bgm_biome_vale_das_flores");
            bgmWind        = LoadClipTracked("Floresta/Audio/Music/bgm_biome_montanhas_do_vento");
            bgmWaters      = LoadClipTracked("Floresta/Audio/Music/bgm_biome_vale_das_aguas");
            bgmSecrets     = LoadClipTracked("Floresta/Audio/Music/bgm_biome_floresta_dos_segredos");
            bgmVillage     = LoadClipTracked("Floresta/Audio/Music/bgm_biome_vila_dos_viajantes");
            bgmVillageAlt  = LoadClipTracked("Floresta/Audio/Music/bgm_biome_vila_dos_viajantes_alt");
            bgmMoon        = LoadClipTracked("Floresta/Audio/Music/bgm_biome_bosque_da_lua");

            AudioClip realBell = LoadClipTracked("Floresta/Audio/SFX/sfx_bike_bell_vintage");
            if (realBell != null)
            {
                clipBell = realBell;
                usingRealBell = true;
            }
        }

        private AudioClip LoadClipTracked(string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) LoadedRealClipsCount++;
            return clip;
        }

        private void BuildSynthesizedClips()
        {
            // Sons dos animais
            clipDogBark        = MakeDoubleBark(360f, 540f, 220f, 0.09f, 0.06f);
            clipThirstPant     = MakePantSound();
            clipCatMeow        = MakeCurveTone(new[] { 540f, 820f, 680f, 460f }, 0.36f, 0.45f);
            clipWolfHowl       = MakeCurveTone(new[] { 260f, 440f, 520f, 490f, 340f }, 0.72f, 0.50f);
            clipSquirrelChirp  = MakeTripleChirp(1150f, 1550f, 0.055f);
            clipRabbitSniff    = MakeTripleChirp(680f, 820f, 0.045f);
            clipCoatiChitter   = MakeDoubleBark(720f, 980f, 620f, 0.07f, 0.04f);
            clipFoxYip         = MakeDoubleBark(820f, 1180f, 640f, 0.08f, 0.05f);
            clipDeerCall       = MakeCurveTone(new[] { 310f, 380f, 290f }, 0.34f, 0.45f);
            clipHedgehogSqueak = MakeTripleChirp(980f, 1240f, 0.042f);
            clipRaccoonTrill   = MakeArpeggio(new[] { 620f, 780f, 700f, 880f }, 0.05f);
            clipBearCub        = MakeCurveTone(new[] { 210f, 260f, 185f }, 0.32f, 0.48f);
            clipBirdTweet      = MakeArpeggio(new[] { 1480f, 1980f, 1760f, 2240f }, 0.045f);
            clipBeaverSlap     = MakeDoubleBark(260f, 190f, 150f, 0.08f, 0.06f);
            clipFrogRibbit     = MakeDoubleBark(280f, 440f, 240f, 0.08f, 0.09f);
            clipOwlHoot        = MakeCurveTone(new[] { 440f, 410f, 340f, 320f }, 0.56f, 0.44f);

            // Efeitos da aventura (preserva sino real se carregado)
            if (clipBell == null)
                clipBell = MakeArpeggio(new[] { 1318f, 1760f, 1318f, 1760f }, 0.055f);

            clipThrow         = MakeCurveTone(new[] { 400f, 680f }, 0.11f, 0.38f);
            clipHappy         = MakeArpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.075f);
            clipWrong         = MakeCurveTone(new[] { 280f, 180f }, 0.20f, 0.40f);
            clipCry           = MakeCurveTone(new[] { 520f, 380f, 450f, 260f }, 0.42f, 0.48f);
            clipWater         = MakeArpeggio(new[] { 587f, 740f, 880f, 1174f }, 0.065f);
            clipDig           = MakeCurveTone(new[] { 190f, 105f }, 0.13f, 0.55f);
            clipDiamond       = MakeArpeggio(new[] { 784f, 987f, 1174f, 1568f, 2093f }, 0.085f);
            clipJump          = MakeCurveTone(new[] { 320f, 760f }, 0.18f, 0.42f);
            clipBump          = MakeCurveTone(new[] { 170f, 85f }, 0.16f, 0.55f);
            clipStar          = MakeArpeggio(new[] { 880f, 1174f, 1568f }, 0.06f);
            clipMusicianTune  = MakeArpeggio(new[] { 523f, 659f, 784f, 987f, 1046f, 784f, 1046f }, 0.085f);
            clipVillagerHappy = MakeArpeggio(new[] { 587f, 740f, 880f, 1174f, 1480f }, 0.065f);

            // Assinaturas sonoras suaves por região
            clipBiomeFlowers = MakeArpeggio(new[] { 659f, 784f, 987f, 1318f }, 0.09f);
            clipBiomeWind    = MakeArpeggio(new[] { 523f, 659f, 880f, 1046f }, 0.10f);
            clipBiomeWaters  = MakeArpeggio(new[] { 587f, 784f, 987f, 1174f }, 0.09f);
            clipBiomeSecrets = MakeArpeggio(new[] { 392f, 466f, 587f, 698f }, 0.11f);
            clipBiomeVillage = MakeArpeggio(new[] { 523f, 659f, 784f, 659f }, 0.09f);
            clipBiomeMoon    = MakeArpeggio(new[] { 440f, 523f, 659f, 880f, 987f }, 0.11f);

            // 4 notas musicais de instrumentos naturais
            natureNotes[0] = MakeCurveTone(new[] { 523f, 530f, 523f }, 0.28f, 0.48f);
            natureNotes[1] = MakeCurveTone(new[] { 330f, 290f }, 0.20f, 0.55f);
            natureNotes[2] = MakeCurveTone(new[] { 659f, 784f }, 0.22f, 0.45f);
            natureNotes[3] = MakeCurveTone(new[] { 1046f, 1060f, 1046f }, 0.30f, 0.42f);

            bgmSynthFallback = MakeArpeggio(new[] { 523f, 659f, 784f, 659f, 587f, 659f, 523f, 392f }, 0.38f);
        }

        private void Update()
        {
            float dt = Mathf.Min(0.1f, Time.unscaledDeltaTime);

            // Ducking suave por estado de jogo / painel da cozinha
            float targetDuck = ComputeTargetDuckMultiplier();
            currentDuck = Mathf.MoveTowards(currentDuck, targetDuck, dt * 2.4f);

            // Pausa real dos decks quando o usuário zera o volume da música (economiza CPU)
            bool shouldMutePause = MusicVolume <= 0.001f;
            if (shouldMutePause != musicMutedPaused)
            {
                musicMutedPaused = shouldMutePause;
                if (musicMutedPaused)
                {
                    if (musicDeckA != null && musicDeckA.isPlaying) musicDeckA.Pause();
                    if (musicDeckB != null && musicDeckB.isPlaying) musicDeckB.Pause();
                }
                else
                {
                    AudioSource active = GetActiveDeck();
                    if (active != null && active.clip != null) active.UnPause();
                }
            }

            if (musicMutedPaused)
            {
                if (musicDeckA != null) musicDeckA.volume = 0f;
                if (musicDeckB != null) musicDeckB.volume = 0f;
                return;
            }

            // Atualiza crossfade de potência constante
            AudioSource activeDeck = GetActiveDeck();
            AudioSource outgoingDeck = GetOutgoingDeck();

            float inGainBase = GetClipMasterGain(activeMusicClip) * MusicVolume * currentDuck;
            float outGainBase = GetClipMasterGain(outgoingMusicClip) * MusicVolume * currentDuck;

            if (isCrossfading)
            {
                crossfadeElapsed += dt;
                float t = Mathf.Clamp01(crossfadeElapsed / Mathf.Max(0.05f, crossfadeDuration));
                float fadeIn = Mathf.Sin(t * Mathf.PI * 0.5f);
                float fadeOut = Mathf.Cos(t * Mathf.PI * 0.5f);

                if (activeDeck != null) activeDeck.volume = inGainBase * fadeIn;
                if (outgoingDeck != null) outgoingDeck.volume = outGainBase * fadeOut;

                if (t >= 1f)
                {
                    isCrossfading = false;
                    if (outgoingDeck != null)
                    {
                        outgoingDeck.volume = 0f;
                        outgoingDeck.Stop();
                    }
                    outgoingMusicClip = null;
                }
            }
            else
            {
                if (activeDeck != null) activeDeck.volume = inGainBase;
                if (outgoingDeck != null && outgoingDeck.isPlaying)
                {
                    outgoingDeck.volume = 0f;
                    outgoingDeck.Stop();
                }

                // Transição limpa perto do fim de músicas longas (evita silêncio de fim de faixa e alterna variantes)
                if (activeDeck != null && activeDeck.isPlaying && activeDeck.clip != null
                    && activeDeck.clip.length > 18f
                    && activeDeck.time >= activeDeck.clip.length - 1.8f
                    && Time.unscaledTime - lastTailTriggerTime > 4.0f)
                {
                    lastTailTriggerTime = Time.unscaledTime;
                    AudioClip nextLoopClip = ResolveTailCompanionClip(activeDeck.clip);
                    StartCrossfadeTo(nextLoopClip, 1.8f, forceRestartIfSame: true);
                }
            }
        }

        // ─── API de Contexto Musical (Menu, 6 Biomas, Dia/Noite, Pausa, Cozinha) ───

        public void SetContext(BiomeType biome, DayPeriod period)
        {
            currentBiome = biome;
            currentPeriod = period;
            SelectAndCrossfadeTrack(immediate: false, fadeSeconds: 2.0f);
        }

        public void SetBiome(BiomeType biome)
        {
            if (biome == currentBiome && currentFlowState == GameFlowState.Playing) return;
            currentBiome = biome;
            if (biome == BiomeType.CampoFlorido) flowersVisitCount++;
            else if (biome == BiomeType.PequenaVila) villageVisitCount++;

            if (currentFlowState == GameFlowState.Playing)
                SelectAndCrossfadeTrack(immediate: false, fadeSeconds: 2.2f);
        }

        public void SetDayPeriod(DayPeriod period)
        {
            if (period == currentPeriod) return;
            currentPeriod = period;
            if (currentFlowState == GameFlowState.Playing)
                SelectAndCrossfadeTrack(immediate: false, fadeSeconds: 2.6f);
        }

        public void SetGameFlowState(GameFlowState newState)
        {
            GameFlowState prev = currentFlowState;
            currentFlowState = newState;

            if (newState == GameFlowState.TitleScreen && prev != GameFlowState.TitleScreen)
            {
                titleVisitCount++;
                SelectAndCrossfadeTrack(immediate: false, fadeSeconds: 1.5f);
            }
            else if (newState == GameFlowState.Playing && (prev == GameFlowState.TitleScreen || prev == GameFlowState.Loading))
            {
                SelectAndCrossfadeTrack(immediate: false, fadeSeconds: 1.6f);
            }
        }

        public void SetKitchenFocused(bool focused)
        {
            kitchenFocused = focused;
        }

        public void StopMusic(float fadeSeconds = 0.6f)
        {
            if (fadeSeconds <= 0.05f)
            {
                isCrossfading = false;
                if (musicDeckA != null) musicDeckA.Stop();
                if (musicDeckB != null) musicDeckB.Stop();
                activeMusicClip = null;
                outgoingMusicClip = null;
                CurrentTrackName = "Stopped";
                return;
            }

            StartCrossfadeTo(null, fadeSeconds, forceRestartIfSame: true);
        }

        public string GetAudioStatusReport()
        {
            return $"[FlorestaSoundFX] Clipes reais carregados: {LoadedRealClipsCount}/10 | " +
                   $"Trilha atual: {CurrentTrackName} | Sino real: {(usingRealBell ? "SIM" : "FALLBACK")} | " +
                   $"Vol Música: {Mathf.RoundToInt(MusicVolume * 100f)}% | Vol SFX: {Mathf.RoundToInt(SfxVolume * 100f)}%";
        }

        private float ComputeTargetDuckMultiplier()
        {
            switch (currentFlowState)
            {
                case GameFlowState.Paused:
                case GameFlowState.ExitConfirm:
                    return 0.48f;
                case GameFlowState.Settings:
                    return 0.68f;
                case GameFlowState.MissionComplete:
                    return 0.62f;
                case GameFlowState.ChallengeFailed:
                    return 0.56f;
                case GameFlowState.Playing:
                    return kitchenFocused ? 0.76f : 1.0f;
                default:
                    return 1.0f;
            }
        }

        private void SelectAndCrossfadeTrack(bool immediate, float fadeSeconds)
        {
            AudioClip desired = ResolveDesiredMusicClip();
            if (desired == null) desired = bgmSynthFallback;
            if (desired == null) return;

            if (desired == activeMusicClip && GetActiveDeck() != null && GetActiveDeck().isPlaying)
                return;

            StartCrossfadeTo(desired, immediate ? 0.1f : fadeSeconds, forceRestartIfSame: false);
        }

        private AudioClip ResolveDesiredMusicClip()
        {
            if (currentFlowState == GameFlowState.TitleScreen || currentFlowState == GameFlowState.Loading)
            {
                bool useAlt = (titleVisitCount % 2 == 1) && bgmMenuAlt != null;
                return useAlt ? bgmMenuAlt : (bgmMenuPrimary != null ? bgmMenuPrimary : bgmMenuAlt);
            }

            // Noite e Madrugada trazem a atmosfera mágica de Moonlit Forest em toda a travessia
            if (currentPeriod == DayPeriod.Noite || currentPeriod == DayPeriod.Madrugada)
            {
                if (bgmMoon != null) return bgmMoon;
            }

            switch (currentBiome)
            {
                case BiomeType.CampoFlorido:
                    // Vale das Flores usa "vale das flores" como tema principal e alterna com "The Forest Whimsy (1)" à tarde/revisitas
                    if ((currentPeriod == DayPeriod.Tarde || flowersVisitCount % 2 == 1) && bgmMenuAlt != null && flowersVisitCount > 0)
                        return bgmMenuAlt;
                    return bgmFlowers != null ? bgmFlowers : bgmMenuPrimary;

                case BiomeType.Colinas:
                    return bgmWind != null ? bgmWind : bgmFlowers;

                case BiomeType.RegiaoDoRio:
                    return bgmWaters != null ? bgmWaters : bgmFlowers;

                case BiomeType.TrilhaDaFloresta:
                    return bgmSecrets != null ? bgmSecrets : bgmMenuPrimary;

                case BiomeType.PequenaVila:
                    // Vila dos Viajantes alterna entre Woodland Market (manhã/chegada) e Woodland Market (1) (tarde/entardecer/revisita)
                    bool calmVillage = (currentPeriod == DayPeriod.Tarde || currentPeriod == DayPeriod.PorDoSol || villageVisitCount % 2 == 1);
                    if (calmVillage && bgmVillageAlt != null) return bgmVillageAlt;
                    return bgmVillage != null ? bgmVillage : bgmVillageAlt;

                case BiomeType.BosqueDaLua:
                    return bgmMoon != null ? bgmMoon : bgmMenuAlt;

                default:
                    return bgmFlowers != null ? bgmFlowers : bgmMenuPrimary;
            }
        }

        private AudioClip ResolveTailCompanionClip(AudioClip finishedClip)
        {
            if (finishedClip == bgmMenuPrimary && bgmMenuAlt != null) return bgmMenuAlt;
            if (finishedClip == bgmMenuAlt)
            {
                if (currentFlowState == GameFlowState.TitleScreen && bgmMenuPrimary != null) return bgmMenuPrimary;
                if (currentBiome == BiomeType.CampoFlorido && bgmFlowers != null) return bgmFlowers;
            }
            if (finishedClip == bgmFlowers && bgmMenuAlt != null) return bgmMenuAlt;
            if (finishedClip == bgmVillage && bgmVillageAlt != null) return bgmVillageAlt;
            if (finishedClip == bgmVillageAlt && bgmVillage != null) return bgmVillage;
            return finishedClip;
        }

        private void StartCrossfadeTo(AudioClip nextClip, float durationSeconds, bool forceRestartIfSame)
        {
            if (!forceRestartIfSame && nextClip == activeMusicClip && GetActiveDeck() != null && GetActiveDeck().isPlaying)
                return;

            AudioSource prevActive = GetActiveDeck();
            outgoingMusicClip = activeMusicClip;

            activeIsA = !activeIsA;
            AudioSource nextActive = GetActiveDeck();
            activeMusicClip = nextClip;
            CurrentTrackName = nextClip != null ? nextClip.name : "Stopped";

            if (nextActive != null)
            {
                nextActive.Stop();
                nextActive.clip = nextClip;
                nextActive.time = 0f;
                nextActive.loop = true;
                nextActive.volume = 0f;
                if (nextClip != null && !musicMutedPaused)
                    nextActive.Play();
            }

            if (prevActive == null || !prevActive.isPlaying)
            {
                isCrossfading = nextClip != null && durationSeconds > 0.12f;
                crossfadeDuration = Mathf.Max(0.12f, durationSeconds);
                crossfadeElapsed = isCrossfading ? 0f : crossfadeDuration;
                if (!isCrossfading && nextActive != null && nextClip != null && !musicMutedPaused)
                    nextActive.volume = GetClipMasterGain(nextClip) * MusicVolume * currentDuck;
                return;
            }

            isCrossfading = true;
            crossfadeDuration = Mathf.Max(0.15f, durationSeconds);
            crossfadeElapsed = 0f;
        }

        private AudioSource GetActiveDeck() => activeIsA ? musicDeckA : musicDeckB;
        private AudioSource GetOutgoingDeck() => activeIsA ? musicDeckB : musicDeckA;

        private float GetClipMasterGain(AudioClip clip)
        {
            if (clip == null) return 0f;
            if (clip == bgmSynthFallback) return SynthMusicMasterGain;
            return RealMusicMasterGain;
        }

        // ─── Controle de Volumes & Reprodução de Efeitos ──────────────────────

        public void ApplyVolumes(float musicVol, float sfxVol)
        {
            MusicVolume = Mathf.Clamp01(musicVol);
            SfxVolume = Mathf.Clamp01(sfxVol);

            if (source != null) source.volume = SfxMasterGain * SfxVolume;
            if (bellSource != null) bellSource.volume = (usingRealBell ? RealBellMasterGain : SfxMasterGain) * SfxVolume;

            if (!isCrossfading && !musicMutedPaused)
            {
                AudioSource active = GetActiveDeck();
                if (active != null && active.clip != null)
                    active.volume = GetClipMasterGain(active.clip) * MusicVolume * currentDuck;
            }
        }

        public void PlayAnimalVoice(ParkSpriteFactory.AnimalKind kind)
        {
            if (source == null || SfxVolume <= 0.001f) return;
            AudioClip c = kind switch
            {
                ParkSpriteFactory.AnimalKind.Gato          => clipCatMeow,
                ParkSpriteFactory.AnimalKind.Lobo          => clipWolfHowl,
                ParkSpriteFactory.AnimalKind.Esquilo       => clipSquirrelChirp,
                ParkSpriteFactory.AnimalKind.Coelho        => clipRabbitSniff,
                ParkSpriteFactory.AnimalKind.Quati         => clipCoatiChitter,
                ParkSpriteFactory.AnimalKind.Raposa        => clipFoxYip,
                ParkSpriteFactory.AnimalKind.Cervo         => clipDeerCall,
                ParkSpriteFactory.AnimalKind.Ourico        => clipHedgehogSqueak,
                ParkSpriteFactory.AnimalKind.Guaxinim      => clipRaccoonTrill,
                ParkSpriteFactory.AnimalKind.FilhoteUrso   => clipBearCub,
                ParkSpriteFactory.AnimalKind.Passarinhos   => clipBirdTweet,
                ParkSpriteFactory.AnimalKind.Castor        => clipBeaverSlap,
                ParkSpriteFactory.AnimalKind.Sapo          => clipFrogRibbit,
                ParkSpriteFactory.AnimalKind.CorujaNoturna => clipOwlHoot,
                _ => clipDogBark
            };
            source.PlayOneShot(c);
        }

        public void PlayBell()
        {
            if (SfxVolume <= 0.001f || clipBell == null) return;
            // Evita sobreposição metálica excessiva em cliques rápidos (funciona também pausado)
            if (Time.unscaledTime - lastBellUnscaledTime < 0.22f) return;
            lastBellUnscaledTime = Time.unscaledTime;

            if (bellSource != null)
            {
                bellSource.Stop();
                bellSource.clip = clipBell;
                bellSource.volume = (usingRealBell ? RealBellMasterGain : SfxMasterGain) * SfxVolume;
                bellSource.pitch = usingRealBell ? Random.Range(0.97f, 1.03f) : 1f;
                bellSource.Play();
            }
            else if (source != null)
            {
                source.PlayOneShot(clipBell);
            }
        }

        public void PlayDogBark()       { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipDogBark); }
        public void PlayThirst()        { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipThirstPant); }
        public void PlayThrow()         { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipThrow); }
        public void PlayHappy()         { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipHappy); }
        public void PlayWrong()         { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipWrong); }
        public void PlayCry()           { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipCry); }
        public void PlayWater()         { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipWater); }
        public void PlayDig()           { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipDig); }
        public void PlayDiamond()       { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipDiamond); }
        public void PlayJump()          { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipJump); }
        public void PlayBump()          { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipBump); }
        public void PlayStar()          { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipStar); }
        public void PlayMusicianTune()  { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipMusicianTune); }
        public void PlayVillagerHappy() { if (source != null && SfxVolume > 0.001f) source.PlayOneShot(clipVillagerHappy); }

        public void PlayBiomeCue(BiomeType biome)
        {
            if (source == null || SfxVolume <= 0.001f) return;
            AudioClip c = biome switch
            {
                BiomeType.CampoFlorido     => clipBiomeFlowers,
                BiomeType.Colinas          => clipBiomeWind,
                BiomeType.RegiaoDoRio      => clipBiomeWaters,
                BiomeType.TrilhaDaFloresta => clipBiomeSecrets,
                BiomeType.PequenaVila      => clipBiomeVillage,
                BiomeType.BosqueDaLua      => clipBiomeMoon,
                _ => clipBiomeFlowers
            };
            // Volume mais sutil quando há música real tocando para não competir com o crossfade
            float cueScale = LoadedRealClipsCount > 0 ? 0.30f : 0.55f;
            source.PlayOneShot(c, cueScale);
        }

        public void PlayNatureNote(int noteIdx)
        {
            if (source == null || SfxVolume <= 0.001f || noteIdx < 0 || noteIdx >= natureNotes.Length) return;
            if (natureNotes[noteIdx] != null) source.PlayOneShot(natureNotes[noteIdx], 0.72f);
        }

        // ─── Síntese Procedural (Fallback + Animais/Eventos) ──────────────────

        private AudioClip MakeCurveTone(float[] pitchPoints, float duration, float amp)
        {
            int sr = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sr * duration));
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float scaled = t * (pitchPoints.Length - 1);
                int idx = Mathf.Min(pitchPoints.Length - 2, Mathf.FloorToInt(scaled));
                float frac = scaled - idx;
                float freq = Mathf.Lerp(pitchPoints[idx], pitchPoints[idx + 1], frac);

                phase += 2f * Mathf.PI * freq / sr;
                float env = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;
                data[i] = wave * env * amp;
            }

            AudioClip clip = AudioClip.Create("Voice", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeDoubleBark(float startF, float peakF, float endF, float barkDur, float gapDur)
        {
            int sr = 22050;
            int bSamp = Mathf.FloorToInt(sr * barkDur);
            int gSamp = Mathf.FloorToInt(sr * gapDur);
            int total = bSamp * 2 + gSamp;
            float[] data = new float[total];

            for (int b = 0; b < 2; b++)
            {
                int offset = b * (bSamp + gSamp);
                float phase = 0f;
                for (int i = 0; i < bSamp; i++)
                {
                    float t = i / (float)bSamp;
                    float f = t < 0.35f ? Mathf.Lerp(startF, peakF, t / 0.35f) : Mathf.Lerp(peakF, endF, (t - 0.35f) / 0.65f);
                    phase += 2f * Mathf.PI * f / sr;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float wave = Mathf.Sin(phase) * 0.65f + Mathf.Sin(phase * 2.5f) * 0.35f;
                    data[offset + i] = wave * env * 0.5f;
                }
            }

            AudioClip clip = AudioClip.Create("Bark", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeTripleChirp(float f1, float f2, float chirpDur)
        {
            int sr = 22050;
            int cSamp = Mathf.FloorToInt(sr * chirpDur);
            int gSamp = Mathf.FloorToInt(sr * 0.03f);
            int total = cSamp * 3 + gSamp * 2;
            float[] data = new float[total];

            for (int c = 0; c < 3; c++)
            {
                int offset = c * (cSamp + gSamp);
                float phase = 0f;
                for (int i = 0; i < cSamp; i++)
                {
                    float t = i / (float)cSamp;
                    float f = Mathf.Lerp(f1, f2, t);
                    phase += 2f * Mathf.PI * f / sr;
                    data[offset + i] = Mathf.Sin(phase) * Mathf.Sin(t * Mathf.PI) * 0.42f;
                }
            }

            AudioClip clip = AudioClip.Create("Chirp", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakePantSound()
        {
            int sr = 22050;
            int burst = Mathf.FloorToInt(sr * 0.11f);
            int gap = Mathf.FloorToInt(sr * 0.07f);
            int total = burst * 3 + gap * 2;
            float[] data = new float[total];
            System.Random rng = new System.Random(42);

            for (int b = 0; b < 3; b++)
            {
                int offset = b * (burst + gap);
                for (int i = 0; i < burst; i++)
                {
                    float t = i / (float)burst;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 380f, t) * (i / (float)sr)) * 0.25f;
                    data[offset + i] = (noise * 0.22f + whine) * Mathf.Sin(t * Mathf.PI);
                }
            }

            AudioClip clip = AudioClip.Create("Thirst", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeArpeggio(float[] freqs, float noteDuration)
        {
            int sr = 22050;
            int noteSamples = Mathf.FloorToInt(sr * noteDuration);
            int total = noteSamples * freqs.Length;
            float[] data = new float[total];
            for (int n = 0; n < freqs.Length; n++)
            {
                float phase = 0f;
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)noteSamples;
                    phase += 2f * Mathf.PI * freqs[n] / sr;
                    data[n * noteSamples + i] = Mathf.Sin(phase) * Mathf.Sin(t * Mathf.PI) * 0.42f;
                }
            }
            AudioClip clip = AudioClip.Create("Arp", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
