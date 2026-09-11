# TODO — Problemas técnicos e de segurança

Lista de achados do diagnóstico técnico do repositório, ordenados por severidade.

## Média

- [x] **CORS não configurado.** ~~Não havia `AddCors`/`UseCors` em `Program.cs`.~~ Adicionado `AddCors`/`UseCors("Default")`, com as origens permitidas lidas de `Cors:AllowedOrigins` em `appsettings.json` (array vazio por padrão — bloqueia tudo até você adicionar a URL do frontend real). (`Service.API/Program.cs`, `Service.API/appsettings.json`)
- [ ] **Configurar `Cors:AllowedOrigins` para o ambiente real.** Hoje está vazio (bloqueia todo cross-origin) — falta adicionar a(s) URL(s) real(is) do frontend em produção/staging, provavelmente via `appsettings.Production.json`/variável de ambiente equivalente a `Cors__AllowedOrigins__0`, quando essas URLs existirem. (`Service.API/appsettings.json`)

## Baixa

- [x] **Dependência do AutoMapper não utilizada.** ~~O pacote era referenciado e importado mas nunca usado.~~ Removida a `PackageReference` e o `using`; mapeamento continua manual via `ToGetDTO`/`ToEntity`/`ApplyUpdate`. (`Service.Application/Service.Application.csproj`, `Service.Application/Services/BaseService.cs`)
- [x] **`IBaseRepository.Exists(string id)` com tipos inconsistentes.** ~~Comparava `e.Id.ToString() == id` sendo que `Id` é `int`.~~ Assinatura ajustada para `Exists(int id, ...)`, comparando `e.Id == id` diretamente. (`Service.Domain/Interfaces/IBaseRepository.cs`, `Service.Infra.Data/Repositories/BaseRepository.cs`)

## Lacunas de processo (não são bugs, mas faltam)

- [x] **Nenhum teste automatizado.** ~~Não havia projeto de testes na solução.~~ Criado `Service.Tests` (xUnit + NSubstitute + EF Core InMemory), cobrindo `UserService`/`AuthenticateService` (fakes) e `BaseRepository`/`UserRepository` (InMemory) — 33 testes, todos passando. Ainda falta cobertura de `UserController`/`ExceptionMiddleware`/`ApiResponseWrapperFilter` (camada HTTP) e CI rodando isso automaticamente. (`Service.Tests/`)
- [ ] **Nenhuma pipeline de CI/CD.** Sem `.github/workflows` nem qualquer outro arquivo de pipeline.
- [ ] **Sem Dockerfile / infraestrutura de deploy.** Deploy é totalmente manual hoje.
- [ ] **Sem refresh token.** O JWT expira em 1h e não há fluxo de renovação — usuário precisa logar de novo.
- [ ] **Autorização por permissão, não por papel fixo.** Cada rota de controller deve declarar qual **permissão** ela exige, e a checagem deve verificar se o **perfil (`Role`) do usuário** tem essa permissão — configurável em runtime pelo usuário (perfil "Gerente" tem X permissões, "Vendedor" tem Y), não hardcoded em `[Authorize(Roles = "Admin")]`. O `new Claim(ClaimTypes.Role, role)` comentado em `AuthenticateProvider.GenerateToken` é o modelo antigo/errado — não descomentar, é RBAC simples demais pro que se quer aqui.
  - **Modelo de dados necessário**: entidade `Permission` (catálogo de permissões, ex.: `users.create`, `roles.manage`) + `RolePermission` (junção N:N entre `Role`, que já existe, e `Permission`), com endpoints pra administrar essa associação.
  - **Como o controller declara a permissão exigida**: usar policy-based authorization do ASP.NET Core (`[Authorize(Policy = "users.create")]` ou um atributo customizado tipo `[RequirePermission("users.create")]`), nunca `Roles =`.
  - **Onde a checagem acontece — decisão em aberto**: (a) permissões resolvidas no login e embutidas como claims no JWT (rápido, sem consulta ao banco por request, mas fica desatualizado até o usuário logar de novo se um admin mudar as permissões do perfil dele — depende de resolver "Sem refresh token" primeiro pra não ficar ruim); ou (b) checagem no banco a cada requisição via `IAuthorizationHandler` customizado, com cache (efeito imediato quando a config muda, custa uma consulta a mais, mitigável com cache). Como a ideia é o usuário reconfigurar isso em runtime, opção (b) com cache tende a fazer mais sentido.

## Warnings do build (`dotnet build`)

26 warnings no total, agrupados por código e marcados com gravidade.

- [ ] **[WARNING] CS8604 — Gravidade: Média.** Possível argumento nulo passado para `Encoding.GetBytes(...)` ao ler `configuration["Jwt:SecretKey"]`. Se a chave não estiver configurada, isso vira `ArgumentNullException`/`NullReferenceException` em runtime em vez de uma falha clara de configuração na inicialização. (`Service.Infra.Data/Identity/AuthenticateProvider.cs:34`, `Service.Infra.Ioc/DependencyInjection.cs:45`)
- [ ] **[WARNING] ASP0019 — Gravidade: Média.** `Headers.Add(...)` é usado para setar `Pagination` e `Access-Control-Expose-Headers`; `IDictionary.Add` lança `ArgumentException` se a chave já existir (ex.: se CORS ou outro middleware já tiver setado esses headers antes). Trocar por `Headers.Append(...)` ou pelo indexador. (`Service.API/Extensions/HttpExtensions.cs:14-15`)
- [ ] **[WARNING] CS8618 — Gravidade: Baixa.** Propriedades não anuláveis (`Name`, `Email`, `Password`, `Role`) sem valor garantido ao sair do construtor — faltam os modificadores `required` ou tornar as propriedades anuláveis. (`Service.Domain/Entities/User.cs:9-11,13`, `Service.Domain/Entities/Role.cs:5`, `Service.Application/DTOs/User/UserPutDto.cs:12,17`, `Service.Application/DTOs/User/UserPostDto.cs:12,17,21`, `Service.Application/DTOs/User/UserGetDto.cs:10-11`, `Service.Application/DTOs/Role/RolePostDto.cs:9`, `Service.Application/DTOs/Role/RolePutDto.cs:9`, `Service.Application/DTOs/Role/RoleGetDto.cs:6`, `Service.API/Models/UserLogin.cs:10,15`)
- [ ] **[WARNING] CS8603 — Gravidade: Baixa.** Possível retorno de referência nula em métodos que buscam entidade por id/e-mail (comportamento intencional — retornam `null` quando não encontram —, mas a assinatura não está marcada como anulável). (`Service.Infra.Data/Repositories/BaseRepository.cs:23,50`, `Service.Infra.Data/Repositories/UserRepository.cs:20`)
- [ ] **[WARNING] CS8981 — Gravidade: Baixa.** Nome de tipo `init` (gerado automaticamente pelo EF Core para a migration) contém só caracteres ASCII minúsculos, o que pode virar palavra reservada em versões futuras do C#. Renomear a migration não é necessário/recomendado agora (afetaria o histórico), mas vale dar nomes diferentes de `init` em migrations futuras. (`Service.Infra.Data/Migrations/20260911042059_init.cs:10`, `Service.Infra.Data/Migrations/20260911042059_init.Designer.cs:16`)
