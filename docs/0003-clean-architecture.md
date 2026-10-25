# 0003. Clean Architecture com quatro camadas

- Status: Aceita
- Data: 2026-10-24

## Contexto
As regras de dinheiro precisam ser testadas a fundo e não podem quebrar quando a
infraestrutura muda. O mesmo fluxo é disparado por várias portas de entrada (a
API, o Worker e, mais para frente, um servidor MCP). E a fila vai ser trocada do
Azure Service Bus para o AWS SQS na Fase 6.

## Decisão
Separar o código em quatro camadas, com as dependências sempre apontando para as
regras do negócio:

- `Domain`: as regras do negócio. Não conhece nenhum outro projeto.
- `Application`: os casos de uso. Coordena o fluxo e declara as interfaces do
  que precisa de fora (o contrato fica com quem precisa dele).
- `Infrastructure`: banco, fila e chamadas a sistemas externos. Implementa as
  interfaces da Application.
- `Api`: a porta de entrada HTTP e o composition root, onde o `Program.cs` liga
  cada interface à sua implementação.

A regra é vigiada pelo compilador, que impede ciclos entre os projetos, e por
testes de arquitetura, que impedem o Domain e a Application de usarem pacotes
técnicos (EF Core, ASP.NET, Npgsql).

## Alternativas consideradas
- Tudo no endpoint — rápido para um CRUD, mas cada porta de entrada repetiria o
  fluxo, e testar uma regra exigiria banco e servidor.
- Modelo em que a regra e o acesso ao banco ficam na mesma classe (Active
  Record) — produtivo em sistemas simples, mas as regras passam a depender do
  banco.
- Três camadas, sem a Application — cada porta de entrada assumiria a
  coordenação, e o mesmo fluxo de dinheiro seria escrito mais de uma vez.

## Consequências
- As regras do Domain são testadas em milissegundos, sem banco, sem servidor e
  sem rede.
- Trocar banco, fila ou nuvem significa adicionar uma implementação na
  Infrastructure e registrá-la no `Program.cs`, sem tocar no Domain.
- Cada fluxo é escrito uma vez e reaproveitado por todas as portas de entrada.
- Há mais projetos e mais camadas para atravessar ao seguir uma requisição. Em
  um CRUD simples, esse custo não compensaria.
- Toda regra de negócio fica no Domain. Um `if` de regra fora dele é uma regra
  vazando.
