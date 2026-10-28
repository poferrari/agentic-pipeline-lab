---
name: csharp-hexagonal-ddd
description: >-
  Use esta skill sempre que for arquitetar, criar ou modificar código Backend em C# (.NET 8/9+).
  Fornece a estrutura exata e padrões de código mínimo (KISS / Clean Code) para Minimal APIs,
  Arquitetura Hexagonal (Ports & Adapters), Domain-Driven Design (DDD), Result Pattern,
  FluentValidation, PostgreSQL (EF Core), RabbitMQ (MassTransit Outbox), HybridCache,
  autenticação Supabase JWT e testes automatizados (xUnit, NetArchTest, Testcontainers).
---

# Padrões Backend C# (.NET Minimal API + Arquitetura Hexagonal + DDD + RabbitMQ)

Siga estes padrões para produzir código C# legível, enxuto (**KISS**), de fácil manutenção (**Clean Code**) e 100% aderente à **Arquitetura Hexagonal**.

---

## 1. Estrutura de Projetos e Dependências Estritas

```text
src/backend/
├── Directory.Build.props
├── src/
│   ├── App.Domain/           # Sem dependências externas! Apenas C# puro.
│   ├── App.Application/      # Referencia apenas: App.Domain + FluentValidation
│   ├── App.Infrastructure/   # Referencia: App.Domain, App.Application + EF Core Npgsql, MassTransit.RabbitMQ, HybridCache, JwtBearer
│   └── App.Api/              # Referencia: App.Application, App.Infrastructure (apenas para DI)
└── tests/
    ├── App.UnitTests/        # xUnit, FluentAssertions, NSubstitute
    ├── App.ArchTests/        # NetArchTest.Rules
    └── App.IntegrationTests/ # Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql, Testcontainers.RabbitMq
```

---

## 2. Padrões Essenciais de Código (Clean Code & KISS)

### 2.1. Result Pattern & Error (`App.Domain/Common/Result.cs`)
Nunca use `throw new Exception()` para regras de negócio ou validações esperadas.

```csharp
namespace App.Domain.Common;

public enum ErrorType { Validation = 0, NotFound = 1, Conflict = 2, Forbidden = 3, Failure = 4 }

public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
            throw new InvalidOperationException("Invalid error state for Result.");
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;
    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value of a failed Result.");
}
```

### 2.2. Aggregate Root & Domain Events (`App.Domain`)
Entidades protegem suas invariantes com construtores privados e métodos de fábrica claros:

```csharp
namespace App.Domain.Common;

public interface IDomainEvent { DateTimeOffset OccurredOnUtc { get; } }

public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public TId Id { get; protected init; } = default!;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 2.3. Minimal API Limpa com `IEndpoint` e `ProblemDetails` (`App.Api`)
Mantenha `Program.cs` mínimo e isole cada grupo de rotas:

```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess) throw new InvalidOperationException("Successful result cannot be mapped to ProblemDetails.");

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound   => StatusCodes.Status404NotFound,
            ErrorType.Conflict   => StatusCodes.Status409Conflict,
            ErrorType.Forbidden  => StatusCodes.Status403Forbidden,
            _                    => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: result.Error.Code,
            detail: result.Error.Description,
            type: $"https://httpstatuses.com/{statusCode}");
    }
}
```

### 2.4. Autenticação Delegada ao Supabase Auth (Custo Zero)
Valide o JWT emitido pelo Supabase diretamente no ASP.NET Core usando JWKS (`/.well-known/jwks.json` do projeto Supabase):

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var supabaseUrl = builder.Configuration["Supabase:Url"]!; // ex: https://<project>.supabase.co
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
```
Extraia o `UserId` autenticado através de uma porta `ICurrentUserProvider` lendo a claim `ClaimTypes.NameIdentifier` / `"sub"`.

### 2.5. Mensageria RabbitMQ + Transactional Outbox (`MassTransit`)
Nunca publique eventos diretamente no RabbitMQ fora da transação do banco. Configure o Outbox no `AppDbContext`:

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(DependencyInjection).Assembly);

    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2)));
        cfg.ConfigureEndpoints(context);
    });
});
```

### 2.6. Cache de Alta Performance e Baixo Custo (`HybridCache`)
Use `HybridCache` para evitar chamadas repetidas ao PostgreSQL com proteção automática contra *Cache Stampede*:

```csharp
var dto = await cache.GetOrCreateAsync(
    $"orders:{userId}:{orderId}",
    async cancel => await repository.GetByIdAsNoTrackingAsync(orderId, userId, cancel),
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(2) },
    cancellationToken: ct);
```

---

## 3. Teste de Arquitetura Automatizado (`App.ArchTests`)
Inclua sempre este teste para garantir em CI que nenhum desenvolvedor quebrou a Arquitetura Hexagonal:

```csharp
[Fact]
public void Domain_Should_Not_Have_Dependency_On_Other_Projects()
{
    var result = Types.InAssembly(typeof(AggregateRoot<>).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny("App.Application", "App.Infrastructure", "App.Api", "Microsoft.EntityFrameworkCore", "MassTransit")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

