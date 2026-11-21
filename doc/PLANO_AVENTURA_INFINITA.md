# Plano Mestre de Desenvolvimento — **PEDAL NO BOSQUE**

> **Tagline:** *Pequenas entregas. Grandes descobertas.*
> **Posicionamento:** *Uma pequena viagem. Um mundo inteiro para descobrir.*
> **Conceito Central:** A personagem (com seu cachorrinho reativo na cesta) não pedala apenas para chegar ao fim da pista — ela pedala porque o mundo está vivo, muda continuamente e sempre há um animal, morador ou segredo esperando para ser descoberto.

---

## Ciclo Principal de Experiência (Retenção)

1. **Explorar o Bosque** — Pedalar por 6 regiões com identidades próprias (paleta, som e eventos exclusivos).
2. **Descobrir algo inesperado** — Um segredo escondido, uma reação do ambiente, um filhote perdido ou um objeto raro.
3. **Interagir e ajudar** — Alimentar animais com respeito ao ritmo deles, preparar lanches, ajudar moradores e reconstruir a vila.
4. **Receber recompensas memoráveis** — Moedas, estrelas, registros no Livro dos Animais, animações raras, animais companheiros e novas músicas/cenários.
5. **Evoluir o universo** — Desbloquear construções na Vila dos Viajantes, decorar a casinha e abrir novas descobertas.

---

## As 6 Regiões do Universo (Identidade Própria)

Cada região combina **paleta de cores**, **trilha/ambiência sonora** e **acontecimentos exclusivos**:

| Região | Identidade Visual & Sensação | Fauna & Acontecimentos Exclusivos |
| :--- | :--- | :--- |
| **1. Vale das Flores** *(ex-Campo Florido)* | Campos floridos, borboletas, abelhas, riachos — *alegria e tranquilidade* | Coelhos, Esquilos, Passarinhos, Ouriços; flores que abrem ao passar e borboleta rara |
| **2. Montanhas do Vento** *(ex-Colinas)* | Subidas longas, descidas embaladas, rampas, folhas voando, mirantes — *aventura e movimento* | Cervos, Raposas, Filhotes de Urso; rajadas de vento e saltos com estrelas aéreas |
| **3. Vale das Águas** *(ex-Região do Rio)* | Rios, cachoeiras com arco-íris, pontes de madeira que balançam, ilhotas — *liberdade e contemplação* | Castores, Sapos, Quatis, peixes saltando; garrafa com mapa secreto no rio |
| **4. Floresta dos Segredos** *(ex-Trilha da Floresta)* | Árvores gigantes, cogumelos luminosos, raízes, tocas — *curiosidade e mistério* | Lobos, Guaxinins, Ouriços, Esquilos; raposa na toca e passagem na árvore antiga |
| **5. Vila dos Viajantes** *(ex-Pequena Vila)* | Casinhas, praça, músicos, postes e estações — *acolhimento e pertencimento* | Gatos, Moradores, Estação de Abastecimento; construções que evoluem com o progresso |
| **6. Bosque da Lua** *(Nova Região — Etapa 6/8)* | Vaga-lumes, flores noturnas, luz prateada, sons misteriosos — *encantamento e descoberta* | Corujas noturnas, animais raros e segredos exclusivos da noite/madrugada |

---

## Plano de Ação — 10 Etapas Passo a Passo

> **Status técnico:** Scripts das Etapas 1 a 5 **compilados sem erros** no Unity `2022.3.62f3` (`Assembly-CSharp.dll`). As Etapas 2 a 5 aguardam **validação visual e funcional em Play Mode** no Editor antes de avançarmos para a Etapa 6.

