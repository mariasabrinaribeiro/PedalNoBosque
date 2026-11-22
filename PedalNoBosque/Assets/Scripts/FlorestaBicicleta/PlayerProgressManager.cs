using System;
using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    public enum CollectibleKind
    {
        Estrelas,
        Flores,
        Sementes,
        FolhasEspeciais,
        Cogumelos,
        PedrasBrilhantes,
        TesourosSecretos
    }

    public enum CustomizationCategory
    {
        Bicicletas,
        Cestas,
        Acessorios,
        Personagem,
        Cachorrinho
    }

    public class AnimalBookEntry
    {
        public ParkSpriteFactory.AnimalKind kind;
        public string displayName;
        public string personality;
        public string favoriteFood;
        public string nativeRegion;
        public string curiosity;
    }

    public class AchievementDef
    {
        public string id;
        public string title;
        public string description;
        public int starReward;
    }

    public class CustomizationItemDef
    {
        public string id;
        public CustomizationCategory category;
        public string displayName;
        public string badgeText;
        public int starCost;
        public Color tintColor;
    }

    /// <summary>
    /// Etapa 7 — Gerenciador Central de Progresso, Livro de Descobertas, Colecionáveis,
    /// Conquistas e Personalização do PEDAL NO BOSQUE (com persistência local em JSON via SaveSystem).
    /// </summary>
    public class PlayerProgressManager : MonoBehaviour
    {
        public static PlayerProgressManager Instance { get; private set; }

        public PlayerSaveData Data { get; private set; }

        public readonly List<AnimalBookEntry> AnimalCatalog = new List<AnimalBookEntry>();
        public readonly List<AchievementDef> Achievements = new List<AchievementDef>();
        public readonly List<CustomizationItemDef> Customizations = new List<CustomizationItemDef>();

        public event Action<string, Color> OnMilestoneBanner;
        public event Action OnCustomizationChanged;

        private void Awake()
        {
            Instance = this;
            BuildAnimalCatalog();
            BuildAchievements();
            BuildCustomizations();
            Data = SaveSystem.Load();
            RefreshSpecialRecipeUnlocks();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && Data != null) SaveSystem.Save(Data);
        }

        private void OnApplicationQuit()
        {
            if (Data != null) SaveSystem.Save(Data);
        }

        // ─── Catálogo do Livro dos Animais (14 espécies) ─────────────

        private void BuildAnimalCatalog()
        {
            AnimalCatalog.Clear();
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Esquilo,
                displayName = "Esquilo",
                personality = "Curioso e brincalhão",
                favoriteFood = "Nozes e Sementes",
                nativeRegion = "Vale das Flores / Floresta dos Segredos",
                curiosity = "Adora esconder tesouros na terra fofa e compartilha diamantes com quem cuida dele!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Coelho,
                displayName = "Coelho",
                personality = "Gentil e saltitante",
                favoriteFood = "Cenoura e Maçã",
                nativeRegion = "Vale das Flores / Vila dos Viajantes",
                curiosity = "Suas orelhas compridas ouvem a campainha da bicicleta de longe!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Gato,
                displayName = "Gato",
                personality = "Carinhoso e companheiro",
                favoriteFood = "Suco e Sanduíche",
                nativeRegion = "Vila dos Viajantes",
                curiosity = "Quando fica bem alimentado, adora acompanhar a bicicleta soltando corações!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Lobo,
                displayName = "Lobo",
                personality = "Protetor e cauteloso",
                favoriteFood = "Hot Dog e Mel",
                nativeRegion = "Floresta dos Segredos",
                curiosity = "Prefere quem chega bem devagar e com respeito; quando confia, vira um grande guardião."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Quati,
                displayName = "Quati",
                personality = "Alegre e farejador",
                favoriteFood = "Maçã e Mel",
                nativeRegion = "Vale das Águas",
                curiosity = "Usa seu focinho ágil para explorar frutas doces perto dos riachos."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Raposa,
                displayName = "Raposa",
                personality = "Esperta e observadora",
                favoriteFood = "Maçã e Sanduíche",
                nativeRegion = "Montanhas do Vento / Bosque da Lua",
                curiosity = "Move-se silenciosamente entre as colinas e adora admirar o pôr do sol."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Cervo,
                displayName = "Cervo",
                personality = "Sereno e elegante",
                favoriteFood = "Maçã e Cenoura",
                nativeRegion = "Montanhas do Vento / Vale das Águas",
                curiosity = "É um dos guardiões mais tranquilos da floresta e adora tardes calmas."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Ourico,
                displayName = "Ouriço",
                personality = "Tímido e doce",
                favoriteFood = "Maçã e Nozes",
                nativeRegion = "Vale das Flores / Floresta dos Segredos",
                curiosity = "Apesar dos espinhos, é super amigável e faz uma fungadinha fofa ao ganhar comida."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Guaxinim,
                displayName = "Guaxinim",
                personality = "Travesso e inventivo",
                favoriteFood = "Nozes e Coxinha",
                nativeRegion = "Floresta dos Segredos / Vila dos Viajantes",
                curiosity = "Com suas patinhas habilidosas, adora lavar frutas na água antes de comer!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.FilhoteUrso,
                displayName = "Filhote de Urso",
                personality = "Fofinho e guloso",
                favoriteFood = "Mel e Maçã",
                nativeRegion = "Montanhas do Vento / Bosque da Lua",
                curiosity = "Sente o cheirinho de mel dourado a metros de distância!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Passarinhos,
                displayName = "Passarinhos",
                personality = "Cantores e festivos",
                favoriteFood = "Sementes e Maçã",
                nativeRegion = "Vale das Flores / Montanhas do Vento",
                curiosity = "Sempre viajam em dupla e cantam melodias alegres quando a bicicleta passa."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Castor,
                displayName = "Castor",
                personality = "Trabalhador e criativo",
                favoriteFood = "Cenoura e Nozes",
                nativeRegion = "Vale das Águas",
                curiosity = "É o grande engenheiro dos rios e ajuda a manter as margens sempre seguras!"
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.Sapo,
                displayName = "Sapo",
                personality = "Saltitante e tranquilo",
                favoriteFood = "Sementes e Suco",
                nativeRegion = "Vale das Águas",
                curiosity = "Adora descansar nas vitórias-régias e dar pulinhos alegres ao som da água."
            });
            AnimalCatalog.Add(new AnimalBookEntry
            {
                kind = ParkSpriteFactory.AnimalKind.CorujaNoturna,
                displayName = "Coruja Noturna",
                personality = "Sábia e misteriosa",
                favoriteFood = "Nozes e Mel",
                nativeRegion = "Bosque da Lua / Floresta dos Segredos",
                curiosity = "Seus grandes olhos dourados brilham sob o luar enquanto vigia os vaga-lumes."
            });
        }

        private void BuildAchievements()
        {
            Achievements.Clear();
            Achievements.Add(new AchievementDef { id = "first_feed", title = "Primeiro Carinho", description = "Alimente seu primeiro animal no bosque.", starReward = 2 });
            Achievements.Add(new AchievementDef { id = "first_km", title = "Primeira Viagem", description = "Pedale 1.0 km pelas trilhas do bosque.", starReward = 3 });
            Achievements.Add(new AchievementDef { id = "first_region", title = "Nova Paisagem", description = "Descubra uma nova região além do início.", starReward = 2 });
            Achievements.Add(new AchievementDef { id = "owl_night", title = "Amiga da Lua", description = "Encontre a sábia Coruja Noturna.", starReward = 3 });
            Achievements.Add(new AchievementDef { id = "squirrel_secret", title = "Segredo dos Diamantes", description = "Ajude o Esquilo a cavar e achar diamantes!", starReward = 5 });
            Achievements.Add(new AchievementDef { id = "ten_deliveries", title = "Entregadora do Bosque", description = "Faça 10 entregas felizes para animais ou moradores.", starReward = 4 });
            Achievements.Add(new AchievementDef { id = "animal_lover", title = "Diário Vivo", description = "Descubra 6 espécies diferentes no Livro dos Animais.", starReward = 4 });
            Achievements.Add(new AchievementDef { id = "six_regions", title = "Exploradora Completa", description = "Visite todas as 6 regiões do Pedal no Bosque!", starReward = 6 });
            Achievements.Add(new AchievementDef { id = "treasure_hunter", title = "Colecionadora", description = "Colete 10 itens especiais da natureza.", starReward = 4 });
        }

        private void BuildCustomizations()
        {
            Customizations.Clear();
            // Bicicletas
            Customizations.Add(new CustomizationItemDef { id = "bike_default", category = CustomizationCategory.Bicicletas, displayName = "Bike Clássica", badgeText = "Original", starCost = 0, tintColor = Color.white });
            Customizations.Add(new CustomizationItemDef { id = "bike_rosa", category = CustomizationCategory.Bicicletas, displayName = "Bike Rosa Flor", badgeText = "Rosa", starCost = 3, tintColor = new Color(1f, 0.88f, 0.94f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "bike_azul", category = CustomizationCategory.Bicicletas, displayName = "Bike Azul Celeste", badgeText = "Azul", starCost = 4, tintColor = new Color(0.86f, 0.95f, 1f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "bike_verde", category = CustomizationCategory.Bicicletas, displayName = "Bike Verde Bosque", badgeText = "Verde", starCost = 4, tintColor = new Color(0.88f, 1f, 0.90f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "bike_lilas", category = CustomizationCategory.Bicicletas, displayName = "Bike Lilás Sonho", badgeText = "Lilás", starCost = 5, tintColor = new Color(0.94f, 0.88f, 1f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "bike_amarela", category = CustomizationCategory.Bicicletas, displayName = "Bike Amarela Sol", badgeText = "Amarela", starCost = 5, tintColor = new Color(1f, 0.97f, 0.82f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "bike_estelar", category = CustomizationCategory.Bicicletas, displayName = "Bike Estelar Especial", badgeText = "Especial", starCost = 8, tintColor = new Color(0.90f, 0.94f, 1f, 1f) });

            // Cestas
            Customizations.Add(new CustomizationItemDef { id = "basket_default", category = CustomizationCategory.Cestas, displayName = "Cesta de Madeira", badgeText = "Madeira", starCost = 0, tintColor = new Color(0.76f, 0.56f, 0.34f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "basket_flores", category = CustomizationCategory.Cestas, displayName = "Cesta de Flores", badgeText = "Flores", starCost = 3, tintColor = new Color(1f, 0.62f, 0.80f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "basket_vime", category = CustomizationCategory.Cestas, displayName = "Cesta de Vime", badgeText = "Vime", starCost = 4, tintColor = new Color(0.90f, 0.74f, 0.44f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "basket_colorida", category = CustomizationCategory.Cestas, displayName = "Cesta Colorida", badgeText = "Festa", starCost = 5, tintColor = new Color(0.48f, 0.86f, 1f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "basket_lua", category = CustomizationCategory.Cestas, displayName = "Cesta da Lua", badgeText = "Temática", starCost = 7, tintColor = new Color(0.74f, 0.68f, 1f, 1f) });

            // Acessórios
            Customizations.Add(new CustomizationItemDef { id = "acc_default", category = CustomizationCategory.Acessorios, displayName = "Campainha Clássica", badgeText = "Trim!", starCost = 0, tintColor = Color.white });
            Customizations.Add(new CustomizationItemDef { id = "acc_capacete", category = CustomizationCategory.Acessorios, displayName = "Capacete Florido", badgeText = "Flor", starCost = 3, tintColor = new Color(1f, 0.72f, 0.84f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "acc_lanterna", category = CustomizationCategory.Acessorios, displayName = "Lanterna Dourada", badgeText = "Brilho", starCost = 5, tintColor = new Color(1f, 0.90f, 0.42f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "acc_borboletas", category = CustomizationCategory.Acessorios, displayName = "Enfeite Borboletas", badgeText = "Voo", starCost = 6, tintColor = new Color(0.62f, 0.92f, 1f, 1f) });

            // Personagem
            Customizations.Add(new CustomizationItemDef { id = "char_default", category = CustomizationCategory.Personagem, displayName = "Passeio Clássico", badgeText = "Padrão", starCost = 0, tintColor = Color.white });
            Customizations.Add(new CustomizationItemDef { id = "char_chapeu", category = CustomizationCategory.Personagem, displayName = "Chapéu de Palha", badgeText = "Sol", starCost = 3, tintColor = new Color(1f, 0.90f, 0.56f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "char_laco", category = CustomizationCategory.Personagem, displayName = "Laço Lavanda", badgeText = "Lavanda", starCost = 4, tintColor = new Color(0.84f, 0.74f, 1f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "char_capa", category = CustomizationCategory.Personagem, displayName = "Capa Exploradora", badgeText = "Aventura", starCost = 6, tintColor = new Color(0.56f, 0.92f, 0.74f, 1f) });

            // Cachorrinho
            Customizations.Add(new CustomizationItemDef { id = "puppy_default", category = CustomizationCategory.Cachorrinho, displayName = "Cachorrinho Natural", badgeText = "Au-au", starCost = 0, tintColor = Color.white });
            Customizations.Add(new CustomizationItemDef { id = "puppy_lenco", category = CustomizationCategory.Cachorrinho, displayName = "Lenço Vermelho", badgeText = "Lenço", starCost = 3, tintColor = new Color(1f, 0.42f, 0.46f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "puppy_coleira", category = CustomizationCategory.Cachorrinho, displayName = "Coleira Estrelinha", badgeText = "Estrela", starCost = 4, tintColor = new Color(1f, 0.88f, 0.34f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "puppy_oculos", category = CustomizationCategory.Cachorrinho, displayName = "Óculos de Aventura", badgeText = "Estiloso", starCost = 5, tintColor = new Color(0.48f, 0.84f, 1f, 1f) });
            Customizations.Add(new CustomizationItemDef { id = "puppy_chapeu", category = CustomizationCategory.Cachorrinho, displayName = "Chapéu Explorador", badgeText = "Chapéu", starCost = 6, tintColor = new Color(0.82f, 0.64f, 0.40f, 1f) });
        }

        // ─── API de Progresso, Descobertas e Colecionáveis ───────────

        public bool IsAnimalDiscovered(ParkSpriteFactory.AnimalKind kind)
        {
            return Data != null && Data.discoveredAnimals.Contains(kind.ToString());
        }

        public void RecordAnimalDiscovery(ParkSpriteFactory.AnimalKind kind)
        {
            if (Data == null) return;
            string key = kind.ToString();
            if (kind == ParkSpriteFactory.AnimalKind.CorujaNoturna && !Data.owlMet)
            {
                Data.owlMet = true;
                TryUnlockAchievement("owl_night");
            }

            if (Data.discoveredAnimals.Contains(key)) return;

            Data.discoveredAnimals.Add(key);
            AnimalBookEntry entry = AnimalCatalog.Find(a => a.kind == kind);
            string name = entry != null ? entry.displayName : key;

            FlorestaSoundFX.Instance?.PlayStar();
            OnMilestoneBanner?.Invoke($"Nova descoberta no Livro dos Animais: {name}!", new Color(1f, 0.92f, 0.56f, 1f));

            if (Data.discoveredAnimals.Count >= 6)
                TryUnlockAchievement("animal_lover");

            SaveSystem.Save(Data);
        }

        public void RecordBiomeVisited(BiomeType biome)
        {
            if (Data == null) return;
            string key = biome.ToString();
            if (Data.visitedBiomes.Contains(key)) return;

            Data.visitedBiomes.Add(key);
            if (Data.visitedBiomes.Count >= 2)
                TryUnlockAchievement("first_region");
            if (Data.visitedBiomes.Count >= 6)
                TryUnlockAchievement("six_regions");

            SaveSystem.Save(Data);
        }

        public void RecordDelivery(bool isSquirrelSecret = false)
        {
            if (Data == null) return;
            Data.totalDeliveries++;
            RefreshSpecialRecipeUnlocks();
            if (Data.totalDeliveries == 1)
                TryUnlockAchievement("first_feed");
            if (Data.totalDeliveries >= 10)
                TryUnlockAchievement("ten_deliveries");

            if (isSquirrelSecret && !Data.squirrelSecretFound)
            {
                Data.squirrelSecretFound = true;
                AddCollectible(CollectibleKind.TesourosSecretos, 1);
                TryUnlockAchievement("squirrel_secret");
            }
            EvaluateVillageEvolution();
            SaveSystem.Save(Data);
        }

        public bool IsRecipeUnlocked(FoodType recipe)
        {
            if (Data == null) return false;
            if (recipe == FoodType.Sanduiche || recipe == FoodType.HotDog) return true;
            return Data.unlockedRecipes.Contains(recipe.ToString());
        }

        private void RefreshSpecialRecipeUnlocks()
        {
            if (Data == null) return;
            if (Data.totalDeliveries >= 3) UnlockSpecialRecipe(FoodType.SaladaFrutasBosque);
            if (Data.totalDeliveries >= 8) UnlockSpecialRecipe(FoodType.CestaDaLua);
        }

        private void UnlockSpecialRecipe(FoodType recipe)
        {
            string key = recipe.ToString();
            if (Data.unlockedRecipes.Contains(key)) return;
            Data.unlockedRecipes.Add(key);
            FlorestaSoundFX.Instance?.PlayDiamond();
            OnMilestoneBanner?.Invoke(
                $"Nova receita desbloqueada: {FoodInfo.GetDisplayName(recipe)}! Prepare-a na Cozinha do Bosque.",
                new Color(1f, 0.88f, 0.50f, 1f));
        }

        public void RecordAdventureAction(TrailAdventureEvent.EventKind eventKind)
        {
            if (Data == null) return;
            switch (eventKind)
            {
                case TrailAdventureEvent.EventKind.FloresMurchas:
                    Data.flowersWateredCount++;
                    break;
                case TrailAdventureEvent.EventKind.PonteQuebrada:
                    Data.bridgesRepairedCount++;
                    Data.castorBridgeRepaired = true;
                    break;
                case TrailAdventureEvent.EventKind.PonteCastorPronta:
                    Data.castorBridgeFollowupScheduled = true;
                    break;
                case TrailAdventureEvent.EventKind.PassaroCaido:
                    Data.birdsRescuedCount++;
                    break;
                default:
                    Data.totalDeliveries++;
                    break;
            }
            RefreshSpecialRecipeUnlocks();
            EvaluateVillageEvolution();
            SaveSystem.Save(Data);
        }

        public bool TryScheduleCastorBridgeFollowup()
        {
            if (Data == null || !Data.castorBridgeRepaired || Data.castorBridgeFollowupScheduled) return false;
            Data.castorBridgeFollowupScheduled = true;
            SaveSystem.Save(Data);
            return true;
        }

        public void EvaluateVillageEvolution()
        {
            if (Data == null) return;
            int totalGoodDeeds = Data.totalDeliveries + Data.flowersWateredCount + Data.bridgesRepairedCount + Data.birdsRescuedCount;
            int newLevel = totalGoodDeeds >= 10 ? 3 : (totalGoodDeeds >= 6 ? 2 : (totalGoodDeeds >= 3 ? 1 : 0));

            if (newLevel > Data.villageEvolutionLevel)
            {
                Data.villageEvolutionLevel = newLevel;
                string levelName = newLevel == 1 ? "Vila Florida" : (newLevel == 2 ? "Praca Festiva" : "Vila Encantada");
                FlorestaSoundFX.Instance?.PlayDiamond();
                OnMilestoneBanner?.Invoke($"A Vila dos Viajantes evoluiu para o Nivel {newLevel}: {levelName}!", new Color(1f, 0.90f, 0.56f, 1f));
            }
        }

        public void RecordDistanceKm(float km)
        {
            if (Data == null) return;
            if (km > Data.bestDistanceKm) Data.bestDistanceKm = km;
            if (km >= 1.0f) TryUnlockAchievement("first_km");
        }

        public void AddStars(int amount)
        {
            if (Data == null || amount <= 0) return;
            Data.starsAvailable += amount;
            Data.totalStarsEarned += amount;
            AddCollectible(CollectibleKind.Estrelas, amount, saveNow: false);
            SaveSystem.Save(Data);
        }

        public void AddCollectible(CollectibleKind kind, int amount = 1, bool saveNow = true)
        {
            if (Data == null || amount <= 0) return;
            string key = kind.ToString();
            CollectibleSaveEntry entry = Data.collectibles.Find(c => c.key == key);
            if (entry == null)
            {
                entry = new CollectibleSaveEntry { key = key, count = 0 };
                Data.collectibles.Add(entry);
            }
            entry.count += amount;

            if (GetTotalNatureCollectibles() >= 10)
                TryUnlockAchievement("treasure_hunter");

            if (saveNow) SaveSystem.Save(Data);
        }

        public int GetCollectibleCount(CollectibleKind kind)
        {
            if (Data == null) return 0;
            CollectibleSaveEntry entry = Data.collectibles.Find(c => c.key == kind.ToString());
            return entry != null ? entry.count : 0;
        }

        public int GetTotalNatureCollectibles()
        {
            if (Data == null) return 0;
            int total = 0;
            for (int i = 0; i < Data.collectibles.Count; i++)
            {
                if (Data.collectibles[i].key != CollectibleKind.Estrelas.ToString())
                    total += Data.collectibles[i].count;
            }
            return total;
        }

        public bool IsAchievementUnlocked(string id)
        {
            return Data != null && Data.unlockedAchievements.Contains(id);
        }

        public void TryUnlockAchievement(string id)
        {
            if (Data == null || Data.unlockedAchievements.Contains(id)) return;
            AchievementDef def = Achievements.Find(a => a.id == id);
            if (def == null) return;

            Data.unlockedAchievements.Add(id);
            Data.starsAvailable += def.starReward;
            Data.totalStarsEarned += def.starReward;
            AddCollectible(CollectibleKind.Estrelas, def.starReward, saveNow: false);

            FlorestaSoundFX.Instance?.PlayDiamond();
            OnMilestoneBanner?.Invoke($"Medalha desbloqueada: {def.title}! (+{def.starReward} Estrelas)", new Color(1f, 0.88f, 0.42f, 1f));
            SaveSystem.Save(Data);
        }

        // ─── Personalização (Desbloquear, Equipar e Retirar) ─────────

        public bool IsCustomizationUnlocked(string itemId)
        {
            return Data != null && Data.unlockedCustomizations.Contains(itemId);
        }

        public string GetEquippedId(CustomizationCategory cat)
        {
            if (Data == null) return "";
            switch (cat)
            {
                case CustomizationCategory.Bicicletas:  return Data.equippedBike;
                case CustomizationCategory.Cestas:      return Data.equippedBasket;
                case CustomizationCategory.Acessorios:  return Data.equippedAccessory;
                case CustomizationCategory.Personagem:  return Data.equippedCharacter;
                case CustomizationCategory.Cachorrinho: return Data.equippedPuppy;
                default: return "";
            }
        }

        public CustomizationItemDef GetEquippedDef(CustomizationCategory cat)
        {
            string id = GetEquippedId(cat);
            return Customizations.Find(c => c.id == id);
        }

        public bool TryUnlockOrToggleEquip(string itemId, out string feedback)
        {
            feedback = "";
            if (Data == null) return false;
            CustomizationItemDef item = Customizations.Find(c => c.id == itemId);
            if (item == null) return false;

            if (!IsCustomizationUnlocked(itemId))
            {
                if (Data.starsAvailable < item.starCost)
                {
                    feedback = $"Precisa de {item.starCost} Estrelas (voce tem {Data.starsAvailable}).";
                    FlorestaSoundFX.Instance?.PlayWrong();
                    return false;
                }
                Data.starsAvailable -= item.starCost;
                Data.unlockedCustomizations.Add(itemId);
                EquipItem(item);
                FlorestaSoundFX.Instance?.PlayDiamond();
                feedback = $"Desbloqueou e equipou: {item.displayName}!";
                SaveSystem.Save(Data);
                OnCustomizationChanged?.Invoke();
                return true;
            }

            // Se já está equipado, clicar novamente retira e volta ao padrão da categoria!
            if (GetEquippedId(item.category) == itemId)
            {
                string defaultId = GetDefaultId(item.category);
                SetEquippedId(item.category, defaultId);
                feedback = itemId == defaultId ? $"{item.displayName} já está ativo." : $"Retirou {item.displayName}.";
            }
            else
            {
                EquipItem(item);
                feedback = $"Equipou: {item.displayName}!";
            }

            FlorestaSoundFX.Instance?.PlayHappy();
            SaveSystem.Save(Data);
            OnCustomizationChanged?.Invoke();
            return true;
        }

        private void EquipItem(CustomizationItemDef item)
        {
            SetEquippedId(item.category, item.id);
        }

        private void SetEquippedId(CustomizationCategory cat, string id)
        {
            switch (cat)
            {
                case CustomizationCategory.Bicicletas:  Data.equippedBike = id; break;
                case CustomizationCategory.Cestas:      Data.equippedBasket = id; break;
                case CustomizationCategory.Acessorios:  Data.equippedAccessory = id; break;
                case CustomizationCategory.Personagem:  Data.equippedCharacter = id; break;
                case CustomizationCategory.Cachorrinho: Data.equippedPuppy = id; break;
            }
        }

        private static string GetDefaultId(CustomizationCategory cat)
        {
            switch (cat)
            {
                case CustomizationCategory.Bicicletas:  return "bike_default";
                case CustomizationCategory.Cestas:      return "basket_default";
                case CustomizationCategory.Acessorios:  return "acc_default";
                case CustomizationCategory.Personagem:  return "char_default";
                case CustomizationCategory.Cachorrinho: return "puppy_default";
                default: return "bike_default";
            }
        }

        public static string GetCollectibleDisplayName(CollectibleKind kind)
        {
            switch (kind)
            {
                case CollectibleKind.Estrelas:         return "Estrelas";
                case CollectibleKind.Flores:           return "Flores";
                case CollectibleKind.Sementes:         return "Sementes";
                case CollectibleKind.FolhasEspeciais:  return "Folhas Especiais";
                case CollectibleKind.Cogumelos:        return "Cogumelos";
                case CollectibleKind.PedrasBrilhantes: return "Pedras Brilhantes";
                case CollectibleKind.TesourosSecretos: return "Tesouros Secretos";
                default: return "Colecionável";
            }
        }
    }
}
