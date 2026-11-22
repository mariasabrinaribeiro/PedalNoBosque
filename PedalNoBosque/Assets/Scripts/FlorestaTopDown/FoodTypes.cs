namespace FlorestaTopDown
{
    public enum FoodType
    {
        Refrigerante,
        Suco,
        Coxinha,
        Doces,
        Pipoca,
        Sanduiche,
        HotDog,
        // Alimentos naturais da floresta (Etapa 3)
        Nozes,
        Cenoura,
        Maca,
        Mel,
        Sementes
    }

    public static class FoodInfo
    {
        public static string GetDisplayName(FoodType type)
        {
            return type switch
            {
                FoodType.Refrigerante => "Refrigerante",
                FoodType.Suco         => "Suco",
                FoodType.Coxinha      => "Coxinha",
                FoodType.Doces        => "Doces",
                FoodType.Pipoca       => "Pipoca",
                FoodType.Sanduiche    => "Sanduíche",
                FoodType.HotDog       => "Hot Dog",
                FoodType.Nozes        => "Nozes",
                FoodType.Cenoura      => "Cenoura",
                FoodType.Maca         => "Maçã",
                FoodType.Mel          => "Mel",
                FoodType.Sementes     => "Sementes",
                _ => "Lanche"
            };
        }

        public static bool NeedsPreparation(FoodType type)
        {
            return type == FoodType.Sanduiche || type == FoodType.HotDog;
        }

        public static bool IsNaturalFood(FoodType type)
        {
            return type == FoodType.Nozes
                || type == FoodType.Cenoura
                || type == FoodType.Maca
                || type == FoodType.Mel
                || type == FoodType.Sementes;
        }
    }
}
