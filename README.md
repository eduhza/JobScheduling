# Job Scheduling Library Comparison

Este repositório tem como finalidade realizar testes comparativos entre duas bibliotecas de agendamento de jobs (versão MIT):
- **Hangfire** - Biblioteca tradicional e bem estabelecida para job scheduling em .NET
- **TickerQ** - Biblioteca alternativa para job scheduling

## 🎯 Objetivo

Comparar o desempenho, características e funcionalidades das bibliotecas Hangfire e TickerQ através de testes padronizados em um ambiente controlado usando Docker.

## 🏗️ Arquitetura

O projeto utiliza uma arquitetura com:
- **API REST** (.NET 9) para criação e gerenciamento de jobs
- **PostgreSQL** como banco de dados
- **Docker Compose** para orquestração dos serviços
- **Dashboards** para monitoramento visual dos jobs

### Estrutura dos Testes

#### APIs Disponíveis
- **API 1** (porta 8080/8081) - `jobscheduling_1`
- **API 2** (porta 8082/8083) - `jobscheduling_2`

#### Configuração por Biblioteca

**Hangfire:**
- Suporta múltiplas filas (`queues`)
- Permite definir quais filas cada API irá consumir
- API 1 consome: `emails`
- API 2 consome: `default`, `critical`, `notifications`

**TickerQ:**
- Pode consumir utilizando 1 ou 2 APIs
- Não possui conceito de filas separadas como o Hangfire
- **Utiliza sistema de prioridades** para as tasks (Normal, High, etc.)
- As prioridades são processadas de acordo com sua importância

## 🧪 Testes Disponíveis

### Endpoints Hangfire
- `POST /hangfire/teste/1` - **Teste 1**: 1.000.000 jobs na fila "emails" (mesma API consumindo)
- `POST /hangfire/teste/2` - **Teste 2**: 1.000.000 jobs na fila "default" (API diferente consumindo)
- `POST /hangfire/teste/3` - **Teste 3**: 1.000.000 jobs "default" + 1.000 jobs "critical" intercalados
- `POST /hangfire/teste/4` - **Teste 4**: 500.000 jobs "default" + 500.000 jobs "critical" alternados

### Endpoints TickerQ
- `POST /tickerq/teste/1` - **Teste 1**: 1.000.000 jobs (mesma API consumindo)
- `POST /tickerq/teste/2` - **Teste 2**: Não suportado (TickerQ não suporta pod sem servidor)
- `POST /tickerq/teste/3` - **Teste 3**: 1.000.000 jobs normais + 1.000 jobs alta prioridade intercalados
- `POST /tickerq/teste/4` - **Teste 4**: 500.000 jobs normais + 500.000 jobs alta prioridade alternados

## 🚀 Como Executar

### Pré-requisitos
- Docker
- Docker Compose

### Configuração das Variáveis de Ambiente

As configurações são feitas através das variáveis de ambiente no `docker-compose.yml`:

```yaml
environment:
  - Library=Hangfire  # ou TickerQ
  - Hangfire__Server__Enabled=true
  - Hangfire__Server__Queues=emails,default,critical
```

### Executando os Testes

1. **Iniciar os serviços:**
   ```bash
   docker-compose up -d
   ```

2. **⚠️ IMPORTANTE - Para testes com TickerQ:**
   Antes de iniciar os testes, é necessário executar as migrations do banco de dados (JobDbContext):
   ```bash
   # Acessar o container da API
   docker exec -it jobscheduling_1 /bin/bash
   
   # Executar as migrations
   dotnet ef database update --context JobDbContext
   ```

3. **Forçar atualização da API (após alterações):**
   ```bash
   docker-compose up -d --build
   ```

4. **Executar testes via HTTP:**
   ```bash
   # Teste Hangfire
   curl -X POST http://localhost:8080/hangfire/teste/1
   
   # Teste TickerQ
   curl -X POST http://localhost:8080/tickerq/teste/1
   ```

## 📊 Monitoramento

### Dashboards Disponíveis

**Hangfire Dashboard:**
- URL: `http://localhost:8080/hangfire` (API 1)
- URL: `http://localhost:8082/hangfire` (API 2)

**TickerQ Dashboard:**
- URL: `http://localhost:8080/tickerq` (API 1)
- URL: `http://localhost:8082/tickerq` (API 2)

### Métricas Coletadas

O sistema coleta métricas através do `IJobMetricsService`:
- Tempo de inserção dos jobs
- Número de jobs inseridos
- Tempos de execução
- Status dos jobs

## 🛠️ Tecnologias Utilizadas

- **.NET 9**
- **C# 13.0**
- **Hangfire 1.8.21**
- **TickerQ 2.5.3-preview**
- **PostgreSQL**
- **Entity Framework Core 9.0.8**
- **Docker & Docker Compose**

## 📁 Estrutura do Projeto

```
JobScheduling.API/
├── Application/
│   ├── Jobs/DoSomething/          # Interfaces dos jobs
│   └── Services/                  # Serviços da aplicação
├── Database/                      # Contextos do EF Core
├── Endpoints/                     # Endpoints da API
│   ├── HangfireEndpoints.cs      # Endpoints específicos do Hangfire
│   └── TickerQEndpoints.cs       # Endpoints específicos do TickerQ
├── Infrastructure/
│   ├── Jobs/DoSomething/         # Implementações dos jobs
│   └── DependencyInjection.cs   # Configuração de DI
├── Migrations/                   # Migrações do EF Core
├── Models/                       # DTOs e models
└── docker-compose.yml           # Orquestração dos containers
```

## 🔧 Configurações Importantes

### Hangfire
- Utiliza PostgreSQL como storage
- Suporte a múltiplas filas
- Dashboard integrado
- Retry automático configurável

### TickerQ
- Utiliza Entity Framework Core
- **Requer migrations do JobDbContext antes do uso**
- Suporte a prioridades (Normal, High, etc.)
- Dashboard próprio
- Sistema de retry customizável

## 📈 Cenários de Teste

### Teste de Volume (1 e 2)
Avalia a capacidade de processar grandes volumes de jobs (1 milhão)

### Teste de Prioridade (3 e 4)
Avalia como cada biblioteca lida com jobs de diferentes prioridades/filas

### Teste de Distribuição
Compara o comportamento com uma vs. duas instâncias da aplicação

## ⚠️ Observações Importantes

- **Ao realizar alterações no código**, sempre atualizar o container com o comando:
  ```bash
  docker-compose up -d --build
  ```
  Este comando força a atualização da API.

- **Para testes com Hangfire**: Você pode definir quais filas cada API irá consumir através das variáveis de ambiente.

- **Para testes com TickerQ**: 
  - Apenas é possível consumir utilizando 1 ou 2 APIs, sem configuração específica de filas
  - Utiliza sistema de prioridades em vez de filas separadas
  - **OBRIGATÓRIO**: Executar migrations do JobDbContext antes dos testes

## 🤝 Contribuindo

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 Licença

Este projeto é licenciado sob a MIT License - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 📞 Suporte

Para dúvidas ou problemas, abra uma issue no repositório ou entre em contato através do GitHub.