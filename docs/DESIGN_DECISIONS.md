# Decisões de design — Passo 1

## 1. Física: Rigidbody (decidido)
Kart usa `Rigidbody` da Unity, não controlador custom. A velocidade é
"autorada" a cada `FixedUpdate` a partir do input (não acumulada via
`AddForce`), técnica clássica de kart arcade: dá controle previsível e faz
colisão com muro virar deslizamento suave em vez de ricochete caótico.
Ganhamos de graça: gravidade, resolução de colisão com a pista, sem
reinventar física. Se o "feel" não for bom o suficiente com esse approach,
o próximo ajuste é em cima do mesmo Rigidbody (impulsos na colisão, curvas
de aceleração/turn), não uma reescrita.

## 2. Câmera: 3ª pessoa atrás do kart (decidido, como pedido)
`ChaseCamera.cs`: segue atrás/acima do kart (`SmoothDamp` na posição,
`Slerp` na rotação, com "look ahead" na direção do kart). Padrão de kart
racer.

## 3. Pausa para escolha de habilidade/item — proposta multiplayer-friendly
**Default v1 (implementar nos passos 4/5, não implementado ainda):
pausar `Time.timeScale = 0` enquanto a UI de escolha está aberta.**
É a opção mais simples e mais clara pro jogador, e como no protótipo só
existe 1 humano, pausar tudo não prejudica ninguém.

Mas para não precisar reescrever isso quando multiplayer entrar, a
recomendação é **não implementar a escolha como "pausa + bloqueia código
até o clique"**, e sim como um evento de dados desacoplado do loop do jogo:

- `DecisionRequest { playerId, kind (LevelUp | Item), options[], deadline }`
  é criado quando o gatilho acontece (volta completa / caixa coletada).
- Um componente por kart (`DecisionController`, a implementar no passo 4)
  guarda o `DecisionRequest` pendente e expõe `ApplyDecision(playerId, optionId)`
  como o único ponto que aplica o efeito (upgrade permanente ou item).
- Em single-player, a UI chama `ApplyDecision` diretamente após o clique,
  e o `Time.timeScale = 0` cuida do "resto do jogo parado". Isso já
  funciona sozinho.
- O ponto chave: **a lógica de decisão nunca fica sabendo que o jogo está
  pausado ou não** — ela só recebe opções e retorna uma escolha. Quando
  multiplayer entrar, a mudança é local:
  - Cada `DecisionRequest` ganha um timeout com escolha padrão automática
    (ex.: 5s, senão pega a primeira opção) — necessário porque não dá pra
    pausar a física dos outros jogadores no jogo de ninguém, e é como
    Mario Kart/CTR já funcionam hoje (caixa de item não trava os outros).
  - Só o kart do jogador que está escolhendo tem seu input "congelado ou
    mantido reto" enquanto a UI dele está aberta — é só client-side/local,
    não afeta os outros karts, que continuam simulando normalmente.
  - `ApplyDecision` passa a ser chamado a partir de uma resposta de rede
    (RPC do cliente pro servidor) em vez de uma chamada local direta —
    mas a assinatura e o efeito continuam os mesmos.

Ou seja: construir a decisão como request/response de dados desde o passo
4, mesmo rodando 100% local, é o que evita reescrever a arquitetura depois.
Não precisa decidir agora se o multiplayer futuro será client-server
autoritativo ou peer-to-peer — esse desenho de evento funciona pros dois.

## Passo 2 — IA por waypoints
`KartAIDriver` reusa `TrackData.CenterlinePoints` (os mesmos pontos usados
pra desenhar a pista no passo 1) como lista de waypoints — não existe uma
lista de waypoints separada pra IA, evita as duas ficarem dessincronizadas
se a pista mudar de forma no futuro. A IA calcula o ângulo até o próximo
ponto, e usa esse ângulo pra três coisas: quanto virar, quanto cortar
acelerador em curva fechada, e se deve dar drift. Sem rubber-banding
(ajuste de velocidade da IA baseado na posição do jogador) por enquanto —
isso é um ajuste de "sensação de disputa" que só faz sentido calibrar
depois que voltas/posição de corrida existirem (passo 3), e é mais fácil
adicionar depois em cima do `KartAIDriver` do que prever agora.

`KartFactory.SpawnKart` não decide mais sozinho se o kart é do jogador —
ele só monta o kart (corpo, collider, rigidbody, `KartController`) e quem
chama decide se anexa `KartInput` (jogador) ou `KartAIDriver` (IA). Isso
existe justamente pra esse passo: sem essa separação, todo kart nasceria
com input de teclado.

## Passo 3 — checkpoint/detecção de volta
`CheckpointBuilder` distribui 8 gates (triggers, sem colisão física) em
pontos igualmente espaçados da mesma `CenterlinePoints` usada pra desenhar
a pista — garante que os gates ficam alinhados com a pista sem precisar
sincronizar duas listas separadas. Cada gate carrega um `Checkpoint.Index`
sequencial; índice 0 é a linha de largada/chegada.

