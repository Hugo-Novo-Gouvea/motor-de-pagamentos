# Motor de Pagamentos — Especificação do Projeto

> Plataforma de pagamentos **simulada** construída como projeto de portfolio em .NET 10.
> Repositório: `motor-de-pagamentos`. Prefixo do código: `PaymentEngine`. Público-alvo: mercado brasileiro.
> Este documento é a fonte da verdade sobre o *quê* e o *porquê*. O `ROADMAP.md` diz *quando*.
> O `CLAUDE.md` diz *como trabalhar* no repositório.

---

## 1. Visão

O Motor de Pagamentos é uma plataforma de pagamentos no estilo de um gateway: lojistas (merchants) integram via API, criam
pagamentos com cartão de teste, recebem notificações por webhook e acompanham tudo num dashboard.

O objetivo **não** é processar dinheiro de verdade. É provar, num domínio que qualquer recrutador reconhece,
domínio de problemas de sistemas distribuídos que aparecem em vagas .NET pleno/sênior:

| O que o projeto prova | Onde aparece |
|---|---|
| Consistência e correção | Ledger de partidas dobradas, máquina de estados |
| Idempotência | `Idempotency-Key` na API, consumidores idempotentes |
| Event-driven / mensageria | Outbox pattern + fila (Azure Service Bus / AWS SQS) |
| Resiliência | Retry com backoff + jitter, circuit breaker, dead-letter, replay |
| Segurança | OIDC/JWT, API keys, HMAC, isolamento multi-tenant, rate limiting |
| Observabilidade | OpenTelemetry com um trace do POST até a entrega do webhook |
| Cloud + IaC | Deploy no Azure (principal) e na AWS (sob demanda) via Aspire |
| Qualidade | Pirâmide de testes completa, testes de arquitetura, teste de carga |
| Full stack | Dashboard e playground em React + TypeScript |
| Domínio brasileiro | Pix simulado: cobrança, QR Code, notificação deduplicada, devoluções |
| Julgamento de engenharia | ADRs documentando cada decisão relevante |

## 2. Fora de escopo (decisões conscientes)

- **Nenhum dado de cartão real.** O adquirente é simulado e só aceita cartões de teste. PCI-DSS fica fora de escopo (vira ADR).
- Nenhuma integração com adquirente/banco real.
- Sem boleto ou moedas exóticas. Moedas suportadas: `BRL` e `USD`.
- Pix entra **simulado** em uma fase própria (seção 19), depois do núcleo de cartões. Nada de integração real com SPI/DICT.
- Sem microsserviços por modismo. A arquitetura é um **monólito modular** + processo worker + simulador (ver §4).

## 3. Glossário

| Termo | Significado |
|---|---|
| Merchant / lojista | Cliente da plataforma; integra via API para cobrar seus clientes |
| Adquirente (acquirer) | Quem "fala com a bandeira" e aprova/recusa o cartão. Aqui é simulado |
| Autorização | Reserva o valor no cartão; ainda não cobra |
| Captura | Efetiva a cobrança de uma autorização |
| Void / cancelamento | Desfaz uma autorização não capturada |
| Estorno (refund) | Devolve um valor já capturado |
| Liquidação (settlement) | Quando o adquirente efetivamente repassa o dinheiro à plataforma |
| Ledger | Livro-razão: registro imutável de todos os movimentos financeiros |
| Partida dobrada | Todo lançamento tem débitos e créditos que somam o mesmo valor |
| Idempotência | Repetir a mesma operação não produz efeito duplicado |
| Outbox | Tabela onde eventos são gravados na mesma transação da mudança de estado |
| DLQ / dead-letter | Lugar onde vai o que falhou definitivamente, para análise e replay |
| Reconciliação | Comparar o que o ledger diz com o que o adquirente diz ter liquidado |

## 4. Arquitetura

### 4.1 Visão geral

