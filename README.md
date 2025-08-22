# Processing API

API ASP.NET Core para **hospedar e executar scripts de pré-processamento em JavaScript**, persistindo scripts e execuções em PostgreSQL e processando de forma **assíncrona** via _BackgroundService_.  
A API expõe documentação OpenAPI/Swagger e um pipeline de CI (GitHub Actions) para build e testes.

---

## Sumário

- [Processing API](#processing-api)
  - [Sumário](#sumário)
  - [Arquitetura](#arquitetura)
  - [Tecnologias](#tecnologias)
  - [Estrutura de pastas](#estrutura-de-pastas)
  - [Como executar](#como-executar)
    - [⚡ Execução rápida (recomendado)](#-execução-rápida-recomendado)
    - [Execução Manual](#execução-manual)
      - [1) Docker Compose (API + DB)](#1-docker-compose-api--db)
      - [2) Local (sem Docker)](#2-local-sem-docker)
    - [Execução Automática](#execução-automática)
      - [A) Rodar tudo em Docker (API + DB)](#a-rodar-tudo-em-docker-api--db)
      - [B) Rodar API local + DB no Docker](#b-rodar-api-local--db-no-docker)
  - [Variáveis de ambiente](#variáveis-de-ambiente)
  - [Migrações do EF Core](#migrações-do-ef-core)
  - [Fluxo de uso da API](#fluxo-de-uso-da-api)
    - [1) Enviar um script](#1-enviar-um-script)
    - [2) Criar um job de processamento](#2-criar-um-job-de-processamento)
    - [3) Consultar o job](#3-consultar-o-job)
  - [Exemplos](#exemplos)
    - [Script JS de agregação (exemplo)](#script-js-de-agregação-exemplo)
    - [Payload de dados (exemplo)](#payload-de-dados-exemplo)
  - [Testes e CI](#testes-e-ci)
  - [Segurança](#segurança)
  - [Escala e performance](#escala-e-performance)
  - [Licença](#licença)

---

## Arquitetura

- **API (ASP.NET Core)** – Endpoints REST (`/api/Scripts`, `/api/Processing`).
- **Jint (JS engine)** – Executa o `process(data)` enviado pelo usuário em **sandbox**.
- **F# lib (Processing/PreProcessor)** – Pós-processamento opcional quando o output do JS atende ao formato esperado.
- **PostgreSQL** – Persistência de scripts e jobs (com status).
- **BackgroundService** – Fila simples no banco: pega jobs `Pending` e executa.
- **Swagger/OpenAPI** – Documentação da API em `/docs` e JSON em `/openapi/v1.json`.
- **CI (GitHub Actions)** – Build + Test.

---

## Tecnologias

- .NET 9 + ASP.NET Core
- Entity Framework Core + Npgsql
- Jint (sandbox JS)
- F# (biblioteca `Processing`)
- Swagger (Swashbuckle)
- xUnit (testes)
- Docker / Docker Compose
- GitHub Actions

---

## Estrutura de pastas

- ApiProcessing.sln
- Api/
  - Controllers/
  - Data/
  - Dtos/
  - Migrations/
  - Models/
  - Properties/
  - Services/
  - Program.cs
  - appsettings.json
- docker-compose.yml
  - docker-compose.override.yml
- github/workflows/ci.yml
- Api.Tests/
- Processing/ # biblioteca F#

## Como executar

🧰 Pré-requisitos

- Windows + PowerShell 5+ (ou PowerShell 7)
- Docker Desktop instalado e em execução (para o banco).
- .NET SDK 9.0 instalado (para rodar local).

### ⚡ Execução rápida (recomendado)

- Um comando e pronto: sobe API + PostgreSQL via Docker Compose e abre o Swagger.

```bash
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass .\setup.ps1 -Mode docker
```

- Swagger: http://localhost:8080/docs
- OpenAPI JSON: http://localhost:8080/openapi/v1.json

Para e Limpar:

```bash
docker compose down -v
```

### Execução Manual

#### 1) Docker Compose (API + DB)

- Crie um arquivo **`.env`** na raiz do projeto com o conteúdo:

```env
POSTGRES_DB=mlopsdb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

DB_HOST=db
DB_PORT=5432

DB_CONN=Host=${DB_HOST};Port=${DB_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}

ASPNETCORE_URLS=http://0.0.0.0:8080
ASPNETCORE_ENVIRONMENT=Development
```

- Suba os containers:

```bash
docker compose up --build
```

- Acesse:

  - Swagger: http://localhost:8080/docs
  - OpenAPI JSON: http://localhost:8080/openapi/v1.json

#### 2) Local (sem Docker)

- Instale PostgreSQL e crie um banco mlopsdb.
- Crie `Api/appsettings.Local.json` (não versione):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mlopsdb;Username=postgres;Password=postgres"
  }
}
```

- Rode migrações (ver seção de migrações).
- Execute a API:

```bash
dotnet run --project Api/Api.csproj
```

- Swagger: `http://localhost:8080/docs` (ou a porta configurada).
- Nunca versione `appsettings.Local.json` ou `.env` com credenciais reais.

### Execução Automática

#### A) Rodar tudo em Docker (API + DB)

```bash
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\setup.ps1 -Mode docker
```

Parar/remover:

```bash
docker compose down -v
```

#### B) Rodar API local + DB no Docker

```bash
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\setup.ps1 -Mode local
```

## Variáveis de ambiente

- DB_CONN – Connection string do PostgreSQL (ex.: Host=db;Port=5432;Database=mlopsdb;Username=postgres;Password=postgres).

- ASPNETCORE_ENVIRONMENT – Development | Production.
- ASPNETCORE_URLS – Ex.: http://0.0.0.0:8080 (para Docker).

## Migrações do EF Core

Gerar (se necessário):

```bash
dotnet ef migrations add <NomeDaMigracao> -p Api/Api.csproj -s Api/Api.csproj
```

Aplicar:

```bash
dotnet ef database update -p Api/Api.csproj -s Api/Api.csproj
```

No Docker, `o Program.cs` aplica `db.Database.Migrate()`.

## Fluxo de uso da API

### 1) Enviar um script

`POST /api/Scripts`

Body:

```json
{
  "name": "BacenAggregator",
  "code": "function process(data){ /* ... */ return data; }"
}
```

Resposta `201 Created` (exemplo):

```json
{
  "id": "e1834ca2-4504-4f6b-b18e-4297699f906f",
  "name": "BacenAggregator",
  "uploadedAt": "2025-08-20T14:06:41.5331146Z"
}
```

### 2) Criar um job de processamento

`POST /api/Processing`

Body:

```json
{
  "scriptId": "e1834ca2-4504-4f6b-b18e-4297699f906f",
  "data": [
    {
      /* array de registros */
    }
  ]
}
```

Resposta `201 Created`: retorna o JobId (GUID).

### 3) Consultar o job

`GET /api/Processing/{id}`

Resposta `200 OK` (exemplo):

```json
{
  "id": "bf80a279-321c-473a-b85e-1c3ff878067fe",
  "scriptId": "e1834ca2-4504-4f6b-b18e-4297699f906f",
  "status": "completed",
  "createdAt": "2025-08-20T15:20:57.372442Z",
  "finishedAt": "2025-08-20T15:20:58.668932Z",
  "result": [
    {
      /* output do script/pós-processamento */
    }
  ]
}
```

## Exemplos

### Script JS de agregação (exemplo)

Agrupa por `trimestre` e `nomeBandeira`, soma campos e ignora `produto !== 'Empresarial'`:

```js
function process(data) {
  const arr = Array.isArray(data) ? data : [];
  const corp = arr.filter((x) => x.produto === "Empresarial");

  const map = new Map();
  for (const it of corp) {
    const key = `${it.trimestre}-${it.nomeBandeira}`;
    let acc = map.get(key);
    if (!acc) {
      acc = {
        trimestre: it.trimestre,
        nomeBandeira: it.nomeBandeira,
        qtdCartoesEmitidos: 0,
        qtdCartoesAtivos: 0,
        qtdTransacoesNacionais: 0,
        valorTransacoesNacionais: 0,
      };
      map.set(key, acc);
    }
    acc.qtdCartoesEmitidos += Number(it.qtdCartoesEmitidos) || 0;
    acc.qtdCartoesAtivos += Number(it.qtdCartoesAtivos) || 0;
    acc.qtdTransacoesNacionais += Number(it.qtdTransacoesNacionais) || 0;
    acc.valorTransacoesNacionais += Number(it.valorTransacoesNacionais) || 0;
  }
  return Array.from(map.values());
}
```

### Payload de dados (exemplo)

```json
[
  {
    "trimestre": "2023Q2",
    "nomeBandeira": "VISA",
    "nomeFuncao": "Crédito",
    "produto": "Empresarial",
    "qtdCartoesEmitidos": 3508384,
    "qtdCartoesAtivos": 1716709,
    "qtdTransacoesNacionais": 43984982,
    "valorTransacoesNacionais": 12486611557.78,
    "qtdTransacoesInternacionais": 470796,
    "valorTransacoesInternacionais": 397043258.04
  }
]
```

## Testes e CI

Rodar testes localmente

```bash
dotnet test ApiProcessing.sln
```

- GitHub Actions
  - Arquivo: `.github/workflows/ci.yml`
  - Executa restore → build → test a cada push/PR.

## Segurança

- O **Jint** roda em **sandbox**:

  - Tempo de execução limitado (`TimeoutInterval`).
  - Sem acesso a I/O, rede, threads, reflection ou assemblies do CLR.
  - Bloqueios a padrões perigosos (ex.: `eval`, `new Function`, `import` em ES modules) via validação do script.

- **Validação do script** antes de salvar/executar:

  - Deve exportar `function process(data)`.
  - Rejeita patterns proibidos.

- Segredos via `.env` / `appsettings.Local.json` / variáveis de ambiente. Nunca versione credenciais.

## Escala e performance

- Processamento assíncrono: o job é persistido como Pending; o BackgroundService consome e executa.
- Envio em lotes (chunking) para cargas grandes:
  - Ex.: dividir o JSON em blocos de 10 000 itens e criar vários jobs ou vários POST sequenciais.
- Banco: índices em colunas de busca (ex.: ProcessingJobs(Status, CreatedAt)).
- Horizontal: escalar réplicas da API/worker (com fila externa ou coordenação por row locking no DB).

## Licença

Distribuído sob a licença MIT. Veja `LICENSE` (se aplicável).

```

```
