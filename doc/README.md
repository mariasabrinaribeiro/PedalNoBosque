# Documentação do Jogo: Carrinho de Sorvete (Ice Cream Delivery & Survival)

Bem-vindo à documentação oficial do **Carrinho de Sorvete**, um jogo dinâmico desenvolvido na engine Unity (2D) que mistura a clássica mecânica de entrega de encomendas (estilo *Delivery Dash*) com elementos intensos de sobrevivência de combate contra monstros açucarados.

---

## 🍦 1. Visão Geral do Jogo

No controle de um carrinho de sorvete personalizado, o jogador percorre uma pista infinita coletando pacotes de sorvete e entregando-os aos clientes e casas. Porém, o caminho é perigoso: o **Barão Amargo** e sua horda de monstros amargos estão determinados a estragar o dia. O jogador deve desviar de poças de lentidão, derrotar inimigos atirando projéteis de doce e cumprir missões desafiadoras para ganhar pontos e ativar o estado supremo do **Girl Power**.

---

## 🕹️ 2. Mecânicas de Jogabilidade (Gameplay)

### 🎮 Controles
O jogo suporta duas formas principais de entrada, adaptadas tanto para desktop quanto para dispositivos móveis:
*   **Teclado (PC)**:
    *   `W` ou `S` / `Seta para Cima` ou `Seta para Baixo`: Mover para frente ou para trás.
    *   `A` ou `D` / `Seta para a Esquerda` ou `Seta para a Direita`: Rotacionar o veículo.
    *   `Espaço`: Atirar projéteis de doce.
*   **Mobile / Touch (Arraste)**:
    *   O jogador pode tocar na tela e arrastar (gesto de swipe) para guiar o carrinho diretamente na direção do dedo. A velocidade do veículo se adapta à distância do arrasto, e o tiro pode ser automatizado ou acionado por botão na interface móvel.

### 📦 Ciclo de Entrega (Delivery Loop)
1.  **Coleta**: O jogador passa por cima de pacotes de sorvete (`package`) espalhados pela pista para recolhê-los. O carrinho tem uma capacidade de carga inicial limite de **5 sorvetes**. Se estiver cheio, o jogador recebe um aviso no HUD: *"CARGA CHEIA! VÁ ATÉ A ÁREA DE DESCARGA!"*.
2.  **Entrega**: Ao colidir com casas de clientes (tags `Customer`, `Pink House` ou áreas com o nome `Descarga de sorvete`), todos os sorvetes a bordo são entregues.
3.  **Recompensas**: Cada entrega concede **50 pontos** por sorvete. Ao finalizar uma entrega com sucesso, o jogador ativa o modo especial **Girl Power** por 7 segundos!

### ⭐ Modo Girl Power
Quando ativado (por entrega de sorvetes, conclusão de missões ou ao derrotar o Chefe):
*   A velocidade do veículo é elevada para a velocidade de impulso (`boostSpeed = 6.0`).
*   A trilha de velocidade (*Trail Renderer*) muda de rosa para um gradiente de arco-íris pulsante.
*   Estrelas brilhantes douradas são emitidas ao redor do veículo.
*   A velocidade de recarga dos tiros de doce é reduzida para maior frequência de disparos.

### 🎯 Sistema de Missões Dinâmicas (`MissionManager`)
O jogo gera missões aleatórias constantemente para manter o jogador engajado. Completar uma missão concede **+100 pontos** e **5 segundos de Girl Power**. As missões incluem:
1.  **Entregar 3 Sorvetes**: Realizar entregas acumuladas aos clientes.
2.  **Derrotar 5 Monstros sem Sofrer Dano**: Eliminar inimigos em sequência. Se o jogador for atingido por qualquer fonte de dano, o progresso dessa missão é reiniciado para 0.
3.  **Correr em Velocidade Máxima por 8s**: Manter o carro correndo em velocidade acelerada (acima de 4.8 unidades de física por segundo) de forma contínua por 8 segundos.

---

## 👾 3. Sistema de Inimigos e Combate

### 🍬 Combate Ativo
O jogador pode disparar projéteis a partir da frente do carrinho de sorvete. O disparo consome um curto período de recarga e os projéteis podem ser potencializados por itens de melhoria.

### 🛡️ Power-Ups
Itens flutuantes que surgem na pista e ajudam na jornada:
*   🧲 **Ímã (Magnet)**: Atrai todos os pacotes de sorvete em um raio de 6 unidades em direção ao carrinho por 5 segundos.
*   🛡️ **Escudo (Shield)**: Envolve o veículo em uma bolha ciana brilhante que absorve completamente o próximo impacto de dano sofrido.
*   🔫 **Tiro Triplo (Triple Shot)**: Altera o padrão de disparo por 5 segundos, atirando três balas simultâneas (uma frontal e duas em ângulos de 15 graus para a esquerda e direita).

---

