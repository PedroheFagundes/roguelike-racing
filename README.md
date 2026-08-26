# roguelike-racing

Protótipo de corrida arcade 3D (estilo Mario Kart/CTR) com camada roguelike:
level up por volta e escolha de item ao coletar caixa. Ver
`docs/DESIGN_DECISIONS.md` para as decisões de arquitetura tomadas até
agora.

## Status atual: Passo 6 + HUD/correções de feedback de jogo

Implementado:
- Kart com `Rigidbody`, aceleração/freio/ré, curva sensível à velocidade,
  drift com boost ao soltar (mini-turbo), tudo com física simples da Unity.
- 3 pistas selecionáveis (`TrackCatalog`), geradas por código, sem assets
  externos: **Oval** (curvas largas e contínuas), **Estádio** (retas
  longas + curva fechada em cada ponta) e **Técnica** (curvas apertadas
  alternadas). `TrackBuilder` foi separado em "gerar pontos da centerline"
  (uma função por traçado) + "construir pista a partir de pontos" (lógica
  única, reaproveitada pelas 3) — pista nova = só escrever mais um gerador
  de pontos.
- 3 personagens selecionáveis (`CharacterCatalog`): **Equilibrado**,
  **Veloz** (+velocidade máxima, -aceleração/curva) e **Ágil**
  (+aceleração/curva, -velocidade máxima) — ainda só primitiva + cor
  diferente, sem arte nova. O jogador escolhe 1 na tela inicial; a IA
  fica automaticamente com os outros 2, então todo personagem sempre
  aparece na corrida.
- Tela de setup pré-corrida (`RaceSetupUI`): escolhe pista e personagem
  antes de qualquer coisa ser montada, com suporte completo a mouse,
  teclado e controle (mesmo padrão de navegação do painel de level
  up/item).
- Câmera de 3ª pessoa seguindo o kart do jogador.
- 2 oponentes de IA (`KartAIDriver`) seguindo os waypoints da pista (o
  mesmo `CenterlinePoints` usado para desenhar a pista): mira no próximo
  ponto, acelera, corta acelerador em curva fechada e ativa drift acima de
  um ângulo — sem rubber-banding, sem desvio de obstáculo. Karts largam
  num grid escalonado atrás do jogador (índices diferentes da centerline +
  deslocamento lateral) pra não nascer um em cima do outro.
- 8 gates de checkpoint (`CheckpointBuilder`) espalhados pela centerline,
  como triggers visíveis (cubos finos coloridos, ciano; o de largada/
  chegada em amarelo). `LapTracker`, anexado em todo kart (jogador e IA),
  exige que os gates sejam cruzados em ordem antes de contar uma volta —
  isso impede cortar curva ou dar ré pela linha de chegada pra pontuar.
  Volta completa loga no console e aparece num HUD simples (`RaceHud`,
  `OnGUI`) só pro jogador.

- Level up por volta (`LevelUpController`, só no kart do jogador): ao
  completar volta, pausa o jogo (`Time.timeScale = 0`) e mostra um painel
  (`PauseChoiceUI`, `OnGUI`) com 3 upgrades sorteados sem repetição de um
  catálogo de **7** (`KartUpgradeCatalog`): +velocidade máxima,
  +aceleração, +taxa de curva, +boost do mini-turbo, **Blindagem**
  (resiste a mancha de óleo/pulso de choque), **Tração** (atinge curva
  máxima com menos velocidade) e **Reflexo de piloto** (mini-turbo exige
  menos tempo de drift). Efeito permanente e cumulativo (multiplicativo)
  pro resto da corrida. IA não vê esse painel — ao completar volta, ganha
  um upgrade aleatório do mesmo catálogo aplicado na hora, sem pausa, só
  pra não ficar pra trás do jogador enquanto ele sobe de nível.

- Caixas de item (`ItemBox`, `ItemBoxBuilder`): 5 caixas giratórias
  espalhadas pela pista, alternando lado esquerdo/direito da linha de
  corrida. Ao tocar, o jogador pausa e escolhe 1 dos 4 itens
  (`ItemCatalog`, mesmo painel `PauseChoiceUI` do level up): Nitro
  (velocidade temporária), Escudo (bloqueia o próximo efeito ofensivo),
  Mancha de óleo (larga um obstáculo atrás que desacelera quem passar por
  cima) e Pulso de choque (desacelera na hora todo mundo perto). **Item
  escolhido fica guardado** (`KartInventory`, 1 slot) até o jogador
  apertar o botão de usar — não é mais aplicado na hora da escolha. IA
  toca a caixa, ganha um item aleatório do mesmo catálogo e usa na hora
  (ela ainda não tem estratégia de "quando" usar). Caixa reaparece depois
  de um cooldown em vez de sumir de vez.
