# 🐝 ProjectBee

**Sistema de controle de estoque multi-armazém, com rastreabilidade por lote e autenticação por perfil de acesso.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C#](https://img.shields.io/badge/C%23-14-239120)
![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)
![React](https://img.shields.io/badge/React-TypeScript-61DAFB)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Sobre

Trabalhando com ERPs legados, percebi que a maior parte da complexidade não está na tela — está nas regras que impedem o estoque de mentir, e em quem tem permissão pra mexer no quê. O ProjectBee é a minha implementação dessa camada: uma API em .NET 10 que controla saldo de produtos distribuídos entre múltiplos armazéns, com controle de lote e acesso segmentado por perfil, mais um frontend em React que consome tudo isso.

O foco do projeto não é quantidade de endpoints, e sim modelagem correta: chave composta, integridade referencial validada antes da persistência, contratos de entrada e saída separados das entidades, autorização por perfil (não por auto-atribuição) e erros padronizados em RFC 7807.

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
| Frontend | React + TypeScript (Vite), React Router, Axios, Tailwind CSS v4 |

---

## Modelo de dados

```
Product ──┐
          ├──< StockBalance >── Storage
          │    PK: (ProductId, StorageId, Batch)

AppUser ──< IdentityUserRole >── IdentityRole (Manager | Operator)
```

O saldo não é um campo dentro do produto. É uma entidade própria com **chave primária composta por produto + armazém + lote**, o que permite o mesmo item existir em quantidades diferentes em locais diferentes, com rastreabilidade de lote — requisito básico de qualquer operação com validade ou recall.

O SKU tem índice único no banco (`nvarchar(15)`), além da validação assíncrona na aplicação — a regra vale mesmo se alguém escrever direto na base.

---

## Autenticação e perfis de acesso

Autenticação via ASP.NET Core Identity + JWT Bearer, com dois perfis:

- **`Manager`** — dados mestres (produtos, armazéns) e operações destrutivas
- **`Operator`** — movimentações transacionais de estoque

| Método | Rota | Descrição | Autorização |
|---|---|---|---|
| `POST` | `/api/auth/login` | Autentica e retorna o JWT | Público |
| `POST` | `/api/auth/register` | Cria novo usuário (perfil Operator) | Público* |

As roles são atribuídas pelo servidor, nunca escolhidas pelo próprio usuário no payload — auto-atribuição de privilégio é anti-padrão de segurança.

> \* **Decisão consciente de portfólio.** Em um sistema real, `/register` ficaria restrito a `Manager`: criação de usuário passa por aprovação, não por auto-cadastro. Mantive o endpoint aberto aqui para que quem visita o projeto consiga testar o fluxo completo de autenticação sem depender da conta demo. A trava está no código, comentada, com a justificativa registrada.

Um usuário demo com perfil Manager é criado no startup (`IdentitySeed`), com a senha vindo de configuração (`Demo:Password`), nunca do código-fonte.

---

## Endpoints

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
| `PUT` | `/api/storages/{id}` | Atualiza armazém | Manager | `200` `400` `404` |
| `DELETE` | `/api/storages/{id}` | Remove armazém | Manager | `204` `404` |

### Estoque

| Método | Rota | Descrição | Autorização | Respostas |
|---|---|---|---|---|
| `POST` | `/api/stock` | Registra saldo | Manager, Operator | `200` `400` `409` |
| `GET` | `/api/stock` | Lista saldos com produto e armazém | Manager, Operator | `200` |
| `PUT` | `/api/stock/{productId}/{storageId}/{batch}` | Atualiza saldo | Manager, Operator | `200` `400` `404` |
| `DELETE` | `/api/stock/{productId}/{storageId}/{batch}` | Remove saldo | Manager | `204` `404` |

> O arquivo [`ProjectBee.http`](./backend/ProjectBee.http) contém requisições prontas para todos os endpoints, incluindo os casos de erro.

---

## Regras de negócio implementadas

**Produtos**
- Preço maior que zero, nome com no mínimo 3 caracteres
- SKU no padrão `^[A-Z0-9-]{3,15}$` — apenas maiúsculas, dígitos e hífen
- SKU único: validado na aplicação **e** garantido por índice único no banco
- SKU é imutável: não faz parte do contrato de atualização
- `IsActive` é obrigatório na atualização (`bool?` no DTO + `NotNull` no validator): omitir o campo desativaria o produto silenciosamente, já que o default de `bool` é `false`

**Armazéns**
- Código identificador e número do endereço são strings, não inteiros — endereços reais de galpão não cabem em `int` (`"S/N"`, `"1250-A"`)
- Rua, cidade e número obrigatórios

**Estoque**
- Saldo maior que zero
- `ProductId` e `StorageId` verificados contra o banco antes da inserção
- Chave composta com lote permite múltiplos lotes do mesmo produto no mesmo armazém
- Tentativa de registrar produto+armazém+lote já existente retorna `409 Conflict`

---

## Decisões técnicas

**Minimal APIs em vez de Controllers.** Menos cerimônia para uma API sem views. Para evitar um `Program.cs` de 400 linhas, cada módulo virou um extension method (`MapProductEndpoints`, `MapAuthEndpoints`, etc.) em arquivo próprio.

**DTOs separados das entidades.** As entidades carregam `CreatedAt`, `UpdatedAt` e `IsActive`, que são detalhe interno e não vazam na resposta. Entrada e saída separadas também porque criar e atualizar aceitam campos diferentes — `SKU` existe no create e não no update, justamente por ser imutável.

**Validators genéricos com classe base.** `BaseProductValidator<T>` concentra as regras comuns entre criação e edição, evitando duplicação.

**Projeção na query, não em memória.** As listagens fazem `.Select()` antes do `.ToListAsync()`, com `AsNoTracking()`, para que o SQL Server retorne só as colunas necessárias em vez de materializar a entidade inteira.

**Identity com `AddIdentityCore` em vez de `AddIdentity`.** Evita o conflito de scheme de cookie que o Identity padrão registra, já que a autenticação é 100% via JWT Bearer.

**Configuração com fail-fast.** Chave JWT e origens de CORS lançam exceção no boot se não estiverem configuradas. Erro de configuração aparece ao subir a aplicação, não no meio de uma requisição em produção.

**Swagger com cadeado seletivo.** Um `IOperationFilter` inspeciona a metadata de cada endpoint e adiciona o requisito de segurança apenas onde há autorização, em vez de marcar a API inteira como protegida.

**Erros padronizados em RFC 7807.** Um `IExceptionHandler` global converte exceções não tratadas em `ProblemDetails`, com detalhe técnico exposto apenas em desenvolvimento. Falhas de validação retornam `ValidationProblem`, no mesmo formato.

**Escopo consciente: cadastro aberto.** `POST /api/auth/register` é público, contrariando o que seria correto em produção. É uma escolha de portfólio: sem isso, avaliar o controle de acesso do projeto dependeria de credenciais compartilhadas. A linha que aplicaria a restrição (`RequireAuthorization` com perfil `Manager`) está no código, comentada, para deixar explícito que a omissão é deliberada e não esquecimento.

**Escopo consciente: single-tenant.** O projeto assume um único cliente por instância. Um cenário multi-tenant real exigiria uma coluna `TenantId` nas entidades principais e filtro por tenant em todo o `DbContext` — mudança estrutural de modelagem, não configuração pontual. Ficou fora do escopo para manter o foco na modelagem de estoque em si.

---

## Estrutura

```
ProjectBee/
├── backend/                # API .NET
│   ├── Data/                   # AppDbContext e seed do Identity
│   ├── Endpoints/              # Mapeamento HTTP por módulo
│   ├── Filters/                # OperationFilter do Swagger
│   ├── Interfaces/             # Contratos compartilhados entre DTOs
│   ├── Middlewares/            # Tratamento global de exceções
│   ├── Migrations/             # Histórico do schema (EF Core)
│   ├── Models/                 # Entidades de domínio e constantes de perfil
│   ├── Services/               # Geração de JWT
│   └── Validators/             # Regras de validação (FluentValidation)
└── frontend/               # Cliente React + TypeScript
    └── src/
        ├── components/         # AppLayout, Navbar, ProductTable, ProductForm
        ├── pages/              # Landing, Login, Dashboard, Products
        └── services/           # api.ts (Axios + interceptors), token.ts (JWT)
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

A API sobe em `http://localhost:5054`. O Swagger fica em **http://localhost:5054/swagger**.

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

- [ ] Endpoint de health check
- [ ] Seed de dados de demonstração (produtos e armazéns)
- [ ] KPI de valor total do estoque (preço × saldo, agregado no banco)
- [ ] Telas de Armazéns e Estoque no frontend
- [ ] Livro-razão de movimentações (entrada, saída, transferência, ajuste) com saldo derivado
- [ ] Controle de concorrência otimista para impedir saldo negativo em requisições simultâneas
- [ ] Soft delete e paginação nas listagens
- [ ] Rate limiting e log de auditoria
- [ ] Testes unitários e de integração
- [ ] Docker Compose e deploy com demo pública
- [ ] Segundo módulo do mini-ERP: faturamento

---

## Autor

**Nicolas Fernandes**

[LinkedIn](https://linkedin.com/in/fernandesnic) · [GitHub](https://github.com/fernandesnic)

---

Distribuído sob licença MIT.