### 👹 Tipos de Inimigos (`Enemy`)
Os monstros perseguem o jogador ativamente e reagem fisicamente ao receber dano (sofrendo empurrão/*knockback* e piscando em branco). Existem quatro variações de inimigos com características visuais e mecânicas distintas:

| Tipo | Cor Visual | Vida (HP) | Velocidade | Dano ao Colidir | Recompensas (Pontos / Vida) | Comportamento Especial |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **Standard** | Vermelho | 50 | 1.5 | 20 | 2 pts / +15 HP | Perseguição padrão e balanceada. |
| **Slow** | Roxo Escuro | 150 | 0.6 | 30 | 5 pts / +25 HP | Grande e lento, mas atua como "tanque" com muita vida. |
| **Fast** | Verde Neon | 15 | 3.2 | 15 | 2 pts / +10 HP | Pequeno e extremamente ágil, difícil de desviar. |
| **Explosive**| Laranja-Avermelhado | 40 | 2.0 | 35 | 2 pts / +15 HP | Ao se aproximar a 1.3 unidades do jogador, ele para, pisca rapidamente em amarelo/vermelho e explode, causando alto dano em área (raio de 2.5 unidades). |

---

### 👑 Chefe: Barão Amargo (`BossBitterBaron`)
Quando o jogador atinge **1500 pontos** (e subsequentemente a cada 1500 pontos adicionais pós-derrota), o jogo entra no modo de Chefe.
*   **Aparência**: Um grande bloco metálico escuro flutuante (`Barão Amargo`) com bordas chanfradas prateadas.
*   **Comportamento**: Ele surge 8 unidades à frente do jogador, acompanhando a direção do veículo e balançando de um lado para o outro.
*   **Fase de Chefe**: Todos os inimigos comuns e portões de corrida da pista são limpos ao iniciar o combate para focar no duelo.
*   **Ataques**:
    *   **Bomba de Açúcar (Sugar Bomb)**: Lança projéteis de chocolate escuro que explodem ao atingir o jogador, gerando uma poça pegajosa de lentidão.
    *   **Invocação de Minions**: Invoca monstros rápidos e comuns nas laterais a cada 6 segundos para flanquear o jogador.
*   **Derrota**: Ao esgotar seus **2500 pontos de vida**, o Barão explode de forma massiva com forte tremor de tela, rendendo **200 pontos** e ativando **10 segundos de Girl Power** contínuos.

---

## 🛠️ 4. Estrutura Técnica de Scripts (C#)

O código-fonte do jogo é modular e foca em otimização de performance e efeitos dinâmicos:

*   [driver.cs (driver)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/driver.cs): Script principal do jogador. Controla física do carro (`Rigidbody2D` com interpolação), processamento de entradas, aplicação de efeitos de colisão, controle de vida/escudo e gerenciamento de temporizadores de melhorias.
*   [delivery.cs (Delivery)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/delivery.cs): Gerencia a coleta de pacotes com limites de carga e a entrega dos sorvetes nos pontos de descarga, atualizando a pontuação e aplicando o bônus do Girl Power.
*   [Enemy.cs (Enemy)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/Enemy.cs): Controla o ciclo de vida dos monstros comuns (Standard, Slow, Fast, Explosive), seus estados de perseguição, flash de cor ao sofrer dano, empurrão de física (*knockback*) e explosão de partículas procedurais.
*   [BossBitterBaron.cs (BossBitterBaron)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/BossBitterBaron.cs): Implementa a inteligência artificial do chefe, incluindo a barra de vida específica, lançamento de projéteis e invocação de minions.
*   [EnemySpawner.cs (EnemySpawner)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/EnemySpawner.cs): Responsável por criar e reciclar monstros à frente do jogador, gerenciar a transição para a luta do chefe e distribuir portões de velocidade e power-ups na pista.
*   [MissionManager.cs (MissionManager)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/MissionManager.cs): Gerencia as missões dinâmicas do jogo, validando progresso do jogador e premiando metas completas.
*   [ObjectPoolManager.cs (ObjectPoolManager)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/ObjectPoolManager.cs): Sistema de otimização que pré-carrega inimigos, portões e projéteis em memória, evitando operações dispendiosas de criação e destruição repetida de objetos em tempo de execução.
*   [GameUIController.cs (GameUIController)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/GameUIController.cs): Controla toda a interface do usuário (HUD), incluindo barra de vida do jogador, capacidade do porta-malas, alertas de avisos rápidos, painel de missões atuais e a tela de fim de jogo (*Game Over*).
*   [GameAudioController.cs (GameAudioController)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/GameAudioController.cs): Centraliza os disparos de áudio de efeitos sonoros.
*   [GameCameraController.cs (GameCameraController)](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/GameCameraController.cs): Gerencia o foco da câmera no jogador com suavidade e processa o efeito de chacoalhar a tela (*Screen Shake*) proporcional à gravidade das colisões e explosões.

---

## ✨ 5. Polimento Estético e Detalhes Visuais (Juice)

O jogo possui várias técnicas conhecidas como "Game Juice" para tornar a experiência altamente responsiva e viva:

1.  **Engine Sound Loop Procedural (`EngineSound`)**: Em vez de tocar um clipe de som gravado em loop, o jogo sintetiza e gera a frequência do barulho do motor dinamicamente via código, variando o tom (*pitch*) de acordo com a aceleração real do carrinho de sorvete.
2.  **Sugar Dust (`AmbientGlitterManager`)**: Partículas sutis de purpurina doce flutuam constantemente ao redor da câmera do jogador, dando um ar doce e mágico ao cenário.
3.  **Wobble Effect (`WobbleOnPass` & `TextJuice`)**: Pacotes de sorvete e textos da interface realizam um balanço físico responsivo (efeito de mola) quando o jogador passa perto deles ou quando ocorrem atualizações na tela.
4.  **Slow Zones (`SlowZone`)**: Áreas da pista com caldas escuras ou obstáculos de chocolate que reduzem a velocidade do jogador a 40%, exigindo destreza e planejamento de rotas por parte do motorista.
5.  **Trepidação de Câmera (Screen Shake)**: Batidas em obstáculos comuns geram vibrações leves (intensidade 0.14 por 0.18s), colisões com monstros explodindo geram vibrações médias (intensidade 0.2 por 0.25s) e derrotar o Barão Amargo gera um grande abalo sísmico na tela (intensidade 0.35 por 0.6s).
