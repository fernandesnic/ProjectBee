# ProjectBee - Mini ERP Backend 🐝

O ProjectBee representa a modernização da gestão de negócios. Desenvolvido para servir como o coração de um Mini ERP, este projeto pega a densidade das regras de negócio que vivenciei lidando com ERPs legados e as transforma em uma API leve, rápida e padronizada utilizando .NET 8. É a vitrine do meu portfólio, desenhada com foco em boas práticas, código limpo e arquitetura pronta para integrações front-end.
## 🚀 Tecnologias Utilizadas

* **C# 12** & **.NET 8**
* **ASP.NET Core Minimal APIs** (Rotas de alta performance e baixo overhead)
* **Entity Framework Core 8** (Abordagem Code-First com Migrations)
* **SQL Server** (Banco de dados relacional corporativo)
* **FluentValidation** (Validações de domínio robustas e centralizadas)
* **Swagger/OpenAPI** (Documentação interativa de contratos)

## 🏗️ Decisões Arquiteturais e Padrões de Projeto

Para garantir que o sistema opere em nível de produção e simule um cenário corporativo real, apliquei os seguintes padrões:

1.  **Desacoplamento com DTOs (Data Transfer Objects):** As entidades originais do banco de dados nunca são expostas diretamente para o cliente. Criei contratos específicos de entrada e contratos limpos de saída, blindando colunas internas de auditoria e segurança.
2.  **Modularização de Rotas (Extension Methods):** Para evitar um arquivo inflado e ilegível, as rotas foram isoladas em métodos de extensão organizados por contextos.
3.  **Validação Rígida no Pipeline (FluentValidation):** Implementação de validações manuais injetadas diretamente nos endpoints da API, interceptando requisições malformadas e impedindo que "dados sujos" cheguem à camada de persistência.
4.  **Tratamento Global de Erros:** Configuração de um utilizando o padrão (RFC 7807), garantindo respostas padronizadas em caso de falhas inesperadas no sistema.

## 📦 Módulos Implementados Até o Momento

### 1. Produtos (`Products`)
* **CRUD Completo** com respostas HTTP semânticas (201 Created, 204 NoContent, 404 NotFound).
* **Regras de Domínio Aplicadas:**
    * Preço obrigatoriamente maior que zero.
    * Nome com tamanho mínimo de 3 caracteres.
    * **SKU Único e Padronizado:** Validação assíncrona que consulta o banco de dados via EF Core para impedir SKUs duplicados. Validação via Regex para garantir que o SKU contenha apenas letras maiúsculas, números e traços (padrão de código de barras).

### 2. Armazéns / Depósitos (`Storages`)
* **CRUD Completo** mapeando locais físicos de estocagem.
* **Regras de Domínio Aplicadas:**
    * Uso de strings para código de identificação e endereços para suportar ções complexas de galpões (ex: "Galpão 3B", "S/N").
    * Validação rigorosa de obrigatoriedade de campos de endereço (Rua, Cidade, Número) para evitar estoques órfãos.

## 🛠️ Como Executar o Projeto

1.  **Clonar o repositório:**
    ```bash
    git clone [https://github.com/fernandesnic/ProjectBee.git]
    cd project-bee
    ```
2.  **Configurar a Connection String:**
    Abra o arquivo `appsettings.json` e insira as credenciais do seu SQL Server local na chave `DefaultConnection`.
3.  **Rodar as Migrations (Criar o banco e tabelas):**
    ```bash
    dotnet ef database update
    ```
4.  **Executar a aplicação:**
    ```bash
    dotnet run
    ```
5.  **Acessar a documentação (Swagger):**
    Navegue até `http://localhost:XXXX/swagger` (substitua pela porta gerada no console) para interagir com os endpoints via interface gráfica.
