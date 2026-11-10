# 0006. Estorno não devolve a tarifa

- Status: Aceita
- Data: 2026-11-09

## Contexto
Na captura, a plataforma fica com a tarifa e o resto vai para o lojista: num pagamento de R$ 100,00 com a
tarifa padrão (2,9% + R$ 0,30), R$ 3,20 vão para `fee_revenue` e R$ 96,80 para `merchant_pending`.
Quando o pagamento é estornado, o comprador recebe de volta o valor inteiro. Alguém precisa ficar com o custo
da tarifa de uma venda que foi desfeita: a plataforma ou o lojista.

O trabalho de processar o pagamento já aconteceu (autorização, captura, comunicação com o adquirente), e em
processadores reais é comum a tarifa não voltar no estorno. A Stripe, por exemplo, não devolve a tarifa de
processamento quando um pagamento é estornado.

## Decisão
No estorno, o lojista devolve o valor estornado inteiro e a tarifa cobrada na captura fica com a plataforma.
O lançamento de estorno tem só duas linhas, de mesmo valor (débito na conta do lojista, crédito em
`acquirer_payable`), e nenhuma linha em `fee_revenue`.

## Alternativas consideradas
- Devolver a tarifa proporcional ao valor estornado — a plataforma teria prejuízo em todo estorno, já que o
  processamento já foi feito. Também exigiria guardar a tarifa cobrada em cada pagamento e ratear essa tarifa
  entre estornos parciais, com um arredondamento em cada parte.
- Cobrar uma tarifa extra pelo estorno — é uma regra de negócio nova, sem necessidade no escopo atual.
  Pode ser reavaliada no futuro com um novo ADR.

## Consequências
- O lançamento de estorno é simples: duas linhas de mesmo valor, equilibradas por construção.
- A receita de tarifas nunca diminui por causa de um estorno.
- Depois de um estorno total, o saldo do lojista referente a esse pagamento fica **negativo** no valor da tarifa
  (R$ 100,00 capturados: +96,80 na captura, −100,00 no estorno = −3,20). O lojista passa a dever à plataforma.
- Passa a ser obrigatório: nenhuma regra pode supor que o saldo do lojista é sempre ≥ 0. Como esse saldo negativo
  é compensado (descontado de vendas futuras na liquidação, por exemplo) fica para a Fase 4.
- O teste `ForRefund_FullRefund_DoesNotReturnTheFee` protege essa regra.