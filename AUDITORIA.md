# Auditoria do HearthPantry — 04/10/2026

Escopo: código local após a correção 1.0.2, confrontado com as classes Player,
Humanoid, Character, SE_Puke, SEMan e HitData da instalação local do Valheim.
Auditoria estática; os cenários abaixo ainda precisam de validação no jogo.
Nenhuma correção adicional foi aplicada nesta auditoria.

Atualização: os problemas e limitações abaixo foram tratados na versão 1.0.3.
Este documento preserva os achados originais. A validação automatizada não
substitui os cenários de aceitação dentro do jogo descritos ao final.

## Problemas prioritários

### 1. Hidroméis podem ser consumidos durante morte ou teleporte

MeadManager.cs:32–40, 48, 90, 140. As rotinas só verificam se o jogador existe.
Player.ConsumeItem/CanConsumeItem não bloqueiam esses estados por conta própria.
Com vida baixa ou uma ameaça próxima, a automação pode consumir itens durante
essas transições. A proteção adicionada em FoodManager não cobre os hidroméis.
Correção: validar jogador vivo, fora de teleporte e de cenas antes de todas as
rotinas de consumo; limpar o histórico ao trocar de jogador/sessão.

### 2. Pausa das comidas usa uma compensação incorreta

Patches.cs:12–24. O jogo desconta o timer em blocos de um segundo e também
executa atualizações forçadas ao comer. O mod adiciona Time.deltaTime em toda
chamada, mesmo quando não houve desconto, e não usa o dt recebido pelo método.
Isso pode fazer o timer subir entre descontos, perder tempo em chamadas forçadas
e deixar uma comida expirar antes que o postfix consiga restaurá-la. Os valores
de saúde, stamina e eitr já foram calculados antes da compensação.
Correção: preservar o estado real do timer e impedir apenas seu desconto durante
a pausa, mantendo a regeneração. Validar bancada, barco, comida quase expirada,
comer manualmente e taxas diferentes de consumo de comida.

### 3. Criaturas próximas são consideradas ameaça sem verificar hostilidade

MeadManager.cs:193–203. Qualquer criatura não jogadora a até três metros passa
no teste de ameaça antes da consulta à IA. Uma criatura domada com dano de fogo
ou veneno pode provocar consumo de resistência mesmo sendo aliada.
Correção: verificar hostilidade/domesticação antes da distância ou do alvo.

### 4. Golpes recusados pelo jogo contam como golpes recebidos

Patches.cs:49–53 e MeadManager.cs:19–29. O registro ocorre no prefix de
RPC_Damage. O jogo ainda pode rejeitar o golpe por esquiva invulnerável,
teleporte, cena, morte ou outras condições. Mesmo assim, o mod abre a janela
de cura e conta dano bruto de gelo. Com vida baixa, esquivar pode gastar cura;
golpes de gelo esquivados podem disparar resistência.
Correção: registrar golpes efetivamente aceitos, com tratamento explícito de
bloqueios e dano elemental, sem depender apenas da chegada da chamada.

## Outros problemas confirmados no código

### 5. Vulnerabilidade é classificada como resistência

MeadManager.cs:273. Todo modificador diferente de Normal é aceito como proteção,
incluindo Weak, VeryWeak e SlightlyWeak. Um consumível de outro mod que aumente
o dano recebido pode ser escolhido como hidromel de resistência.
Correção: aceitar explicitamente os modificadores protetores e excluir os de
vulnerabilidade. O impacto exige um consumível com esses modificadores.

### 6. Histórico de gelo cresce com a função desativada

MeadManager.cs:19–29, 135–145. OnDamage sempre adiciona entradas; a remoção de
entradas antigas só acontece quando AutoFrostMead está ligado e há jogador.
Com essa função ou o mod desativado, golpes de gelo continuam acumulando
entradas. Os dados também não são reiniciados ao mudar de sessão/jogador.
Correção: limitar e expirar a fila independentemente da função e reiniciar o
estado ao trocar de sessão.

### 7. TimeControl não é considerado no percentual de renovação

FoodManager.cs:90 e 141–142. O timer recebe duração original multiplicada, mas
o percentual é calculado contra a duração original. Com multiplicador 4 e
limiar 45%, a renovação acontece com apenas 11,25% da duração ampliada restante.
CanEatAgain também usa metade da duração original. A renovação deixa de seguir
a proporção anunciada. Correção: alinhar duração efetiva, elegibilidade e
percentual, sem renovar prematuramente ou sobrescrever timers de outros mods.

### 8. Escolha de comida favorece duração e ignora eitr

FoodScorer.cs:14–17. Com pesos padrão iguais, um segundo de duração pesa tanto
quanto um ponto de saúde. Dez minutos extras dão 30.000 pontos; cem pontos de
saúde dão 5.000. A duração domina a escolha e não há peso de eitr, então
"melhor comida" pode ser uma escolha ruim para combate ou magia.
Melhoria: normalizar as escalas e adicionar preferência por eitr ou perfis de
alimentação. É uma limitação do critério atual, não um loop de consumo.

## Limitações adicionais

- MeadManager.cs:210–238 guarda a primeira avaliação por nome de prefab para
  sempre. Equipamentos/ataques diferentes de outras instâncias ou alterações
  por mods não atualizam a detecção de fogo/veneno.
- ModLocalization.cs:42 só registra o inglês embutido se nenhum idioma carregar.
  Uma instalação parcial sem o inglês pode perder o fallback de traduções.
- FoodManager.cs:202–203 interrompe o preenchimento quando a primeira comida
  escolhida não pode ser consumida, sem tentar uma alternativa válida.

## Ordem recomendada

Primeiro corrigir consumo durante transições, pausa de timers e hostilidade.
Depois corrigir registro de golpes, seleção de resistências e fila de gelo.
Por fim revisar TimeControl, pontuação e cache de ameaças.

Para aceitação: testar vômito, morte/respawn, teleporte, esquiva de gelo, criatura
domada com ataque elemental, bancada/barco com comida quase expirada, TimeControl
com multiplicador 4 e consumíveis modificados que dão vulnerabilidade.