- IA também sobe de nível e usa item — ela nunca ficou de fora disso:
  ao completar volta ganha upgrade aleatório do catálogo (mesmo mecanismo
  desde o passo 4), e ao tocar caixa de item ganha e usa item aleatório
  (passo 5, agora passando pelo mesmo `KartInventory` do jogador). Sem
  isso o jogador ficaria trivialmente mais forte a cada volta sem
  nenhuma resposta da IA.
- HUD do jogador (`RaceHud`) agora mostra: volta atual, **posição na
  corrida** (`RaceStandings`, 1º/2º/3º entre os 3 karts, calculado por
  quantos checkpoints+voltas cada um já passou), item guardado, e os
  comandos pra usar item/drift. Aviso grande de **CONTRAMÃO** aparece
  quando a velocidade do kart aponta contra a direção da pista
  (`WrongWayDetector`, só no jogador).
- Muro da pista corrigido: em curvas fechadas (Estádio, Técnica) sobrava
  um vão entre um segmento de muro e o próximo, deixando o kart vazar pra
  fora. Adicionei um "poste" redondo em cada vértice da pista (dos dois
  lados) que fecha esse vão em qualquer ângulo de curva, e aumentei a
  espessura padrão do muro. Ver `docs/DESIGN_DECISIONS.md` pro diagnóstico
  completo — não consegui reproduzir/ver o bug aqui, então vale confirmar
  que sumiu depois de testar.
- Menus (`PauseChoiceUI`, `RaceSetupUI`, `RaceHud`) escalam com o tamanho
  real da tela agora (`OnGuiScale`), em vez de pixel fixo — ficavam
  desproporcionalmente pequenos em monitor grande. Nunca encolhe abaixo do
  tamanho original, só cresce em tela maior que a referência (600px de
  altura).

Não implementado ainda (propositalmente, por ordem de trabalho):
rubber-banding, fim de corrida (N voltas). Ver `docs/DESIGN_DECISIONS.md`
para a arquitetura de decisão compartilhada entre level up e item, pensada
pra não travar quando multiplayer entrar, e para as decisões do passo 6
(pistas/personagens/upgrades) e desta rodada (HUD/contramão/vazamento/item
guardado).

## Teclado + controle (gamepad), incluindo Steam Deck

Todo input do jogador — dirigir e os painéis de pausa (level up/item) —
reconhece teclado e controle ao mesmo tempo, sem precisar trocar de modo:

- `ProjectSettings/InputManager.asset` (commitado explicitamente, não
  gerado pelo Unity) faz os eixos `Horizontal`/`Vertical` lerem teclado
  (WASD/setas) **e** o analógico esquerdo do primeiro joystick conectado.
  Acelerar/ré usa o eixo vertical do analógico (frente/trás), não os
  gatilhos — ver `docs/DESIGN_DECISIONS.md` pra saber por quê.
