# 🐝 ProjectBee

**Sistema de controle de estoque multi-armazém, com livro-razão de movimentações, rastreabilidade por lote e autenticação por perfil de acesso.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C#](https://img.shields.io/badge/C%23-14-239120)
![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)
![React](https://img.shields.io/badge/React-TypeScript-61DAFB)
![Tests](https://img.shields.io/badge/tests-xUnit-5E1F87)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Sobre

Trabalhando com ERPs legados, percebi que a maior parte da complexidade não está na tela — está nas regras que impedem o estoque de mentir, e em quem tem permissão pra mexer no quê. O ProjectBee é a minha implementação dessa camada: uma API em .NET 10 que controla saldo de produtos distribuídos entre múltiplos armazéns, com controle de lote e acesso segmentado por perfil, mais um frontend em React que consome tudo isso.

O saldo não é editado: ele é **consequência do histórico**. Toda mudança de estoque entra como um movimento (entrada, saída, transferência ou ajuste), gravado na mesma transação que atualiza o saldo. Não existe outro caminho de escrita. Por isso dá para provar, com uma query, que o saldo de cada lote é exatamente a soma dos seus movimentos — e há testes automatizados garantindo que isso continua verdade mesmo com requisições simultâneas.

O foco do projeto não é quantidade de endpoints, e sim modelagem correta: chave composta, livro-razão, concorrência otimista, integridade referencial validada antes da persistência, contratos de entrada e saída separados das entidades, autorização por perfil (não por auto-atribuição) e erros padronizados em RFC 7807.

Este é o primeiro módulo de um mini-ERP maior — faturamento e financeiro estão planejados como próximos módulos.

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
               Type, Quantity (com sinal), Reason, TransferId, CreatedBy, CreatedAt

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

Um usuário demo com perfil Manager é criado no startup (`IdentitySeed`), com a senha vindo de configuração (`Demo:Password`), nunca do código-fonte.

---

## Endpoints

### Códigos de resposta

Os erros seguem RFC 7807 (`ProblemDetails`), e cada código tem um significado só:

| Código | Quando | Onde vem a explicação |
|---|---|---|
| `400` | O pedido está malformado (campo vazio, quantidade ≤ 0, lote fora do padrão) | `errors`, uma lista de mensagens por campo |
| `409` | Outra operação alterou o mesmo saldo entre a leitura e a gravação | `detail` — basta consultar o saldo e tentar de novo |
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
| `PUT` | `/api/products/{id}` | Atualiza produto | Manager | `200` `400` `404` |
| `DELETE` | `/api/products/{id}` | Remove produto | Manager | `204` `404` |

### Armazéns

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/storages` | Cadastra armazém | Manager | `201` `400` |
| `GET` | `/api/storages` | Lista armazéns | Manager, Operator | `200` |
| `GET` | `/api/storages/{id}` | Busca por ID | Manager, Operator | `200` `404` |
| `PUT` | `/api/storages/{id}` | Atualiza armazém | Manager | `200` `400` `404` `422` |
| `DELETE` | `/api/storages/{id}` | Remove armazém | Manager | `204` `404` `422` |

> O arquivo [`ProjectBee.http`](./backend/ProjectBee.http) contém requisições prontas para testar os endpoints, incluindo os casos de erro.

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

**Armazéns**
- Código identificador e número do endereço são strings, não inteiros — endereços reais de galpão não cabem em `int` (`"S/N"`, `"1250-A"`)
- Rua, cidade e número obrigatórios
- `IdNumber` é imutável: não faz parte do contrato de atualização, mesma decisão aplicada ao SKU
- `IsActive` é obrigatório na atualização (`bool?` + `NotNull`), pela mesma razão de Produtos
- **Armazém inativo não tem saldo.** Desativar um armazém que ainda tem estoque é recusado com `422`, informando quantos lotes e unidades sobraram — o estoque precisa sair por transferência ou ajuste antes
- Excluir armazém com saldo é recusado. Com saldo zerado mas com histórico, também: apagar o cadastro apagaria a trilha dos movimentos. Nesse caso, o caminho é desativar

**Produtos**
- Preço maior que zero, nome com no mínimo 3 caracteres
- SKU no padrão `^[A-Z0-9-]{3,15}$` — apenas maiúsculas, dígitos e hífen
- SKU único: validado na aplicação **e** garantido por índice único no banco
- SKU é imutável: não faz parte do contrato de atualização
- `IsActive` é obrigatório na atualização (`bool?` no DTO + `NotNull` no validator): omitir o campo desativaria o produto silenciosamente, já que o default de `bool` é `false`

---

## Decisões técnicas

**Saldo materializado + livro-razão, na mesma transação.** O saldo poderia ser calculado na hora com `SUM(Quantity)`, mas toda consulta de estoque pagaria o custo de somar o histórico inteiro. Manter a linha de saldo e o movimento atualizados juntos dá leitura barata e histórico completo. O risco dessa escolha — os dois divergirem — é coberto de três formas: um único caminho de escrita (o `StockMovementService`), gravação atômica e a query de prova real rodando nos testes.

**Concorrência otimista com `rowversion`.** O furo clássico: duas saídas leem saldo 10 ao mesmo tempo, as duas passam na checagem e as duas gravam — uma atualização se perde, e o saldo pode até ficar negativo. O `StockBalance` tem uma coluna `rowversion` que o SQL Server incrementa a cada `UPDATE`. O EF inclui o valor lido no `WHERE`; se outra requisição gravou no meio, nenhuma linha é afetada, o EF lança `DbUpdateConcurrencyException` e a API devolve `409`. Escolhi otimista em vez de lock pessimista porque conflito no mesmo lote é raro: não faz sentido travar toda leitura para proteger um caso eventual.

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

São 29 testes, em duas camadas:

- **Serviço** (`StockMovementServiceTests`) — as regras do livro-razão chamando o `StockMovementService` direto: saldo insuficiente, transferência para o mesmo armazém, ajuste sem diferença, sinal de cada movimento, par de transferência com o mesmo `TransferId`, e a prova real depois de uma sequência mista de operações.
- **API** (`MovementEndpointTests`, `StorageEndpointTests`) — a aplicação inteira via `WebApplicationFactory`, com autenticação de verdade: `400` com erros por campo, `422` com `detail`, `401` sem token, `403` do Operator no ajuste, bloqueios de desativação e exclusão de armazém, e o `/api/stock` recusando escrita.

A concorrência é testada de dois jeitos:

- **Determinístico:** a requisição B lê o saldo, a A grava uma saída, e só então a B tenta gravar com o valor que leu. O teste exige `409`, saldo correto e nenhum movimento a mais. Reproduz o furo sempre, sem depender de timing.
- **Rajada:** 20 saídas disparadas ao mesmo tempo no mesmo lote. Quantas passam depende do timing, mas a regra não: toda saída aceita tem movimento, o saldo cai exatamente o número de aceitas, e a prova real fecha.

Para conferir que os testes pegam erro de verdade, o serviço foi sabotado de propósito: sem a checagem de saldo insuficiente, ou com "o último a gravar vence" no lugar do `409`, os testes correspondentes falham.

Os testes usam o SQL Server de `localhost` com autenticação do Windows. Para apontar para outro servidor (por exemplo, em CI), defina a variável de ambiente `PROJECTBEE_TEST_SQL` com a connection string, sem o nome do banco.

---

## Telas

O frontend cobre os três módulos, com a UI condicionada ao perfil do usuário — um `Operator` logado não vê os botões de criação e exclusão de dados mestres, nem o de ajuste de inventário.

| Rota | Tela | O que mostra |
|---|---|---|
| `/` | Landing | Apresentação do projeto e acesso ao sistema |
| `/login` | Login | Autenticação e obtenção do JWT |
| `/app` | Dashboard | Prévia dos módulos planejados (dados ilustrativos) |
| `/app/products` | Produtos | CRUD, valor em estoque por produto, produtos sem saldo |
| `/app/storages` | Armazéns | CRUD, armazéns ativos, valor armazenado |
| `/app/stock` | Estoque | Saldos por lote, unidades e valor total; entrada, saída, transferência, ajuste e histórico de cada lote |

Na tela de estoque, cada lote já carrega produto, armazém e lote, então os formulários de movimento só pedem o que falta: a quantidade, o destino ou o motivo. No ajuste, a tela deixa claro que o campo é a **quantidade contada** e mostra a diferença que será lançada antes de confirmar. Depois de cada movimento, o saldo é recarregado.

As listagens exibem **valor** (saldo × preço), não só quantidade. Quantidade responde "quanto tem"; valor responde "quanto está parado ali" — que é a pergunta que decide compra, transferência e baixa.

As telas são responsivas: no celular, a lista vira cartões e os formulários abrem como painel inferior.

---

## Estrutura

```
ProjectBee/
├── backend/                # API .NET
│   ├── Data/                   # AppDbContext, seed do Identity e seed de dados de negócio
│   ├── DTO/                    # Contratos de entrada e saída, uma pasta por módulo
│   ├── Endpoints/              # Mapeamento HTTP por módulo
│   ├── Filters/                # OperationFilter do Swagger
│   ├── Interfaces/             # Contratos compartilhados entre DTOs
│   ├── Middlewares/            # Tratamento global de exceções
│   ├── Migrations/             # Histórico do schema e scripts de dados (EF Core)
│   ├── Models/                 # Entidades de domínio e constantes de perfil
│   ├── Services/               # StockMovementService (livro-razão) e geração de JWT
│   └── Validators/             # Regras de validação (FluentValidation)
├── tests/
│   └── ProjectBee.Tests/   # xUnit: testes de serviço e de API contra SQL Server
└── frontend/               # Cliente React + TypeScript
    └── src/
        ├── components/         # AppLayout, tabelas e formulários de cada módulo,
        │                       #   MovementForm, MovementHistory, Modal, ConfirmDialog, Toast
        ├── pages/              # Landing, Login, Dashboard, Products, Storages, Stock
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

No primeiro boot com a base vazia, produtos, armazéns e saldos de demonstração são semeados automaticamente — as três telas já sobem com dado real para navegar.

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

**Próximo**
- [ ] Seed de demonstração registrando os saldos por movimento, para a prova real fechar já no primeiro boot
- [ ] Bloquear exclusão de produto com saldo ou histórico, com a mesma regra dos armazéns
- [ ] Devolver `409` também quando duas entradas simultâneas criam o mesmo lote novo
- [ ] Deploy: Azure App Service + Azure SQL, frontend em Vercel/Netlify, demo pública
- [ ] `GET /api/stock/summary` — agregação de valor no banco (`SUM(Price * Balance)`) em vez de somar no cliente
- [ ] Paginação e busca nas listagens

**Depois**
- [ ] Docker Compose e CI no GitHub Actions (rodando os testes contra um SQL Server em container)
- [ ] Soft delete nas listagens
- [ ] Rate limiting e log de auditoria (o livro-razão já audita o estoque; falta o resto do sistema)
- [ ] Segundo módulo do mini-ERP: faturamento

### Limitação conhecida

O botão *Authorize* do Swagger UI não anexa o header nas requisições. A causa é um bug do `Microsoft.OpenApi` 2.x ([issue #2801](https://github.com/microsoft/OpenAPI.NET/issues/2801)): ao construir o documento em memória, o `OpenApiSecurityRequirement` serializa como objeto vazio (`"security": [{}]`), e a UI conclui que o endpoint é público. A documentação em si funciona normalmente; para testar endpoints protegidos, use o [`ProjectBee.http`](./backend/ProjectBee.http).

---

## Autor

**Nicolas Fernandes**

[LinkedIn](https://linkedin.com/in/fernandesnic) · [GitHub](https://github.com/fernandesnic)

---

Distribuído sob licença MIT.
