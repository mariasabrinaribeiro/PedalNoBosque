# Girl Power 1 — Sorvete

## Auditoria inicial e alterações na cópia do projeto

**Projeto aberto:** `CarrinhoSorvete_NovoProjeto`  
**Cena ativa:** `Assets/Scenes/Girl power 1- Sorvete.unity`  
**Editor:** Unity 2022.3.62f3  
**Game View observado:** `Free Aspect`, escala `1.3x`; área visível aproximada de 872 × 488 px. A resolução não estava fixada no seletor do Game View, portanto essa área é apenas o tamanho de visualização do painel, não uma resolução de build.

## O que a cena realmente contém

A cena é um jogo de condução/entrega de sorvetes em veículo. O fluxo executado apresenta menu `GIRL POWER / JOGAR / SAIR` e, ao iniciar, HUD com vida, quantidade de sorvetes, pontos, velocidade, missão, joystick e botão `ATIRAR`. A cena ainda não implementa a exploração a pé nem o atendimento no balcão descritos no roteiro.

| Recurso | Estado observado | Evidência / observação |
|---|---|---|
| Iniciar o jogo | Funciona | `JOGAR` abre o HUD de condução. |
| Andar e correr a pé | Falta | O veículo usa `DriverPro`; não foi encontrado controlador de personagem a pé na pasta principal de scripts. |
| Dirigir o veículo | Existe com problema | A entrada `W` é reconhecida, mas a vida caiu de 100% para 80% após um toque curto; houve outros danos rápidos em execuções anteriores. A posição inicial ou as colisões precisam ser investigadas. |
| Acompanhar o jogador com a câmera | Funciona parcialmente | `GameCameraController` segue o veículo. Em execução, esse script desativa a `Virtual Camera`, então alterar a lente da Cinemachine não muda o enquadramento usado durante o jogo. |
| Interagir ou estacionar o carrinho | Não foi possível verificar | O objeto atual opera como veículo de entrega; não há ação de estacionar/abrir balcão visível no fluxo testado. |
| Receber pedido, montar casquinha e cobrar | Falta | O HUD não apresenta pedido de cliente, montagem ou pagamento de venda. |
| Fechar turno e comprar melhoria visual | Falta | Não apareceu fluxo de turno ou progressão do carrinho durante a execução. |
| Erros atuais no Console | Nenhum exibido no último teste | Console mostrou contadores zerados depois de importar os recursos essenciais do TextMesh Pro. |

## Problemas priorizados para a próxima etapa

| Hierarchy / área | Evidência observada | Ação proposta | Prioridade |
|---|---|---|---|
| `RUA`, `rua`, `pista` e blocos de cenário | O Game View mostra dois grandes quadros de terreno separados por uma faixa preta central; não se lê como uma rua contínua. | Identificar qual conjunto de tiles/objetos forma o percurso jogável; reposicionar ou desativar temporariamente um conjunto de cenário por vez e comparar no mesmo enquadramento. | P0 |
| `carro` / `Carro1` | O objeto físico e o sprite estavam desalinhados; o filho `Carro1` tinha deslocamento local Y de aproximadamente 9,37 unidades. | Alinhar o sprite ao objeto de controle e revisar collider, ponto de spawn e posição em relação ao percurso. | P0 |
| Obstáculos e colisores na rua | Um toque em `W` causou perda imediata de vida, antes de ser possível avaliar o trajeto. | Visualizar colliders e testar o veículo em ambos os sentidos após corrigir a posição inicial. Evitar alterar dano até localizar a colisão. | P0 |
| `Virtual Camera` / `Main Camera` | A Cinemachine está configurada para seguir `carro`, mas `GameCameraController.Awake()` desativa a Virtual Camera durante o jogo. | Escolher um único sistema de câmera como autoridade e ajustar seu enquadramento em gameplay; reservar alterações da lente da Cinemachine até resolver esse conflito. | P1 |
| Fundo, árvores, cercas e casa grande | Elementos muito contrastados e de escala incompatível com o sprite pequeno do veículo; repetição numerosa de árvores/cercas. | Reorganizar em plano de fundo, faixa jogável e obstáculos; reduzir repetições que invadam o percurso. Fazer teste com instâncias desativadas antes de excluir qualquer objeto. | P1 |
| HUD de gameplay | A barra superior, a caixa de missão, o botão de tiro e o joystick ocupam grande área e competem com a leitura da rua. | Redesenhar a interface para o loop de carrinho e atendimento, removendo controles sem função no novo conceito depois de validar o novo fluxo. | P1 |

## Alterações feitas nesta cópia

1. Preservei uma referência antes dos ajustes em `Assets/Scenes/Girl power 1 - Sorvete - Referencia.unity`.
2. No objeto filho `Carro1`, alterei o deslocamento local Y de aproximadamente `9.3674` para `0`, alinhando o desenho ao objeto seguido/controlado.
3. Testei a lente da Cinemachine em `4.5`, mas restaurei `5.9`: o script de câmera desativa essa Virtual Camera em runtime, então o teste não é representativo do enquadramento jogável.
4. Importei os recursos essenciais do TextMesh Pro solicitados pelo Editor para eliminar a dependência ausente durante a abertura do projeto.

Os assets originais não foram substituídos. Nenhum script foi alterado nesta etapa. A cena do projeto original fora da cópia não foi editada por este ajuste.

## Resultado do teste

O menu e o HUD abrem em Play Mode. O veículo aceita entrada, mas sofre dano quase imediatamente. A cena não atende ainda ao critério principal de andar/correr a pé e chegar ao carrinho para iniciar um turno; por isso não há um comparativo legítimo de “protagonista correndo e chegando ao balcão” nesta versão. O próximo trabalho deve primeiro corrigir o percurso e a colisão inicial e, em seguida, implementar o controle a pé e a transição para o atendimento.

**Pendente:** fixar uma resolução de teste; capturar a mesma rua antes/depois com o personagem correndo; testar o percurso nos dois sentidos; validar o aparelho Android; gravar o vídeo do turno; implementar pedido, casquinha, pagamento e melhoria visível.
