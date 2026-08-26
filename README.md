# roguelike-racing

Protótipo de corrida arcade 3D (estilo Mario Kart/CTR) com camada roguelike:
level up por volta e escolha de item ao coletar caixa. Ver
`docs/DESIGN_DECISIONS.md` para as decisões de arquitetura tomadas até
agora.

## Status atual: Passo 3 — checkpoint/detecção de volta

Implementado:
- Kart com `Rigidbody`, aceleração/freio/ré, curva sensível à velocidade,
  drift com boost ao soltar (mini-turbo), tudo com física simples da Unity.
- Pista oval mínima gerada por código, feita de primitivas (sem assets
  externos).
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

Não implementado ainda (propositalmente, por ordem de trabalho): level up,
caixas de item, rubber-banding, posição de corrida (1º/2º/3º), fim de
corrida (N voltas). Ver `docs/DESIGN_DECISIONS.md` para a proposta de como
o level up/item vai ser feito sem travar em multiplayer futuro.

## Abrir no Unity

1. Abra a pasta do projeto no Unity Hub / Unity Editor **2022.3 LTS**
   (`ProjectSettings/ProjectVersion.txt` pede `2022.3.21f1`; qualquer patch
   2022.3.x deve funcionar).
2. Primeira abertura vai baixar os pacotes do `Packages/manifest.json`
   (precisa de internet) e vai gerar os arquivos `.meta` que faltam — isso
   é esperado, este repo não commitou `.meta` ainda. **Depois de abrir uma
   vez, rode `git status` e commite os `.meta` gerados** para fixar os GUIDs.
3. Abra a cena `Assets/Scenes/Prototype_KartMovement.unity` e dê Play.
   A cena em si está praticamente vazia — todo o cenário (pista, kart,
   câmera, luz) é montado em runtime por `GameBootstrap.cs`. Não precisa
   arrastar nada manualmente.

## Controles

- Acelerar / ré: `W`/`↑` e `S`/`↓`
- Virar: `A`/`←` e `D`/`→`
- Drift: `Shift` ou `Space` (segure enquanto vira; solte para ganhar o
  boost do mini-turbo se segurou tempo suficiente)

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
  Track/    TrackBuilder, Checkpoint, CheckpointBuilder (pista + gates procedurais)
  Race/     LapTracker, RaceHud
  Camera/   ChaseCamera
  Core/     GameBootstrap (monta tudo em runtime)
Assets/Scenes/
  Prototype_KartMovement.unity
Tests/RoguelikeRacing.Logic.Tests/
  Testes headless (dotnet test) do KartPhysicsMath
docs/
  DESIGN_DECISIONS.md
```
