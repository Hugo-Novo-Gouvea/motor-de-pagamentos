# Motor de Pagamentos — Roadmap

> Regra de ouro: **toda fase termina com algo no ar e o README atualizado.** Se o projeto parar em qualquer fase,
> o que existe está apresentável. Projeto pela metade, sem demo, vale pouco para quem avalia.
>
> A fase atual é a primeira com tarefas não marcadas. Marque `[x]` ao concluir cada tarefa.
> Estimativas consideram ~10h por semana e servem só de referência.
>
> **Não espere terminar para se candidatar.** Ao concluir a Fase 1, o projeto já entra no currículo e no LinkedIn.

---

## Fase 0 — Fundação (≈ 1 semana)

**Objetivo:** repositório organizado, solução rodando localmente com Aspire e CI verde desde o primeiro commit.

**Você aprende:** Aspire (AppHost e ServiceDefaults), estrutura de solução, GitHub Actions básico, ADRs.

- [ ] Criar repositório público `motor-de-pagamentos` com `.gitignore` de .NET + Node, licença MIT e proteção da branch `main` (exigir PR + CI verde)
- [ ] Copiar `docs/PROJECT_SPEC.md` e `docs/ROADMAP.md` para o repositório
- [ ] Criar a solução .NET 10 com os projetos de `src/` e `tests/` conforme a seção 17 do spec (projetos vazios, referências corretas)
- [ ] `PaymentEngine.AppHost` subindo Postgres (container) + `PaymentEngine.Api` com endpoint `/health`
- [ ] `PaymentEngine.ServiceDefaults` com OpenTelemetry, health checks e resiliência padrão
- [ ] `PaymentEngine.Architecture.Tests` com as regras de dependência entre camadas (já falhando se alguém quebrar)
- [ ] `ci.yml`: restore, build, test em todo PR
- [ ] `docs/adr/0000-template.md` (template na seção 18 do spec)
- [ ] ADR-0001: usar ADRs para registrar decisões
- [ ] ADR-0002: monólito modular + worker + simulador, e não microsserviços
- [ ] README inicial (em português): uma frase do que é, status "em construção", link para o roadmap

**Pronto quando:** `aspire run` sobe tudo, o dashboard do Aspire mostra a Api saudável e o CI passa no GitHub.

---

## Fase 1 — Núcleo de pagamentos (≈ 3–4 semanas)

**Objetivo:** criar, capturar, cancelar e estornar pagamentos com ledger correto e idempotência, no ar no Azure.

**Você aprende:** modelagem de domínio (agregados, value objects, máquina de estados), partidas dobradas, concorrência
otimista, idempotência, Testcontainers, resiliência de cliente HTTP, deploy com Aspire no Azure.

### Domínio
- [ ] `Money` + `Currency` com testes (soma, subtração, moedas diferentes, arredondamento)
- [ ] Agregado `Payment` com a máquina de estados da seção 5.3 e testes de **todas** as transições (válidas e inválidas)
- [ ] Cálculo de tarifa (`FeePolicy`) com testes
- [ ] `JournalEntry` / `JournalLine` com invariante de balanceamento; lançamentos de captura, cancelamento e estorno
- [ ] Teste de propriedade (FsCheck): sequências aleatórias de operações válidas sempre mantêm débitos = créditos
- [ ] Result pattern para erros de negócio

### Simulador do adquirente
- [ ] `PaymentEngine.AcquirerSimulator` com `authorize`, `capture`, `void`, `refund`, `GET transactions/{id}`
- [ ] Cartões de teste da seção 10, latência aleatória e taxa de falha configuráveis
- [ ] Persistência própria (schema ou banco separado)

### Aplicação e infraestrutura
- [ ] EF Core + Npgsql, migrations, configuração das entidades, concorrência otimista (`Version`)
- [ ] Cliente HTTP do adquirente com timeout e `Microsoft.Extensions.Http.Resilience` (retry só em operações seguras)
- [ ] Tratamento de timeout: pagamento fica `Pending` com `AcquirerOutcomeUnknown` (a resolução automática fica para a Fase 4)
- [ ] Gravação de `PaymentTimelineEntry` em cada etapa

