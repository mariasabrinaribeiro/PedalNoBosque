# Pedal no Bosque

Projeto Unity 2D independente. Abra esta pasta, que contém Assets, Packages e ProjectSettings, pelo Unity Hub com o Editor 2022.3.62f3.

A cena principal é `Assets/Scenes/FlorestaBicicleta.unity`. Ela é a primeira e única cena habilitada para build. O protótipo de visão superior está em `Assets/Scenes/FlorestaBicicletaTopDown.unity`. Os dois modos compartilham tipos de alimentos e recursos gráficos.

Para validar a versão importada, abra a cena principal e selecione **Tools → Pedal no Bosque → Validar cena**. O teste usa um perfil temporário e registra o resultado em `Temp/PedalPlayMode.log`.

A configuração inicial usa a versão 0.1.0 e o identificador Android com.wisocki.pedalnobosque. A assinatura de distribuição deverá ser configurada quando houver uma versão para publicação.

## Aparência da cena lateral

A capa usa a ilustração `Assets/Resources/Floresta/abertura_bosque.png` com o título e os controles desenhados pela interface. Isso permite traduzir os rótulos e ajustar o recorte da imagem ao formato da janela. O botão dourado usa um ícone triangular próprio.

Na trilha, o carvalho ilustrado aparece em duas camadas de paralaxe. As árvores distantes têm menor escala e contraste, e as flores aparecem em pequenos grupos nas margens. O teste acessível pelo menu Tools também confere o carregamento da capa e a transição para a partida.

### Próximas prioridades visuais

1. **Relevo da trilha.** Alinhar a silhueta da estrada e a altura das rodas nas colinas. Hoje a troca de zona altera a velocidade e a inclinação da bicicleta, enquanto o chão permanece plano.
2. **Identidade dos biomas.** O percurso já distingue regiões por elementos locais. Criar silhuetas adicionais de árvores para o rio e o Bosque da Lua reduziria a repetição do carvalho em sessões longas.
3. **Leitura em telas pequenas.** No Editor, a cesta e o pedido dividem uma faixa estreita. Vale medir em aparelho físico se a criança consegue identificar o alimento durante a pedalada. A mensagem de entrega deve ter prioridade sobre informações permanentes de pontuação.
