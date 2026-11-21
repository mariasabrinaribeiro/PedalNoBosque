# Auditoria Técnica e Plano Estratégico — **PEDAL NO BOSQUE**

> **Marca:** PEDAL NO BOSQUE — *Pequenas entregas. Grandes descobertas.*
> **Categoria:** Cozy Adventure / Casual Exploration / Family Game (Crianças e Famílias)
> **Engine:** Unity `2022.3.62f3` (2D URP/Built-in, Input System `1.7.0`, TextMeshPro `3.0.6`)
> **Data da Auditoria:** 2026-11-21

---

## 1. Diagnóstico Técnico do Projeto Atual

- **Compilação:** `Assembly-CSharp.dll` (`311.296 bytes`) compilada sem erros pelo Unity `2022.3.62f3`. Todos os scripts possuem `.meta` válidos.
- **Cena principal:** `Assets/Scenes/FlorestaBicicleta.unity` (`5.850 bytes`) contém apenas `Main Camera` (`orthographicSize = 5.5`) + `AudioListener`; toda a cena é instanciada em tempo de execução por `FlorestaSceneManager` via `[RuntimeInitializeOnLoadMethod]`.
- **Volume de código ativo (`Assets/Scripts/FlorestaBicicleta/`):** 19 scripts C# (~5.490 linhas).
- **Código legado/compartilhado (`Assets/Scripts/FlorestaTopDown/`):** 11 scripts (~2.705 linhas). `FlorestaTopDownSceneManager` está desativado (`TrySpawn()` retorna imediatamente), porém `FlorestaBicicleta` ainda depende de 5 arquivos desse namespace (`FoodTypes.cs`, `ParkSpriteFactory.cs`, `KitchenStation.cs`, `SlopeZone.cs`, `WaterColumn.cs`).
- **Assets ilustrados (`Assets/Resources/Floresta/`):** 13 PNGs (~15,5 MB em disco): `bg_floresta.png`, `girl_bike.png`, `girl_bike_topdown.png`, 7 animais (`cervo`, `coelho`, `esquilo`, `gato`, `lobo`, `quati`, `raposa`), `barraca_cozinha.png`, `coluna_agua.png` e `banco_praca.png`.
- **Configuração de projeto legado:** `ProjectSettings.asset` ainda aponta `productName: Girl power` (`bundleVersion: 1.11`) e `EditorBuildSettings.asset` ainda não inclui `FlorestaBicicleta.unity`. O repositório também mantém cenas legadas de projetos anteriores (`Sorvete`, `Donuts`, `aula 2`) e uma pasta duplicada `Sorvete atualizado 1\New Unity Project\`.

---

## 2. Etapas Efetivamente Concluídas vs. Incompletas

### Concluídas e Validadas
- **Etapa 1 — Física da Bicicleta, Rampas/Saltos e Obstáculos:** subidas, descidas, rampas com física aérea, pulo manual, obstáculos naturais e estrelas aéreas.

### Implementadas e Compiladas *(pendentes de validação completa em Play Mode)*
- **Etapa 2 — Geração Procedural Infinita (5 regiões diurnas ativas):** estrada infinita, regras de segurança para obstáculos/água/cozinha, reciclagem de segmentos, placas de `0.5 km` e marcador `km` na HUD. *(Falta a 6ª região: Bosque da Lua.)*
- **Etapa 3 — Ecossistema de Animais e Comportamentos:** 5 alimentos naturais, 7 novos animais com vozes sintetizadas e comportamentos especiais para Lobo e Gato.
- **Etapa 4 — Estação de Abastecimento e Mini-Preparo Opcional:** coleta rápida, Kit Rápido e mini-jogo opcional Gourmet de Sanduíche/Hot Dog.
- **Etapa 5 — Moradores da Vila e Missões Aleatórias:** 5 moradores com missões, estrelas (`★`) e serenata do Músico.
- **Refinamento Inicial — Vale das Águas & HUD:** 6 camadas de paralaxe, ponte redesenhada com pilares/margens/ilha/salgueiro com passarinho, peixe em pool (`LivingRiverFeature`) e HUD reduzida.
- **Etapa 6 — Ciclo Completo de Dia e Noite (5 Períodos) + 6ª Região `Bosque da Lua`:** compilada sem erros no Unity (`DayNightCycleController`, `BosqueDaLua`, assinaturas sonoras por região e remoção de `FindObjectsOfType` no `ThrowFood()`).
- **Etapa 7 — Livro dos Animais, Colecionáveis, Conquistas, Personalização e Save JSON:** implementada (`SaveSystem`, `PlayerProgressManager`, `TrailCollectibleItem`, `DiscoveryBookAndWorkshopUI`), sem pagamentos reais e 100% offline/sem PII.

### Incompletas / Ainda Não Iniciadas
- **Etapa 8:** Interatividade ambiental completa (flores abrindo ao passar, borboletas acompanhando a bike, cogumelos luminosos com partículas, grama inclinando, cachoeira com névoa/arco-íris).
- **Etapa 9:** Sistema de Surpresas e Segredos (raposa na toca, esquilo em árvores, árvore antiga com passagem, garrafa com mapa no rio, arco-íris secreto, filhote perdido, borboleta rara).
- **Etapa 10:** Vila dos Viajantes evolutiva + **Cachorrinho Reativo** (farejar segredos, latir para descobertas, animações próprias separadas do sprite estático da bicicleta).
- **Preparação Comercial & Internacionalização:** localização PT-BR/EN/ES, controle parental, tela de título/menu dedicada do *Pedal no Bosque*, otimização de texturas mobile e revisão de conformidade LGPD/COPPA.

---

## 3. Avaliação da Qualidade Visual Atual

### Pontos Fortes
- Ilustrações PNG de alta qualidade (`girl_bike.png`, `bg_floresta.png`, 7 animais iniciais, barraca, banco, bebedouro) com paleta acolhedora de livro ilustrado.
- Paralaxe em 6 camadas já estruturada (`0.18x` nuvens → `0.25x` montanhas → `0.48x` colinas/pinheiros/cachoeira → `0.78x` árvores próximas → `1.00x` trilha → `1.28x` primeiro plano).
- HUD recém-reduzida e harmonizada com tons naturais (madeira/sálvia/mel).

### Lacunas Críticas para Padrão Comercial
1. **Contraste estilístico entre PNGs pintados e sprites procedurais:** os novos animais, moradores, árvores de paralaxe, pontes e casinhas usam desenho geométrico via `Texture2D.SetPixel`. Para padrão comercial internacional, os elementos-chave do **Vale das Águas** (e depois das demais regiões) precisam evoluir para artes ilustradas coerentes com `bg_floresta.png` e `girl_bike.png`.
2. **Personagem e Cachorrinho acoplados em um único sprite (`girl_bike.png`):** hoje o cachorrinho não pode reagir de forma independente (abanar cauda, farejar, olhar para animais, pular na cesta) sem uma camada visual própria ou overlays animados.
3. **Fundo único (`bg_floresta.png`) compartilhado por todas as regiões:** mesmo com paralaxe, as 6 regiões precisam de variações visuais reais de montanhas/céu/vegetação de fundo para que entrar no *Vale das Águas*, *Montanhas do Vento* ou *Bosque da Lua* pareça uma viagem de verdade.
4. **Alinhamento fino em Play Mode:** a integração visual exata entre a pista desenhada em `bg_floresta.png` e os objetos do Plano 3 (ponte, rio, margens, ilhota) exige validação visual direta no Editor.

---

## 4. Avaliação da Arquitetura de Código

### Pontos Positivos
- Modularidade clara em `FlorestaBicicleta/` (gerador infinito, fábrica de sprites, áudio procedural, mini-jogo de cozinha, moradores e polimento ambiental desacoplados).
- Regras de segurança bem definidas no `InfiniteTrackGenerator` (zonas bloqueadas para obstáculos, garantias de água e cozinha, reciclagem de segmentos antigos).
- Pooling já adotado em `LivingRiverFeature` para peixes.

### Débitos Técnicos Identificados
1. **Acoplamento com namespace legado `FlorestaTopDown`:** 5 classes compartilhadas vivem na pasta antiga; convém extrair `FoodTypes`, `SlopeZone`, `KitchenStation`, `WaterColumn` e ícones comuns para um módulo compartilhado limpo sem quebrar cenas antigas.
2. **Código morto / duplicado:** `ParallaxLayer.cs` não é usado; `FlorestaSpriteFactory.cs` possui um enum `AnimalKind` duplicado (5 espécies vs. 14 em `ParkSpriteFactory.AnimalKind`).
3. **Consultas e alocações em runtime:**
   - `BikeGirlController.ThrowFood()` usa `FindObjectsOfType<ForestAnimal>()` e `FindObjectsOfType<VillageResident>()` (pode consultar diretamente as listas ativas do `InfiniteTrackGenerator`).
   - Partículas de coração/lágrima/celebração (`FallingParticle`) ainda criam e destroem GameObjects a cada disparo (candidatas a pool simples).
4. **Strings hardcoded em PT-BR:** textos de UI, pedidos e mensagens estão diretamente nos scripts; antes da internacionalização (Fase E), deverão migrar para uma tabela de localização (PT-BR, EN, ES).
5. **Ausência de camada de Save/Dados:** `score`, `starsCollected`, `maxDistanceKm` e descobertas ainda vivem apenas em memória.

---

## 5. Riscos Técnicos e Comerciais

| Tipo | Risco | Impacto | Mitigação |
| :--- | :--- | :--- | :--- |
| **Técnico** | Texturas PNG grandes em `Resources/Floresta/` (~15,5 MB em disco) sem perfil de compressão mobile (ASTC/ETC2) | Alto uso de RAM/VRAM em celulares de entrada | Configurar *Import Settings* por plataforma (max size 1024/2048 + ASTC 6x6) e evitar duplicação de texturas |
| **Técnico** | Construção 100% via código (`new GameObject`) dificulta ajuste fino visual no Editor | Iteração artística mais lenta | Manter prefabs/ScriptableObjects para regiões, animais e moradores na consolidação da Fase A/B |
| **Visual/Produto** | Mistura de arte ilustrada com sprites geométricos procedurais | Percepção de protótipo em vez de produto comercial | Fechar padrão visual no **Vale das Águas** antes de escalar para as demais regiões |
| **Comercial / Legal** | Conformidade infantil (**LGPD**, **COPPA**, **Google Play Families**, **Apple Kids**) | Rejeição nas lojas ou penalidades legais | Desativar coleta de PII/analytics invasivos, exigir *Parental Gate* (barreira parental) para qualquer link externo ou compra futura, zero *loot boxes* / pressão de compra |
| **Marca** | Conflito de nome ou domínio para **PEDAL NO BOSQUE** | Necessidade de rebranding caro | Pesquisar disponibilidade no INPI, Google Play, App Store e domínios antes do anúncio público |

---

## 6. Roadmap Revisado com Prioridades (Fases A–E)

1. **PRIORIDADE IMEDIATA — Fase B (Piloto Visual): Aprovação do `VALE DAS ÁGUAS`**
   - Validar em Play Mode no Unity a composição, ponte, margens, ilhota, salgueiro com passarinho, paralaxe e HUD.
   - Polir desalinhamentos e elevar a coesão artística (substituindo elementos geométricos críticos por arte ilustrada harmoniosa).
2. **Fase A — Consolidação do Core & Etapa 6 (Ciclo Dia/Noite + Bosque da Lua)**
   - Limpar débitos técnicos (pooling de partículas, remover `FindObjectsOfType`, adicionar `FlorestaBicicleta.unity` no Build Settings, criar tela inicial própria de *Pedal no Bosque*).
   - Implementar Ciclo de 5 períodos (Manhã, Tarde, Pôr do Sol, Noite com farol/vaga-lumes/Coruja, Madrugada) + 6ª região **Bosque da Lua** + ambiência sonora distinta por região.
   - Implementar **Etapa 7** (Save local JSON, Livro Ilustrado dos Animais, Conquistas e Personalização básica de bike/cesta).
3. **Fase C — Experiência, Vida Ambiental e Retenção (Etapas 8, 9 e 10)**
   - Replicar o padrão visual aprovado nas 6 regiões completas.
   - Implementar reações ambientais (flores abrindo, borboletas acompanhando, cachoeira com arco-íris), eventos secretos (raposa na toca, garrafa com mapa, árvore antiga) e **Vila Evolutiva + Cachorrinho Reativo**.
4. **Fase D — Arquitetura Comercial Ética**
   - Economia justa (itens básicos via progressão; loja integrada ao universo para cosméticos/expansões temáticas) + *Parental Gate* + revisão LGPD/COPPA/Families.
5. **Fase E — Lançamento Gradual e Expansão Internacional**
   - Internacionalização (PT-BR, EN, ES).
   - Lançamento focado primeiro na plataforma mais viável (**Google Play** em *Soft Launch* no Brasil) → validação de retenção D1/D7 → expansão para **Apple App Store** e avaliação de **Steam** (PC).

---

## 7. Plano de Preparação Comercial (Seguro para o Público Infantil)

- **Modelo Recomendado:** Gratuito para jogar (*Free-to-Start*) com **compras cosméticas e expansões opcionais** (nunca *pay-to-win*, nunca caixas surpresa/gacha, nunca temporizadores punitivos).
- **Categorias da Loja Futura (integrada à Vila dos Viajantes):** Bicicletas, Cestas, Roupas da personagem, Acessórios do cachorrinho, Decoração da casinha/jardim e Expansões de novas regiões.
- **Publicidade:** **Zero anúncios intersticiais/banners invasivos**. Avaliar no máximo anúncio recompensado 100% opcional apenas se compatível com SDK certificado *Google Play Families* e *Apple Kids* — ou manter o jogo **100% livre de anúncios** como diferencial de confiança para pais.
- **Proteção Infantil desde a Arquitetura:**
  - Zero coleta de dados pessoais identificáveis (PII) de crianças; desabilitar módulos de analytics não conformes.
  - **Parental Gate** (desafio simples para adultos) antes de qualquer compra, link externo ou configuração de loja.
  - Transparência total de preços e restauração de compras.

---

## 8. Simulação Financeira Ilustrativa (3 Cenários — 12 Meses)

> *Aviso: Projeções puramente ilustrativas para planejamento estratégico — não constituem garantia de resultado. Taxa estimada de loja: 15% (Google Play / App Store Small Business Program). Impostos/encargos estimados: ~10% sobre receita pós-loja.*

| Indicador (12 meses) | Conservador | Intermediário | Otimista |
| :--- | ---: | ---: | ---: |
| Downloads acumulados | 15.000 | 80.000 | 300.000 |
| Jogadores ativos mensais médios (MAU) | 2.000 | 12.000 | 45.000 |
| Taxa de conversão em compradores | 1,5% | 2,5% | 3,5% |
| Compradores totais | 225 | 2.000 | 10.500 |
| Gasto médio por comprador (ARPPU) | R\$ 18,00 | R\$ 24,00 | R\$ 32,00 |
| **Faturamento Bruto (IAP)** | **R\$ 4.050** | **R\$ 48.000** | **R\$ 336.000** |
| (-) Taxa das lojas (~15%) | -R\$ 608 | -R\$ 7.200 | -R\$ 50.400 |
| (-) Impostos estimados (~10%) | -R\$ 344 | -R\$ 4.080 | -R\$ 28.560 |
| **Receita Líquida** | **R\$ 3.098** | **R\$ 36.720** | **R\$ 257.040** |
| (-) Custos operacionais/divulgação/manutenção | -R\$ 2.500 | -R\$ 15.000 | -R\$ 65.000 |
| **Resultado Operacional Estimado** | **+R\$ 598** | **+R\$ 21.720** | **+R\$ 192.040** |

**Métrica-chave de sustentabilidade:** acompanhar **LTV** (valor gerado por jogador ao longo do tempo) vs. **CAC** (custo de aquisição). Em jogos familiares/infantis, o crescimento orgânico (vídeos curtos de 10–20s mostrando o mundo vivo + busca nas lojas + recomendação entre pais) é essencial para manter o CAC baixo.