### API
- [ ] Merchants + API keys (geração, hash, prefixo, revogação) com seed de um lojista de desenvolvimento
- [ ] Autenticação por `X-Api-Key` + `ITenantContext` + global query filter por `MerchantId`
- [ ] Middleware/filtro de idempotência seguindo as 7 regras da seção 7
- [ ] Endpoints de pagamentos, saldo e ledger (seção 6.1, exceto webhooks), Problem Details, paginação por cursor
- [ ] OpenAPI + página Scalar
- [ ] Rate limiting por API key

### Testes obrigatórios
- [ ] Integração com Testcontainers + Respawn para todos os endpoints
- [ ] 20 requisições paralelas com a mesma `Idempotency-Key` → 1 pagamento, 1 lançamento
- [ ] Lojista A não acessa recursos do lojista B (leitura, captura, estorno)
- [ ] Estorno parcial + estorno do restante → `Refunded`, ledger balanceado

### Deploy e documentação
- [ ] Deploy da Api + Simulador + Postgres no Azure via Aspire (Container Apps)
- [ ] Alerta de orçamento configurado na assinatura Azure
- [ ] ADR-0003: nenhum dado de cartão real / PCI fora de escopo
- [ ] ADR-0004: dinheiro como `long` em unidades menores
- [ ] ADR-0005: estorno não devolve a tarifa
- [ ] README: problema, arquitetura (diagrama), como rodar localmente, link da referência da API no ar

**Pronto quando:** qualquer pessoa consegue abrir a página Scalar no ar, ler a API e (com uma chave de demo
documentada) criar um pagamento real no ambiente publicado.

**Entregável visível:** referência da API (Scalar) publicada.

---

## Fase 2 — Eventos, fila e webhooks (≈ 3 semanas)

**Objetivo:** todo pagamento gera eventos confiáveis e o lojista recebe webhooks assinados com garantia de entrega.

**Você aprende:** outbox pattern, at-least-once e consumidores idempotentes, `SKIP LOCKED`, Azure Service Bus,
HMAC, backoff com jitter, circuit breaker, propagação de trace por mensagem.

- [ ] Tabela `OutboxMessage` gravada na mesma transação das mudanças de estado (todos os eventos da seção 5.3)
- [ ] `PaymentEngine.Worker` com `OutboxPublisher` (polling, lote, `SKIP LOCKED`)
- [ ] Interface `IIntegrationEventPublisher` + consumidor genérico na Application
- [ ] Adapter `PaymentEngine.Messaging.AzureServiceBus` + emulador local via Aspire
- [ ] Consumidor idempotente com `ProcessedEvent`
- [ ] Entidades e endpoints de `WebhookEndpoint` (secret mostrado uma vez, criptografado em repouso)
- [ ] `WebhookDispatcher` com assinatura `PaymentEngine-Signature`, timeout, política de resposta e agenda de retry da seção 9
- [ ] Circuit breaker por endpoint
- [ ] Dead-letter + replay via API
- [ ] **Migrar a lógica útil do `webhook-relay`** para o módulo Webhooks; arquivar o repositório antigo com um README apontando para o Motor de Pagamentos
- [ ] Propagar `traceparent` pelo envelope; spans próprios da seção 14
- [ ] Deploy do Worker + Service Bus no Azure

### Testes obrigatórios
- [ ] Unitário: assinatura HMAC (incluindo rejeição por timestamp antigo) e agenda de retry
- [ ] Integração: evento publicado duas vezes gera uma única entrega
- [ ] Integração: receptor que falha 3 vezes e depois aceita → 4 tentativas registradas, status `Succeeded`
- [ ] Snapshot (Verify) do payload de cada tipo de evento
- [ ] Ponta a ponta com `Aspire.Hosting.Testing`: criar pagamento → webhook chega num receptor de teste

### Documentação
- [ ] ADR-0006: outbox em vez de publicar direto na fila
- [ ] ADR-0007: agendamento de retry no banco, não na fila
- [ ] ADR-0008: fila simples em vez de tópico (custo e um só consumidor)
- [ ] README: seção "Como funcionam os webhooks" com exemplo de verificação da assinatura em C#
- [ ] Print do trace completo (POST → webhook) no README

**Pronto quando:** no ambiente publicado, um pagamento gera um webhook assinado entregue a um endpoint de teste, e um
endpoint fora do ar acumula tentativas até o dead-letter.

**Entregável visível:** print/GIF do trace distribuído no README.