```
                    ┌──────────────────────────────┐
  Visitante ───────►│  PaymentEngine.Web (React + TS)      │  Playground público + Dashboard
                    └──────────────┬───────────────┘
                                   │ HTTPS (JWT no dashboard; sem login no playground)
  Lojista (servidor) ──X-Api-Key──►│
                                   ▼
┌──────────────────────── PaymentEngine.Api (ASP.NET Core) ─────────────────────────┐
│  Módulos: Payments · Ledger · Merchants · Webhooks · Playground           │
│  Escreve no Postgres: estado + lançamentos + outbox NA MESMA TRANSAÇÃO    │
└──────────┬──────────────────────────────────────────────┬─────────────────┘
           │ HTTP (com timeout e resiliência)             │
           ▼                                              ▼
┌─────────────────────────┐                     ┌──────────────────┐
│ PaymentEngine.AcquirerSimulator │                     │   PostgreSQL     │
│ aprova/recusa/atrasa;   │                     └────────┬─────────┘
│ gera arquivo de         │                              │
│ liquidação diário       │        ┌─────────────────────┴───────────────────┐
└─────────────────────────┘        │ PaymentEngine.Worker (BackgroundServices)       │
                                   │  • OutboxPublisher ──► Fila             │
                                   │  • IntegrationEventConsumer ◄── Fila    │
                                   │  • WebhookDispatcher ──► URL do lojista │
                                   │  • ReconciliationJob                    │
                                   └─────────────────────────────────────────┘
                                   Fila: Azure Service Bus (Azure) | SQS (AWS)
```

### 4.2 Por que monólito modular

Um portfolio feito por uma pessoa não tem os problemas organizacionais que justificam microsserviços (times
independentes, deploys independentes). A escolha madura é:

- **PaymentEngine.Api**: monólito modular, com módulos separados por pastas e fronteiras claras.
- **PaymentEngine.Worker**: processo separado porque o trabalho é de natureza diferente (polling, filas, jobs).
- **PaymentEngine.AcquirerSimulator**: separado porque representa **um sistema externo**, e tem que se comportar como um.

Isso vira o **ADR-0002**. Em entrevista, saber dizer "por que eu *não* usei microsserviços" vale mais do que ter usado.

### 4.3 Clean Architecture

```
PaymentEngine.Domain          → entidades, value objects, regras. Zero dependências externas.
PaymentEngine.Application     → casos de uso, interfaces (ports): repositórios, IAcquirerClient,
                        IIntegrationEventPublisher, IClock, ITenantContext...
PaymentEngine.Infrastructure  → EF Core/Postgres, cliente HTTP do adquirente, outbox, implementações.
PaymentEngine.Messaging.*     → adapters de fila (AzureServiceBus, AwsSqs). Implementam a interface da Application.
PaymentEngine.Api / Worker    → composição (DI), endpoints, hosted services.
```

Regra de dependência garantida por **testes de arquitetura** (§15).

### 4.4 Motor de Pagamentoss principais

**Criar pagamento (captura automática):**
1. Lojista faz `POST /v1/payments` com `X-Api-Key` e `Idempotency-Key`.
2. Middleware de idempotência verifica a chave (§7).
3. Caso de uso cria `Payment` em `Pending` e chama o simulador (`authorize` + `capture`).
4. Numa **única transação**: atualiza o estado do `Payment`, grava os lançamentos no ledger, grava `payment.captured` na outbox e grava a linha da timeline.
5. Responde `201 Created`.
6. O `OutboxPublisher` publica o evento na fila.
7. O `IntegrationEventConsumer` recebe, deduplica pelo `EventId` e cria uma `WebhookDelivery` para cada endpoint do lojista inscrito naquele tipo de evento.
8. O `WebhookDispatcher` entrega com assinatura HMAC; se falhar, agenda nova tentativa (§9).

**Timeout do adquirente:** se o simulador não responde dentro do timeout, o resultado é **desconhecido**
(a cobrança pode ter acontecido). O pagamento fica `Pending` com `AcquirerOutcomeUnknown = true`, e um job
consulta `GET /acquirer/transactions/{id}` depois para resolver. **Nunca** se assume que falhou. Esse é um
dos detalhes mais valiosos do projeto para entrevistas.

## 5. Modelo de domínio

### 5.1 Money

