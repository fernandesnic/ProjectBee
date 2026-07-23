# 🐝 ProjectBee

**API REST para controle de estoque multi-armazém, com rastreabilidade por lote.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C#](https://img.shields.io/badge/C%23-14-239120)
![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)
![License](https://img.shields.io/badge/license-MIT-green)

<!-- Adicione quando a Fase 6 estiver pronta:
![CI](https://github.com/fernandesnic/ProjectBee/actions/workflows/ci.yml/badge.svg)
-->

---

## Sobre

Trabalhando com ERPs legados, percebi que a maior parte da complexidade não está na tela — está nas regras que impedem o estoque de mentir. O ProjectBee é a minha implementação dessa camada: uma API em .NET 10 que controla saldo de produtos distribuídos entre múltiplos armazéns, com controle de lote.

O foco do projeto não é quantidade de endpoints, e sim modelagem correta: chave composta, integridade referencial validada antes da persistência, contratos de entrada e saída separados das entidades e erros padronizados em RFC 7807.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 / C# 14 |
| API | ASP.NET Core Minimal APIs |
| ORM | Entity Framework Core 10 (Code-First + Migrations) |
| Banco | SQL Server |
| Validação | FluentValidation |
| Documentação | Swagger / OpenAPI (Swashbuckle) |

---

## Modelo de dados

```
Product ──┐
          ├──< StockBalance >── Storage
          │    PK: (ProductId, StorageId, Batch)
```

O saldo não é um campo dentro do produto. É uma entidade própria com **chave primária composta por produto + armazém + lote**, o que permite o mesmo item existir em quantidades diferentes em locais diferentes, com rastreabilidade de lote — requisito básico de qualquer operação com validade ou recall.

---

## Endpoints

### Produtos

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `POST` | `/api/products` | Cadastra produto | `201` `400` |
| `GET` | `/api/products` | Lista produtos | `200` |
| `GET` | `/api/products/{id}` | Busca por ID | `200` `404` |
| `PUT` | `/api/products/{id}` | Atualiza produto | `200` `400` `404` |
| `DELETE` | `/api/products/{id}` | Remove produto | `204` `404` |

### Armazéns

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `POST` | `/api/storages` | Cadastra armazém | `201` `400` |
| `GET` | `/api/storages` | Lista armazéns | `200` |
| `GET` | `/api/storages/{id}` | Busca por ID | `200` `404` |
| `PUT` | `/api/storages/{id}` | Atualiza armazém | `200` `400` `404` |
| `DELETE` | `/api/storages/{id}` | Remove armazém | `204` `404` |

### Estoque

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `POST` | `/api/stock` | Registra saldo | `200` `400` |
| `GET` | `/api/stock` | Lista saldos com produto e armazém | `200` |
| `PUT` | `/api/stock/{productId}/{storageId}` | Atualiza saldo | `200` `400` `404` |
| `DELETE` | `/api/stock/{productId}/{storageId}` | Remove saldo | `204` `404` |

> O arquivo [`ProjectBee.http`](./ProjectBee.http) contém requisições prontas para todos os endpoints, incluindo os casos de erro. Abra no Visual Studio ou VS Code e execute direto.

---

## Regras de negócio implementadas

**Produtos**
- Preço maior que zero
- Nome com no mínimo 3 caracteres
- SKU no padrão `^[A-Z0-9-]{3,15}$` — apenas maiúsculas, dígitos e hífen
- SKU único: validação assíncrona consulta o banco antes de persistir

**Armazéns**
- Código identificador e número do endereço são strings, não inteiros — endereços reais de galpão não cabem em `int` (`"S/N"`, `"1250-A"`)
- Rua, cidade e número obrigatórios, para não existir armazém sem localização

**Estoque**
- Saldo maior que zero
- `ProductId` e `StorageId` verificados contra o banco antes da inserção: não é possível registrar saldo de produto ou armazém inexistente
- Chave composta com lote permite múltiplos lotes do mesmo produto no mesmo armazém

---

## Decisões técnicas

**Minimal APIs em vez de Controllers.** Menos cerimônia para uma API sem views, e o roteamento fica explícito. Para evitar um `Program.cs` de 400 linhas, cada módulo virou um extension method (`MapProductEndpoints`, `MapStorageEndpoints`, `MapStockEndpoints`) em arquivo próprio.

**DTOs separados das entidades.** Contratos distintos de entrada e saída. As entidades carregam `CreatedAt`, `UpdatedAt` e `IsActive`, que são detalhe interno e não vazam na resposta. Entrada e saída separadas também porque criar e atualizar aceitam campos diferentes.

**Validators genéricos com classe base.** `CreateProductDTO` e `UpdateProductDTO` implementam a mesma interface, e um `BaseProductValidator<T>` concentra as regras comuns. Evita duplicar validação entre criação e edição.

**Erros padronizados em RFC 7807.** Um `IExceptionHandler` global converte exceções não tratadas em `ProblemDetails`, com o detalhe técnico exposto apenas em ambiente de desenvolvimento. As falhas de validação retornam `ValidationProblem`, no mesmo formato.

---

## Estrutura

```
ProjectBee/
├── Data/           # AppDbContext e configuração do modelo
├── DTOs/           # Contratos de entrada e saída
├── Endpoints/      # Mapeamento HTTP por módulo
├── Interfaces/     # Contratos compartilhados entre DTOs
├── Middlewares/    # Tratamento global de exceções
├── Migrations/     # Histórico do schema (EF Core)
├── Models/         # Entidades de domínio
└── Validators/     # Regras de validação (FluentValidation)
```

---

## Executando localmente

**Pré-requisitos:** [.NET SDK 10](https://dotnet.microsoft.com/download) e SQL Server (LocalDB, Express ou Docker).

```bash
git clone https://github.com/fernandesnic/ProjectBee.git
cd ProjectBee
```

Configure a connection string via User Secrets, para não versionar credenciais:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=ProjectBeeDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Crie o banco e suba a aplicação:

```bash
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update
dotnet run
```

A API sobe em `http://localhost:5054`. O Swagger fica em **http://localhost:5054/swagger**.

---

## Roadmap

- [ ] Livro-razão de movimentações (entrada, saída, transferência, ajuste) com saldo derivado
- [ ] Controle de concorrência otimista para impedir saldo negativo em requisições simultâneas
- [ ] Índice único de SKU no banco, além da validação na aplicação
- [ ] Soft delete e paginação nas listagens
- [ ] Autenticação JWT com perfis de acesso (operador / gerente)
- [ ] Rate limiting e log de auditoria
- [ ] Testes unitários e de integração
- [ ] Front-end em React + TypeScript
- [ ] Docker Compose e deploy com demo pública

---

## Autor

**Nicolas Fernandes**

[LinkedIn](https://linkedin.com/in/SEU-USUARIO) · [GitHub](https://github.com/fernandesnic)

---

Distribuído sob licença MIT.