---

## Fase 3 — Frontend: playground e dashboard (≈ 3–4 semanas)

**Objetivo:** a vitrine. Um recrutador entende o projeto em 30 segundos sem ler código.

**Você aprende:** React com **TypeScript**, TanStack Query, OIDC com PKCE, validação de JWT na API, RBAC, Static Web Apps.

### Playground (prioridade — vem primeiro)
- [ ] `PaymentEngine.Web` com Vite + React + TypeScript + Tailwind + shadcn/ui
- [ ] Endpoints `/playground/*` da seção 6.3 com rate limit por IP
- [ ] Receptor de webhooks demo com modos `healthy` / `flaky` / `down`
- [ ] Página de checkout fictício com seletor de cartões de teste
- [ ] Timeline ao vivo (polling 1s) com explicação curta de cada etapa
- [ ] Painel dos lançamentos contábeis do pagamento
- [ ] Botão de replay de entrega
- [ ] Reset diário dos dados do lojista demo (job no Worker)

### Dashboard
- [ ] Registro do app no Microsoft Entra External ID (SPA + API com escopo)
- [ ] Login com MSAL React (Authorization Code + PKCE)
- [ ] API validando JWT (`AddJwtBearer`: issuer, audience, assinatura, expiração)
- [ ] `MerchantMember` + políticas por papel (`Viewer` / `Developer` / `Owner`)
- [ ] Primeira entrada cria um lojista sandbox para o usuário
- [ ] Páginas: visão geral, pagamentos (lista + detalhe + ações), ledger, webhooks (endpoints + entregas), API keys

### Testes e deploy
- [ ] Vitest + Testing Library nos componentes principais
- [ ] Playwright: fluxo completo do playground (aprovado, recusado, receptor instável)
- [ ] Integração: requisição sem token → 401; token de `Viewer` tentando criar API key → 403
- [ ] Deploy do front no Azure Static Web Apps, CORS restrito
- [ ] ADR-0009: provedor de identidade externo, sem autenticação própria
- [ ] README: link "Teste ao vivo" no topo + GIF do playground (gravado pelo Playwright)

**Pronto quando:** o link do README abre o playground, um visitante faz um pagamento, vê a timeline, derruba o
receptor, vê os retries e faz replay. Sem login, sem instrução.

**Entregável visível:** playground público + dashboard com login.

---

## Fase 4 — Robustez e prova (≈ 2–3 semanas)

**Objetivo:** mostrar com números e cenários adversos que o sistema aguenta.

**Você aprende:** reconciliação financeira, teste de carga e leitura de percentis, hardening de segurança.

- [ ] Arquivo de liquidação CSV no simulador, com injeção opcional de divergências
- [ ] `ReconciliationJob` + lançamentos de liquidação (pendente → disponível) + `ReconciliationReport`
- [ ] Página de reconciliação no dashboard
- [ ] Job de resolução de `AcquirerOutcomeUnknown` (consulta o simulador e conclui o pagamento)
- [ ] Teste de integração do cenário de timeout: a cobrança aconteceu no simulador e o Motor de Pagamentos reconhece depois
- [ ] Scripts k6 em `tests/load/` (criação de pagamentos, leitura, mistura realista)
- [ ] `docs/PERFORMANCE.md`: ambiente, cenário, throughput, p95/p99, gargalos encontrados e o que foi feito
- [ ] Métricas customizadas da seção 14 + painel (Aspire local / Application Insights)
- [ ] `docs/SECURITY.md` com checklist OWASP API Top 10
- [ ] CodeQL + Dependabot + secret scanning ativos, badges no README
- [ ] (Opcional) Stryker.NET no Domain com score no README
- [ ] (Opcional) Timeline v2 com SignalR + `LISTEN/NOTIFY`

**Pronto quando:** o README tem uma seção de performance com números reais e uma de segurança, e a reconciliação
encontra as divergências injetadas.

**Entregável visível:** números de carga e página de reconciliação.

---

## Fase 5 — Pix simulado (≈ 2 semanas)

**Objetivo:** o meio de pagamento que todo recrutador brasileiro reconhece, reaproveitando ledger, outbox e webhooks.

**Você aprende:** conceitos da API Pix (cobrança, `txid`, `endToEndId`), BR Code / QR Code, deduplicação de
notificações externas, casos de borda de pagamento instantâneo.

