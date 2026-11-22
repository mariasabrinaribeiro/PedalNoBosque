using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    public enum GameLanguage
    {
        PT,
        EN,
        ES
    }

    /// <summary>
    /// Fase E — Sistema leve de Internacionalização (PT-BR, EN, ES) para PEDAL NO BOSQUE.
    /// Zero dependências externas, 100% offline e persistido no SaveSystem.
    /// </summary>
    public static class LocalizationManager
    {
        public static GameLanguage Current { get; private set; } = GameLanguage.PT;
        public static event System.Action OnLanguageChanged;

        private static readonly Dictionary<string, (string pt, string en, string es)> Table =
            new Dictionary<string, (string, string, string)>
            {
                { "brand_title",   ("PEDAL NO BOSQUE", "PEDAL IN THE WOODS", "PEDAL EN EL BOSQUE") },
                { "brand_slogan",  ("Pequenas entregas. Grandes descobertas.", "Small deliveries. Big discoveries.", "Pequenas entregas. Grandes descubrimientos.") },
                { "btn_play",      ("Jogar", "Play", "Jugar") },
                { "btn_parents",   ("Area dos Pais", "Parents Area", "Area de Padres") },
                { "btn_back",      ("Voltar", "Back", "Volver") },
                { "btn_stroll",    ("Passear", "Stroll", "Pasear") },
                { "btn_pedal",     ("Pedalar", "Pedal", "Pedalear") },
                { "btn_jump",      ("Pular", "Jump", "Saltar") },
                { "btn_deliver",   ("Entregar", "Deliver", "Entregar") },
                { "btn_book",      ("Livro", "Book", "Libro") },
                { "btn_workshop",  ("Oficina", "Workshop", "Taller") },
                { "btn_bell",      ("Sino", "Bell", "Timbre") },
                { "btn_close",     ("Fechar", "Close", "Cerrar") },
                { "parent_gate",   ("Verificacao para Adultos (Barreira Parental)", "Adult Verification (Parental Gate)", "Verificacion para Adultos (Control Parental)") },
                { "parent_info",   ("PEDAL NO BOSQUE e seguro para criancas:\n- 100% offline, sem coleta de dados pessoais (LGPD / COPPA).\n- Sem anuncios invasivos e sem compras reais ativas.\n- Progresso salvo apenas neste dispositivo.",
                                    "PEDAL IN THE WOODS is child-safe:\n- 100% offline, zero personal data collected (COPPA / GDPR).\n- No intrusive ads and no real-money purchases enabled.\n- Progress is saved locally on this device only.",
                                    "PEDAL EN EL BOSQUE es seguro para ninos:\n- 100% sin conexion, sin recopilacion de datos personales.\n- Sin anuncios invasivos ni compras reales activas.\n- El progreso se guarda solo en este dispositivo.") },
                { "btn_reset",     ("Reiniciar Progresso", "Reset Progress", "Reiniciar Progreso") },
                { "reset_done",    ("Progresso reiniciado com seguranca!", "Progress safely reset!", "Progreso reiniciado con seguridad!") }
            };

        public static void LoadFromSave(PlayerSaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.language)) { Current = GameLanguage.PT; return; }
            if (data.language == "en") Current = GameLanguage.EN;
            else if (data.language == "es") Current = GameLanguage.ES;
            else Current = GameLanguage.PT;
        }

        public static void SetLanguage(GameLanguage lang)
        {
            Current = lang;
            if (PlayerProgressManager.Instance != null && PlayerProgressManager.Instance.Data != null)
            {
                PlayerProgressManager.Instance.Data.language = lang == GameLanguage.EN ? "en" : (lang == GameLanguage.ES ? "es" : "pt");
                SaveSystem.Save(PlayerProgressManager.Instance.Data);
            }
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var entry)) return key;
            switch (Current)
            {
                case GameLanguage.EN: return entry.en;
                case GameLanguage.ES: return entry.es;
                default:              return entry.pt;
            }
        }
    }
}
