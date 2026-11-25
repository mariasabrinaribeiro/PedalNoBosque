# Baía do Avesso — Plano Mestre Completo de Produção

> **Objetivo:** Transformar *Baía do Avesso* (Unity 3D — `Assets/GirlPower/Orla3D`) em uma experiência costeira completa, viva e comercial, com arte 3D gerada no **Blender 5.3**, front-end inspirado em *Pedal no Bosque* e *Cozinha do Bosque*, mecânicas táteis, progressão econômica, esportes de praia, restaurante/quiosque com batata frita, horta, pesca e versionamento contínuo no **GitHub** (`origin/main`).

---

## Checklist Mestre de Etapas (Revisado e Concluído)

### Etapa 1 — Arte 3D no Blender 5.3 (`build_baia_mega_life_assets.py` → `.fbx`)
- [x] `Skateboard.fbx` — Skate cruiser pastel com trucks e rodinhas
- [x] `Scooter.fbx` — Patinete costeiro estilizado com guidão
- [x] `RollerSkates.fbx` — Par de patins no pé com rodinhas coloridas
- [x] `HawaiianCanoe.fbx` — Canoa Havaiana (Va'a) com flutuador lateral e remo
- [x] `BeachTennisCourt.fbx` — Quadra de Beach Tennis com rede, marcações, raquetes e bola
- [x] `CampingTent.fbx` — Barraca de acampamento familiar na areia + cooler + esteira + fogueira
- [x] `RestaurantKioskTable.fbx` — Mesa de quiosque restaurante com guarda-sol, bancos e cestinha de **Batata Frita** + bebidas
- [x] `RestaurantFriesKitchen.fbx` — Quiosque Restaurante "Batata & Mar" com balcão, fritadeira e cardápio
- [x] `MarketStall_Doces.fbx` — Barraca de Doces, Cupcakes & Guloseimas
- [x] `MarketStall_SucosRefri.fbx` — Barraca de Água, Refri & Sucos Naturais
- [x] `GardenPlanterBed.fbx` — Canteiros de plantação/horta (Manjericão, Hortelã, Alecrim, Morango)
- [x] `FishingPierDock.fbx` — Píer de pesca restaurável com vara e balde de peixes
- [x] `VillageNoticeBoard.fbx` — Mural de Avisos da Vila no centro do calçadão
- [x] `WardrobeHatProp.fbx` — Chapéu de palha + óculos escuros da protagonista
- [x] `PetBandanaProp.fbx` — Bandana/coleira estilosa para o Pipoca
- [x] `FishProp.fbx` — Peixe tropical 3D visível na água rasa e na pescaria

### Etapa 2 — Front-End Inspirado nas Referências (*Pedal no Bosque* & *Cozinha do Bosque*)
- [x] **Tela Inicial estilo *Pedal no Bosque*:** Layout alinhado à esquerda sobre a câmera viva da baía, título em destaque, subtítulo itálico, botão dourado `▶ Jogar`, verde-menta `Continuar`, `Configurações` e `Sair`, chips de idioma `PT | EN | ES`, botão `Capítulos` e faixa de estatísticas (`Moedas | Entregas | Estrelas | Prestígio`).
- [x] **Tela de Cardápio Ilustrado estilo *Cozinha do Bosque*:** Modal creme arredondado com título verde-mar, botão `X` coral no topo direito, grade de cartões quadrados ilustrados (fileira menta + fileira creme) e abas/pílulas coloridas abaixo cobrindo:
  - **Roupas & Acessórios** (cores de blusa, chapéu de palha, óculos escuros, avental, bandana do Pipoca)
  - **Sorvetes** (Limão c/ Manjericão, Morango c/ Hortelã, Baunilha, Trio da Festa)
  - **Água, Refri & Sucos** (Água Mineral, Água de Coco, Refri Guaraná, Refri Laranja, Suco de Limão, Suco de Morango)
  - **Doces** (Brigadeiro, Cocada, Cupcake, Picolé, Tortinha)
  - **Artesanatos** (Colar de Conchas, Pulseira, Mini Quadro, Filtro dos Sonhos)
  - **Sushi** (Combinado Salmão, Hot Roll, Temaki, Sashimi Fresco)
  - **Comida & Restaurante** (Porção de Batata Frita, Batata com Cheddar, Pastel, Sanduíche, Peixe Grelhado, Tigela de Açaí)
  - **Upgrades, Receitas & Kit Rápido**

### Etapa 3 — Locomoção Radical & Atividades de Praia
- [x] Alternância rápida de locomoção (`[V]` ou botão no HUD): **A Pé**, **Skate**, **Patinete** e **Patins no Pé** (cada um com modelo Blender 3D, velocidade própria, inclinação em curva e partículas/som).
- [x] Atividades na água: **Surfar na Prancha**, **Nadar na Água** (braçadas + anéis de espuma) e **Remar de Canoa Havaiana**.
- [x] **Beach Tennis:** Minijogo de rebatida/rally na quadra da areia rendendo moedas e prestígio.
- [x] **Acampamento Familiar & Comércio Vivo:** Famílias reunidas nas barracas de camping, cadeiras de praia e mesas de restaurante servidas pela protagonista.

### Etapa 4 — Game Feel ("Juice"), Áudio Dinâmico & Minigames Táteis
- [x] **Juice:** Poeira ao correr, pegadas na areia, tilt em curvas, *pop* elástico (`120% -> 100%`) em colheita/pintura/reforma, *screen shake* sutil e **moedas voando fisicamente** até o contador no HUD com som metálico crescente.
- [x] **Áudio Dinâmico:** Volume das ondas reativo à distância do mar, gaivotas distantes, música adaptativa acelerando quando o tempo está baixo, SFX variados (passos por superfície, papel de pedido, pincel/rolo, fritura, skate/patins, pesca, latido do Pipoca).
- [x] **Fatiar & Macerar:** Minigame rítmico de arrastar/cortar limão, macerar manjericão ou fritar batata para qualidade **"PERFEITO!"** (gorjeta extra).
- [x] **Equilíbrio na Bandeja:** Barra de equilíbrio ao carregar pedidos de restaurante/empilhados correndo com `Shift`.
- [x] **Pintura Manual com Rolo (`[P]`):** Tela rápida interativa onde o jogador passa o rolo de tinta na parede da casa e ganha bônus por cobertura completa + Prestígio.

### Etapa 5 — Pipoca Ativo, Horta, Pescaria, NPCs Vivos, Progressão & Save JSON
- [x] **Pipoca Mecânico:** `[B]` fareja ingredientes/tesouros na areia na maré baixa; espanta caranguejos/gaivotas ladrões perto do carrinho e da horta; `[K]` faz truques (*Sentar, Dar a Pata, Girar*) para recuperar paciência de clientes e ganhar gorjeta.
- [x] **Horta & Cultivo:** Canteiros para plantar Manjericão, Hortelã, Alecrim e Morango; crescem com a mudança de horário (`Manhã -> Fim de Tarde`); regar/adubar produz versão **Orgânica (2x valor)**.
- [x] **Pescaria no Píer:** Lançar linha e fisgar no tempo certo para pescar peixes frescos usados em Sushi e Restaurante.
- [x] **Restaurante & Mesas do Quiosque:** Clientes sentados pedem Batata Frita, Bebidas, Sushi e Sorvetes; entregar gera moedas, gorjeta e prestígio.
- [x] **NPCs & Fauna Vivos:** Moradores passeando, olhando vitrines, acenando para o jogador; gaivotas na areia levantam voo quando você corre perto; peixes nadando no raso.
- [x] **Economia, Reconstrução, Mural de Avisos, Festivais & Receitas:**
  - Upgrades (Tênis velozes, Mochila maior, Carrinho expandido, Reconstrução do Píer/Bancos/Luzes de cordão) que aumentam **Prestígio da Baía** e atraem **Clientes VIP (2x pagamento)**.
  - **Caderno de Receitas** desbloqueável.
  - **Mural de Avisos** com missões diárias dos moradores.
  - **Festivais Sazonais** (*Luau na Praia*, *Feira de Artesanato*, *Torneio de Pesca*).
  - **Personalização Profunda:** Telhado, vasos na janela, padrão do toldo, fairy lights, chapéu, óculos, avental e bandana do Pipoca.
  - **Onboarding Invisível:** Pan de câmera suave de 2s no início do Cap. 1 mostrando a Florista (`!`) e o Carrinho.
  - **Auto-Save JSON + PlayerPrefs** persistindo todo o progresso.

### Etapa 6 — Integração na Cena, Verificação e Push no GitHub
- [x] Integrar todos os novos FBX do Blender e sistemas em `GirlPowerOrla3DSceneBuilder.cs`.
- [x] Compilar runtime + editor com **0 erros**.
- [x] Atualizar este documento marcando todas as etapas concluídas.
- [x] Fazer commit limpo e **push para o GitHub (`origin main`)**.

---

### Etapa 7 — Detetive Pipoca vs. Ladrões de Rua & Polícia Costeira (Derrota e Vitória Principal)
- [x] **Modelagem Blender 5.3 (`build_baia_detective_assets.py`):** `Char_Thief.fbx` (ladrão com máscara e saco), `Char_PoliceOfficer.fbx` (policial com uniforme e estrela dourada), `PolicePatrolCar.fbx` (viatura costeira com giroflex) e `DetectiveHatPipoca.fbx` (chapéu de detetive do Pipoca).
- [x] **Ladrões de Rua nas Feiras e Restaurantes (`GirlPowerOrla3DThiefDetective.cs`):** Ladrões infiltram-se no calçadão para furtar as barracas, o quiosque e o restaurante *Batata & Mar*.
- [x] **5 Sinais Expressivos do Detetive Pipoca:** Pipoca veste o chapéu de detetive, corre até o suspeito e dá os sinais progressivos: **Latir**, **Rolar no chão**, **Morder o ladrão** (atrasando o furto em 45%), **Se Chacoalhar** e **Chorar/Ganir** quando o roubo está prestes a acontecer.
- [x] **Ação dos Trabalhadores + Chamar a Polícia com Antecedência (`[L]` / `🚓 Chamar Polícia 190`):** Os trabalhadores alertam a orla, a Viatura e o Policial chegam com som de sirene, prendem o ladrão antes do furto, concedem **Pontos de Detetive + Moedas + Prestígio** e deixam o **Pipoca super feliz** dançando!
- [x] **Vitória & Derrota:** Evitar **3 furtos com antecedência** consagra o jogador e o Pipoca como **Vencedores da Baía do Avesso** (tela de vitória com confetes e 3 estrelas); deixar **3 furtos** ocorrerem dispara **Game Over** com tentativa imediata.

---

### Etapa 8 — Correções da Auditoria Quadro a Quadro de Vídeo (QA Final)
- [x] **Eliminação do retângulo branco translúcido e Z-fighting:** Maré alta limitada à linha da areia úmida (`+0.06` Y, `-0.95` Z), `Offset 1, 1` no oceano, `_CurveStrength` unificado (`0.0008`), e `AC_BlobShadow.mat` serializado diretamente.
- [x] **Fim do vazio cinza e limites físicos do mapa:** Extensão de calçada/gramado sul (`z = -8..-26`) com cercas de madeira, arbustos e pedras costeiras nas bordas laterais + bounds ajustados.
- [x] **Textos de diálogo e tela de Derrota blindados + Retrato 2D:** `GUI.color = Color.white` explícito, guarda de frame ao abrir diálogo (`openedFrame`), retrato ilustrado 2D da Florista/Nina/Marina/Bia/Pavilhão, texto de Derrota sempre legível e alinhado à fase atual.
- [x] **Inventário da Horta sincronizado + Retry íntegro:** Contador de folhas do HUD unificado (`BasilLeavesCount + Data.organicHerbs`) e `RestartCurrentChapter` restaurando moedas iniciais do capítulo.
- [x] **Locomoção suave, animação do patinete e câmera com horizonte:** Aceleração/desaceleração suaves, arrasto de 46% do patinete/skate na areia fofa, desmontagem automática na linha do mar, animação de impulso com o pé + inclinação do guidão nas curvas, e `Vector3.SmoothDamp` a `31.5°` (FOV `44°`) exibindo céu e horizonte.
- [x] **HUD limpo, Cardápio sem cortes, Horta praiana e letreiros padronizados:** Removidos slots vazios e `GIRL POWER` roxo (trocado para `SORVETES DA BAÍA` em verde-mar pastel), alinhados `BATATA & MAR` e `FLORES DA BAÍA`, abas largas no Cardápio com feedback de compra (`x1`, `-Xm`, som), botão `Regar` e verbo dinâmico de preparo (nunca mais "Fritar Sorvete"), além da barra de progresso visual ao montar o sorvete.

---

### Etapa 9 — Direção Visual Hero, Água Estilizada por Profundidade (Depth-Buffer), Câmera Limpa e HUD Unificado
- [x] **Hero Carrinho de Sorvete & Vegetação Variada no Blender 5.3 (`build_baia_hero_polish.py`):** Novo `IceCreamCart.fbx` hero com guarda-sol listrado rosa e branco escalopado, vitrine aberta exibindo as 4 cubas pastéis de sorvete, rodas vintage raiadas com cubo rosa, porta-casquinha dourado e placa frontal `"SORVETES"`, mais `CoastalVegetationA.fbx` e `CoastalVegetationB.fbx` em verde-costeiro suave.
- [x] **Água Estilizada por Profundidade (`OrlaACNHOcean.shader`) + Fog Linear (`35–85`):** Ativado `DepthTextureMode.Depth` na câmera (`CreateCamera` + `GirlPowerOrla3DCameraPost`); shader lê `_CameraDepthTexture` para criar degradê limpo Turquesa Raso → Azul Petróleo Fundo (`_DepthDistance = 2.5`), espuma animada orgânica concentrada **apenas** na margem, pedras e estacas (`_FoamDistance = 0.65`, sem pontilhado branco no mar aberto), ondulação suave (`0.055`), sombra nítida e rastro de espuma branca sob a prancha de surf.
- [x] **Câmera Elevada & Corredor Visual Sem Telhados:** Câmera ajustada (`offset = (0, 8.2, -9.6)`, `pitch = 39.5°`, `FOV = 42°`) com avanço visual à frente da protagonista; `CoastalHouse` reposicionadas fora da linha da lente (`z = -12.8/-14.2`, escala `0.82`) e coqueiros afastados da entrada do pavilhão e do píer de pesca; meios-fios contínuos delimitando calçadão, jardim e areia; paleta costeira calma (creme `#EFE5D2`, madeira clara, verdes dessaturados, rosa vibrante `#F26392` reservado para Protagonista + Carrinho).
- [x] **HUD Unificado & Cardápio Simplificado (6 Cards):** Todas as ações contextuais `[E]` ancoradas na **mesma região fixa inferior-central** com o único botão em cor de destaque; barra secundária do Detetive Pipoca oculta fora de alertas de furto; `GirlPowerOrla3DCardapioUI` redesenhado para **6 cartões grandes e legíveis (2×3)** por aba com botões de categoria neutros e destaque exclusivo na aba ativa.

---

### Etapa 10 — Acabamento Comercial: Texturas Pintadas, 36 Ícones Exclusivos, Personagens Articulados, PT/EN/ES, Área dos Pais & Build Multiplataforma
- [x] **6 Texturas Pintadas Costeiras Sem Emenda (`build_baia_textures_and_icons.py` + `OrlaACNHCozy.shader`):** Geradas `Tex_WalkwayTiles.png`, `Tex_BeachSand.png`, `Tex_CoastalGrass.png`, `Tex_WoodPlanks.png`, `Tex_CobbleFeira.png` e `Tex_StripedAwning.png` com suporte a mapeamento world-space (`_UseWorldUV` / `_TileScale`) aplicado automaticamente no calçadão, areia, grama, píer, bancos e barracas.
- [x] **36 Ícones Ilustrados Individuais no Cardápio (`Resources/CardapioIcons/*.png`):** Cada um dos 36 itens das 6 abas (Comidas, Bebidas, Sorvetes & Doces, Sushi & Mar, Moda & Esportes, Artesanatos) possui agora sua própria ilustração exclusiva em 128×128 carregada com cache em `GirlPowerOrla3DCardapioUI.cs`.
- [x] **Personagens Articulados no Blender 5.3 (`build_baia_rigged_characters.py`):** `Char_Protagonist.fbx` e `PetDog.fbx` (Pipoca) reconstruídos com hierarquia de pivôs nos ombros, quadris, pescoço, olhos, rabo e orelhas — permitindo braços e pernas balançando ao andar/correr, piscar de olhos natural, impulso no patinete/skate, braçadas no nado/canoa e Pipoca abanando o rabo, balançando orelhas e galopando.
- [x] **Tradução PT / EN / ES + Área dos Pais + Bossa Nova Costeira:** Criado `GirlPowerOrla3DLocalization.cs` integrado aos chips `PT | EN | ES`; adicionada **Área dos Pais** na tela inicial (`Modo Livre Infantil sem derrota` + `Limite de Tempo de Sessão` com tela de descanso); trilha evoluída para **Bossa Nova costeira** (violão nylon sincopado Cmaj7–Am7–Dm7–G7, baixo acústico e ganzá/shaker); eliminada concorrência dupla de câmera no `GameFlow`.
- [x] **Ferramentas de Build 1-Clique (`GirlPowerOrla3DBuildTools.cs`) & Build Settings:** Cena 3D promovida para **Index 0** em `EditorBuildSettings.asset` + menu **Girl Power → Build Windows PC (.exe) / Build WebGL / Build Android (.apk)**.

---

### Etapa 11 — Identidade Original (Garçonetes Fadas), Câmera Aberta, Café Entrável sem Loading, Walk Cycle Sincronizado & Nitidez MSAA
- [x] **Modelos Originais no Blender 5.3 (`build_baia_magical_coast.py`):** Criados `Char_FairyWaitress.fbx` (proporção mais esguia e original, asas articuladas `Fairy_WingL`/`Fairy_WingR`, coroa de flores e bandeja tropical), `CoastalCafe_Base.fbx` + `CoastalCafe_RoofFade.fbx` (**Café das Fadas** com porta aberta e interior decorado com vitrine de bolos, cafeteira, tapete e mesas), `CoastalFacadeRow.fbx` (prédios costeiros coloridos com varandas ao fundo), `GlowingMushroomCluster.fbx`, `FloatingLantern.fbx` e `FlowerLampPost.fbx`.
- [x] **Câmera Ampla, Corredores Alargados & HUD nos Cantos:** Câmera recuada e inclinada para o horizonte (`offset = (0, 9.2, -14.2)`, `pitch = 32.5°`, `FOV = 48°`), calçadão sul ampliado sem Z-fighting até o Café das Fadas (`minWorldZ = -10.8`), e alerta `"Polícia (190) [L]"` movido do centro para cartão compacto semi-transparente no canto superior direito.
- [x] **Walk Cycle Sincronizado + Fadas Flutuantes + Turistas com Waypoints:** `GirlPowerOrla3DCharacterAnimator` sincroniza a cadência dos passos diretamente com a velocidade real no chão (sem patinar), com *bobbing* vertical e inclinação ao correr; `GirlPowerOrla3DNpcAI` anima **3 Garçonetes Fadas** flutuando entre o Café, Quiosque e mesas com batida rápida de asas e rastro de brilho mágico, turistas seguindo rotas de waypoints com paradas de ~5s, lanternas flutuantes e barquinhos deslizando no horizonte.
- [x] **Interior Entrável Sem Tela de Carregamento (`GirlPowerOrla3DCafeRoofFade.cs`):** Ao cruzar a porta do **Café das Fadas**, o teto some suavemente revelando o interior decorado; fade da casa principal restrito somente ao material `AC_HouseRoof`.
- [x] **Paleta Própria Pôr do Sol Dourado-Rosado & Nitidez MSAA 4x:** Areia dourada-rosada (`#F0CFA4`), mar azul-turquesa vibrante (`#38D4D2`/`#188BA3`), decks de madeira escura (`#6E442A`), oceano blindado contra depth buffer zerado, `MSAA 4x` e `AnisotropicFiltering.ForceEnable` ativados em `GirlPowerOrla3DCameraPost`.

---

### Etapa 12 — Arquitetura Modular Otimizada: 10 Arquétipos Harmonizados, Cabelo ao Vento (GPU), Restaurante Pet Friendly (Calopsita/Cão/Gato) & Potes de Água
- [x] **GPU Vertex Animation de Cabelo (`OrlaACNHHairWind.shader`):** Todos os cabelos compridos dos personagens balançam suavemente com a brisa da praia diretamente na GPU (custo ~0 de CPU), usando a mesma iluminação `LightingCozyACNH` do cenário para harmonia total.
- [x] **13 Novos Modelos Harmonizados no Blender 5.3 (`build_baia_modular_archetypes.py`):** Base modular única exportada 100% em pé para `Char_Camponesa.fbx`, `Char_Gaucho.fbx`, `Char_Espia.fbx`, `Char_Barbie.fbx`, `Char_Princesa.fbx`, `Char_Vovo.fbx`, `Char_KungFu.fbx`, `Char_Japones.fbx`, `Char_FutebolPraia.fbx`, `Char_FutebolAmericano.fbx`, além de `Pet_Calopsita.fbx` (com asas articuladas), `PetWaterBowl.fbx` (com malha de líquido dinâmica) e `OrchardFruitTree.fbx`.
- [x] **Configuração Modular `ScriptableObject` (`GirlPowerOrla3DArchetypeConfig.cs`):** Matriz centralizada definindo silhueta, zona do mapa, pratos favoritos (Churrasco/Chimarrão, Expresso Duplo, Limonada Rosa, Bolo Decorado, Café Coado/Fubá, Tigela Proteica, Combo Sushi, Açaí/Sanduíche), ritmo de refeição (Espiã rápida, Vovó lenta com gorjeta 1.9x) e pet acompanhante.
- [x] **Restaurante Pet Friendly, Potes de Água & Horta da Camponesa (`GirlPowerOrla3DPetFriendlySystem.cs`):**
  - **Camponesa:** Colhe Laranja, Limão, Manjericão e Maçã na horta lateral e abastece a cozinha automaticamente.
  - **Balões Duplos de Pedido:** Clientes acompanhados de Cachorro, Gato ou **Calopsita voando/pousada** abrem simultaneamente o balão humano + o balão do pet (*Petisco de Carne*, *Sachê de Peixe*, *Semente & Frutas*).
  - **5 Potes de Água & Reputação Pet:** Indicador visual dinâmico (Cheio azul → Metade âmbar → Vazio vermelho pulsante); pet sedento perto de pote vazio desconta `-5 Reputação`; Protagonista (`[E]`) ou Garçonete Fada reabastecem.
- [x] **4 Zonas Lógicas no Mapa (`GirlPowerOrla3DSceneBuilder.cs`):** Fundo Verde/Pomar (Camponesa), Calçadão Central (Gaúcho, Espiã, Barbie, Princesa, Vovó, Kung Fu, Japonês + 5 Potes de Água), Restaurante Pet Friendly (Mesas largas + Fadas) e Areia/Mar (Praia/Futebol/Futebol Americano).


---

### Etapa 13 — Controles Mobile em 3 Camadas Exclusivas, HUD Minimalista 15%, Feedback Diegético por Ícones, Andar com Contrarrotação & Paleta 3 Cores
- [x] **Sistema de Toque Mobile Exclusivo em 3 Camadas (`GirlPowerOrla3DMobileTouchController.cs` + `GirlPowerOrla3DTouchTarget.cs`):** Funciona em Mobile Touch (`EnhancedTouch`) e Mouse (Editor/WebGL) sem nunca misturar câmera com movimentação ou arraste:
  - **Camada 1 — Arrastar no Cenário Vazio = Pan Suave da Câmera + Inércia + Pinça com 2 Dedos:** Desliza o mapa suavemente com amortecimento e travas nas bordas da cena; zoom com pinça (ou scroll) limitado entre `0.75–1.62` para nunca encostar no chão.
  - **Camada 2 — Toque Simples no Chão = Tap-to-Move da Protagonista:** Envia a personagem suavemente ao ponto tocado com anel circular turquesa expansivo confirmando o toque (`Indicador_Toque_Chao`).
  - **Camada 3 — Drag & Drop Direto em Hitboxes +40% (Regra de Ouro Mobile):** Todos os alvos interativos (3 Fadas Garçonetes, 5 Potes de Água, Mesas Pet Friendly, Carrinho de Sorvete, Cozinha, Horta/Pomar, 8 Barracas e NPCs da História) receberam colliders de toque `isTrigger` `+40%` maiores que a malha visual. Arrastar da **Fada Garçonete** desenha um arco pontilhado rosa-lilás com halo magnético até uma mesa ou pote vazio e despacha a fada imediatamente (`GirlPowerOrla3DNpcAI.DispatchFairyTo`).
- [x] **HUD Minimalista ≤15% da Tela & Feedback Diegético Puro por Ícones (`GirlPowerOrla3DFirstDelivery.cs`, `GirlPowerOrla3DLifeActivities.cs`, `GirlPowerOrla3DPetFriendlySystem.cs`, `GirlPowerOrla3DUITheme.cs`):**
  - Barra inferior removida completamente — mais de **88% do centro e da base da tela** ficam 100% livres para arrastar o dedo.
  - Canto superior esquerdo compacto: mini-card ilustrado + barra única de ícones e números (Moedas, Reputação Pet %, Ervas, Bolsa).
  - Canto superior direito compacto: barra de vidro fosco somente com ícones (`Cardápio`, `Mobilidade`, `Pipoca`, `Sol/Lua`, `Maré`, `Foto`, `Pausa`).
  - **Mundo 100% por Ícones:** Pote de água vazio exibe apenas uma **gota d'água azul-turquesa pulsante** (`IconWaterDrop`); mesas Pet Friendly exibem **bolhas arredondadas apenas com o ícone ilustrado do prato (`CardapioIcons`) e o petisco do pet** — zero paredes de texto.
- [x] **Ciclo de Andar Natural sem Rigidez & Paleta Fixa de 3 Cores (`GirlPowerOrla3DCharacterAnimator.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Movimento secundário real no `Protag_Torso` e `Protag_Head`: contrarrotação suave de ombros/tronco (`±7.5°` andando / `±11.5°` correndo, oposta ao balanço das pernas), deslocamento lateral de peso e curva de rotação orgânica (`turnSpeed = 7.8`).
  - Paleta unificada em 3 cores fixas na fatia jogável: **Turquesa Costeiro** (`#2AC6CC`), **Areia Dourada / Madeira Clara** (`#EED2A4` / `#C9996B`) e **Rosa-Lilás Mágico das Fadas** (`#EC6FA4` / `#C68CE0`).


---

### Etapa 14 — Orla das Fadas: Rebrand Unificador, Direção de Arte Cozy, Fatia Vertical Fase 1 (+50% Larga) & Atmosfera Viva
- [x] **Rebrand & Premissa Unificadora — `"ORLA DAS FADAS"` (`GirlPowerOrla3DGameFlow.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Tela inicial, subtítulo multilíngue e todos os letreiros 3D da cena atualizados para **ORLA DAS FADAS** (*Resort mágico à beira-mar • Culinária das Fadas & Pets*), unificando todos os turistas sob a premissa de hóspedes visitando o mesmo resort mágico pet friendly.
- [x] **Fim do Botão Administrativo "CHAMAR POLÍCIA (190)" → Fada Guardiã (`GirlPowerOrla3DThiefDetective.cs`):**
  - Removidos o botão vermelho/policial, a viatura policial e a sirene dissonante; re-tematizado para **Fada Guardiã / Guarda-Vidas da Orla** com pílula creme discreta no canto superior direito (`✨ Fada Guardiã [L]`) e ritmo calmo e acolhedor.
- [x] **Fatia Vertical Fase 1 Focada, +50% Mais Larga & Proporções Mobile Robustas (`GirlPowerOrla3DSceneBuilder.cs` v20):**
  - Terreno da praia, calçadão e praça alargado em **~+50% no eixo profundidade** (`localScale = (1.18, 1.0, 1.48)`, limites `minWorldZ = -21.5`, `maxWorldZ = 24.5`) para arrastar o dedo sem claustrofobia.
  - Cercas do pomar engrossadas para `0.36m` (sem serrilhado fino no celular) e mesas/postes reforçados com silhuetas arredondadas legíveis de longe.
  - **Ciclo Completo Fase 1 (60 FPS limpo):** `1 Horta` com `1 Camponesa` → `1 Quiosque/Restaurante` → `1 Fada Garçonete` → `2 Potes de Água` → `3 Turistas com Pets` (**Barbie + Cachorro**, **Japonês + Gato**, **Vovó + Calopsita**). Todos os demais arquétipos, fadas e potes extras permanecem preservados em `Fase2_Expansao_Conteudo` para ativação na Etapa 5.
- [x] **Iluminação Quente, Sombras Móveis de Nuvens & Estrelas Pet (`GirlPowerOrla3DAmbient.cs`, `GirlPowerOrla3DFirstDelivery.cs`):**
  - Sol direcional dourado quente (`#FFD298`) + `Soft Shadows` + `Ambient Trilight` quente + neblina linear costeira (`32m–78m`) e **4 sombras suaves de nuvens deslizando lentamente pela areia e calçadão** (`Sombra_Nuvem_Movel_1..4`).
  - Barra superior minimalista atualizada com **Moedas** + **5 Estrelas Douradas de Reputação Pet** (`★★★★★`) + **Ervas**.
