
# MarcaAi.Backend (.NET 6) — Upload Local + Migrations limpas

- net6.0 + EF Core 6 + Npgsql 6
- Snake_case no PostgreSQL (sem FKs duplicadas)
- Tabela de junção `funcionarios_servicos`
- Upload local de imagens para serviços e funcionários
- Static files servidos em `/images/services` e `/images/employees`

## Rodar
```bash
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef database update
dotnet run
```
Swagger: http://localhost:5000/swagger

## Popular dados
```bash
psql -h localhost -U postgres -d marcaai -f populate.sql
```
