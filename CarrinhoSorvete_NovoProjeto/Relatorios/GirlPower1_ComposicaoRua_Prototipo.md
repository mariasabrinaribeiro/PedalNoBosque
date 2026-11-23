# Girl Power 1 — Recomposição da rua (protótipo)

## Escopo e preservação

- Projeto de trabalho: `CarrinhoSorvete_NovoProjeto` (cópia isolada dentro do repositório).
- Cena de trabalho: `Assets/Scenes/Girl power 1- Sorvete.unity`.
- Cena de referência preservada: `Assets/Scenes/Girl power 1 - Sorvete - Referencia.unity`.
- Antes da recomposição: `Assets/Scenes/Girl power 1 - Sorvete - AntesRecomposicao.unity`.
- Os assets originais não foram sobrescritos; os objetos antigos foram desativados na cena de trabalho.

## Comparação observada

Antes e depois foram observados na aba Game com `Full HD (1920x1080)`, `16:9 Aspect` e escala de visualização do editor `0,57x`. A escala `0,57x` apenas ajusta a pré-visualização ao painel do Editor; o alvo continua em 1920×1080.

- **Antes:** dois quadros de terreno separados por uma larga faixa preta; fachada fantasiosa grande e muitas árvores/cercas repetidas; o trajeto não se lê como uma rua.
- **Depois:** faixa contínua com mar, areia, área verde, calçada, meio-fio e asfalto; protagonista, carrinho e quiosque `GIRL POWER` no mesmo enquadramento; HUD de exploração reduzido aos controles de andar/correr.

As capturas comparáveis foram exibidas na conversa a partir do Game View do Unity. Não foi exportado um arquivo PNG nem gravado vídeo.

## Alterações executadas

1. A textura `neighbourhood.png` estava sendo usada como sprite único no objeto `pista`, embora represente um atlas com trechos desconectados. A cena de trabalho agora usa uma arte contínua própria para a rua de teste.
2. Desativei as instâncias antigas do cenário na cena de trabalho. Também desativei os filhos legados da `Main Camera` (Virtual Camera, Canvas antigo, árvores e cercas) que continuavam aparecendo mesmo com as raízes do cenário desligadas.
3. Adicionei uma protagonista placeholder em pixel art, com controlador de caminhada/corrida (`WASD` ou setas; `Shift` para correr), animação em dois quadros e limite de percurso.
4. Configurei a câmera ortográfica para seguir a protagonista, criar espaço à frente segundo a direção de movimento e ampliar o enquadramento para incluir o carrinho quando a personagem está próxima.
5. Mantive um carrinho visível e adicionei um ponto de venda provisório com toldo e placa Girl Power. A interação próxima exibe `E — ABRIR CARRINHO`.
6. A tela de exploração não exibe vida, velocidade, missão de entrega nem `ATIRAR`; mostra somente os controles relevantes e o aviso contextual do carrinho.

## Estado dos testes em Play Mode

| Item | Resultado observado |
|---|---|
| Rua contínua e leitura do caminho | Atendido visualmente no Game View; a faixa preta do antes não aparece no trecho novo. |
| Protagonista e carrinho no enquadramento inicial | Visíveis juntos. |
| Acompanhamento da câmera e espaço à frente | Sistema de câmera único implementado; a cena iniciou enquadrando personagem e carrinho. Não foi possível validar o comportamento durante deslocamento manual. |
| HUD de exploração | Controles essenciais visíveis; elementos antigos de combate/entrega não aparecem na composição nova. |
| Aviso para abrir o carrinho | O aviso contextual aparece quando a personagem inicia perto do carrinho. |
| Caminhar/correr nos dois sentidos | Controlador implementado, mas **não validado em execução**: o envio de teclas pela automação do Unity não manteve as teclas pressionadas, e a posição do jogador não mudou nas tentativas. |
| Abrir/fechar o carrinho com `E` | A instrução contextual aparece; a alternância do estado não foi confirmada, pois a automação não registrou o pressionamento como entrada contínua/confiável. |
| Colisões nos dois sentidos, performance/Profiler, Android | Pendentes; sem validação de movimento, não seria correto afirmar aprovação desses testes. |
| Vídeo de corrida, chegada e atendimento | Não produzido. O atendimento completo ainda não foi implementado. |

O Console do Editor manteve um erro de ambiente do Burst ao tentar carregar uma biblioteca nativa não gerenciada. O menu de construção do protótipo executou e a composição foi salva; não observei uma exceção de execução atribuída ao novo controlador no teste feito.

## Arquivos da implementação

- `Assets/Scenes/Girl power 1- Sorvete.unity` — cena de trabalho recomposta.
- `Assets/GirlPower/Editor/GirlPowerStreetPrototypeBuilder.cs` — construtor do trecho de teste.
- `Assets/GirlPower/Scripts/GirlPowerStreetController.cs` — movimento, animação, câmera e aviso do carrinho.
- `Assets/GirlPower/Art/MatinhosStreet.png` — piso contínuo de teste.
- `Assets/GirlPower/Art/Girl_Idle.png`, `Girl_Walk_A.png`, `Girl_Walk_B.png` — protagonista provisória.
- `Assets/GirlPower/Art/PanelWhite.png` — base persistente para placas e elementos do quiosque.
- Arquivos `.meta` correspondentes do Unity.
- Duas cópias de cena listadas acima para referência e comparação.

A importação dos recursos essenciais do TextMesh Pro também aparece como alteração no Git da cópia, pois o projeto precisava desses recursos para renderizar os textos da interface.

## Pendências imediatas

1. Validar com teclado físico a caminhada, a corrida em ambos os sentidos, a câmera e a interação `E`.
2. Ajustar o piso de teste e os obstáculos depois de observar o deslocamento real, sobretudo alinhamento de pés/rodas e colisões.
3. Gravar um vídeo comparável de antes/depois com a personagem correndo e chegando ao carrinho.
4. Desenvolver o turno (pedido, casquinha, entrega, pagamento, fechamento) e a melhoria visual do carrinho numa etapa posterior.