- Value object `Money(long MinorUnits, Currency Currency)`. R$ 10,50 = `1050 BRL`.
- **Nunca** `double`/`float`. Operações entre moedas diferentes lançam erro de domínio.
- Arredondamento explícito e documentado (banker's rounding nas tarifas).

### 5.2 Merchant

- `Id`, `Name`, `CreatedAt`, `IsDemo` (o lojista público do playground).
- `ApiKey`: `Id`, `MerchantId`, `Prefix` (ex.: `pe_test_a1b2`), `SecretHash` (SHA-256), `CreatedAt`, `RevokedAt`, `LastUsedAt`.
  A chave completa é mostrada **uma única vez**, na criação.
- `MerchantMember`: liga um usuário do provedor de identidade (claim `sub`/`oid`) ao lojista, com papel `Owner` | `Developer` | `Viewer`.
- `FeePolicy`: percentual + fixo (padrão 2,9% + R$ 0,30), configurável por lojista.

### 5.3 Payment (agregado)

Campos: `Id`, `MerchantId`, `Amount (Money)`, `Status`, `CaptureMethod (Automatic|Manual)`, `CardBrand`,
`CardLast4`, `Description`, `Metadata (dict)`, `AcquirerTransactionId`, `AcquirerOutcomeUnknown`,
`FailureCode`, `AmountRefunded`, `CreatedAt`, `UpdatedAt`, `Version` (concorrência otimista).

**Máquina de estados:**

| De | Ação | Para | Evento |
|---|---|---|---|
| — | criar | `Pending` | — |
| `Pending` | adquirente aprova (manual) | `Authorized` | `payment.authorized` |
| `Pending` | adquirente aprova + captura | `Captured` | `payment.captured` |
| `Pending` | adquirente recusa | `Failed` | `payment.failed` |
| `Authorized` | capturar | `Captured` | `payment.captured` |
| `Authorized` | cancelar | `Canceled` | `payment.canceled` |
| `Captured` | estorno parcial | `PartiallyRefunded` | `payment.refunded` |
| `Captured` / `PartiallyRefunded` | estorno que completa o valor | `Refunded` | `payment.refunded` |

Qualquer outra transição retorna erro de domínio (`Result.Failure`), não exceção.

### 5.4 Ledger

Contas (por lojista quando indicado):

| Conta | Tipo | Significado |
|---|---|---|
| `acquirer_receivable` | Ativo | O adquirente nos deve |
| `platform_cash` | Ativo | Dinheiro já recebido pela plataforma |
| `merchant_pending:{id}` | Passivo | Devemos ao lojista, ainda não liquidado |
| `merchant_available:{id}` | Passivo | Devemos ao lojista, disponível |
| `acquirer_payable` | Passivo | Devemos ao adquirente (estornos a devolver) |
| `fee_revenue` | Receita | Tarifas da plataforma |

`JournalEntry`: `Id`, `MerchantId`, `PaymentId?`, `Type`, `CreatedAt`, `Lines[]`.
`JournalLine`: `AccountCode`, `Direction (Debit|Credit)`, `Amount (Money)`.

**Invariantes:**
1. Em todo `JournalEntry`, soma dos débitos = soma dos créditos.
2. Lançamentos são **imutáveis**. Correção é um novo lançamento de estorno, nunca `UPDATE`.
3. Saldo é **derivado** dos lançamentos. Pode existir projeção para performance, mas ela tem que ser reconstruível do zero.

**Exemplos (captura de R$ 100,00, tarifa 2,9% + R$ 0,30 = R$ 3,20):**

```
Captura:
  D acquirer_receivable          100,00
  C merchant_pending:{m}          96,80
  C fee_revenue                    3,20

Liquidação (simulada em D+2):
  D platform_cash                100,00
  C acquirer_receivable          100,00
  D merchant_pending:{m}          96,80
  C merchant_available:{m}        96,80

Estorno total de R$ 100,00 (tarifa não é devolvida — decisão documentada em ADR):
  D merchant_available:{m}       100,00
  C acquirer_payable             100,00
```

### 5.5 Webhooks

- `WebhookEndpoint`: `Id`, `MerchantId`, `Url`, `Secret` (criptografado em repouso), `EventTypes[]`, `IsActive`, `CreatedAt`.
- `WebhookDelivery`: `Id`, `EndpointId`, `EventId`, `EventType`, `Payload`, `Status (Pending|Succeeded|DeadLettered)`, `AttemptCount`, `NextAttemptAt`, `CreatedAt`.
- `WebhookAttempt`: `DeliveryId`, `AttemptNumber`, `StartedAt`, `DurationMs`, `HttpStatus?`, `Error?`.

### 5.6 Infraestrutura de domínio

- `OutboxMessage`: `Id (= EventId)`, `Type`, `Payload (jsonb)`, `TraceParent`, `OccurredAt`, `ProcessedAt?`, `Attempts`.
- `ProcessedEvent`: `EventId`, `Consumer`, `ProcessedAt` (deduplicação do consumidor).
- `IdempotencyRecord`: `MerchantId`, `Key`, `RequestHash`, `Status (InProgress|Completed)`, `ResponseStatus`, `ResponseBody`, `CreatedAt`, `ExpiresAt`.
- `PaymentTimelineEntry`: `PaymentId`, `Step`, `Detail`, `OccurredAt` (alimenta a timeline visual).

## 6. API

Erros sempre em **Problem Details (RFC 9457)**. Listagens com paginação por **cursor**. Versão no path (`/v1`).
Documentação OpenAPI gerada pelo próprio ASP.NET Core e exposta com **Scalar**.

### 6.1 API pública (autenticação: `X-Api-Key`)

| Método | Rota | Observação |
|---|---|---|
| POST | `/v1/payments` | exige `Idempotency-Key` |
| GET | `/v1/payments/{id}` | |
| GET | `/v1/payments` | filtros: status, período, cursor |
| POST | `/v1/payments/{id}/capture` | exige `Idempotency-Key` |
| POST | `/v1/payments/{id}/cancel` | exige `Idempotency-Key` |
| POST | `/v1/payments/{id}/refunds` | exige `Idempotency-Key`; aceita valor parcial |
| GET | `/v1/balance` | pendente e disponível |
| GET | `/v1/ledger/entries` | |
| POST/GET/DELETE | `/v1/webhook-endpoints` | secret retornado só na criação |
| GET | `/v1/webhook-deliveries` | com tentativas |
| POST | `/v1/webhook-deliveries/{id}/replay` | |

Exemplo de criação:

```json
POST /v1/payments
X-Api-Key: pe_test_...
Idempotency-Key: 7f9c2b1e-...

{
  "amount": 10000,
  "currency": "BRL",
  "captureMethod": "automatic",
  "card": { "number": "4111111111111111", "expMonth": 12, "expYear": 2030, "cvc": "123" },
  "description": "Pedido #1234",
  "metadata": { "orderId": "1234" }
}
```

### 6.2 API do dashboard (autenticação: JWT do provedor OIDC)

Rotas em `/dashboard/*` espelhando as consultas acima, mais:
- `POST /dashboard/api-keys` (retorna a chave completa uma vez), `DELETE /dashboard/api-keys/{id}`
- `GET /dashboard/reconciliation-reports`
- Autorização por papel: `Viewer` lê; `Developer` gerencia chaves e endpoints; `Owner` tudo.

### 6.3 Playground (público, sem login, rate limit por IP)

- `POST /playground/checkout`: cria pagamento no lojista demo.
- `GET /playground/payments/{id}/timeline`: timeline do pagamento.
- `POST /playground/receiver/mode`: alterna o receptor de webhooks demo entre `healthy`, `flaky` (falha 50%) e `down`, para o visitante ver retries e dead-letter acontecendo.
- `POST /playground/receiver` (interno): o **receptor de webhooks demo**. Valida a assinatura e se comporta conforme o modo.

### 6.4 Saúde

`/health` e `/alive` via ServiceDefaults do Aspire.

## 7. Idempotência

1. Toda operação que altera estado exige o header `Idempotency-Key` (até 255 caracteres). Sem ele: `400`.
2. A chave tem escopo por lojista. É gravada com o **hash do corpo da requisição**.
3. Mesma chave + mesmo corpo + concluída → devolve **a mesma resposta gravada** (status e corpo), sem reexecutar.
4. Mesma chave + corpo diferente → `422` ("chave reutilizada com outros parâmetros").
5. Mesma chave ainda em processamento (requisição concorrente) → `409`.
6. A reserva da chave usa constraint única no banco (`INSERT` que falha se já existe). Nada de "verificar e depois inserir" (race condition).
7. Expiração: 24h.

**Teste obrigatório:** 20 requisições paralelas com a mesma chave resultam em exatamente 1 pagamento e 1 lançamento.

## 8. Outbox e mensageria

**Problema que resolve:** gravar no banco e publicar na fila são duas operações. Se o processo cai entre as duas, o
estado e os eventos ficam inconsistentes. Com a outbox, o evento é gravado **na mesma transação** que o estado.
Outro processo publica depois.

- `OutboxPublisher`: polling a cada 1s, lote de até 100, `SELECT ... FOR UPDATE SKIP LOCKED` (permite várias instâncias sem duplicar trabalho), marca `ProcessedAt` após publicar. Limpeza de mensagens antigas por job.
- Garantia: **at-least-once**. Duplicatas são possíveis por design, então **todo consumidor é idempotente** (tabela `ProcessedEvent`, gravada na mesma transação do efeito).
- Ordem **não é garantida** (documentar no README e no payload: `created` + estado atual).

**Abstração (na Application):**

```csharp
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken ct);
}
```

O consumidor é um `BackgroundService` que recebe do adapter configurado.

**Adapters:**

| Provider | Produção | Local |
|---|---|---|
| `AzureServiceBus` | fila no Azure Service Bus | emulador do Service Bus via Aspire |
| `AwsSqs` | fila SQS | LocalStack via Aspire/container |

Seleção por configuração: `Messaging:Provider`. Começar com **fila simples** (não tópico): há um único consumidor,
e fila é mais barata (ADR).

**Envelope:**

```json
{
  "id": "evt_01J...",
  "type": "payment.captured",
  "occurredAt": "2026-11-03T14:22:05Z",
  "merchantId": "mer_...",
  "traceParent": "00-...",
  "data": { "...": "snapshot do pagamento" }
}
```

O `traceParent` propaga o trace do OpenTelemetry através da fila (§14).

## 9. Entrega de webhooks

Este módulo é a evolução do projeto `webhook-relay`.

- O consumidor cria uma `WebhookDelivery` por endpoint ativo inscrito no tipo do evento.
- O `WebhookDispatcher` busca entregas com `NextAttemptAt <= agora` (`SKIP LOCKED`) e faz `POST` na URL.
- O **agendamento de retries fica no banco**, não na fila. Isso deixa o estado observável e consultável pelo dashboard (ADR).

**Assinatura:**

```
PaymentEngine-Signature: t=1730643725,v1=<hex(HMAC-SHA256(secret, "{t}.{body}"))>
```

O receptor deve rejeitar se `|agora - t| > 5 min` (proteção contra replay). Documentar com exemplo de verificação em C#.

**Política de resposta:**
- `2xx` → `Succeeded`.
- `410 Gone` → desativa o endpoint.
- Timeout (10s), `5xx`, `429` ou erro de rede → nova tentativa.
- Outros `4xx` → nova tentativa (o lojista pode corrigir), mas conta para desativação.

**Agenda de retry** (exponencial com jitter, configurável):
- Produção: 1min, 5min, 30min, 2h, 6h, 12h, 24h → depois `DeadLettered`.
- Modo demo: 2s, 4s, 8s, 16s, 32s (para o visitante ver acontecer).

**Circuit breaker por endpoint:** muitas falhas seguidas → pausa as entregas daquele endpoint por um intervalo, sem bloquear os outros.

**Replay:** qualquer entrega (inclusive `DeadLettered`) pode ser reenviada pelo dashboard ou pela API, com novo registro de tentativa.

## 10. Simulador do adquirente

Serviço separado (`PaymentEngine.AcquirerSimulator`) que se comporta como um sistema externo imperfeito.

**Cartões de teste:**

| Número | Comportamento |
|---|---|
| `4111111111111111` | aprovado |
| `5555555555554444` | aprovado (Mastercard) |
| `4000000000000002` | recusado (genérico) |
| `4000000000009995` | recusado (saldo insuficiente) |
| `4000000000000119` | erro do processador (`500`) |
| `4000000000003220` | timeout (responde depois do limite do cliente; a cobrança **é efetivada**) |
| qualquer outro | `400 card_not_supported` |

- Latência aleatória configurável (ex.: 50–400ms).
- Taxa de falha transitória configurável (ex.: 2% de `503`) para exercitar a resiliência.
- Endpoints: `POST /authorize`, `/capture`, `/void`, `/refund`; `GET /transactions/{id}`; `GET /settlements/{date}.csv`.
- Arquivo de liquidação diário em CSV, com opção de **injetar divergências** (transação faltando, valor diferente) para testar a reconciliação.
- Persistência própria (Postgres separado ou schema separado). **Não** compartilha tabelas com a Api.

## 11. Reconciliação

`ReconciliationJob` diário (e disparável manualmente no demo):
1. Baixa o CSV de liquidação do simulador.
2. Compara cada linha com o ledger: transações casadas, faltando de um lado, valores divergentes.
3. Para as casadas, gera os lançamentos de liquidação (pendente → disponível).
4. Grava um `ReconciliationReport` com as divergências, exibido no dashboard.

## 12. Segurança

### 12.1 Dois tipos de cliente, dois tipos de autenticação

| Quem | Como autentica | Por quê |
|---|---|---|
| Servidor do lojista (máquina) | API key no header `X-Api-Key` | padrão do mercado para integração servidor-servidor |
| Pessoa no dashboard | OIDC (Authorization Code + PKCE) → **access token JWT** | padrão para login de usuários |

**JWT:** o dashboard faz login num provedor de identidade (**Microsoft Entra External ID**, alinhado ao foco Azure e com
camada gratuita). O provedor emite um access token JWT assinado. A API valida com `AddJwtBearer`: assinatura (chaves
públicas via JWKS), `issuer`, `audience` e expiração. **A plataforma não emite os próprios JWTs nem guarda senhas**
(ADR: "não implementar autenticação própria"). Alternativa documentada: Keycloak em container via Aspire.

**API keys:** geradas com `RandomNumberGenerator` (32 bytes, base62), prefixo `pe_test_`, guardadas só como hash
SHA-256. Comparação em tempo constante. Atualizam `LastUsedAt`. Revogáveis.

### 12.2 Autorização e isolamento multi-tenant

- `ITenantContext` resolve o `MerchantId` a partir da API key ou do JWT + `MerchantMember`.
- **EF Core global query filter** por `MerchantId` em todas as entidades de lojista.
- Teste obrigatório: o lojista A **nunca** consegue ler, alterar ou replayar recursos do lojista B, nem adivinhando IDs (OWASP API1: BOLA).
- Políticas por papel (`Viewer` / `Developer` / `Owner`).

### 12.3 Demais controles

- Rate limiting nativo do ASP.NET Core: por API key na API pública e por IP no playground (mais restrito).
- CORS restrito à origem do front.
- HTTPS obrigatório, HSTS, headers de segurança.
- Validação de entrada em todos os endpoints.
- Secret dos webhooks criptografado em repouso (Data Protection, chaves no Key Vault/Secrets Manager).
- Segredos: `dotnet user-secrets` localmente; Key Vault (Azure) ou Secrets Manager (AWS) em produção, acessados por identidade gerenciada/IAM role.
- CI faz deploy no Azure via **OIDC federado do GitHub Actions** (sem segredo de longa duração no repositório).
- GitHub: Dependabot, CodeQL e secret scanning ativados.
- Documento `docs/SECURITY.md` com o checklist OWASP API Top 10 e como cada item é tratado.

## 13. Frontend (`PaymentEngine.Web`)

**Stack:** React + TypeScript + Vite, TanStack Query, roteamento com React Router, UI com Tailwind + shadcn/ui.
Cliente OIDC: MSAL React (Entra).

### 13.1 Playground (público, sem login) — a vitrine

É a página que o recrutador abre pelo link do README. Ele precisa entender o projeto em 30 segundos.

- Um "checkout" de uma loja fictícia com seletor dos cartões de teste.
- Ao pagar, aparece uma **timeline ao vivo**: API recebeu → adquirente autorizou (Xms) → ledger lançado → evento na outbox → publicado na fila → webhook entregue (tentativa N, status HTTP).
- Um controle do **receptor de webhooks demo** (`saudável` / `instável` / `fora do ar`). O visitante vê retries com backoff e o dead-letter acontecendo, e pode clicar em "replay".
- Mostra os lançamentos contábeis gerados por aquele pagamento.
- "Modo explicação": cada etapa da timeline tem um texto curto dizendo *o que* aconteceu e *por quê* (ex.: "evento gravado na outbox na mesma transação, para não se perder se o processo cair").
- Dados do lojista demo são resetados diariamente.

Timeline v1: polling a cada 1s com TanStack Query. v2: push com SignalR (a Api escuta `LISTEN/NOTIFY` do Postgres
quando o Worker grava etapas). Começar pelo v1.

### 13.2 Dashboard (login OIDC)

- Visão geral: volume, taxa de aprovação, saldo pendente/disponível.
- Pagamentos: lista com filtros, detalhe com timeline, botões de capturar/cancelar/estornar.
- Ledger: lançamentos com débitos/créditos.
- Webhooks: endpoints, entregas com cada tentativa, replay.
- API keys: criar (exibe uma vez), revogar.
- Reconciliação: relatórios e divergências.
- Usuário novo ganha um lojista sandbox próprio na primeira entrada.

### 13.3 Referência da API

Página Scalar gerada a partir do OpenAPI, publicada junto com a API. É o primeiro "visual" no ar (Fase 1).

## 14. Observabilidade

- OpenTelemetry via `PaymentEngine.ServiceDefaults` (traces, métricas, logs) em todos os processos.
- `ActivitySource` próprio com spans: `payment.create`, `acquirer.authorize`, `ledger.post`, `outbox.publish`, `event.consume`, `webhook.deliver`.
- **Um único trace** do `POST /v1/payments` até a entrega do webhook, propagando `traceparent` pelo envelope da fila. Print desse trace no README.
- Métricas: pagamentos por status, latência do adquirente, lag da outbox (idade da mensagem mais antiga não publicada), tentativas de webhook por resultado, profundidade da fila.
- Logs estruturados com `TraceId`, sem dados sensíveis (nada de corpo de cartão, chave ou secret).
- Destinos: dashboard do Aspire (local), Application Insights (Azure), CloudWatch/X-Ray via ADOT (AWS).

## 15. Estratégia de testes

| Tipo | Ferramenta | O que cobre |
|---|---|---|
| Unitário | xUnit v3 + Shouldly | `Money`, máquina de estados, tarifas, balanceamento do ledger, assinatura HMAC, agenda de retry |
| Propriedade | FsCheck | "qualquer sequência válida de operações mantém débitos = créditos" |
| Integração | xUnit + `WebApplicationFactory` + Testcontainers (Postgres) + Respawn | endpoints reais com banco real: idempotência concorrente, isolamento multi-tenant, outbox |
| Snapshot/contrato | Verify | payload dos webhooks e OpenAPI não mudam sem querer |
| Arquitetura | NetArchTest ou ArchUnitNET | Domain não depende de nada; Application não depende de Infrastructure |
| Ponta a ponta (backend) | Aspire.Hosting.Testing | sobe o AppHost inteiro, cria pagamento, verifica webhook recebido |
| Front unitário | Vitest + Testing Library | componentes e hooks |
| Front E2E | Playwright | fluxo do playground; gera vídeo/GIF para o README |
| Carga | k6 | throughput e p95/p99 de `POST /v1/payments`; resultados em `docs/PERFORMANCE.md` |
| Mutação (opcional) | Stryker.NET | qualidade dos testes do Domain |

Regras: nunca usar o provider InMemory do EF (não se comporta como Postgres); bug corrigido começa com teste que
reproduz o bug; cobertura é consequência, não meta (referência: Domain ≥ 90%).

## 16. Infraestrutura, deploy e custos

**Orquestração:** Aspire (`PaymentEngine.AppHost`) para rodar localmente e para publicar/deployar.

| Peça | Azure (principal, sempre no ar) | AWS (sob demanda) |
|---|---|---|
| Api, Worker, Simulador | Azure Container Apps | ECS Fargate (via integração AWS do Aspire / CDK) |
| Banco | Azure Database for PostgreSQL Flexible (burstable) | RDS PostgreSQL |
| Fila | Azure Service Bus (fila) | SQS |
| Segredos | Key Vault + Managed Identity | Secrets Manager + IAM role |
| Front | Azure Static Web Apps | S3 + CloudFront |
| Observabilidade | Application Insights | CloudWatch + X-Ray |

**Custos (decisão de engenharia, documentar em ADR):**
- Azure fica no ar com o mínimo: Container Apps escalando a zero onde possível, Postgres na menor camada, Static Web Apps gratuito, **alerta de orçamento** configurado.
- AWS sobe sob demanda (workflow manual de deploy + workflow de destroy) para demonstração. O README explica por quê.
- Conferir camadas gratuitas e preços vigentes antes de provisionar.

**CI/CD (GitHub Actions):**
- `ci.yml`: build, testes .NET (Testcontainers funciona nos runners Linux), testes e lint do front, Playwright, relatório de cobertura.
- `deploy-azure.yml`: no merge na `main`, autenticação OIDC federada, deploy via Aspire.
- `deploy-aws.yml` / `destroy-aws.yml`: `workflow_dispatch` manual.
- `codeql.yml` + Dependabot.

## 17. Estrutura da solução

```
motor-de-pagamentos/
├── CLAUDE.md
├── README.md                      (português — vitrine)
├── README.en.md                   (opcional — versão em inglês)
├── docs/
│   ├── PROJECT_SPEC.md
│   ├── ROADMAP.md
│   ├── SECURITY.md
│   ├── PERFORMANCE.md
│   └── adr/
├── src/
│   ├── PaymentEngine.AppHost/
│   ├── PaymentEngine.ServiceDefaults/
│   ├── PaymentEngine.Domain/              (pastas por módulo: Payments, Ledger, Merchants, Webhooks)
│   ├── PaymentEngine.Application/
│   ├── PaymentEngine.Infrastructure/
│   ├── PaymentEngine.Messaging.AzureServiceBus/
│   ├── PaymentEngine.Messaging.AwsSqs/
│   ├── PaymentEngine.Api/
│   ├── PaymentEngine.Worker/
│   ├── PaymentEngine.AcquirerSimulator/
│   └── PaymentEngine.Web/                 (React + TS + Vite)
├── tests/
│   ├── PaymentEngine.Domain.Tests/
│   ├── PaymentEngine.Application.Tests/
│   ├── PaymentEngine.Api.IntegrationTests/
│   ├── PaymentEngine.Architecture.Tests/
│   ├── PaymentEngine.EndToEnd.Tests/
│   └── load/                      (scripts k6)
└── .github/workflows/
```

## 18. Template de ADR

Arquivo `docs/adr/NNNN-titulo-curto.md`:

```markdown
# NNNN. Título da decisão

- Status: Proposta | Aceita | Substituída por NNNN
- Data: AAAA-MM-DD

## Contexto
Qual problema ou força motivou a decisão.

## Decisão
O que foi decidido, em uma ou duas frases afirmativas.

## Alternativas consideradas
- Alternativa A — por que não.
- Alternativa B — por que não.

## Consequências
O que fica melhor, o que fica pior, o que passa a ser obrigatório.
```

## 19. Pix simulado (Fase 5)

**Por que entra:** para o mercado brasileiro, Pix é o meio de pagamento mais reconhecível. E ele reforça exatamente o
que o projeto já prova: no Pix real, o recebedor é avisado por **notificação assíncrona** quando o dinheiro cai.

Modelado a partir dos **conceitos** da API Pix do Banco Central (cobrança, `txid`, `endToEndId`), sem pretender
conformidade com a especificação oficial. Isso fica registrado em ADR.

### 19.1 Fluxo

1. Lojista cria uma cobrança imediata: `POST /v1/pix/charges` com `Idempotency-Key`, `amount`, `expiresInSeconds`, `description`.
2. A plataforma gera um `txid`, um payload "copia e cola" (formato BR Code, com chave fictícia) e a imagem do QR Code.
3. Um **simulador de banco pagador** (no `PaymentEngine.AcquirerSimulator`) "paga" a cobrança. No playground, isso é um botão "Pagar pelo app do banco".
4. O simulador envia uma notificação de pagamento recebido para a plataforma, com um `endToEndId` único.
5. A plataforma conclui a cobrança, grava os lançamentos e o evento `pix.charge.completed` na outbox (mesma transação).
6. O lojista recebe o webhook pelo mesmo módulo de entrega da seção 9.

### 19.2 Estados da cobrança

| De | Ação | Para | Evento |
|---|---|---|---|
| — | criar | `Active` | `pix.charge.created` |
| `Active` | pagamento recebido | `Completed` | `pix.charge.completed` |
| `Active` | prazo vencido (job) | `Expired` | `pix.charge.expired` |
| `Active` | lojista cancela | `Canceled` | `pix.charge.canceled` |
| `Completed` | devolução (total ou parcial) | `Completed` + `AmountRefunded` | `pix.refund.completed` |

### 19.3 Regras que valem entrevista

- **A notificação do banco pode chegar duplicada.** Deduplicação pelo `endToEndId` (constraint única). A segunda notificação é aceita (responde 200) mas não gera nada.
- **Pagamento depois do vencimento:** o simulador consegue enviar um pagamento para cobrança já `Expired`. A plataforma registra, não credita ao lojista e gera uma devolução automática. Documentar em ADR.
- **Valor divergente:** pagamento com valor diferente do cobrado é rejeitado e devolvido.
- Liquidação é instantânea (D+0): o valor vai direto para `merchant_available`.

### 19.4 Ledger (cobrança de R$ 100,00, tarifa Pix de 0,99% = R$ 0,99)

```
Pix recebido:
  D platform_cash                100,00
  C merchant_available:{m}        99,01
  C fee_revenue                    0,99
```

### 19.5 Bibliotecas

Geração de QR Code com biblioteca de licença permissiva (ex.: QRCoder, MIT). Confirmar a licença antes de adicionar.

## 20. IA (opcional, Fase 7)

`PaymentEngine.Mcp`: servidor MCP (Streamable HTTP, autenticado por API key) para operação assistida:
- Tools somente leitura: `get_payment`, `get_payment_timeline`, `list_failed_webhook_deliveries`, `get_balance`.
- Tool de escrita com confirmação explícita: `replay_webhook_delivery`.
- Caso de uso: "por que os webhooks do lojista X estão falhando desde ontem?" no Claude Code ou num cliente .NET.

Só entra depois que todo o resto estiver no ar. É diferencial, não fundação.
