using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlorestaBicicleta
{
    [Serializable]
    public class CollectibleSaveEntry
    {
        public string key;
        public int count;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int saveVersion = 1;
        public string language = "pt"; // "pt", "en", "es"
        public int starsAvailable = 0;
        public int totalStarsEarned = 0;
        public float bestDistanceKm = 0f;
        public int totalDeliveries = 0;
        public bool squirrelSecretFound = false;
        public bool owlMet = false;

        // Impacto do jogador no mundo vivo e evolução da Vila dos Viajantes (Etapa 10)
        public int flowersWateredCount = 0;
        public int bridgesRepairedCount = 0;
        public bool castorBridgeRepaired = false;
        public bool castorBridgeFollowupScheduled = false;
        public int hedgehogStoryStage = 0;
        public int birdsRescuedCount = 0;
        public int villageEvolutionLevel = 0;

        public int balanceSkillXp = 0;
        public int pedalingSkillXp = 0;
        public int explorationSkillXp = 0;
        public int puppyScentSkillXp = 0;
        public int gardeningSkillXp = 0;
        public int cookingSkillXp = 0;
        public int pedalingDistanceSteps = 0;

        public List<string> discoveredAnimals = new List<string>();
        public List<string> visitedBiomes = new List<string>();
        public List<CollectibleSaveEntry> collectibles = new List<CollectibleSaveEntry>();
        public List<string> unlockedAchievements = new List<string>();
        public List<string> unlockedCustomizations = new List<string>();
        public List<string> unlockedRecipes = new List<string>();

        public string equippedBike = "bike_default";
        public string equippedBasket = "basket_default";
        public string equippedAccessory = "acc_default";
        public string equippedCharacter = "char_default";
        public string equippedPuppy = "puppy_default";

        // Configurações persistentes e estado de sessão para o fluxo Continuar / Menu
        public float musicVolume = 0.80f;
        public float sfxVolume = 0.85f;
        public bool gentleControlAssist = true;
        public bool hasActiveSession = false;
        public float lastSavedPositionX = 0f;
        public int completedChallengesCount = 0;
    }

    /// <summary>
    /// Etapa 7 — Sistema de Persistência Local em JSON (100% offline, zero dados pessoais/PII):
    /// Inclui versionamento do formato, validação dos dados carregados, backup automático (.bak)
    /// e recuperação segura contra arquivos inexistentes ou corrompidos.
    /// </summary>
    public static class SaveSystem
    {
        private const int CurrentSaveVersion = 1;
        private static string SaveDirectory
        {
            get
            {
#if UNITY_EDITOR
                if (VerticalSliceValidator.TestDirectory != null) return VerticalSliceValidator.TestDirectory;
#endif
                return Application.persistentDataPath;
            }
        }

        private static string PrimaryPath => Path.Combine(SaveDirectory, "pedal_no_bosque_save.json");
        private static string BackupPath => Path.Combine(SaveDirectory, "pedal_no_bosque_save.bak");

        public static PlayerSaveData Load()
        {
            PlayerSaveData loaded = TryReadFile(PrimaryPath);
            if (loaded != null)
            {
                Sanitize(loaded);
                return loaded;
            }

            // Se o arquivo principal estiver ausente ou corrompido, tenta recuperar do backup
            loaded = TryReadFile(BackupPath);
            if (loaded != null)
            {
                Sanitize(loaded);
                WriteSave(loaded, preservePrevious: false);
                return loaded;
            }

            PlayerSaveData fresh = CreateDefault();
            Save(fresh);
            return fresh;
        }

        public static bool Save(PlayerSaveData data) => WriteSave(data, preservePrevious: true);

        public static bool HasSavedProgress(PlayerSaveData data)
        {
            if (data == null) return false;
            return data.hasActiveSession
                || data.totalDeliveries > 0
                || data.bestDistanceKm > 0.01f
                || data.totalStarsEarned > 0
                || data.completedChallengesCount > 0
                || data.bridgesRepairedCount > 0
                || data.flowersWateredCount > 0;
        }

        private static bool WriteSave(PlayerSaveData data, bool preservePrevious)
        {
            if (data == null) return false;
            Sanitize(data);

            try
            {
                string dir = Path.GetDirectoryName(PrimaryPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(data, true);

                // Cria backup do arquivo anterior válido antes de sobrescrever
                if (preservePrevious && File.Exists(PrimaryPath))
                {
                    File.Copy(PrimaryPath, BackupPath, true);
                }

                File.WriteAllText(PrimaryPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PedalNoBosque.SaveSystem] Erro ao salvar progresso: {ex.Message}");
                return false;
            }
        }

        private static PlayerSaveData TryReadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                if (data == null || data.saveVersion <= 0) return null;
                return data;
            }
            catch
            {
                return null;
            }
        }

        public static PlayerSaveData CreateDefault()
        {
            PlayerSaveData d = new PlayerSaveData { saveVersion = CurrentSaveVersion };
            Sanitize(d);
            return d;
        }

        private static void Sanitize(PlayerSaveData data)
        {
            data.saveVersion = CurrentSaveVersion;
            if (data.starsAvailable < 0) data.starsAvailable = 0;
            if (data.totalStarsEarned < data.starsAvailable) data.totalStarsEarned = data.starsAvailable;
            if (data.bestDistanceKm < 0f) data.bestDistanceKm = 0f;
            if (data.totalDeliveries < 0) data.totalDeliveries = 0;

            if (data.discoveredAnimals == null) data.discoveredAnimals = new List<string>();
            if (data.visitedBiomes == null) data.visitedBiomes = new List<string>();
            if (data.collectibles == null) data.collectibles = new List<CollectibleSaveEntry>();
            if (data.unlockedAchievements == null) data.unlockedAchievements = new List<string>();
            if (data.unlockedCustomizations == null) data.unlockedCustomizations = new List<string>();
            if (data.unlockedRecipes == null) data.unlockedRecipes = new List<string>();

            // Garante itens gratuitos iniciais sempre desbloqueados
            string[] defaults = { "bike_default", "basket_default", "acc_default", "char_default", "puppy_default" };
            for (int i = 0; i < defaults.Length; i++)
            {
                if (!data.unlockedCustomizations.Contains(defaults[i]))
                    data.unlockedCustomizations.Add(defaults[i]);
            }

            if (string.IsNullOrEmpty(data.equippedBike)) data.equippedBike = "bike_default";
            if (string.IsNullOrEmpty(data.equippedBasket)) data.equippedBasket = "basket_default";
            if (string.IsNullOrEmpty(data.equippedAccessory)) data.equippedAccessory = "acc_default";
            if (string.IsNullOrEmpty(data.equippedCharacter)) data.equippedCharacter = "char_default";
            if (string.IsNullOrEmpty(data.equippedPuppy)) data.equippedPuppy = "puppy_default";

            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
            if (data.lastSavedPositionX < 0f) data.lastSavedPositionX = 0f;
            if (data.completedChallengesCount < 0) data.completedChallengesCount = 0;
        }
    }
}
