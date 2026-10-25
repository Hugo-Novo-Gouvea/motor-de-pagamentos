# 0004. Dinheiro como long em unidades menores

- Status: Aceita
- Data: 2026-10-25

## Contexto
O sistema inteiro gira em torno de valores: pagamentos, tarifas, estornos, saldos e lançamentos no ledger.
O `double` (e o `float`) guarda números em binário e não representa com exatidão valores decimais simples
como 0,1: `0.1 + 0.2` resulta em `0.30000000000000004`. Em milhões de operações, essas diferenças se acumulam
e o ledger deixa de fechar. Além disso, um número solto não sabe em que moeda está: nada impede somar reais
com dólares.

## Decisão
Todo valor monetário é um `Money`: um value object com o valor inteiro na menor unidade da moeda (`long`,
centavos no caso do real: R$ 10,50 = 1050) e a moeda (`Currency`, com os códigos da ISO 4217). Operações entre
moedas diferentes lançam `CurrencyMismatchException`. Quando uma conta gera fração de centavo, como nas
tarifas, o arredondamento é explícito e usa banker's rounding (`MidpointRounding.ToEven`).

## Alternativas consideradas
- `double` / `float` — imprecisos para valores decimais. Inaceitável para dinheiro.
- `decimal` — exato em base 10, seria uma escolha correta. Ficou de fora porque permite guardar fração de
  centavo sem ninguém perceber (30,45 centavos, por exemplo), o que deixa o arredondamento para um lugar
  qualquer do código. Com `long`, todo valor guardado é um número inteiro de centavos, e o arredondamento
  precisa ser feito de forma explícita no momento da conta. Também é o formato usado nas APIs das grandes
  plataformas de pagamento.
- `long` sem um tipo `Money` — exato, mas não carrega a moeda. A regra de não misturar moedas teria que ser
  lembrada em cada lugar do código.

## Consequências
- Melhor: contas exatas, igualdade por valor e a regra da moeda num lugar só, coberta por testes.
- Pior: a conversão entre o que a pessoa vê (R$ 10,50) e o que o sistema guarda (1050) precisa acontecer nas
  bordas (API, front, relatórios), e um erro nessa conversão multiplica ou divide valores por 100. A suposição
  de duas casas decimais vale para BRL e USD; uma moeda com outra quantidade de casas (o iene não tem
  centavos) exigiria guardar essa informação na `Currency`.
- Obrigatório: nenhum `double` ou `float` para dinheiro em nenhuma camada. Todo valor monetário passa por
  `Money` ou é convertido para ele na entrada do sistema.