- [ ] Agregado `PixCharge` com a máquina de estados da seção 19.2 e testes de todas as transições
- [ ] `POST /v1/pix/charges` (com `Idempotency-Key`), `GET`, cancelamento e devolução
- [ ] Geração do payload "copia e cola" e da imagem do QR Code
- [ ] Simulador de banco pagador no `PaymentEngine.AcquirerSimulator`, enviando a notificação de pagamento
- [ ] Endpoint de notificação com deduplicação por `endToEndId`
- [ ] Lançamentos contábeis do Pix (seção 19.4) e eventos `pix.*` na outbox
- [ ] Job de expiração de cobranças
- [ ] Casos de borda: pagamento após vencimento e valor divergente geram devolução automática
- [ ] Playground: aba "Pagar com Pix" com QR Code e botão "Pagar pelo app do banco"
- [ ] Teste de integração: a mesma notificação enviada 5 vezes credita uma única vez
- [ ] ADR-0010: Pix simulado a partir dos conceitos da API do Banco Central, sem conformidade oficial
- [ ] README: seção "Pix" com GIF do pagamento acontecendo

**Pronto quando:** um visitante do playground gera uma cobrança Pix, "paga pelo app", vê a cobrança concluir, o
lançamento aparecer no ledger e o webhook chegar.

**Entregável visível:** fluxo Pix completo no playground.

---

## Fase 6 — Multi-cloud: AWS (≈ 2 semanas)

**Objetivo:** provar portabilidade. O mesmo domínio roda na AWS usando os serviços nativos dela.

**Você aprende:** SQS, IAM, ECS Fargate, Secrets Manager, LocalStack, integração AWS do Aspire / CDK.

- [ ] Adapter `PaymentEngine.Messaging.AwsSqs` passando **os mesmos testes** de contrato do adapter do Service Bus
- [ ] LocalStack no AppHost para desenvolvimento com `Messaging:Provider=AwsSqs`
- [ ] Deploy na AWS via Aspire (conferir o alvo suportado na época: ECS Fargate via CDK)
- [ ] RDS Postgres, Secrets Manager, IAM roles com menor privilégio
- [ ] Front no S3 + CloudFront
- [ ] Workflows `deploy-aws.yml` e `destroy-aws.yml` manuais
- [ ] ADR-0011: AWS sob demanda por custo
- [ ] README: seção "Roda no Azure e na AWS" com o diagrama das duas topologias

**Pronto quando:** um clique no workflow sobe o Motor de Pagamentos na AWS, o playground funciona lá, e outro clique destrói tudo.

**Entregável visível:** vídeo curto ou GIF mostrando o deploy e o playground rodando na AWS.

---

## Fase 7 — IA (opcional, ≈ 1–2 semanas)

**Objetivo:** um diferencial de IA com uso legítimo, sem forçar.

**Você aprende:** servidor MCP em C# com Streamable HTTP, design de tools, autenticação em MCP.

- [ ] `PaymentEngine.Mcp` com as tools da seção 20 (somente leitura primeiro)
- [ ] Tool de replay com confirmação explícita
- [ ] Autenticação por API key, com isolamento por lojista
- [ ] README: exemplo de conversa no Claude Code investigando falhas de webhook

**Pronto quando:** dá para perguntar "por que os webhooks estão falhando?" e receber uma resposta baseada nos dados reais do ambiente.

---

## Fase 8 — Polimento e divulgação (contínuo)

- [ ] README final em português: frase de impacto, link "Teste ao vivo", GIF, diagrama, decisões principais (links para ADRs), números, como rodar
- [ ] (Opcional) `README.en.md` resumido em inglês, para não fechar a porta internacional
- [ ] README de perfil do GitHub em português apontando para o Motor de Pagamentos
- [ ] Post no LinkedIn por fase concluída (o que construiu, um problema difícil, o que aprendeu)
- [ ] Um artigo técnico (ex.: "Chaves de idempotência do jeito certo no ASP.NET Core") no LinkedIn, TabNews ou dev.to
- [ ] Ensaiar a explicação do projeto em 2 minutos, em português (e, se quiser, em inglês)
- [ ] Lista de perguntas de entrevista que o projeto responde (idempotência, outbox, consistência, por que não microsserviços)
