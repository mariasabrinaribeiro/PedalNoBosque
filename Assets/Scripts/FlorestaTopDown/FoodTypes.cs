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
        Sementes,
        SaladaFrutasBosque,
        CestaDaLua,
        FrutasSilvestres,
        Pao,
        GeleiaSilvestre,
        TortinhaDeMaca
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
                FoodType.SaladaFrutasBosque => "Salada do Bosque",
                FoodType.CestaDaLua => "Cesta da Lua",
                FoodType.FrutasSilvestres => "Frutas silvestres",
                FoodType.Pao => "Pão",
                FoodType.GeleiaSilvestre => "Geleia silvestre",
                FoodType.TortinhaDeMaca => "Tortinha de maçã",
                _ => "Lanche"
            };
        }

        public static bool NeedsPreparation(FoodType type)
        {
            return type == FoodType.Sanduiche || type == FoodType.HotDog
                || type == FoodType.SaladaFrutasBosque || type == FoodType.CestaDaLua
                || type == FoodType.GeleiaSilvestre || type == FoodType.TortinhaDeMaca;
        }

        public static bool IsNaturalFood(FoodType type)
        {
            return type == FoodType.Nozes
                || type == FoodType.Cenoura
                || type == FoodType.Maca
                || type == FoodType.Mel
                || type == FoodType.Sementes
                || type == FoodType.FrutasSilvestres;
        }
    }
}
