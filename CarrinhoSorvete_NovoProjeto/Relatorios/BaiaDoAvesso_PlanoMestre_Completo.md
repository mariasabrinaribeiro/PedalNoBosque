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


---

### Auditoria Completa de Correção de Erros (v21 — `0 Erros / 0 Avisos`)
- [x] **Correção do erro ao vivo no Console da Unity (`Look rotation viewing vector is zero`):**
  - [GirlPowerOrla3DAmbient.cs](file:///c:/renato/GitHub/CarrinhoSorvete_Aula1/CarrinhoSorvete_NovoProjeto/Assets/GirlPower/Orla3D/Scripts/GirlPowerOrla3DAmbient.cs): `AnimateButterflies` agora valida o vetor planar `flatDelta = (delta.x, 0, delta.z)` antes de chamar `Quaternion.LookRotation`.
  - [GirlPowerOrla3DGameFlow.cs](file:///c:/renato/GitHub/CarrinhoSorvete_Aula1/CarrinhoSorvete_NovoProjeto/Assets/GirlPower/Orla3D/Scripts/GirlPowerOrla3DGameFlow.cs): órbita de câmera do menu protegida contra vetor de direção nulo.
- [x] **Sincronização 1:1 de Terreno, Pavilhão, Placas e Alturas (`GirlPowerOrla3DSceneBuilder.cs` v21 + `GirlPowerOrla3DFirstDelivery.cs`):**
  - Restaurada a escala `1:1` de `OrlaEnvironmentBase` para alinhar milimetricamente calçadão, areia, linha do mar e `SampleGroundHeight(x, z)`.
  - Corrigidos os letreiros do Pavilhão (`Letreiro Apagado/Restaurado Pavilhao`) que flutuavam em `(15.6, 3.34, 3.40)` e reposicionados exatamente sobre `PavilionBay` em `(26.0, 3.34, -8.35)`.
  - Corrigida a altura fantasma (`+0.36m` sobre a areia vazia em `x: 12.6..18.6`) em `SampleGroundHeight` e sincronizados os pontos de palco do Capítulo 3/4 e do 4º cartão postal com `pavilion.position`.
  - Corrigida a rotina da Florista Luna (`floristMorningPos/EveningPos`) para permanecer em frente à sua banca em `z = -4.15..-4.60`.
- [x] **Proteção de Clique/Toque sobre HUD (`GirlPowerOrla3DMobileTouchController.cs`):**
  - Bloqueio via `GUIUtility.hotControl != 0` e proteção da pílula contextual `[E]` na base, evitando andar/arrastar a câmera ao tocar em botões.
- [x] **Zero Avisos de Compilação em Todo o Projeto (`0 Erros, 0 Avisos`):**
  - Eliminados todos os `warning CS0414` restantes em `Enemy.cs`, `DriverPro.cs` e `GirlPowerFirstDelivery.cs`.


---

### Etapa 15 — Correções Técnicas Obrigatórias v22: Fade Roof Interno 0.5m, NavMeshObstacle + Desvio NPCs, Corredor +50%, Fontes +70% e Parallax 3 Camadas
- [x] **1. Remoção de Bloqueio Visual & Fade Roof Estritamente Interno (`GirlPowerOrla3DCafeRoofFade.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Casas e fachadas do primeiro plano sul recuadas para `z = -17.8..-25.2` com altura reduzida (`scaleY = 0.72..0.80`) para nunca tamparem a área jogável nem a protagonista.
  - `BoxCollider (isTrigger = true)` do Fade Roof agora fica estritamente dentro do piso interno com **margem interna de `0.50m`** das paredes externas, com transição suave por `Mathf.Lerp` e histerese (`0.14m`) — fim total do pisca-pisca ao encostar na parede externa.
- [x] **2. Colisores + `NavMeshObstacle (Carve)` nos Arbustos Verdes & Desvio entre Personagens (`GirlPowerOrla3DPropsBuilder.cs`, `GirlPowerOrla3DCharacterAvoidance.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Todos os arbustos verdes (`Restinga_1..14`, `Arbusto_Sul_*`), pedras, jardineiras, bancos, postes, cercas e barracas receberam `BoxCollider` físico + `NavMeshObstacle (carving = true)`.
  - Protagonista e todos os NPCs/Turistas/Fadas configurados com `NavMeshAgent` (`HighQualityObstacleAvoidance`, `radius = 0.45`, `angularSpeed = 420`, prioridades distintas) + resolvedor runtime de separação suave (`GirlPowerOrla3DCharacterAvoidance.cs`) para ninguém se atravessar.
- [x] **3. Corredor +50% Mais Largo & Câmera Mais Aberta (`GirlPowerOrla3DSceneBuilder.cs`, `GirlPowerOrla3DPropsBuilder.cs`):**
  - Cerca sul e parede sul recuadas para `z = -23.6`, calçada sul alargada e espaçamento entre barracas da feira (`z = -5.6`) e casas sul (`z = -17.8+`) ampliado em +50%+; câmera mais aberta (`FOV = 50°`, `offset = (0, 10.6, -16.4)`, `pitch = 31.5°`, `farClipPlane = 185`).
- [x] **4. Caminhada Sincronizada sem Deslize & Giro 420°/s (`GirlPowerOrla3DCharacterAnimator.cs`, `GirlPowerOrla3DFirstDelivery.cs`):**
  - Cadência e `Animator.speed = actualSpeed / baseWalkSpeed` sincronizados à velocidade real de deslocamento + rotação suavizada com `Quaternion.Slerp` limitada por `angularSpeedDeg = 420°/s`.
- [x] **5. Fontes +65% a +75% com Contorno Escuro para Mobile (`GirlPowerOrla3DUITheme.cs`, `GirlPowerOrla3DFirstDelivery.cs`, `GirlPowerOrla3DDialogue.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Aumento de +65% a +75% nas fontes de HUD (moedas, missões, timer), balões de diálogo e todas as placas 3D (`TextMeshPro` com `outlineWidth = 0.22` escuro).
- [x] **6. Novo Efeito Parallax Real em 3 Camadas no Horizonte do Mar (`GirlPowerOrla3DParallaxHorizon.cs`, `GirlPowerOrla3DPropsBuilder.cs`):**
  - **Camada 1 (10% da velocidade da câmera):** 5 ilhas tropicais estilizadas com colinas/palmeiras + 6 nuvens volumosas no horizonte (`z = 38..55`).
  - **Camada 2 (30% da velocidade da câmera):** 5 barcos e veleiros coloridos navegando continuamente no mar médio (`z = 20.5..30`) com balanço nas ondas.
  - **Camada 3 (Beira-mar):** 8 faixas de espuma das ondas animadas em loop suave avançando e recuando sobre a areia molhada.


---

### Correção de Orientação, Deslocamento Real, Root Motion & Tolerância em Paredes (`GirlPowerOrla3DCharacterFacing.cs`)
- [x] **Detecção automática da frente real do rosto (`GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign`):** Mede em tempo de execução a posição local dos olhos (`Protag_EyeL`, `EyeL`, `P_EyeL`, `DogEyeL`, `CatEyeL`) em relação à cabeça/tronco para garantir que o rosto de **todos** os personagens (Protagonista, Moradores, Turistas, Camponesa, Fadas e Pets) aponte sempre para o sentido exato do movimento — nunca 180° invertido.
- [x] **Conversão do movimento relativa à câmera (`GirlPowerOrla3DFirstDelivery.cs`):** Teclado e joystick agora projetam o vetor de entrada em `camRight` e `camForwardXZ` da câmera ativa, garantindo correspondência exata com a tela.
- [x] **Fim de Root Motion conflitante e pés escorregando (`GirlPowerOrla3DCharacterAnimator.cs`):** `applyRootMotion = false` forçado em todos os `Animator`s; passada orientada exclusivamente pelo deslocamento real no chão (`stepMeters`) com amplitude zerando suavemente quando a personagem desacelera ou encosta em parede.
- [x] **Moradores mirando o deslocamento real nas curvas (`GirlPowerOrla3DNpcAI.cs`, `GirlPowerOrla3DPetFriendlySystem.cs`, `GirlPowerOrla3DPetCompanion.cs`):** Todos os moradores, turistas, Camponesa, fadas e pets agora rotacionam pelo vetor real `posAtual - posAnterior` do frame (em vez de mirar no destino final atrás de uma barraca/arbusto), eliminando caminhadas de costas nas curvas.
- [x] **Tolerância contra viradas ao parar ou oscilar em paredes:** `lastValidFacingYaw` / `lastValidRot` só atualizam acima da tolerância mínima (`actualPlanarSpeed > 0.35 m/s` e `input > 0.2` para a protagonista; `> 0.25 m/s` para NPCs/pets), mantendo a última orientação válida ao parar ou encostar em paredes.

---

### Etapa 16 (v23) — Colisão OBB Real, Navegação A\* no Toque/Moradores & Reorganização Espacial da Orla (`LayoutEspacoso`)
- [x] **1. Colisão Real por Caixa Orientada — OBB (`GirlPowerOrla3DPathfinder.DistanceSqToOrientedBoxXZ`):**
  - Substituída a colisão baseada em `BoxCollider.bounds` (AABB alinhada ao mundo que inflava quinas de peças giradas) por distância exata no espaço local de cada `BoxCollider` (`TryMoveAxis` em `GirlPowerOrla3DFirstDelivery.cs` e `GirlPowerOrla3DCharacterAvoidance.cs`), com volumes ajustados à base física de bancos, postes, caixas, placas e guarda-sóis.
- [x] **2. Sistema Único de Movimento com Rotas A\* para Moradores (`GirlPowerOrla3DNpcAI.cs`, `GirlPowerOrla3DPathfinder.cs`, `GirlPowerOrla3DCharacterAvoidance.cs`):**
  - Removido o conflito entre dois scripts movendo moradores (`charAvoid.applyTransformSeparation = false`): agora `GirlPowerOrla3DNpcAI` é o único dono do deslocamento, traçando rotas suavizadas por A\* com string-pulling (`GirlPowerOrla3DPathfinder`) que contornam barracas/arbustos, com parada lateral junto aos balcões e desvio lateral cortês ao cruzar com a protagonista.
- [x] **3. Rota Caminhável A\* no Toque para Andar (`GirlPowerOrla3DFirstDelivery.cs`):**
  - `SetTapDestination` agora calcula uma rota caminhável suavizada por A\* (`GirlPowerOrla3DPathfinder.FindPath`) contornando automaticamente barracas e obstáculos (ex.: tocar na Florista a partir do outro lado da feira), além de ajustar automaticamente destinos clicados sobre balcões para o ponto livre em frente ao atendimento.
- [x] **4. Reorganização Espacial da Orla (`GirlPowerOrla3DSceneBuilder.cs` v23 & `GirlPowerOrla3DPropsBuilder.cs`):**
  - **Calçadão principal:** faixa contínua livre de **`5,0 m`** (`z = -2,2 .. +2,8`, ~6 a 8 larguras da protagonista) do carrinho até a florista, sem bancos, postes nem barracas no trajeto.
  - **Carrinho de sorvete:** posicionado em recuo próprio (`(0.4, 0, -3.45)`) com frente totalmente aberta para montar pedidos enquanto moradores passam livremente no calçadão.
  - **Praça lateral de Feira & Gastronomia:** barracas (`z = -9.6/-12.6`), mesas (`z = -6.5`), *Batata & Mar* (`(-14.2, 0, -9.8)`) e *Café das Fadas* (`(6.2, -0.08, -12.4)`) agrupados na praça lateral com abertura visível de **`6,0 m`** (`x = 4..10`) a partir do calçadão e espaço livre diante de cada balcão.
  - **Florista:** área dedicada junto à fachada (`(24, 0, -6.8)` e *Florista Luna* em `(24, 0, -5.25)`), com caixas e vasos movidos para a lateral (`x = 21.1`) para entregas fora do fluxo principal.
  - **Faixa de areia:** guarda-sóis espaçados (`x = -1.2, 12.2, 24.5` em `z = 6.6`) e **3 acessos diretos abertos** no meio-fio e na restinga (`x = -7.2..-3.5`, `x = 5.5..9.5`, `x = 18.0..22.0`).
  - **Bancos, postes e potes para pets:** recuados nas bordas (`z = -3.4..-3.75`) com colisores compactos que preservam todas as esquinas.
  - **Telhados e câmera:** telhados de primeiro plano reduzidos com `ApplyFadeMaterials` + enquadramento inteligente na chegada ao Carrinho e à Florista (`FollowAnimalCrossingCamera`).

---

### Etapa 17 (v28) — Direção de Arte de Personagens, Silhuetas Contrastantes, Linguagem de Fadas/Pets & Exportação Padronizada (`+Z` Forward)
- [x] **1. Protagonista (`Char_Protagonist.fbx`, `build_orla_art_direction_characters.py`):**
  - Pernas **+15% mais longas** (`hip z = 0.41`, `LegTube = 0.30`) e cabeça **~8% menor** (`r = 0.266` vs `0.290`), preservando rosto expressivo e olhos piscantes.
  - Penteado reconhecível de costas (rabo de cavalo alto em 2 camadas + laço turquesa/dourado), **bolsa curta tiracolo** com alça diagonal visível frente/costas e combinação exclusiva de cores (framboesa, creme, jeans turquesa, bolsa dourada).
  - Passada calibrada para as novas pernas em `GirlPowerOrla3DCharacterAnimator.cs` (`1.70 m` por ciclo completo + amplitude `31°/46°` + altura da casquinha/chapéu ajustadas).
- [x] **2. As Duas Amigas — Silhuetas Contrastantes Mesmo Sem Cor (`Char_FriendNina.fbx` e `Char_FriendBia.fbx`):**
  - **Nina:** silhueta alta e esguia, coque alto vertical esticado e jaqueta reta/quadrada com calça reta.
  - **Bia:** silhueta ~20% mais baixa e compacta, cabelo volumoso cacheado em nuvem e vestido/mangas balonê arredondadas.
- [x] **3. Moradores Modulares & Florista Reconhecíveis de Costas (`Char_Florist.fbx`, `Char_PedestrianBlue.fbx`, `Char_PedestrianCoral.fbx`, `Char_SisterMarina.fbx`, `build_baia_modular_archetypes.py`):**
  - Base modular com variações de altura e largura (`0.90x–1.12x`) + juntas articuladas (`LegL/R`, `ArmL/R`) e acessórios legíveis de costas (mochila costeira, viseira + rabo lateral + pochete, laço solar).
  - **Florista Luna:** avental botânico verde com alças cruzadas em X e laço nas costas + **cesto de mudas trançado** com brotos e flores visível frente/costas.
- [x] **4. Fadas e Pets com Contraste e Linguagem Própria (`Char_FairyWaitress.fbx`, `PetDog.fbx`, `PetCat.fbx`, `Pet_Calopsita.fbx`):**
  - **Fadas (`GirlPowerOrla3DNpcAI.cs`):** asas com moldura luminosa dourada + núcleo lilás/rosa que se destacam sobre o mar e o piso, e movimento aéreo próprio alternando **planar suave** com **pausas breves suspensas no ar**.
  - **Pets (`GirlPowerOrla3DPetCompanion.cs`, `GirlPowerOrla3DPetFriendlySystem.cs`):** **Pipoca** e **Mingau** (agora também articulado!) com pelagem de alto contraste (manchas chocolate/ameixa + coleiras vibrantes), **baixando a cabeça para farejar o chão** e **variando o ritmo da passada** (trote rápido, passo curioso e pausas farejando).
- [x] **5. Padronização de Exportação (`+Z` Forward) e Fim do Vetor Invertido:**
  - Todos os 21+ modelos de personagens, arquétipos, fadas e pets reexportados no Blender 5.3 com `axis_forward='Z', axis_up='Y', bake_space_transform=True` (`det = +1.0`), padronizando a frente real em `+Z` do Unity (`fallbackSign = +1f` em `GirlPowerOrla3DCharacterFacing.cs` e em todos os controladores).

---

### Etapa 18 (v29) — Orla Contínua sem Rua Cinza, Cobertura do Carrinho Elevada, Horizonte Atmosférico & UX de Montagem/Entrega Acessível
- [x] **1. Calçadão Contínuo sem Rua Cinza nem Manchas Abruptas (`build_orla_acnh.py` → `OrlaEnvironmentBase.fbx`, `GirlPowerOrla3DPropsBuilder.cs`):**
  - Removidos o asfalto cinza, faixas tracejadas, `MeioFio_Jardim_Interno` e manchas soltas de grama (`GrassPatch_*`).
  - Todo o percurso (`z = -15.6..+3.45`) agora é uma superfície contínua quente de orla com mudanças de material intencionais e chanfradas apenas nas entradas do Carrinho, Praça da Feira, Café, Florista e nos 3 acessos abertos à areia (`Mureta` segmentada em 4 blocos com aberturas reais).
- [x] **2. Travessias Limpas & Transparência Suave de Coberturas (`GirlPowerOrla3DSceneBuilder.cs`):**
  - Bandeirinhas (`FestoonLights`), lanternas flutuantes e postes (`StreetLamp`, `FlowerLampPost`) concentrados na Praça Lateral e fachadas (`z = -8.2..-9.8`), liberando totalmente pés, travessia e destino da protagonista.
  - `ApplyFadeMaterials` aplicado em todas as 8 barracas da feira, quiosque, florista e carrinho de sorvete para transparência suave quando qualquer toldo encobre a protagonista.
- [x] **3. Carrinho de Sorvete com Balcão 100% Visível (`build_baia_hero_polish.py` → `IceCreamCart.fbx`):**
  - Cobertura rosa listrada mantida como assinatura visual, porém elevada em `+0,48 m`, deslocada para trás (`+0.34 Y`) e levemente reduzida no raio para expor o balcão, a vitrine de sabores e o lado de atendimento.
- [x] **4. Mar, Barcos e Horizonte Equilibrados (`GirlPowerOrla3DPropsBuilder.cs`, `GirlPowerOrla3DSceneBuilder.cs`):**
  - Removidos barcos estáticos e colinas duplicadas de `OrlaEnvironmentBase.fbx`; 3 veleiros bem espaçados (`x = -22, 12, 44`) com velas suaves de baixo contraste, colinas rebaixadas fundidas ao céu (`AC_HillDistant`/`AC_HillFar` suaves) e 10 faixas de espuma dupla na beira-mar dando vida à margem.
- [x] **5. Montagem Inferior, Entrega Curta & Acessibilidade PT/EN/ES (`GirlPowerOrla3DFirstDelivery.cs`, `GirlPowerOrla3DGameFlow.cs`, `GirlPowerOrla3DPhotoOverlay.cs`):**
  - **Painel inferior para uma mão (`Ch1_AtCart`):** pedido da Florista sempre visível no topo, casquinha montada ao centro, ingredientes ilustrados e nomeados com **símbolos de forma (`◆ / ● / ▲ / ♣`)** além de cor, e botão **Confirmar pedido** fixo exatamente na mesma posição durante toda a montagem.
  - **Entrega sem cobrir o percurso:** ao entregar à Florista Luna, exibe reação curta não bloqueante no topo, atualiza objetivo, desbloqueia a foto silenciosamente no ícone superior e devolve controle imediato.
  - **Acessibilidade e segurança de progresso:** `Screen.safeArea` em telas estreitas e largas, quebra de linha (`wordWrap`) para textos longos em PT/EN/ES, alto contraste sobre mar claro, selo de partida salva no menu e **diálogo de confirmação explícito (`SubModal.ConfirmAction`)** antes de sobrescrever/reiniciar progresso ou sair ao menu.
