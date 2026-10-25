# 0002. Monólito modular com worker e simulador, e não microsserviços

- Status: Aceita
- Data: 2026-10-24

## Contexto
O projeto é desenvolvido por uma pessoa só. Ele precisa de trabalho em segundo
plano (publicar eventos, consumir a fila, entregar webhooks, rodar jobs) e de um
adquirente externo para simular.

Microsserviços resolvem problemas de organização de muitos times: cada time
publica sua parte sem esperar os outros e partes do sistema crescem de forma
independente. Este projeto não tem esses problemas. E microsserviços cobram um
preço alto: toda conversa entre as partes passa pela rede, os dados ficam
espalhados em vários bancos, e subir, monitorar e depurar vários serviços dá
muito mais trabalho.

## Decisão
O sistema tem três processos:

- `PaymentEngine.Api`: um monólito modular. Os módulos (Payments, Ledger,
  Merchants, Webhooks) ficam separados por pastas, com fronteiras claras.
- `PaymentEngine.Worker`: um processo separado, porque o trabalho em segundo
  plano tem natureza diferente da API.
- `PaymentEngine.AcquirerSimulator`: um processo separado, porque representa um
  sistema de fora e precisa se comportar como um (latência, falhas, banco próprio).

## Alternativas consideradas
- Microsserviços, um serviço por módulo — custo de rede, de consistência entre
  bancos e de operação, sem o benefício principal, que é dar autonomia a times
  diferentes.
- Monólito sem módulos — mais simples no começo, mas as fronteiras se misturam
  com o tempo e fica difícil separar uma parte depois.
- Tudo num único processo, inclusive o worker e o simulador — o trabalho em
  segundo plano disputaria recursos com a API, e o simulador deixaria de se
  comportar como um sistema externo.

## Consequências
- Mudança de estado, lançamentos do ledger e eventos da outbox podem ser
  gravados na mesma transação do banco.
- Subir, depurar e publicar é simples: o Aspire sobe os três processos juntos.
- A API escala como um bloco só.
- As fronteiras entre os módulos dependem de disciplina. Se um módulo precisar
  virar um serviço separado no futuro, a fronteira já existe.
