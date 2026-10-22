# 🐝 ProjectBee

**Mini-ERP de estoque multi-armazém, compras e faturamento, com livro-razão de movimentações, rastreabilidade por lote, notas de entrada e de saída numa unidade de trabalho e autenticação por perfil de acesso.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C#](https://img.shields.io/badge/C%23-14-239120)
![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)
![React](https://img.shields.io/badge/React-TypeScript-61DAFB)
![Tests](https://img.shields.io/badge/tests-xUnit-5E1F87)

**🔗 [Ver demo online](https://project-bee-xi.vercel.app/)** — clique em *Entrar na demo* para acessar com o login já preenchido.

### Destaques

- **Saldo é consequência do histórico:** todo movimento entra no livro-razão na mesma transação que atualiza o saldo, e uma query prova que as duas coisas batem.
- **Concorrência tratada:** `rowversion` + `409`, com testes de rajada de requisições simultâneas.
- **Compras com casos de borda:** recebimento parcial, excedente, divergência de preço, nota duplicada e cancelamento do pendente, tudo numa unidade de trabalho.
- **Faturamento de ponta a ponta:** pedido de venda com reserva de saldo, boleto simulado com linha digitável FEBRABAN, pagamento e nota fiscal que baixa o estoque em FIFO entre lotes.
- **Acesso por perfil:** `Manager` e `Operator`, com a UI e a API respeitando o perfil.
- **98 testes** contra SQL Server real (não InMemory).
- **No ar:** frontend na Vercel, API em container Docker no Azure e banco no Azure SQL.

### Índice

[Sobre](#sobre) · [Stack](#stack) · [Modelo de dados](#modelo-de-dados) · [Livro-razão](#livro-razão) · [Autenticação](#autenticação-e-perfis-de-acesso) · [Endpoints](#endpoints) · [Regras de negócio](#regras-de-negócio-implementadas) · [Decisões técnicas](#decisões-técnicas) · [Testes](#testes) · [Telas](#telas) · [Estrutura](#estrutura) · [Executando localmente](#executando-localmente) · [Roadmap](#roadmap)

---

## Sobre

Trabalhando com ERPs legados, percebi que a maior parte da complexidade não está na tela — está nas regras que impedem o estoque de mentir, e em quem tem permissão pra mexer no quê. O ProjectBee é a minha implementação dessa camada: uma API em .NET 10 que controla saldo de produtos distribuídos entre múltiplos armazéns, com controle de lote e acesso segmentado por perfil, mais um frontend em React que consome tudo isso.

O saldo não é editado: ele é **consequência do histórico**. Toda mudança de estoque entra como um movimento (entrada, saída, transferência ou ajuste), gravado na mesma transação que atualiza o saldo. Não existe outro caminho de escrita. Por isso dá para provar, com uma query, que o saldo de cada lote é exatamente a soma dos seus movimentos — e há testes automatizados garantindo que isso continua verdade mesmo com requisições simultâneas.

O segundo módulo é o de **compras**, que é por onde o estoque recebe mercadoria. Fornecedores, pedidos de compra que mudam de status ao longo do tempo (aberto, parcial, recebido, cancelado) e recebimento por nota fiscal, parcial ou total. Cada nota entra numa unidade de trabalho: nota, quantidades recebidas, status do pedido e entradas no estoque são gravados juntos ou não são gravados. Os casos de borda que costumam virar furo num ERP têm regra explícita: recebimento maior que o pedido, cancelamento depois de receber parte, preço da nota diferente do pedido e a mesma nota lançada duas vezes.

O foco do projeto não é quantidade de endpoints, e sim modelagem correta: chave composta, livro-razão, concorrência otimista, unit of work, integridade referencial validada antes da persistência, contratos de entrada e saída separados das entidades, autorização por perfil (não por auto-atribuição) e erros padronizados em RFC 7807.

O terceiro módulo é o de **faturamento**, que é por onde a mercadoria sai. Clientes, pedidos de venda que já nascem com o boleto emitido, simulação do retorno bancário e emissão da nota fiscal. O pedido só reserva o saldo; quem baixa o estoque é a nota, e ela só sai depois do boleto pago. Nota, saídas lote a lote e status do pedido entram na mesma unidade de trabalho, e duas emissões simultâneas do mesmo pedido terminam em `409` para a segunda.

Financeiro está planejado como próximo módulo. O recebimento de compras já deixa o ponto de extensão pronto para gerar as contas a pagar, e o boleto é a origem natural das contas a receber.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 / C# 14 |
| API | ASP.NET Core Minimal APIs |
| ORM | Entity Framework Core 10 (Code-First + Migrations) |
| Banco | SQL Server |
| Autenticação | ASP.NET Core Identity + JWT Bearer |
| Validação | FluentValidation |
| Documentação | Swagger / OpenAPI (Swashbuckle) |
| PDF | QuestPDF (licença Community) |
| Testes | xUnit + `WebApplicationFactory`, contra SQL Server real |
| Frontend | React + TypeScript (Vite), React Router, Axios, Tailwind CSS v4 |

---

## Modelo de dados

```
Product ──┐
          ├──< StockBalance >── Storage          saldo atual (1 linha por lote)
          │    PK: (ProductId, StorageId, Batch)
          │    RowVersion (concorrência otimista)
          │
          └──< StockMovement >── Storage         histórico imutável (N linhas por lote)
               Type, Quantity (com sinal), Reason, TransferId, PurchaseReceiptId, SalesInvoiceId, CreatedBy, CreatedAt

Supplier ──< PurchaseOrder >── Storage           pedido de compra (Number sequencial, Status, RowVersion)
               ├──< PurchaseOrderItem >── Product        pedido, recebido, preço do pedido
               └──< PurchaseReceipt                      uma nota fiscal do fornecedor
                      └──< PurchaseReceiptItem           lote, quantidade, preço da nota

Customer ──< SalesOrder >── Storage              pedido de venda (Number sequencial, Status, PaymentMethod, RowVersion)
               ├──< SalesOrderItem >── Product           quantidade, preço do produto no pedido
               ├─── Boleto (1:1)                         nosso número, linha digitável, valor, vencimento, pago em
               └─── SalesInvoice (1:1)                   nota fiscal de saída (Number sequencial)
                      └──< SalesInvoiceItem              produto, lote, quantidade, preço

AppUser ──< IdentityUserRole >── IdentityRole (Manager | Operator)
```

O saldo não é um campo dentro do produto. É uma entidade própria com **chave primária composta por produto + armazém + lote**, o que permite o mesmo item existir em quantidades diferentes em locais diferentes, com rastreabilidade de lote — requisito básico de qualquer operação com validade ou recall.

Ao lado do saldo fica o **livro-razão** (`StockMovement`): cada linha é um fato — quem moveu, quanto, quando e por quê. A quantidade é gravada **com sinal** (`+` soma, `−` subtrai), então o saldo de um lote é literalmente `SUM(Quantity)` dos seus movimentos. Os movimentos referenciam produto e armazém com `ON DELETE RESTRICT`: o banco impede que um cadastro seja apagado levando o histórico junto.

O SKU tem índice único no banco (`nvarchar(15)`), além da validação assíncrona na aplicação — a regra vale mesmo se alguém escrever direto na base.

---

## Livro-razão

| Movimento | Tipo | Efeito no saldo | Observação |
|---|---|---|---|
| Entrada | `Entry` | `+` quantidade | Cria o lote se ele ainda não existir |
| Saída | `Exit` | `−` quantidade | Recusada se o saldo for insuficiente |
| Transferência | `TransferOut` + `TransferIn` | `−` na origem, `+` no destino | Dois movimentos ligados pelo mesmo `TransferId`, gravados juntos |
| Ajuste | `Adjustment` | `±` diferença | Recebe a **quantidade contada**; o sistema calcula a diferença. Motivo obrigatório, só `Manager` |
| Saldo inicial | `OpeningBalance` | `±` diferença | Gerado uma única vez pela migration que implantou o livro-razão |
| Recebimento de compra | `PurchaseReceipt` | `+` quantidade | Ligado à nota do fornecedor (`PurchaseReceiptId`) |
| Nota de venda | `SalesInvoice` | `−` quantidade | Ligado à nota de saída (`SalesInvoiceId`); um item pode gerar um movimento por lote |

Cada operação grava o movimento e atualiza o saldo **no mesmo `SaveChanges`** — ou os dois entram, ou nenhum entra.

### Prova real

Esta query compara o saldo de cada lote com a soma dos seus movimentos. Se o livro-razão está íntegro, ela **volta vazia**:

```sql
SELECT b.ProductId, b.StorageId, b.Batch, b.Balance,
       ISNULL(SUM(m.Quantity), 0) AS SomaMovimentos
FROM StockBalances b
LEFT JOIN StockMovements m
       ON m.ProductId = b.ProductId
      AND m.StorageId = b.StorageId
      AND m.Batch     = b.Batch
GROUP BY b.ProductId, b.StorageId, b.Batch, b.Balance
HAVING b.Balance <> ISNULL(SUM(m.Quantity), 0);
```

A mesma verificação roda nos testes automatizados depois de sequências de operações e de rajadas de requisições simultâneas.

### Saldo inicial

O livro-razão foi implantado com o sistema já tendo saldos — criados quando o estoque ainda era editado direto. Para a conta fechar, a migration `AddOpeningBalanceMovements` grava, para cada lote, um movimento `OpeningBalance` com a **diferença** entre o saldo e a soma dos movimentos que ele já tinha (lotes que já fechavam ficam de fora). A correção é um script de dados versionado no git: roda sozinha em qualquer ambiente, e o `Down` desfaz exatamente o que o `Up` fez.

---

## Autenticação e perfis de acesso

Autenticação via ASP.NET Core Identity + JWT Bearer, com dois perfis:

- **`Manager`** — dados mestres (produtos, armazéns), ajuste de inventário e operações destrutivas
- **`Operator`** — movimentações do dia a dia: entrada, saída e transferência

| Método | Rota | Descrição | Autorização |
|---|---|---|---|
| `POST` | `/api/auth/login` | Autentica e retorna o JWT | Público |
| `POST` | `/api/auth/register` | Cria novo usuário (perfil Operator) | Público* |

As roles são atribuídas pelo servidor, nunca escolhidas pelo próprio usuário no payload — auto-atribuição de privilégio é anti-padrão de segurança.

> \* **Decisão consciente de portfólio.** Em um sistema real, `/register` ficaria restrito a `Manager`: criação de usuário passa por aprovação, não por auto-cadastro. Mantive o endpoint aberto aqui para que quem visita o projeto consiga testar o fluxo completo de autenticação sem depender da conta demo. A trava está no código, comentada, com a justificativa registrada.

Um usuário demo com perfil Manager é criado no startup (`IdentitySeed`). No backend, a senha vem de configuração (`Demo:Password`), nunca do código-fonte. A credencial de demonstração é pública de propósito e aparece na tela de login, para quem visita testar sem precisar se cadastrar.

---

## Endpoints

### Códigos de resposta

Os erros seguem RFC 7807 (`ProblemDetails`), e cada código tem um significado só:

| Código | Quando | Onde vem a explicação |
|---|---|---|
| `400` | O pedido está malformado (campo vazio, quantidade ≤ 0, lote fora do padrão) | `errors`, uma lista de mensagens por campo; se o JSON nem desserializa, `detail` |
| `409` | Outra operação alterou o mesmo saldo, produto ou armazém entre a leitura e a gravação | `detail` — basta consultar de novo e repetir |
| `422` | O pedido é válido, mas a regra de negócio recusa (saldo insuficiente, armazém com saldo) | `detail` |

### Movimentações

| Método | Rota | Corpo | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/movements/entry` | `productId`, `storageId`, `batch`, `quantity` | Manager, Operator | `200` `400` `409` |
| `POST` | `/api/movements/exit` | `productId`, `storageId`, `batch`, `quantity` | Manager, Operator | `200` `400` `409` `422` |
| `POST` | `/api/movements/transfer` | `productId`, `fromStorageId`, `toStorageId`, `batch`, `quantity` | Manager, Operator | `200` `400` `409` `422` |
| `POST` | `/api/movements/adjustment` | `productId`, `storageId`, `batch`, `countedQuantity`, `reason` | Manager | `200` `400` `403` `409` `422` |
| `GET` | `/api/movements?productId=&storageId=&batch=` | — | Manager, Operator | `200` |

O sucesso devolve `{ mensagem, saldoAtual }`. O histórico traz os 200 movimentos mais recentes, filtráveis por produto, armazém e lote.

### Estoque

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `GET` | `/api/stock` | Lista saldos com produto e armazém | Manager, Operator | `200` |

O `/api/stock` é **somente leitura**. Os antigos `POST`, `PUT` e `DELETE` gravavam direto no saldo, sem histórico — bastava um `PUT` com `"balance": 999` para o saldo deixar de bater com a soma dos movimentos. Eles foram removidos, e um teste de integração garante que não voltem.

### Produtos

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/products` | Cadastra produto | Manager | `201` `400` |
| `GET` | `/api/products` | Lista produtos | Manager, Operator | `200` |
| `GET` | `/api/products/{id}` | Busca por ID | Manager, Operator | `200` `404` |
| `PUT` | `/api/products/{id}` | Atualiza produto (exige `rowVersion`) | Manager | `200` `400` `404` `409` |
| `DELETE` | `/api/products/{id}` | Remove produto | Manager | `204` `404` `422` |

### Armazéns

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/storages` | Cadastra armazém | Manager | `201` `400` |
| `GET` | `/api/storages` | Lista armazéns | Manager, Operator | `200` |
| `GET` | `/api/storages/{id}` | Busca por ID | Manager, Operator | `200` `404` |
| `PUT` | `/api/storages/{id}` | Atualiza armazém (exige `rowVersion`) | Manager | `200` `400` `404` `409` `422` |
| `DELETE` | `/api/storages/{id}` | Remove armazém | Manager | `204` `404` `422` |

### Fornecedores

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/suppliers` | Cadastra fornecedor | Manager | `201` `400` |
| `GET` | `/api/suppliers` | Lista fornecedores | Manager, Operator | `200` |
| `GET` | `/api/suppliers/{id}` | Busca por ID | Manager, Operator | `200` `404` |
| `PUT` | `/api/suppliers/{id}` | Atualiza fornecedor (exige `rowVersion`) | Manager | `200` `400` `404` `409` |
| `DELETE` | `/api/suppliers/{id}` | Remove fornecedor | Manager | `204` `404` `422` |

### Compras

| Método | Rota | Corpo | Autorização | Respostas |
|---|---|---|---|---|
| `GET` | `/api/purchase-orders?status=&supplierId=` | — | Manager, Operator | `200` |
| `GET` | `/api/purchase-orders/{id}` | — | Manager, Operator | `200` `404` |
| `POST` | `/api/purchase-orders` | `supplierId`, `storageId`, `notes`, `items[]` (`productId`, `quantity`, `unitPrice`) | Manager | `201` `400` |
| `POST` | `/api/purchase-orders/{id}/receipts` | `invoiceNumber`, `priceDivergenceReason`, `rowVersion`, `items[]` (`purchaseOrderItemId`, `batch`, `quantity`, `unitPrice`) | Manager, Operator | `200` `400` `404` `409` `422` |
| `POST` | `/api/purchase-orders/{id}/cancel` | `reason`, `rowVersion` | Manager | `200` `400` `403` `404` `409` `422` |

Criar, receber e cancelar devolvem o pedido completo, com itens, recebimentos e o `rowVersion` novo.

### Clientes

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/customers` | Cadastra cliente | Manager | `201` `400` |
| `GET` | `/api/customers` | Lista clientes | Manager, Operator | `200` |
| `GET` | `/api/customers/{id}` | Busca por ID | Manager, Operator | `200` `404` |
| `PUT` | `/api/customers/{id}` | Atualiza cliente (exige `rowVersion`) | Manager | `200` `400` `404` `409` |
| `DELETE` | `/api/customers/{id}` | Remove cliente | Manager | `204` `404` `422` |

### Faturamento

| Método | Rota | Corpo | Autorização | Respostas |
|---|---|---|---|---|
| `GET` | `/api/sales-orders?status=&customerId=` | — | Manager, Operator | `200` |
| `GET` | `/api/sales-orders/{id}` | — | Manager, Operator | `200` `404` |
| `POST` | `/api/sales-orders` | `customerId`, `storageId`, `paymentMethod`, `notes`, `items[]` (`productId`, `quantity`) | Manager | `201` `400` `422` |
| `GET` | `/api/sales-orders/{id}/boleto/pdf` | — | Manager, Operator | `200` (`application/pdf`) `404` |
| `POST` | `/api/sales-orders/{id}/boleto/payment` | `rowVersion` | Manager | `200` `400` `403` `404` `409` `422` |
| `GET` | `/api/invoices?customerId=` | — | Manager, Operator | `200` |
| `GET` | `/api/invoices/{id}` | — | Manager, Operator | `200` `404` |
| `POST` | `/api/invoices` | `salesOrderId`, `rowVersion` | Manager, Operator | `201` `400` `404` `409` `422` |

O pedido devolve o boleto junto (linha digitável de 47 dígitos, valor, vencimento e status) e, depois de faturado, o número da nota. A nota devolve os itens já quebrados por lote.

> O arquivo [`ProjectBee.http`](./backend/ProjectBee.http) roda de ponta a ponta, incluindo os casos de erro: o login guarda o token e as criações guardam os IDs, então basta executar as requisições na ordem. Cada uma traz no título o status esperado.

---

## Regras de negócio implementadas

**Movimentações**
- Quantidade maior que zero; no ajuste, a quantidade contada não pode ser negativa
- Produto e armazém precisam existir **e estar ativos** — vale para todos os tipos de movimento, inclusive origem e destino da transferência
- Saída e transferência não deixam o saldo negativo: são recusadas com `422`, informando o disponível e o solicitado
- Transferência exige origem e destino diferentes, e cria o lote no destino se ele ainda não existir
- Ajuste recebe a quantidade **contada**, não a diferença; ajuste sem diferença é recusado. Motivo obrigatório (5 a 250 caracteres) e exclusivo do `Manager`
- Lote no padrão `^[A-Z0-9-]{1,30}$` — maiúsculas, dígitos e hífen
- O usuário autenticado (e-mail do token) fica gravado em cada movimento

**Concorrência**
- Duas operações simultâneas sobre o mesmo lote não podem se sobrescrever. Se o saldo mudou entre a leitura e a gravação, a segunda é recusada com `409` e **nada** é gravado — nem o saldo, nem o movimento
- Duas edições do mesmo produto ou armazém também não: o `PUT` envia o `rowVersion` lido ao abrir o formulário, e quem salva com a versão velha recebe `409` em vez de apagar sem aviso a alteração do outro

**Armazéns**
- Código identificador e número do endereço são strings, não inteiros — endereços reais de galpão não cabem em `int` (`"S/N"`, `"1250-A"`)
- Rua, cidade e número obrigatórios
- `IdNumber` único (3 a 20 caracteres): validado na aplicação **e** garantido por índice único no banco, inclusive contra armazéns excluídos
- `IdNumber` é imutável: não faz parte do contrato de atualização, mesma decisão aplicada ao SKU
- `IsActive` é obrigatório na atualização (`bool?` + `NotNull`), pela mesma razão de Produtos
- **Armazém inativo não tem saldo.** Desativar um armazém que ainda tem estoque é recusado com `422`, informando quantos lotes e unidades sobraram — o estoque precisa sair por transferência ou ajuste antes
- Excluir armazém com saldo é recusado. Com saldo zerado mas com histórico, também: apagar o cadastro apagaria a trilha dos movimentos. Nesse caso, o caminho é desativar

**Produtos**
- Preço maior que zero, nome com no mínimo 3 caracteres
- SKU no padrão `^[A-Z0-9-]{3,15}$` — apenas maiúsculas, dígitos e hífen
- SKU único: validado na aplicação **e** garantido por índice único no banco. O SKU de um produto excluído continua reservado, para o histórico não ficar ambíguo
- SKU é imutável: não faz parte do contrato de atualização
- `IsActive` é obrigatório na atualização (`bool?` no DTO + `NotNull` no validator): omitir o campo desativaria o produto silenciosamente, já que o default de `bool` é `false`
- Excluir produto com saldo é recusado com `422`. Com saldo zerado mas com histórico, também — a mesma regra dos armazéns. Nesse caso, o caminho é desativar

**Fornecedores**
- CPF (11 dígitos) ou CNPJ (14), gravado só com dígitos, único inclusive entre excluídos e imutável — como o SKU
- E-mail e telefone opcionais, validados quando informados
- Fornecedor inativo não recebe pedido novo; excluir fornecedor com pedido aberto ou parcial é recusado com `422`

**Compras**
- O pedido nasce **Aberto** (aprovado, aguardando entrega) e muda de status pelos recebimentos: **Parcial** enquanto falta algo, **Recebido** quando todos os itens chegaram. **Cancelado** só por ação do `Manager`
- Fornecedor, armazém e produtos ativos; cada produto uma vez por pedido, quantidade e preço maiores que zero
- Cada recebimento é uma nota fiscal e dá entrada no estoque do armazém do pedido, lote a lote, com movimento do tipo `PurchaseReceipt` ligado à nota — a prova real do livro-razão continua fechando
- **Recebimento menor que o pedido** deixa o item pendente e o pedido Parcial, aguardando as próximas notas. Um item que não veio na nota simplesmente fica de fora dela
- **Recebimento maior que o pendente** é recusado com `422`, informando pendente e informado: o excedente volta com o fornecedor ou vira um pedido novo. Aceitar a sobra em silêncio inflaria o estoque e o futuro título a pagar
- **Preço da nota diferente do pedido** é aceito, mas só com `priceDivergenceReason` (5 a 250 caracteres); sem ele, `422` listando cada item com o preço do pedido e o da nota. O preço gravado no recebimento é o da nota — é o que será pago
- **Cancelar depois de receber parte** cancela só o pendente: o que chegou continua no estoque e as notas seguem registradas. Pedido Recebido não tem o que cancelar; pedido Cancelado não aceita nota
- A mesma nota do mesmo fornecedor não entra duas vezes, nem em pedidos diferentes
- Produto ou armazém desativados depois do pedido bloqueiam o recebimento com `422`, como nos movimentos manuais
- Receber e cancelar exigem o `rowVersion` do pedido: duas pessoas lançando a mesma entrega, ou um cancelamento em cima de um recebimento, terminam em `409` para a segunda

**Clientes**
- Mesmas regras dos fornecedores: CPF ou CNPJ só com dígitos, único inclusive entre excluídos e imutável; e-mail e telefone opcionais
- Cliente inativo não recebe pedido novo; excluir cliente com pedido aguardando pagamento ou faturamento é recusado com `422`

**Faturamento**
- O pedido nasce **Aguardando pagamento**, passa a **Pago** quando o boleto é pago e a **Faturado** quando a nota é emitida
- Cliente, armazém de expedição e produtos ativos; cada produto uma vez por pedido. O preço é o do produto no momento do pedido, gravado no item — reajuste depois não muda venda já feita
- Condição de pagamento `Boleto`, `Pix` ou `CreditCard` no contrato; por enquanto só boleto é aceito (`400` para as outras)
- **Reserva de saldo:** o pedido não mexe no estoque, mas só é aceito se o armazém tiver saldo **disponível** — o físico menos o que outros pedidos ainda não faturados já venderam. Sem isso, o mesmo pote seria vendido duas vezes
- O boleto é emitido junto com o pedido, com vencimento em 3 dias contados do dia em Brasília. A linha digitável segue o layout FEBRABAN (banco, moeda, fator de vencimento, valor, campo livre e dígitos verificadores módulo 10 e 11 de verdade), mas o título não é registrado em banco nenhum: o pagamento é simulado
- O boleto sai em **PDF** no layout da ficha de compensação: recibo do pagador, linha de corte e a ficha com o código de barras Intercalado 2 de 5 (103 × 13 mm), remontado a partir da linha digitável. O documento traz a marca "simulação — sem valor" e, se já pago, a data do pagamento no recibo
- A nota só sai com o boleto pago, e uma vez por pedido. Ela baixa o estoque do armazém do pedido em **FIFO**: o lote que entrou primeiro sai primeiro, e um item pode sair de mais de um lote. Saldo que sumiu entre o pedido e a nota (uma saída avulsa, por exemplo) recusa a emissão com `422` e nada é gravado
- Pagar e faturar exigem o `rowVersion` do pedido; duas emissões simultâneas do mesmo pedido terminam em `409` para a segunda, e a mercadoria sai uma vez só

---

## Decisões técnicas

**Saldo materializado + livro-razão, na mesma transação.** O saldo poderia ser calculado na hora com `SUM(Quantity)`, mas toda consulta de estoque pagaria o custo de somar o histórico inteiro. Manter a linha de saldo e o movimento atualizados juntos dá leitura barata e histórico completo. O risco dessa escolha — os dois divergirem — é coberto de três formas: um único caminho de escrita (o `StockMovementService`), gravação atômica e a query de prova real rodando nos testes.

**Concorrência otimista com `rowversion`.** O furo clássico: duas saídas leem saldo 10 ao mesmo tempo, as duas passam na checagem e as duas gravam — uma atualização se perde, e o saldo pode até ficar negativo. O `StockBalance` tem uma coluna `rowversion` que o SQL Server incrementa a cada `UPDATE`. O EF inclui o valor lido no `WHERE`; se outra requisição gravou no meio, nenhuma linha é afetada, o EF lança `DbUpdateConcurrencyException` e a API devolve `409`. Escolhi otimista em vez de lock pessimista porque conflito no mesmo lote é raro: não faz sentido travar toda leitura para proteger um caso eventual.

**Unit of Work nas compras.** Um recebimento mexe em várias tabelas ao mesmo tempo: nota, itens da nota, quantidade recebida, status do pedido, movimentos e saldos — e, no futuro, contas a pagar. O `UnitOfWork` abre uma transação, roda o trabalho inteiro e só confirma se ele terminar com sucesso; regra recusada, conflito de concorrência ou exceção em qualquer passo desfazem tudo, mesmo o que já tinha passado por `SaveChanges`, e limpam o `ChangeTracker`. Como a API usa `EnableRetryOnFailure` (Azure SQL acordando), a transação roda dentro da estratégia de execução do EF: se a conexão cair no meio, o bloco inteiro é repetido do zero, por isso o trabalho faz as próprias leituras. A entrada no estoque reaproveita o `StockMovementService` (`StageEntryAsync`), que lança movimento e saldo sem gravar, então continua existindo um único caminho de escrita no livro-razão.

**Contas a pagar engatilhado.** O `PurchaseService` chama cada `IPurchaseReceiptListener` registrado dentro da mesma unidade de trabalho do recebimento, com o pedido e a nota (número, fornecedor, itens e preço da nota). O módulo financeiro só precisa registrar um listener que crie o título: se ele falhar, o recebimento inteiro é desfeito, e nunca existe nota no estoque sem título ou título sem nota. Hoje nenhum está registrado.

**Faturar reivindica o pedido primeiro.** A nota tem índice único por pedido. Se ela fosse inserida antes do `UPDATE` do pedido, duas emissões simultâneas disputariam esse índice e a segunda terminaria em `500` (violação de chave), não em `409`. Por isso o `SalesService` grava o status Faturado primeiro, com o `rowVersion` lido no `WHERE`, e só então lança a nota e as saídas, na mesma transação: a segunda requisição espera o lock da linha do pedido e sai com `409`. Um teste dispara cinco emissões ao mesmo tempo e exige exatamente uma nota e uma baixa.

**`409` e `422` são coisas diferentes.** `422` diz "isso não pode ser feito" (saldo insuficiente) — repetir não adianta. `409` diz "o mundo mudou enquanto você decidia" — consultar o saldo e repetir pode dar certo. Separar os dois deixa o cliente reagir certo a cada um.

**Correção de dados em migration, não em script avulso.** O saldo inicial foi implantado por uma migration com SQL, não por um script rodado à mão no banco. Fica versionado, revisável no git e aplicado automaticamente em qualquer ambiente, inclusive no deploy.

**Testes contra SQL Server real, não InMemory.** O provider InMemory e o SQLite não têm `rowversion`: um teste de concorrência neles passaria sem provar nada. Cada classe de teste cria um banco com nome único, aplica as migrations e o apaga no fim.

**Minimal APIs em vez de Controllers.** Menos cerimônia para uma API sem views. Para evitar um `Program.cs` de 400 linhas, cada módulo virou um extension method (`MapProductEndpoints`, `MapMovementEndpoints`, etc.) em arquivo próprio.

**DTOs separados das entidades.** As entidades carregam `CreatedAt`, `UpdatedAt` e `IsActive`, que são detalhe interno e não vazam na resposta. Entrada e saída separadas também porque criar e atualizar aceitam campos diferentes — `SKU` existe no create e não no update, justamente por ser imutável. Os DTOs ficam em `DTO/`, uma pasta por módulo.

**Validators genéricos com classe base.** `BaseProductValidator<T>` concentra as regras comuns entre criação e edição, evitando duplicação.

**Projeção na query, não em memória.** As listagens fazem `.Select()` antes do `.ToListAsync()`, com `AsNoTracking()`, para que o SQL Server retorne só as colunas necessárias em vez de materializar a entidade inteira.

**Identity com `AddIdentityCore` em vez de `AddIdentity`.** Evita o conflito de scheme de cookie que o Identity padrão registra, já que a autenticação é 100% via JWT Bearer.

**Configuração com fail-fast.** Chave JWT e origens de CORS lançam exceção no boot se não estiverem configuradas. Erro de configuração aparece ao subir a aplicação, não no meio de uma requisição em produção.

**Swagger com cadeado seletivo.** Um `IOperationFilter` inspeciona a metadata de cada endpoint e adiciona o requisito de segurança apenas onde há autorização, em vez de marcar a API inteira como protegida.

**Erros padronizados em RFC 7807.** Um `IExceptionHandler` global converte exceções não tratadas em `ProblemDetails`, com detalhe técnico exposto apenas em desenvolvimento. Falhas de validação retornam `ValidationProblem`, e recusas de regra de negócio e de concorrência retornam `Problem` com `detail` — o mesmo formato em todos os casos.

**Escopo consciente: cadastro aberto.** `POST /api/auth/register` é público, contrariando o que seria correto em produção. É uma escolha de portfólio: sem isso, avaliar o controle de acesso do projeto dependeria de credenciais compartilhadas. A linha que aplicaria a restrição (`RequireAuthorization` com perfil `Manager`) está no código, comentada, para deixar explícito que a omissão é deliberada e não esquecimento.

**Valor calculado no cliente, por enquanto.** O `StockResponseDTO` devolve o preço do produto junto do saldo, e o frontend multiplica. A alternativa — um endpoint de agregação com `SUM(Price * Balance)` no SQL Server — é mais correta conforme a base cresce, mas devolveria só o total: não daria para exibir valor por linha nem quebrar por armazém sem criar vários endpoints. A troca fica registrada no roadmap para quando o volume justificar.

**Escopo consciente: single-tenant.** O projeto assume um único cliente por instância. Um cenário multi-tenant real exigiria uma coluna `TenantId` nas entidades principais e filtro por tenant em todo o `DbContext` — mudança estrutural de modelagem, não configuração pontual. Ficou fora do escopo para manter o foco na modelagem de estoque em si.

---

## Testes

```bash
cd ProjectBee/tests/ProjectBee.Tests
dotnet test
```

São 98 testes, em duas camadas:

- **Serviço** (`StockMovementServiceTests`, `PurchaseServiceTests`, `UnitOfWorkTests`, `BoletoGeneratorTests`) — as regras do livro-razão chamando o `StockMovementService` direto: saldo insuficiente, transferência para o mesmo armazém, ajuste sem diferença, sinal de cada movimento, par de transferência com o mesmo `TransferId`, e a prova real depois de uma sequência mista de operações. Nas compras, cada caso de borda: recebimento parcial e total, excedente, cancelamento depois de receber parte, nota em pedido cancelado, preço da nota diferente do pedido, nota repetida, `rowVersion` velho e um listener que falha depois do `SaveChanges` — sem deixar nota, saldo ou movimento para trás. No boleto, os dígitos verificadores são recalculados de forma independente no teste, e as barras do código são decodificadas de volta para os 44 dígitos.
- **API** (`MovementEndpointTests`, `StorageEndpointTests`, `ProductEndpointTests`, `SupplierEndpointTests`, `CustomerEndpointTests`, `PurchaseOrderEndpointTests`, `SalesOrderEndpointTests`, `InvoiceEndpointTests`, `SeedTests`) — a aplicação inteira via `WebApplicationFactory`, com autenticação de verdade: `400` com erros por campo, `422` com `detail`, `401` sem token, `403` do Operator no ajuste e na criação e cancelamento de pedido, bloqueios de desativação e exclusão de armazém, produto e fornecedor, o `/api/stock` recusando escrita, e a prova real fechando com os dados de demonstração num banco recém-criado. Os testes de compras passam pelo `EnableRetryOnFailure` da API, então também provam que a transação do `UnitOfWork` roda dentro da estratégia de execução. No faturamento: reserva de saldo entre pedidos, boleto pago uma vez só, nota antes do pagamento, nota repetida, FIFO entre lotes (com o lote mais antigo vindo depois em ordem alfabética) e saldo que some entre o pedido e a nota.

A concorrência é testada de dois jeitos:

- **Determinístico:** a requisição B lê o saldo, a A grava uma saída, e só então a B tenta gravar com o valor que leu. O teste exige `409`, saldo correto e nenhum movimento a mais. Reproduz o furo sempre, sem depender de timing.
- **Rajada:** 20 saídas disparadas ao mesmo tempo no mesmo lote. Quantas passam depende do timing, mas a regra não: toda saída aceita tem movimento, o saldo cai exatamente o número de aceitas, e a prova real fecha.

Para conferir que os testes pegam erro de verdade, o serviço foi sabotado de propósito: sem a checagem de saldo insuficiente, ou com "o último a gravar vence" no lugar do `409`, os testes correspondentes falham.

Os testes usam o SQL Server de `localhost` com autenticação do Windows. Para apontar para outro servidor (por exemplo, em CI), defina a variável de ambiente `PROJECTBEE_TEST_SQL` com a connection string, sem o nome do banco.

---

## Telas

O frontend cobre todos os módulos, com a UI condicionada ao perfil do usuário — um `Operator` logado não vê os botões de criação e exclusão de dados mestres, nem o de ajuste de inventário, nem os de criar e cancelar pedido de compra (mas registra o recebimento das notas), nem os de criar pedido de venda e simular o pagamento do boleto (mas emite as notas fiscais).

| Rota | Tela | O que mostra |
|---|---|---|
| `/` | Landing | Apresentação do projeto e acesso ao sistema |
| `/login` | Login | Autenticação e obtenção do JWT |
| `/app` | Dashboard | Prévia dos módulos planejados (dados ilustrativos) |
| `/app/products` | Produtos | CRUD, valor em estoque por produto, produtos sem saldo |
| `/app/storages` | Armazéns | CRUD, armazéns ativos, valor armazenado |
| `/app/stock` | Estoque | Saldos por lote, unidades e valor total; entrada, saída, transferência, ajuste e histórico de cada lote |
| `/app/suppliers` | Fornecedores | CRUD com CPF/CNPJ mascarado e contato |
| `/app/purchases` | Pedidos de compra | Pedidos pendentes, unidades a receber, filtro por status e criação de pedido |
| `/app/purchases/:id` | Pedido de compra | Itens com pedido, recebido e pendente; notas recebidas; recebimento de nota e cancelamento do pendente |
| `/app/customers` | Clientes | CRUD com CPF/CNPJ mascarado e contato |
| `/app/sales` | Pedidos de venda | Boletos em aberto, pedidos pagos a faturar, filtro por status e criação de pedido com saldo do armazém por item |
| `/app/sales/:id` | Pedido de venda | Itens, boleto com linha digitável para copiar, PDF do boleto e simulação do pagamento, nota emitida |
| `/app/invoices` | Notas fiscais | Pedidos pagos aguardando nota, emissão da nota e notas emitidas com os lotes que saíram |

No recebimento, cada item pendente já vem com a quantidade pendente e o preço do pedido: o caso comum (a nota bate com o pedido) é só informar a nota e os lotes. Preço diferente fica destacado e pede o motivo; quantidade acima do pendente é barrada antes de enviar.

Na tela de estoque, cada lote já carrega produto, armazém e lote, então os formulários de movimento só pedem o que falta: a quantidade, o destino ou o motivo. No ajuste, a tela deixa claro que o campo é a **quantidade contada** e mostra a diferença que será lançada antes de confirmar. Depois de cada movimento, o saldo é recarregado.

As listagens exibem **valor** (saldo × preço), não só quantidade. Quantidade responde "quanto tem"; valor responde "quanto está parado ali" — que é a pergunta que decide compra, transferência e baixa.

As telas são responsivas: no celular, a lista vira cartões e os formulários abrem como painel inferior.

---

## Estrutura

```
ProjectBee/
├── backend/                # API .NET
│   ├── Data/                   # AppDbContext, UnitOfWork, seed do Identity e seed de dados de negócio
│   ├── DTO/                    # Contratos de entrada e saída, uma pasta por módulo
│   ├── Endpoints/              # Mapeamento HTTP por módulo
│   ├── Filters/                # OperationFilter do Swagger
│   ├── Interfaces/             # Contratos compartilhados entre DTOs
│   ├── Middlewares/            # Tratamento global de exceções
│   ├── Migrations/             # Histórico do schema e scripts de dados (EF Core)
│   ├── Models/                 # Entidades de domínio e constantes de perfil
│   ├── Services/               # StockMovementService (livro-razão), PurchaseService (compras),
│   │                           #   SalesService, BoletoGenerator e BoletoPdf (faturamento) e JWT
│   └── Validators/             # Regras de validação (FluentValidation)
├── tests/
│   └── ProjectBee.Tests/   # xUnit: testes de serviço e de API contra SQL Server
└── frontend/               # Cliente React + TypeScript
    └── src/
        ├── components/         # AppLayout, tabelas e formulários de cada módulo,
        │                       #   MovementForm, MovementHistory, Modal, ConfirmDialog, Toast
        ├── pages/              # Landing, Login, Dashboard, Products, Storages, Stock,
        │                       #   Suppliers, PurchaseOrders, PurchaseOrder, Customers,
        │                       #   SalesOrders, SalesOrder, Invoices
        └── services/           # api.ts (Axios + interceptors), token.ts (JWT),
                                #   navigation.ts (redirect 401 via React Router)
```

---

## Executando localmente

### Backend

**Pré-requisitos:** [.NET SDK 10](https://dotnet.microsoft.com/download) e SQL Server (LocalDB, Express ou Docker).

```bash
git clone https://github.com/fernandesnic/ProjectBee.git
cd ProjectBee/backend
```

Configure os segredos via User Secrets, para não versionar credenciais:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=ProjectBeeDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "uma-chave-longa-e-aleatoria-com-pelo-menos-32-caracteres"
dotnet user-secrets set "Demo:Password" "SuaSenhaDemo@123"
```

Crie o banco e suba a aplicação:

```bash
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update
dotnet run
```

A API sobe em `http://localhost:5054`. O Swagger fica em **http://localhost:5054/swagger** e o health check em **http://localhost:5054/health** (verifica também a conexão com o banco, respondendo `503` se o SQL Server estiver inacessível).

No primeiro boot com a base vazia, produtos, armazéns e saldos de demonstração são semeados automaticamente — as telas já sobem com dado real para navegar. Fornecedores e clientes de demonstração têm checagem própria: bancos criados antes dos módulos de compras e de faturamento também os recebem no próximo boot.

> As origens de CORS são lidas de `Cors:AllowedOrigins` no `appsettings.json` — já configurado para `http://localhost:5173` em desenvolvimento.

### Frontend

**Pré-requisitos:** Node.js 18+.

```bash
cd ProjectBee/frontend
npm install
npm run dev
```

O frontend sobe em `http://localhost:5173` e aponta para `http://localhost:5054` por padrão (configurável via `VITE_API_URL`).

---

## Roadmap

**Feito**
- [x] Livro-razão de movimentações (entrada, saída, transferência, ajuste) com saldo derivado e prova real
- [x] Controle de concorrência otimista para impedir saldo negativo em requisições simultâneas
- [x] Testes de serviço e de integração, incluindo concorrência
- [x] Seed de demonstração registrando os saldos por movimento, para a prova real fechar já no primeiro boot
- [x] Bloquear exclusão de produto com saldo ou histórico, com a mesma regra dos armazéns
- [x] Deploy com demo pública: frontend na Vercel, API em container Docker no Azure e banco no Azure SQL
- [x] Dockerfile da API, com build em duas etapas (SDK para compilar, runtime para rodar)
- [x] Módulo de compras: fornecedores, pedidos com recebimento parcial ou total por nota, cancelamento do pendente e divergência de preço, numa unidade de trabalho
- [x] Módulo de faturamento: clientes, pedidos de venda com reserva de saldo, boleto simulado e nota fiscal com baixa de estoque em FIFO

**Próximo**
- [ ] CI no GitHub Actions, rodando os testes contra um SQL Server em container
- [ ] Devolver `409` também quando duas entradas simultâneas criam o mesmo lote novo
- [ ] `GET /api/stock/summary` — agregação de valor no banco (`SUM(Price * Balance)`) em vez de somar no cliente
- [ ] Paginação e busca nas listagens
- [ ] Contas a pagar: um `IPurchaseReceiptListener` que gera o título de cada nota recebida
- [ ] Editar itens de um pedido ainda sem recebimento (hoje o caminho é cancelar e criar outro)
- [ ] Cancelar pedido de venda com boleto em aberto, liberando a reserva de saldo
- [ ] Pix e cartão como condições de pagamento (o contrato da API já prevê os dois)
- [ ] Contas a receber a partir dos boletos

**Depois**
- [ ] Docker Compose para subir API e banco com um comando
- [ ] Soft delete nas listagens
- [ ] Rate limiting e log de auditoria (o livro-razão já audita o estoque; falta o resto do sistema)
- [ ] Próximo módulo do mini-ERP: financeiro

### Limitação conhecida

O botão *Authorize* do Swagger UI não anexa o header nas requisições. A causa é um bug do `Microsoft.OpenApi` 2.x ([issue #2801](https://github.com/microsoft/OpenAPI.NET/issues/2801)): ao construir o documento em memória, o `OpenApiSecurityRequirement` serializa como objeto vazio (`"security": [{}]`), e a UI conclui que o endpoint é público. A documentação em si funciona normalmente; para testar endpoints protegidos, use o [`ProjectBee.http`](./backend/ProjectBee.http).

---

## Autor

**Nicolas Fernandes**

[LinkedIn](https://linkedin.com/in/fernandesnic) · [GitHub](https://github.com/fernandesnic)
