using System;
using UnityEngine;

/// <summary>
/// Data-driven modular ScriptableObject configuration for Baía do Avesso Character Archetypes.
/// Allows designers to swap mesh outfits + hair + archetype profile without duplicating character prefabs!
/// </summary>
[CreateAssetMenu(fileName = "ArchetypeProfile", menuName = "Baia do Avesso/Archetype Profile")]
public sealed class GirlPowerOrla3DArchetypeConfig : ScriptableObject
{
    public enum ArchetypeKind
    {
        Camponesa = 0,
        GarconeteFada = 1,
        Gaucho = 2,
        Espia = 3,
        Barbie = 4,
        Princesa = 5,
        Vovo = 6,
        KungFu = 7,
        Japones = 8,
        PraiaFutebol = 9,
        FutebolAmericano = 10
    }

    public enum MapZone
    {
        OrchardGarden = 0,   // Fundo / Lateral Verde (Horta cercada)
        CentralPromenade = 1, // Calçadão Central + Potes de Água
        PetRestaurant = 2,    // Área dos Quiosques e Restaurante Pet Friendly
        BeachAndSea = 3       // Areia e Mar (Praia / Futebol)
    }

    public enum PetKind
    {
        None = 0,
        Cachorro = 1,
        Gato = 2,
        Calopsita = 3
    }

    [Serializable]
    public struct ProfileData
    {
        public ArchetypeKind kind;
        public string displayName;
        public string fbxModelName;
        public MapZone homeZone;
        public bool isStaff;
        public string[] favoriteOrders;
        public float eatDurationSec;
        public int baseCoins;
        public float tipMultiplier;
        public PetKind preferredPet;
    }

    public ProfileData profile;

    /// <summary>Returns the canonical balanced profile matrix for all 11 Baía do Avesso archetypes.</summary>
    public static ProfileData GetDefaultProfile(ArchetypeKind kind)
    {
        switch (kind)
        {
            case ArchetypeKind.Camponesa:
                return new ProfileData
                {
                    kind = kind, displayName = "Camponesa", fbxModelName = "Char_Camponesa",
                    homeZone = MapZone.OrchardGarden, isStaff = true,
                    favoriteOrders = new[] { "Laranja", "Limão", "Manjericão", "Maçã" },
                    eatDurationSec = 0f, baseCoins = 0, tipMultiplier = 1f, preferredPet = PetKind.None
                };

            case ArchetypeKind.GarconeteFada:
                return new ProfileData
                {
                    kind = kind, displayName = "Garçonete Fada", fbxModelName = "Char_FairyWaitress",
                    homeZone = MapZone.PetRestaurant, isStaff = true,
                    favoriteOrders = new[] { "Entregar Pedidos", "Reabastecer Água" },
                    eatDurationSec = 0f, baseCoins = 0, tipMultiplier = 1f, preferredPet = PetKind.None
                };

            case ArchetypeKind.Gaucho:
                return new ProfileData
                {
                    kind = kind, displayName = "Gaúcho", fbxModelName = "Char_Gaucho",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Churrasco no Espeto", "Chimarrão Quentinho", "Prato Reforçado" },
                    eatDurationSec = 12f, baseCoins = 14, tipMultiplier = 1.25f, preferredPet = PetKind.Cachorro
                };

            case ArchetypeKind.Espia:
                return new ProfileData
                {
                    kind = kind, displayName = "Espiã", fbxModelName = "Char_Espia",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Expresso Duplo", "Energético Tropical", "Lanche Rápido" },
                    eatDurationSec = 5.5f, baseCoins = 13, tipMultiplier = 1.35f, preferredPet = PetKind.Gato
                };

            case ArchetypeKind.Barbie:
                return new ProfileData
                {
                    kind = kind, displayName = "Estilo Barbie", fbxModelName = "Char_Barbie",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Limonada Rosa", "Suco Laranja c/ Morango", "Doce Leve Berry" },
                    eatDurationSec = 10f, baseCoins = 14, tipMultiplier = 1.30f, preferredPet = PetKind.Calopsita
                };

            case ArchetypeKind.Princesa:
                return new ProfileData
                {
                    kind = kind, displayName = "Princesa", fbxModelName = "Char_Princesa",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Bolo Decorado", "Chá Gelado de Frutas", "Sobremesa Real" },
                    eatDurationSec = 13f, baseCoins = 16, tipMultiplier = 1.50f, preferredPet = PetKind.Gato
                };

            case ArchetypeKind.Vovo:
                return new ProfileData
                {
                    kind = kind, displayName = "Vovó", fbxModelName = "Char_Vovo",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Café Coado", "Bolo de Fubá", "Torta de Maçã" },
                    eatDurationSec = 18f, baseCoins = 15, tipMultiplier = 1.90f, preferredPet = PetKind.Calopsita
                };

            case ArchetypeKind.KungFu:
                return new ProfileData
                {
                    kind = kind, displayName = "Mestre Kung Fu", fbxModelName = "Char_KungFu",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Tigela Proteica", "Arroz c/ Carne & Gengibre", "Prato da Força" },
                    eatDurationSec = 9f, baseCoins = 15, tipMultiplier = 1.30f, preferredPet = PetKind.Cachorro
                };

            case ArchetypeKind.Japones:
                return new ProfileData
                {
                    kind = kind, displayName = "Estilo Japonês", fbxModelName = "Char_Japones",
                    homeZone = MapZone.CentralPromenade, isStaff = false,
                    favoriteOrders = new[] { "Combo de Sushi", "Temaki Especial", "Chá Verde Gelado" },
                    eatDurationSec = 11f, baseCoins = 16, tipMultiplier = 1.40f, preferredPet = PetKind.Gato
                };

            case ArchetypeKind.PraiaFutebol:
                return new ProfileData
                {
                    kind = kind, displayName = "Praia & Futebol", fbxModelName = "Char_FutebolPraia",
                    homeZone = MapZone.BeachAndSea, isStaff = false,
                    favoriteOrders = new[] { "Água de Coco", "Tigela de Açaí", "Isotônico Gelado" },
                    eatDurationSec = 8.5f, baseCoins = 12, tipMultiplier = 1.15f, preferredPet = PetKind.Cachorro
                };

            default: // FutebolAmericano
                return new ProfileData
                {
                    kind = ArchetypeKind.FutebolAmericano, displayName = "Futebol Americano", fbxModelName = "Char_FutebolAmericano",
                    homeZone = MapZone.BeachAndSea, isStaff = false,
                    favoriteOrders = new[] { "Sanduíche Grande", "Espetinho Duplo", "Açaí Turbo" },
                    eatDurationSec = 8.0f, baseCoins = 14, tipMultiplier = 1.25f, preferredPet = PetKind.Cachorro
                };
        }
    }

    public static string GetPetOrderLabel(PetKind pet)
    {
        switch (pet)
        {
            case PetKind.Cachorro: return "Petisco de Carne";
            case PetKind.Gato: return "Sachê de Peixe";
            case PetKind.Calopsita: return "Semente & Frutas";
            default: return "";
        }
    }
}
