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

## Passo 6 — mais pistas, personagens e upgrades

**Pistas: refatorei `TrackBuilder` em vez de duplicar código por pista.**
Separei "gerar a lista de pontos da centerline" (uma função por formato:
`GenerateOvalCenterline`, `GenerateStadiumCenterline`,
`GenerateTechnicalCenterline`) de "construir pista a partir de uma lista
de pontos" (`TrackBuilder.Build`, que não sabe nem precisa saber a forma —
só percorre pontos consecutivos e constrói pista/muro entre cada par).
Isso significa que toda a base já construída (checkpoints, waypoints de
IA, spawn de item box) continua funcionando sem mudar uma linha pras
pistas novas, porque tudo já consumia `TrackData.CenterlinePoints`
genericamente desde o passo 1 — só precisei trocar quem gera esses
pontos. Adicionar uma 4ª pista no futuro é só escrever mais um gerador de
pontos, não mexer no resto.

A pista "Técnica" usa um truque pra garantir que o traçado não se cruza
sozinho sem eu precisar validar isso visualmente (não tenho Editor):
gero os pontos com ângulo estritamente crescente ao redor de um centro
(uma volta completa, um ponto por ângulo), variando só o raio a cada
ponto. Isso torna o polígono "estrelado" em relação ao centro por
construção — geometricamente garantido não se auto-intersectar,
independente de quão bruscamente o raio varia entre pontos vizinhos. Foi
a forma de conseguir curvas fechadas/alternadas "de verdade" sem arriscar
uma pista com bug de geometria que eu não teria como perceber sem abrir o
Editor.

**Não fiz pista em formato de oito (cruzamento real).** Um cruzamento de
pista de verdade (tipo autódromo em 8) precisaria de desnível/rampa pra
os dois trechos não ocuparem o mesmo plano — isso é problema de
level design 3D de verdade (altura, rampa, colisão em Y), não só
"gerar mais pontos", e não dava pra validar sem testar fisicamente. Fica
pra depois se fizer sentido.

**Personagens: 3 arquétipos veloz/ágil/equilibrado, aplicados uma vez no
spawn.** `CharacterDefinition.ApplyTo` multiplica os mesmos campos
públicos do `KartController` que os upgrades de level up já usam
(`maxForwardSpeed`, `acceleration`, `baseTurnRateDegPerSec`) — a
diferença é que personagem aplica uma vez só, no spawn, e upgrade
continua empilhando depois disso a cada volta. Escolhi exatamente 3
porque bate com os 2 karts de IA existentes: jogador escolhe 1, os outros
2 personagens vão automaticamente pros 2 oponentes, garantindo que todo
arquétipo sempre aparece numa corrida. Isso cria um acoplamento implícito
(catálogo de personagem = karts de IA + 1) que documentei com comentário
no código — se um dia quiser mais personagens sem aumentar o número de
IA, essa conta precisa ser revisitada.

**Tela de setup pré-corrida em vez de menu separado/nova cena.**
`RaceSetupUI` segue o mesmo padrão OnGUI + navegação por teclado/
controle do `PauseChoiceUI` (não dava pra adicionar escolha de pista/
personagem e deixar isso mouse-only, seria o mesmo problema que corrigi
no painel de pausa). Não criei uma cena separada de menu porque isso
exigiria configurar `EditorBuildSettings.asset` (lista de cenas do build)
— mais um arquivo de projeto pra acertar sem Editor pra validar. Em vez
disso, o bootstrap simplesmente não constrói a corrida até o jogador
confirmar na tela de setup, tudo na mesma cena única já existente.

**Upgrades: catálogo foi de 4 pra 7, cobrindo mecânicas que ainda não
tinham upgrade nenhum.** Os 3 novos (Blindagem, Tração, Reflexo de
piloto) usam alavancas do `KartController` que os 4 antigos não tocavam:
resistência a lentidão (`slowResistance`, campo novo, usado dentro de
`ApplySlow`), limiar de velocidade pra atingir curva máxima
(`minSpeedFactorForFullTurn`) e tempo mínimo de drift pro mini-turbo
(`minDriftSecondsForBoost`). Blindagem em particular cria uma
interação de propósito com os itens do passo 5: reduz o efeito de mancha
de óleo/pulso de choque, então level up e item deixam de ser sistemas
isolados.

## Pista e kart
- Pista: fechada, gerada por código (`TrackBuilder.cs`), cubos para
  pista/muros, sem malha externa. 3 tracados desde o passo 6 (ver acima).
- Kart: primitivas (cubo + cápsula + cilindros), sem collider múltiplo —
  um único `BoxCollider` no root pra evitar jitter de física.
- Toda a cena (`Prototype_KartMovement.unity`) é montada em runtime por
  `GameBootstrap.cs` via `RuntimeInitializeOnLoadMethod`. A cena `.unity`
  em si fica praticamente vazia — isso foi proposital: elimina a
  necessidade de referenciar GUIDs de script à mão no YAML da cena (risco
  alto de erro sem Editor disponível pra validar). Nos próximos passos, dá
  pra mover essa construção pro Editor (GameObjects reais na cena) se for
  mais conveniente para inspecionar/ajustar valores visualmente.

## HUD de volta/posição/contramão, correção de vazamento de pista e item guardado

