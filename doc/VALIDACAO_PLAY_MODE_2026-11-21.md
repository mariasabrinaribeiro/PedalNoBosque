# Pedal no Bosque — validação em Play Mode

Projeto CarrinhoSorvete_Aula1, cena lateral FlorestaBicicleta, Windows, Unity 2022.3.62f3. Horários locais de 21 de novembro de 2026.

## Ambiente e execução

O Editor instalado respondeu à consulta de versão. A tentativa de iniciar outro processo em modo batch encerrou com código 199 após 60 segundos de espera pelo canal IPC do Unity Licensing Client. O projeto já estava aberto no desktop. Pelo Unity Hub, foi possível restaurar a janela, atualizar os scripts e executar o teste na instância gráfica com a licença disponível.

Os scripts do jogo e do Editor compilaram. O compilador apontou dois campos preexistentes sem uso, Enemy.scoreReward e DriverPro.regularSpeed.

## Alterações locais desta rodada

- O comando Tools → Pedal no Bosque → Validar cena, disponível no Editor com FlorestaBicicleta aberta, cria um perfil de teste e entra em Play Mode. Na execução bem-sucedida, sai do Play Mode, entra novamente e carrega o progresso do mesmo perfil. A saída do teste devolve o Editor ao modo de edição.
- A entrega automatizada usa o evento do botão Entregar da interface. A verificação compara os itens da cesta, as refeições recebidas pelo animal e o contador de entregas.
- O sistema de salvamento preserva a cópia de segurança válida ao reparar o arquivo principal. Antes, a rotina de recuperação chamava a gravação comum e copiava o arquivo principal danificado por cima do backup. O teste danifica somente o JSON do perfil temporário.

## Alcance dos resultados

O sanduíche preparado e o alimento pedido pelo esquilo são verificações distintas. Os botões são acionados pelo evento de clique do Unity. A pedalada usa os controles da própria bicicleta e o teste espera o deslocamento e a chegada do alimento. A passagem nas colinas verifica a entrada nas zonas de subida e descida. A altura do chão usada pela bicicleta ainda é fixa fora dos saltos.

A recarga ocorre entre duas entradas em Play Mode na mesma instância do Editor. Encerrar o aplicativo, reabrir um executável distribuído e testar em aparelho móvel são cenários adicionais.

## Registro das execuções

O trecho abaixo foi copiado do registro emitido pelo validador no projeto.

```text
2026-11-21T18:17:54 Inicio do teste interativo
2026-11-21T18:17:57 OK Progresso inicializado
2026-11-21T18:17:57 OK Perfil isolado novo
2026-11-21T18:17:57 OK BtnJogar
2026-11-21T18:17:57 OK Esquilo inicial presente
2026-11-21T18:17:58 OK Cozinha acessivel pela estrada
2026-11-21T18:17:58 OK Pedalada deslocou a bicicleta
2026-11-21T18:17:58 OK BtnPrepSanduiche
2026-11-21T18:17:58 OK BtnAutoPrep
2026-11-21T18:18:00 OK Sanduiche preparado
2026-11-21T18:18:00 OK FoodCard_Nozes
2026-11-21T18:18:00 OK Cesta abastecida com o pedido
2026-11-21T18:18:03 OK Animal ao alcance
2026-11-21T18:18:03 OK Animal recebeu o alimento
2026-11-21T18:18:04 OK Uma refeicao, um consumo e uma entrega registrada
2026-11-21T18:18:04 OK Entrega gravada no disco
2026-11-21T18:18:09 OK Travessia da primeira emenda
2026-11-21T18:18:29 OK Entrada na subida
2026-11-21T18:18:32 OK Entrada na descida
2026-11-21T18:18:32 OK Mudanca do periodo do dia
2026-11-21T18:18:32 PASSED
2026-11-21T18:18:36 OK Progresso inicializado
2026-11-21T18:18:36 OK Entrega preservada em outra sessao
2026-11-21T18:18:36 PASSED
2026-11-21T18:19:12 Inicio do teste interativo
2026-11-21T18:19:16 OK Progresso inicializado
2026-11-21T18:19:16 OK Perfil isolado novo
2026-11-21T18:19:16 OK BtnJogar
2026-11-21T18:19:16 OK Esquilo inicial presente
2026-11-21T18:19:17 OK Cozinha acessivel pela estrada
2026-11-21T18:19:17 OK Pedalada deslocou a bicicleta
2026-11-21T18:19:17 OK BtnPrepSanduiche
2026-11-21T18:19:17 OK BtnAutoPrep
2026-11-21T18:19:18 OK Sanduiche preparado
2026-11-21T18:19:18 OK FoodCard_Nozes
2026-11-21T18:19:18 OK Cesta abastecida com o pedido
2026-11-21T18:19:21 OK Animal ao alcance
2026-11-21T18:19:21 OK BtnComida
2026-11-21T18:19:21 OK Animal recebeu o alimento
2026-11-21T18:19:22 OK Uma refeicao, um consumo e uma entrega registrada
2026-11-21T18:19:22 OK Entrega gravada no disco
2026-11-21T18:19:27 OK Travessia da primeira emenda
2026-11-21T18:19:55 OK Entrada na subida
2026-11-21T18:19:58 OK Entrada na descida
2026-11-21T18:19:58 OK Mudanca do periodo do dia
2026-11-21T18:19:58 PASSED
2026-11-21T18:20:02 OK Progresso inicializado
2026-11-21T18:20:02 OK Entrega preservada em outra sessao
2026-11-21T18:20:02 OK Backup de teste criado
2026-11-21T18:20:02 OK Entrega recuperada do backup
2026-11-21T18:20:02 OK Backup preservado durante a recuperacao
2026-11-21T18:20:02 OK Arquivo principal reparado
2026-11-21T18:20:02 PASSED
```
