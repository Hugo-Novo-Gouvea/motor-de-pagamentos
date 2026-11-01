# 0005. Valor mínimo de pagamento

- Status: Aceita
- Data: 2026-11-01

## Contexto
A tarifa padrão da plataforma é 2,9% do valor + R$ 0,30 fixos. Em pagamentos muito pequenos, a parte fixa
sozinha passa do valor: num pagamento de R$ 0,10, a tarifa seria R$ 0,30, e o lojista receberia -R$ 0,20,
ou seja, pagaria para vender. O spec não definia o que fazer nesse caso.

## Decisão
Todo pagamento tem um valor mínimo por moeda: 50 unidades menores (R$ 0,50 e US$ 0,50). Abaixo disso, o
`Payment.Create` devolve `Result.Failure` com o erro `payment.amount_below_minimum`, que substitui a regra
anterior de valor maior que zero. Um teste garante que a tarifa padrão, aplicada ao valor mínimo, fica abaixo
dele: R$ 0,31 de tarifa sobre R$ 0,50.

## Alternativas consideradas
- Deixar o valor do lojista ficar negativo — o lojista pagaria para vender, e o ledger registraria saldos
  negativos vindos de pagamentos legítimos.
- Limitar a tarifa ao valor do pagamento — o lojista nunca ficaria negativo, mas a plataforma absorveria o
  prejuízo em silêncio, sem que ninguém percebesse.

## Consequências
- Melhor: a regra é explícita e visível para o lojista, que recebe um erro claro em vez de uma cobrança sem
  sentido. É também como as grandes plataformas de pagamento trabalham, com um valor mínimo de cobrança.
- Pior: pagamentos abaixo de R$ 0,50 deixam de ser possíveis. E o mínimo foi calculado para a tarifa padrão:
  uma tarifa personalizada com fixo maior pode voltar a passar do valor mínimo, e vai precisar ser validada
  quando a configuração de tarifas por lojista existir.
- Obrigatório: toda moeda nova precisa de um valor mínimo, e qualquer mudança na tarifa padrão tem que
  continuar abaixo dele. Os dois casos têm testes que quebram se a regra for esquecida.