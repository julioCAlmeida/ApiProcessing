## Questionário – MLOps Pre-processamento

### 1) Como lidar com grandes volumes de dados enviados para pré-processamento? O design atual é suficiente?

- **Hoje (OK para demo/POC):**

  - Chunking no cliente (ex.: 10k registros por lote) + endpoint que cria jobs assíncronos.
  - Background worker lê a fila no banco (`ProcessingJobs`) e executa.
  - Resultados consultáveis via `GET /api/Processing/{id}`.

- **Para escalar:**
  - Streaming/NDJSON ou upload para Object Storage (S3, GCS, Azure Blob) e só enviar o pointer (URL/Key) para a API; o worker lê do storage.
  - Compactação (gzip/br) e `Transfer-Encoding: chunked` para cargas muito grandes.
  - Backpressure: limites de payload, número de jobs por tenant, e rejeição com 429.
  - Fila externa (RabbitMQ / Kafka / SQS / Azure Queue) para desacoplar API ↔ execução, com multiple workers.
  - Idempotência: `Idempotency-Key` por submission e checagem no servidor.
  - Banco: índices (já existe em `Status`, `CreatedAt`), connection pooling (Npgsql), batch writes e minimizar roundtrips.
  - Observabilidade: métricas de tempo/size, filas, dead-letter queue.

**Resultado:** processamento previsível, resiliente e com custo controlável.

### 2) Que medidas você implementaria para se certificar que a aplicação não execute scripts maliciosos?

- Sandbox (Jint) endurecida:
  - Sem CLR/IO/rede, sem `eval/new Function`, sem `setTimeout/setInterval`.
  - Limites de tempo, memória e recursão.
- Validação estática (AST): bloquear imports, new Function, eval antes de persistir.
- Whitelisting das globals expostas ao script (ex.: só `process(data)`).
- Quotas: CPU/memória/tempo por job e por tenant.
- Isolamento: pods/containers separados para workers, read-only FS.
- Segredos fora do runtime (Vault/KeyVault/Secrets), nunca expostos.
- Auditoria: log de submissões, versão do script, duração, consumo e erro.
- Review/approval para scripts “privilegiados”.

### 3) Como aprimorar a implementação para suportar um alto volume de execuções concorrentes de scripts?

- Arquitetura producer/consumer: API publica jobs na fila; N workers consomem.
- Escala horizontal dos workers (HPA/autoscaling) com bounded concurrency.
- Cache de script compilado por `ScriptVersionId` para evitar recompilações.
- Separar banco de leitura/escrita, connection pool configurado, retries jittered.
- Particionamento por `scriptId`/tenant para reduzir lock contention.
- Resultados no storage (S3/Blob) + DB guarda metadados (tamanho/URL/hash).
- Circuit breaker / rate-limit por cliente.

### 4) Como você evoluiria a API para suportar o versionamento de scripts?

- Tabela `ScriptVersions`:
  `ScriptVersionId (PK)`, `ScriptId (FK)`, `Version (semver)`, `Code`, `IsActive`, `CreatedAt`.
- Jobs referenciam ScriptVersionId (imutável).
- Fluxo draft → publish; rollback para versões anteriores.
- Compatibilidade do schema de entrada: `InputSchema` (JSON Schema opcional) por versão.
- APIs: `POST /scripts/{id}/versions`, `PATCH /scripts/{id}/versions/{v}/activate`, `GET /scripts/{id}/versions`.

### 5) Que tipo de política de backup de dados você aplicaria neste cenário?

- PostgreSQL PITR (WAL archiving):
  - Full diário + retenção de WAL (ex.: 7–30 dias).
  - Restores testados periodicamente (DR drills).
- Backup dos volumes (Docker/Cloud volume snapshots) + off-site.
- Criptografia at rest e in transit.
- Retenção e expurgo por compliance (LGPD), com jobs de limpeza.
- Documentação de RPO/RTO: ex. RPO 15min, RTO 1h.

### 6) Como tratar massas de dados com potenciais informações sensíveis na API e no banco de dados?

- Minimização: processe só o necessário; anonimização/masking/tokenização quando possível.
- Criptografia:
  - Em trânsito (HTTPS/TLS) e em repouso (disco/volume).
  - Colunar (pgcrypto) para campos sensíveis (`email`, `cpf`, etc.), se necessário.
- Segregação de ambientes (dev/stg/prod) e de acessos por função (least privilege).
- Segredos via Secret Manager; nunca em `appsettings.json` versionado.
- Logs sem PII (ou com masking).
- Auditoria e trilha de acesso (quem viu, quando e por quê).
- Política de retenção e direito de eliminação (LGPD).

### 7) Como você enxerga o paradigma funcional beneficiando a solução deste problema?

- Pureza/Imutabilidade → previsibilidade e reprodutibilidade do pipeline.
- Testabilidade: `process(data)` é determinística, fácil de unit-testar.
- Composição: encadear transformações menores (map/filter/reduce) é natural.
- Concorrência segura: menos shared state/locks.
- Modelagem expressiva (F#): discriminated unions, pattern matching, domínio mais rígido.
- Menos efeitos colaterais: perfeito para transformação de dados (ETL/ELT).

No projeto, o módulo em F# ilustra bem: recebe JSON, aplica regras, retorna JSON.
