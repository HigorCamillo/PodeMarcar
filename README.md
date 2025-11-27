
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

### Explicação Técnica

*   **Tecnologia**: **BCrypt** e **JWT (JSON Web Tokens)**.
*   **Conceito**: **Fluxo de Autenticação**.
*   **Relevância**: Este código ilustra o momento exato da validação de credenciais.
    *   A linha `SenhaHelper.Verificar(senha, master.SenhaHash)` é onde a segurança da criptografia BCrypt é aplicada. O sistema não acessa a senha pura, apenas o *hash* salvo.
    *   Após a verificação bem-sucedida, o sistema prossegue para a **geração do JWT** (código omitido, mas que é o próximo passo no *controller*). O JWT é um token assinado que contém as informações de identidade do usuário e é usado para autorizar requisições futuras à API, implementando o conceito de **Autenticação Stateless (sem estado)**.