`LapTracker` é anexado em todo kart (jogador e IA — voltas não são
conceito exclusivo do jogador, e IA vai precisar disso pra posição de
corrida mais pra frente). A regra é simples e clássica de kart racer: o
kart só pode disparar o gate N se o próximo esperado for N (senão ignora).
Como o kart nasce em cima/perto do gate 0, mas o primeiro índice esperado
já é 1, o cruzamento inicial da linha de largada é ignorado automaticamente
— só quando o kart passa por 1, 2, ..., N-1 em ordem e volta a cruzar o
gate 0 é que conta como volta completa. Isso, de graça, impede: cortar
caminho pulando checkpoints, e dar ré pra cruzar a linha de chegada de trás
pra frente pra "completar" voltas.

Feedback visual: os gates são cubos finos e coloridos (visíveis de
propósito, não invisíveis) pra dar pra conferir no Editor se estão bem
posicionados; e um HUD mínimo via `OnGUI` (`RaceHud`) mostra `Lap: N` pro
jogador. Isso é só pra dar visibilidade nesse passo — não é o sistema de UI
que vai ser usado pra escolha de habilidade/item (isso é decisão do passo
4/5, provavelmente Canvas de verdade).

Ainda não existe: posição de corrida (1º/2º/3º), fim de corrida (N voltas
e acabou), nem tempo de volta. Não é necessário pra "detectar volta" —
fica pra quando isso importar de fato pro gameplay.

## Passo 4 — level up por volta
Implementado exatamente como desenhado na seção 3 acima: `ChoicePrompt`
é o tipo genérico de "opção com efeito" (título, descrição, `Action Apply`)
que tanto o level up quanto os itens do passo 5 vão usar — a UI
(`PauseChoiceUI`) e o pause não sabem se estão mostrando um upgrade ou um
item, só sabem mostrar uma lista de `ChoicePrompt` e chamar `Apply` no que
for clicado. `KartUpgrade`/`KartUpgradeCatalog` são a parte específica de
level up: dado + catálogo de 4 upgrades, cada um vira um `ChoicePrompt`
sorteando 3 sem repetição a cada volta (`LevelUpController`).

Upgrades são aplicados direto nos campos públicos do `KartController`
(multiplicativo, então empilha se escolher o mesmo de novo numa volta
seguinte) — não existe um sistema de "stats base + modificadores"
separado; pra 4 upgrades simples isso seria over-engineering agora. Se a
lista de upgrades crescer bastante ou precisar de upgrades temporários
(tipo os itens do passo 5), aí sim vale revisitar.

IA não usa `LevelUpController` (não tem UI pra mostrar pra ninguém) — ao
completar volta, aplica um upgrade aleatório do mesmo catálogo direto,
sem pausa. Isso não estava no pedido original, é uma decisão de default:
sem isso, o jogador ficaria trivialmente mais forte a cada volta enquanto
a IA fica parada, o que underminaria testar se a camada roguelike é
divertida (ela deixaria de ser um desafio rapidamente). Se não for o
comportamento desejado, é uma linha pra remover em `GameBootstrap`.

## Passo 5 — caixa de item
Reusa a mesma arquitetura do passo 4 quase sem mudança: `ItemDefinition`
tem o mesmo formato de `KartUpgrade` (nome, descrição, `Action<KartController>`),
`ItemBox` monta uma lista de `ChoicePrompt` a partir de `ItemCatalog.All` e
abre no mesmo `PauseChoiceUI`. Isso confirma a aposta feita nos passos
anteriores: a UI de pausa+escolha não precisou mudar uma linha pra servir
os itens, só quem constrói as opções mudou.

