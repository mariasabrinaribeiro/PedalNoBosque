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
        HotDog
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
                _ => "Lanche"
            };
        }

        public static bool NeedsPreparation(FoodType type)
        {
            return type == FoodType.Sanduiche || type == FoodType.HotDog;
        }
    }
}
