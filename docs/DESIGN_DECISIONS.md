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