**Decisão: item é aplicado na hora que é escolhido, não fica guardado num
slot pra usar depois com um botão.** O pedido original ("pausa, escolhe 1
de 4 itens") não deixava claro se o item vira um "held item" estilo Mario
Kart (aperta um botão depois pra ativar) ou se o efeito já é a própria
escolha. Optei pelo segundo porque: (1) reaproveita 100% o pipeline de
`ChoicePrompt`/`PauseChoiceUI` já construído pro level up, sem precisar de
inventário, slot de UI, nem botão de "usar item"; (2) pra escudo e nitro
faz sentido ativar na hora mesmo; pra mancha de óleo e pulso de choque,
"escolher" já é o momento tático (você decide se quer isso agora, vendo
quem está por perto). Se no futuro fizer sentido guardar item pra usar no
momento certo (ex.: guardar escudo pra quando alguém for te atacar), isso
vira um `HeldItem` + um input de "usar" — dá pra adicionar em cima do que
já existe (`ItemDefinition.Use` continua sendo o efeito; só muda quando
ele é chamado), não precisa reescrever.

Os 4 itens cobrem tipos diferentes de efeito de propósito: Nitro (buff
instantâneo em si mesmo), Escudo (buff temporizado em si mesmo, bloqueia
`ApplySlow`), Mancha de óleo (obstáculo largado no mundo, afeta quem
passar por cima depois — inclusive você mesmo se der ré), Pulso de choque
(ofensivo instantâneo em área, via `Physics.OverlapSphere` nos karts
próximos). Os efeitos temporários (boost, escudo, lentidão) viraram estado
novo no `KartController` (`ApplyItemBoost`, `ApplyShield`, `ApplySlow`),
separado do boost do mini-turbo do passo 1 pra poderem empilhar em vez de
se sobrescrever.

IA que toca a caixa não vê o painel — ganha um item aleatório do catálogo
aplicado na hora, mesmo raciocínio do level up automático da IA (passo 4):
sem isso a IA nunca teria Nitro/Escudo/etc. e ficaria em desvantagem cada
vez maior conforme o jogador acumula itens ao longo da corrida.

Caixa de item reaparece depois de um cooldown (`ItemBox`, coroutine) em
vez de sumir depois de coletada — numa corrida de várias voltas, caixa que
não volta significa a pista ficar sem item já na segunda volta.

## Input: teclado + controle, incluindo Steam Deck
Pedido: o jogo tem que rodar no Steam Deck e reconhecer controle ou
teclado. Decisões tomadas:

**Legacy Input Manager em vez do pacote novo Input System.** A Unity tem
dois sistemas de input: o legado (`UnityEngine.Input`, configurado via
`ProjectSettings/InputManager.asset`) e o pacote novo `com.unity.inputsystem`
(action maps, mais moderno, também funciona bem no Deck). Fiquei com o
legado porque: (1) o pacote novo precisaria de mais um pacote no
`manifest.json` (mais uma dependência baixada na primeira abertura) e de
um asset de Input Actions com schema próprio — dando pra hand-authorar
sem Editor, mas é bem mais arriscado de acertar sem poder validar; (2) o
legado já resolve o pedido (teclado + controle reconhecidos) com uma
mudança pequena e de baixo risco: um `InputManager.asset` com 4 entradas
de eixo, que eu validei sintaticamente com um parser YAML (não é o
parser da Unity, mas confirma que a estrutura/campos batem com o que a
Unity espera). Se mais pra frente precisar de coisa que o legado não
faz bem (rebind de tecla pelo jogador, vibração/rumble, mostrar o ícone
certo do botão dependendo do controle conectado), aí vale migrar — não
antes.

**Acelerar/ré no eixo Y do analógico, não nos gatilhos.** Gatilho
analógico (RT/LT) exigiria mapear o "3º/4º eixo" do joystick, cujo índice
varia bastante entre driver/SO — no Linux/Steam Deck especificamente isso
é uma fonte conhecida de inconsistência com o Input Manager legado (é
inclusive uma das razões de existir o Input System novo). Manter tudo no
analógico esquerdo (X = virar, Y = acelerar/ré, igual W/S no teclado)
evita esse problema por completo e continua sendo um esquema de controle
comum em jogo de corrida.

**Drift lido direto por `KeyCode.JoystickButton0`/`5`, sem depender do
`InputManager.asset`.** Filtrei pra usar `KeyCode` puro em vez de mais
eixos nomeados no arquivo de config: `KeyCode.JoystickButtonN` (sem
número de joystick) já significa "botão N em qualquer controle
conectado" nativamente na Unity, então funciona sem eu precisar acertar
mais nenhuma entrada de YAML. Isso reduz a superfície de coisa que pode
dar errado sem eu poder testar.

**Painel de pausa (level up/item) ganhou navegação por teclado/controle.**
Isso não era sobre o Steam Deck especificamente — era um buraco real: o
`PauseChoiceUI` só aceitava clique de mouse, então um jogo 100% teclado ou
100% controle travava sem solução no primeiro level up. Corrigido com
seta/analógico pra navegar e Enter/Space/botão sul pra confirmar, mouse
continua funcionando também.

**Fora do escopo por decisão, não por esquecimento:**
- Ícones de botão específicos do Steam Deck (mostrar o botão real do
  Deck em vez de "A"/"RB" genérico) exige integrar a Steamworks SDK
  (`ISteamInput`) — é uma dependência nativa e créditos de App ID que não
  faz sentido puxar pra esse estágio do protótipo.
- Não existe pipeline de build/empacotamento pra Steam neste repo (não
  foi pedido, e não dá pra testar isso sem Steamworks/loja configurada).
- Nada disso foi testado num Deck real ou mesmo no Editor — ver
  checklist de verificação no `README.md`.

## Pista e kart
- Pista: oval fechado gerado por código (`TrackBuilder.cs`), cubos para
  pista/muros, sem malha externa.
- Kart: primitivas (cubo + cápsula + cilindros), sem collider múltiplo —
  um único `BoxCollider` no root pra evitar jitter de física.
- Toda a cena (`Prototype_KartMovement.unity`) é montada em runtime por
  `GameBootstrap.cs` via `RuntimeInitializeOnLoadMethod`. A cena `.unity`
  em si fica praticamente vazia — isso foi proposital: elimina a
  necessidade de referenciar GUIDs de script à mão no YAML da cena (risco
  alto de erro sem Editor disponível pra validar). Nos próximos passos, dá
  pra mover essa construção pro Editor (GameObjects reais na cena) se for
  mais conveniente para inspecionar/ajustar valores visualmente.