### Implementadas e Compiladas (Etapas 1 a 5)
- [x] **Etapa 1 — Física da Bicicleta, Lombadas/Saltos e Obstáculos:** subidas longas, descidas embaladas, rampas de salto com física aérea, obstáculos (pedras, raízes, troncos) e estrelas aéreas.
- [~] **Etapa 2 — Geração Procedural Infinita por Segmentos *(compilada — validar em Play Mode)*:** 5 regiões diurnas ativas (*Vale das Flores, Montanhas do Vento, Vale das Águas, Floresta dos Segredos e Vila dos Viajantes*), regras de segurança para obstáculos/água/cozinha, reciclagem de segmentos, placas de distância (`0.5 km`) e marcador em `km` na HUD. *(A 6ª região, **Bosque da Lua**, entra na Etapa 6 com o ciclo Dia/Noite.)*
- [~] **Etapa 3 — Ecossistema de Animais e Comportamentos *(compilada — validar em Play Mode)*:** 5 alimentos naturais por espécie (Nozes, Cenoura, Maçã, Mel, Sementes), 7 novos animais com artes e vozes (Ouriço, Guaxinim, Filhote de Urso, Passarinhos, Castor, Sapo, Coruja Noturna) e comportamentos especiais para o **Lobo** (cauteloso com velocidade) e **Gato** (acompanha a bicicleta).
- [~] **Etapa 4 — Estação de Abastecimento e Mini-Preparo Opcional *(compilada — validar em Play Mode)*:** coleta rápida de prontos/naturais + botão "Encher Cesta (Kit Rápido)" + mini-jogo opcional de montagem de Sanduíche e Hot Dog passo a passo para Bônus Gourmet (`+60 pts` no preparo + `+50 pts` extras na entrega).
- [~] **Etapa 5 — Moradores da Vila e Missões Aleatórias + Polimento Visual *(compilada — validar em Play Mode)*:** Criança, Senhor no Banco, Família no Piquenique, Viajante com Sede e Músico da Vila (com serenata e notas musicais) concedendo Estrelas (`★`), além de paralaxe em 6 camadas, Vale das Águas demonstrativo com ponte integrada e HUD discreta.
- [~] **Etapa 6 — Ciclo Completo de Dia e Noite (5 Períodos) + 6ª Região `Bosque da Lua` *(compilada — validar em Play Mode)*:** Manhã, Tarde, Pôr do Sol, Noite (lua crescente, estrelas piscando, vaga-lumes, farol na bicicleta e Coruja Noturna) e Madrugada, **todas as 6 regiões ativas** no gerador procedural (com flores luminosas e cogumelos encantados no Bosque da Lua), assinaturas musicais sutis por região e remoção de `FindObjectsOfType` no arremesso de comida.
- [~] **Etapa 7 — Colecionáveis, Livro dos Animais, Conquistas, Personalização e Persistência JSON *(implementada — validar em Play Mode)*:**
  - `SaveSystem.cs`: salvamento local em JSON com versionamento, validação, backup `.bak` e zero dados pessoais (100% offline).
  - `PlayerProgressManager.cs`: Livro de Descobertas (14 espécies com personalidade, alimento favorito, região, curiosidade e silhuetas para espécies não descobertas), 7 colecionáveis da natureza, 9 medalhas de conquistas e 25 itens de personalização (Bicicletas, Cestas, Acessórios, Personagem e Cachorrinho) desbloqueáveis por Estrelas (`★`), com suporte a equipar/retirar.
  - `TrailCollectibleItem.cs` e `DiscoveryBookAndWorkshopUI.cs`: colecionáveis espalhados pelas 6 regiões + Diário ilustrado de 3 abas na paleta do Pedal no Bosque.

### Próximos Passos (Passo a Passo)
- [ ] **Etapa 8 — Cenários Vivos e Exploração Ambiental:** flores que abrem ao passar, borboletas que acompanham a bike, cogumelos luminosos com partículas, peixes saltando no rio, ponte de madeira que balança suavemente, vento movimentando folhas/flores, grama inclinando e cachoeira com névoa/arco-íris.
- [ ] **Etapa 9 — Surpresas, Segredos e Acontecimentos Inesperados:** raposa que surge e some numa toca, esquilo escondendo objetos em árvores, coruja em noites específicas, árvore antiga com passagem secreta, mapa em garrafa no rio, arco-íris revelando área escondida, filhote perdido e borboleta rara conduzindo a um jardim secreto.
- [ ] **Etapa 10 — Progressão, Vila Evolutiva e Cachorrinho Reativo:** vila que evolui com as descobertas (reconstruir ponte, desbloquear praça, plantar flores/árvores, abrigo de animais, estação melhorada e decoração da casinha da personagem) + **cachorrinho reativo** na cesta que fareja segredos, late ao detectar tesouros/animais raros e reage emocionalmente aos eventos.

---

## Diretrizes de Marca & Divulgação

- **Nome sugerido:** **PEDAL NO BOSQUE** *(verificar disponibilidade futura na Google Play, App Store, domínios e INPI antes do lançamento comercial)*.
- **Comunicação:** vender a *sensação de descoberta*, não apenas uma corrida. Vídeos curtos de 10–20s mostrando gameplay real (paisagem viva, cachorrinho detectando segredo, transição dia/noite, salto no riacho, área secreta).
