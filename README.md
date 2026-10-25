# Motor de Pagamentos

![CI](https://github.com/Hugo-Novo-Gouvea/motor-de-pagamentos/actions/workflows/ci.yml/badge.svg)

Plataforma de pagamentos **simulada** em .NET 10, construída como projeto de
portfolio: ledger de partidas dobradas, idempotência, outbox, entrega garantida
de webhooks e Pix simulado.

> **Status:** em construção. A Fase 0 (fundação) está concluída. Acompanhe o
> progresso no [roadmap](docs/ROADMAP.md).

Nenhum dado de cartão real é usado: o adquirente é simulado e só aceita cartões
de teste.

## O que já existe

- Solução em Clean Architecture com quatro camadas: Domain, Application,
  Infrastructure e Api
- Orquestração local com Aspire: um comando sobe o PostgreSQL (em container) e a API
- Observabilidade e health checks com ServiceDefaults (OpenTelemetry)
- Testes de arquitetura que falham se as camadas internas usarem pacotes técnicos
- CI no GitHub Actions a cada Pull Request, com a `main` protegida

## Stack

.NET 10 · ASP.NET Core · Aspire · PostgreSQL · xUnit v3 · NetArchTest · GitHub Actions

## Como rodar

Pré-requisitos: .NET 10 SDK, Docker Desktop e Aspire CLI.

```bash
# sobe o PostgreSQL e a API, e abre o dashboard do Aspire
aspire run

# roda os testes
dotnet test
```

## Estrutura

```
src/
  PaymentEngine.Domain            regras do negócio
  PaymentEngine.Application       casos de uso e contratos
  PaymentEngine.Infrastructure    banco, fila e sistemas externos
  PaymentEngine.Api               porta de entrada HTTP
  PaymentEngine.AppHost           orquestração com Aspire
  PaymentEngine.ServiceDefaults   observabilidade e health checks
tests/
  PaymentEngine.Architecture.Tests
```

## Documentação

- [Especificação do projeto](docs/PROJECT_SPEC.md)
- [Roadmap](docs/ROADMAP.md)
- [Decisões de arquitetura (ADRs)](docs/adr/)

## Licença

[MIT](LICENSE)
