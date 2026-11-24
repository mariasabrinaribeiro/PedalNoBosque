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
