# 0001. Registrar decisões de arquitetura em ADRs

- Status: Aceita
- Data: 2026-10-24

## Contexto
O código mostra o que foi feito, mas não por que, nem quais alternativas
foram descartadas. Algumas configurações nem ficam no repositório: a
proteção da main, por exemplo, foi configurada no site do GitHub. Daqui a
alguns meses, quem abrir o projeto, inclusive eu, não vai saber o motivo
das escolhas.

## Decisão
Registrar cada decisão importante num ADR: um arquivo curto em docs/adr/,
numerado e seguindo o modelo 0000-template.md. ADR aceito não é editado;
se a decisão mudar, um ADR novo substitui o antigo.

## Alternativas consideradas
- Bloco de notas ou Notion — ficam fora do repositório, se perdem e não
  acompanham o código.
- Comentários no código — explicam uma linha, não uma decisão do projeto.
- Descrição dos Pull Requests — o motivo fica espalhado e difícil de achar.

## Consequências
- O porquê das decisões fica junto do código, acessível a qualquer pessoa.
- Quem chega no projeto entende as escolhas sem precisar perguntar.
- Cada ADR leva tempo para escrever, e é preciso disciplina para criar um
  a cada decisão importante.