- Drift lê `KeyCode.JoystickButton0`/`5` (botão sul / ombro direito, "em
  qualquer joystick") além de `Shift`/`Space`.
- `PauseChoiceUI` (o painel de level up e de item) agora também navega com
  seta cima/baixo ou analógico/d-pad, e confirma com Enter/Space/botão sul
  — antes só dava pra clicar com o mouse, o que quebrava jogo 100%
  teclado ou 100% controle bem no primeiro level up. Clique com mouse
  continua funcionando.

No Steam Deck especificamente: o Steam Input, no template padrão de
"Gamepad", emula um controle Xbox 360 (XInput) pro jogo — é por isso que
"ler joystick genérico" funciona no Deck sem nenhum código específico da
Valve. O que isso NÃO cobre (fora do escopo por enquanto, ver
`docs/DESIGN_DECISIONS.md`): ícones de botão que mostram os botões reais
do Deck (isso exige integrar a Steamworks SDK), e build/empacotamento pra
Steam em si (não existe pipeline de build neste repo). **Nada disso foi
testado em hardware real** — não tenho Editor nem um Deck aqui; ver seção
de verificação abaixo.

## Abrir no Unity

**Testado e rodando em Unity 6 (`6000.5.7f1`)** — `.meta` e
`ProjectSettings/*` já estão commitados a partir dessa versão real (não
mais gerados na hora, como nas primeiras vezes). O código usa a API atual
do `Rigidbody` (`linearVelocity`, `linearDamping`) e `PhysicsMaterial`
(sem o "s" era o nome antigo, pré-Unity 6) — se abrir numa versão mais
antiga (2022.3 LTS, por exemplo) vai dar erro de compilação nessas
chamadas e vai precisar reverter pros nomes antigos.

1. Abra a pasta do projeto no Unity Hub / Unity Editor **6000.5.x** (ou
   mais recente da série Unity 6).
2. Primeira abertura pode baixar/atualizar pacotes do
   `Packages/manifest.json` (precisa de internet).
3. Abra a cena `Assets/Scenes/Prototype_KartMovement.unity` e dê Play.
   A cena em si está praticamente vazia — todo o cenário (tela de setup,
   pista, kart, câmera, luz) é montado em runtime por `GameBootstrap.cs`.
   Não precisa arrastar nada manualmente.
4. Escolha pista e personagem na tela inicial e clique (ou confirme com
   teclado/controle) em "Iniciar corrida".

## Controles

| Ação | Teclado | Controle |
| --- | --- | --- |
| Acelerar / ré | `W`/`↑` e `S`/`↓` | Analógico esquerdo (frente/trás) |
| Virar | `A`/`←` e `D`/`→` | Analógico esquerdo (esquerda/direita) |
| Drift | `Shift` ou `Space` | Botão sul (A/Cross) ou ombro direito (RB/R1) |
| **Usar item guardado** | `E` ou `Ctrl` | Botão oeste (X/Square) |
| Navegar (setup / painel de escolha) | Setas ou `WASD` | Analógico ou d-pad |
| Confirmar escolha | `Enter` ou `Space` | Botão sul (A/Cross) |

Drift: segure enquanto vira; solte para ganhar o boost do mini-turbo se
segurou tempo suficiente. Item: escolher no painel da caixa só *guarda* o
item (mostrado no HUD) — apertar o botão de usar é que ativa o efeito. O
painel de level up/item também aceita clique de mouse em qualquer opção,
além da navegação por teclado/controle acima.

## Verificar controle/Steam Deck (não testado neste ambiente)

Sem Editor nem hardware aqui, nada disto foi confirmado rodando de
verdade. Ao testar:

- [ ] Conectar um controle (Xbox/Xinput ou Steam Deck em modo Desktop com
      controle) antes de dar Play e conferir se dirige com o analógico
      esquerdo e faz drift com A/RB.
- [ ] Conferir que teclado continua funcionando junto (sem precisar
      desconectar o controle).
- [ ] Completar uma volta / pegar um item e navegar o painel só com
      teclado (sem mouse), depois só com controle (sem mouse).
- [ ] No Steam Deck: rodar via Steam (Desktop Mode primeiro é mais fácil
      de depurar) com o template de Input padrão "Gamepad" e repetir os
      itens acima.
- [ ] Build Linux nativo (Unity Hub → instalar "Linux Build Support") ou
      build Windows rodando via Proton — qualquer um deveria funcionar,
      já que o projeto não usa plugins nativos nem nada específico de
      plataforma.

## Testes headless (fora do Editor)

A matemática pura do kart (integração de velocidade, curva de giro, boost
de drift) está isolada em `Assets/Scripts/Kart/KartPhysicsMath.cs`, sem
depender de `UnityEngine`. Isso permite testar sem abrir o Editor:

```bash
cd Tests/RoguelikeRacing.Logic.Tests
dotnet test
```

(Não validado neste ambiente — sem `dotnet` disponível aqui. Os testes
foram revisados manualmente linha a linha contra a implementação, mas
rodar `dotnet test` de verdade antes de confiar neles é recomendado.)

Verificação visual real (feel de direção, drift, câmera) depende de abrir
o Editor e jogar — isso não foi validado neste ambiente.

## Estrutura

```
Assets/Scripts/
  Kart/     KartController, KartInput, KartAIDriver, KartPhysicsMath, KartFactory
  Track/    TrackBuilder, TrackCatalog, Checkpoint, CheckpointBuilder
            (pista procedural + 3 tracados + gates)
  Race/     LapTracker, RaceHud, RaceStandings, WrongWayDetector,
            LevelUpController, PauseChoiceUI, ChoicePrompt, OnGuiScale,
            KartUpgrade, KartUpgradeCatalog,
            ItemBox, ItemBoxBuilder, ItemDefinition, ItemCatalog,
            ItemHazards, OilSlickHazard, KartInventory,
            CharacterDefinition, CharacterCatalog
  Camera/   ChaseCamera
  Core/     GameBootstrap (monta a corrida em runtime), RaceSetupUI
            (tela de escolha de pista/personagem)
Assets/Scenes/
  Prototype_KartMovement.unity
ProjectSettings/
  InputManager.asset (eixos teclado + joystick, commitado explicitamente)
Tests/RoguelikeRacing.Logic.Tests/
  Testes headless (dotnet test) do KartPhysicsMath
docs/
  DESIGN_DECISIONS.md
```