**Vazamento de kart pra fora da pista — diagnóstico e correção.** Não
consegui reproduzir isso aqui (sem Editor), então é diagnóstico por
leitura de código, não observação direta — vale confirmar que sumiu de
verdade depois de testar. A causa mais provável: cada segmento de muro é
uma caixa reta orientada na direção daquele trecho específico da pista;
onde dois segmentos se encontram (todo vértice da centerline), cada um
aponta pra uma direção um pouco diferente, e isso deixa um vão no canto —
quase imperceptível em curvas suaves (o Oval original), mas real e maior
em curvas fechadas, exatamente o que as pistas Estádio e Técnica do passo
6 adicionaram. Corrigi adicionando um "poste" cilíndrico em cada vértice
da pista, dos dois lados (interno/externo): por ser redondo, ele fecha o
vão em qualquer ângulo de curva sem precisar calcular o ângulo exato de
encontro entre os dois muros (matemática de "miter" de canto, que seria
mais frágil de acertar sem visualizar). Também aumentei a espessura padrão
do muro (0.5 → 0.8) como margem extra contra atravessar o muro em alta
velocidade (kart pode chegar a uns 40+ m/s depois de várias voltas
empilhando upgrade de velocidade máxima). Se o vazamento persistir depois
desse fix, pode ser outra causa (ex.: velocidade alta demais tunelando
mesmo com Continuous Collision Detection) — me avisa com detalhe de qual
pista/velocidade pra eu investigar mais.

**Posição de corrida: aproximada, não é distância real percorrida.**
`RaceStandings` ranqueia os karts por `LapTracker.Progress`
(`voltas * total_de_checkpoints + próximo_checkpoint_esperado`), um
inteiro que só sobe quando o kart cruza um checkpoint. Isso significa que
dois karts entre o mesmo par de checkpoints aparecem empatados na posição
até um deles cruzar o próximo gate, mesmo que um esteja fisicamente bem
mais à frente. Fiz assim porque calcular distância real ao longo da pista
(arc-length ao longo da centerline) é mais preciso mas também mais código
e mais chance de erro que eu não teria como validar visualmente. Pra um
protótipo de 3 karts isso é suficiente; se a granularidade incomodar no
teste, o próximo passo natural é interpolar a distância até o próximo
checkpoint dentro do próprio segmento.

**Contramão: comparado contra o segmento de pista mais próximo, só pro
jogador.** `WrongWayDetector` acha o ponto mais próximo da centerline e
compara a velocidade do kart com a direção daquele segmento (produto
escalar). Não fiz isso pra IA porque a IA já segue os waypoints em ordem
por construção (`KartAIDriver`) — não existe cenário onde ela estaria de
contramão, então não haveria nada útil pra mostrar.

**Você perguntou se a IA sobe de nível e usa item — sim, sempre usou.**
IA ganha upgrade aleatório do catálogo de level up ao completar volta
desde o passo 4, e ganha item aleatório da caixa desde o passo 5 — isso
não mudou agora. O que mudou é *como* o item chega até ela (ver abaixo).

**Item deixou de ser aplicado na escolha — agora fica guardado até usar.**
Isso é uma mudança de decisão em relação ao passo 5: lá eu decidi
deliberadamente que escolher o item já era o próprio efeito, sem botão de
"usar" separado, pra reaproveitar 100% o pipeline de pausa+escolha sem
precisar de inventário. Você pediu pra escrever na HUD o comando de
"soltar" o poder — isso só faz sentido existir se existir de fato um
botão que solta algo guardado, então reverti aquela decisão: `KartInventory`
(1 slot, pega item novo substitui o antigo) segura o item escolhido; o
jogador aperta `E`/`Ctrl`/botão X pra ativar (`KartInput` chama
`KartInventory.UseHeldItem()`); a IA ainda não tem noção de "o momento
certo" de usar, então continua usando imediatamente depois de pegar —
só que agora passando pelo mesmo `KartInventory`, não mais chamando o
efeito direto. `ItemDefinition.Use` continua sendo o mesmo tipo de dado
de antes (`Action<KartController>`), só mudou quando ele é invocado — não
foi preciso reescrever o catálogo de itens nem a UI de escolha, só o
`ItemBox` (que agora chama `Hold` em vez de `Use` direto) e o `KartInput`
(que agora também lê o botão de usar).

## Descoberto ao integrar: o projeto roda em Unity 6, não 2022.3 LTS

Você abriu o projeto no seu Unity Hub, e o que estava instalado aí era
**Unity 6 (`6000.5.7f1`)**, não a 2022.3 LTS que eu tinha escolhido como
alvo lá no passo 1 (decisão que tomei sem confirmar com você, baseada só
em "é uma LTS comum" — deveria ter perguntado, ou pelo menos deixado mais
claro que era um chute). O `.meta`/`ProjectSettings` que você commitou
vieram dessa versão real, e junto vieram (a Unity ou você, via API
Updater) 3 renomeações de API que o Unity 6 exige:
`Rigidbody.velocity` → `Rigidbody.linearVelocity`,
`Rigidbody.drag`/`angularDrag` → `linearDamping`/`angularDamping`, e
`PhysicMaterial` → `PhysicsMaterial`. Ao integrar isso de volta pro meu
código desta sessão, apliquei essas mesmas renomeações nos arquivos novos
que ainda usavam os nomes antigos (`WrongWayDetector.cs` e o método
`BuildCornerPost` que eu tinha acabado de escrever em `TrackBuilder.cs` —
esses não existiam no seu commit, então o merge automático do git não
tinha como saber que precisavam do mesmo ajuste). Atualizei
`ProjectVersion.txt`/README pra refletir Unity 6 como versão real do
projeto daqui pra frente.

Isso também derruba a suposição registrada no passo 1 de que "Unity
regenera `ProjectSettings`/`.meta` ausentes com defaults na primeira
abertura" — isso continua verdade, mas eu não tinha como prever qual
versão real do Editor você tinha instalada, e agora sabemos: Unity 6.
