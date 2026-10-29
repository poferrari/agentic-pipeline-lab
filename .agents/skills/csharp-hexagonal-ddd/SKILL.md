---
name: csharp-hexagonal-ddd
description: >-
  Use esta skill sempre que for arquitetar, criar ou modificar código Backend em C# (.NET 8/9+).
  Fornece a estrutura exata e padrões de código mínimo (KISS / Clean Code) para Minimal APIs,
  Arquitetura Hexagonal (Ports & Adapters), Domain-Driven Design (DDD), Result Pattern,
  FluentValidation, PostgreSQL (EF Core), RabbitMQ (MassTransit Outbox), HybridCache,
  autenticação Supabase JWT, guard rails do Microsoft .NET Team (dotnet/skills) e testes automatizados.
---

# Padrões Backend C# (.NET 9+ Minimal API + Arquitetura Hexagonal + DDD + RabbitMQ)

Siga rigorosamente estes padrões e guard rails oficiais alinhados às diretrizes do time do **.NET da Microsoft (`dotnet/skills`)**, **LazySkills C# standards** e **Clean Code**.

---

## 1. Estrutura de Solução e Dependências Estritas

```text
src/backend/
├── Directory.Build.props      # <Nullable>enable</Nullable>, <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
├── src/
│   ├── App.Domain/           # Sem dependências externas! Apenas C# puro.
│   ├── App.Application/      # Referencia apenas: App.Domain + FluentValidation
│   ├── App.Infrastructure/   # Referencia: App.Domain, App.Application + EF Core Npgsql, MassTransit, HybridCache, JwtBearer
│   └── App.Api/              # Referencia: App.Application, App.Infrastructure (apenas para DI)
└── tests/
    ├── App.UnitTests/        # xUnit, FluentAssertions, NSubstitute
    ├── App.ArchTests/        # NetArchTest.Rules
    └── App.IntegrationTests/ # WebApplicationFactory, Testcontainers (PostgreSQL, RabbitMQ)
```

---

## 2. Guard Rails Estritos de Linguagem C# 12/13 (.NET 9)

### 2.1. Regras para DTOs e Contratos de API
- **`public sealed record` obrigatório**: DTOs de Request e Response devem ser `sealed record` com propriedades posicionais ou `{ get; init; }`. Garante imutabilidade, comparação por valor e otimização JIT.
- **Nomenclatura Padrão**: `Create{Entity}Request`, `Update{Entity}Request`, `{Entity}Response`, `{Entity}ListResponse`.
- **`DateTimeOffset` Exclusivo**: Jamais use `DateTime` cru em contratos de API ou eventos. Use sempre `DateTimeOffset` para precisão UTC e conformidade ISO 8601.
- **Docstrings XML**: Todos os DTOs devem conter `<summary>` nos seus parâmetros para enriquecer automaticamente a documentação OpenAPI gerada.

```csharp
namespace App.Application.MarketPulse.DTOs;

/// <summary>
/// Request contract to register a new currency pair for real-time tracking.
/// </summary>
/// <param name="BaseCurrency">3-letter ISO code for the base currency (e.g. USD).</param>
/// <param name="TargetCurrency">3-letter ISO code for the target currency (e.g. BRL).</param>
/// <param name="TargetRate">The target exchange rate triggering alert notifications.</param>
/// <param name="Notes">Optional contextual user notes.</param>
public sealed record CreateWatchItemRequest(
    string BaseCurrency,
    string TargetCurrency,
    decimal TargetRate,
    string? Notes);
```

### 2.2. Sintaxe Idiomática C# 12/13
- **Collection Expressions**: Use `[...]` para coleções, listas vazias ou literais:
  ```csharp
  List<string> symbols = [];
  ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
  ```
- **Primary Constructors**: Adote construtores primários concisos em services, controllers e records:
  ```csharp
  public sealed class CurrencyAlertConsumer(ILogger<CurrencyAlertConsumer> logger) : IConsumer<CurrencyAlertEvent>
  ```
- **`System.Threading.Lock` (.NET 9)**: Substitua `lock(new object())` pelo novo tipo `System.Threading.Lock`:
  ```csharp
  private readonly System.Threading.Lock _syncLock = new();
  // ...
  lock (_syncLock) { /* secao critica otimizada */ }
  ```
- **Raw String Literals**: Use `"""` para JSON, SQL ou templates multilinhas sem caracteres de escape desnecessários.

---

## 3. Padrões de Domínio e Aplicação (Hexagonal & DDD)

### 3.1. Result Pattern & Erros Sem Exceções (`App.Domain/Common/Result.cs`)
Nunca lance exceções para validações ou regras esperadas de negócio. Retorne `Result<T>`:

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

### 3.2. Aggregate Roots e Invariantes Protegidas
Entidades blindam seu estado interno com construtores privados e métodos factory estáticos:

```csharp
namespace App.Domain.MarketPulse;

public sealed class CurrencyWatchItem : AggregateRoot<Guid>
{
    private CurrencyWatchItem() { } // Para o EF Core

    public string BaseCurrency { get; private set; } = null!;
    public string TargetCurrency { get; private set; } = null!;
    public decimal TargetRate { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<CurrencyWatchItem> Create(
        string baseCurrency,
        string targetCurrency,
        decimal targetRate)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency) || baseCurrency.Length != 3)
            return Result.Failure<CurrencyWatchItem>(Error.Validation("Currency.InvalidBase", "Base currency must be a 3-letter ISO code."));

        if (string.IsNullOrWhiteSpace(targetCurrency) || targetCurrency.Length != 3)
            return Result.Failure<CurrencyWatchItem>(Error.Validation("Currency.InvalidTarget", "Target currency must be a 3-letter ISO code."));

        if (targetRate <= 0)
            return Result.Failure<CurrencyWatchItem>(Error.Validation("Currency.InvalidRate", "Target rate must be strictly greater than zero."));

        var item = new CurrencyWatchItem
        {
            Id = Guid.NewGuid(),
            BaseCurrency = baseCurrency.ToUpperInvariant(),
            TargetCurrency = targetCurrency.ToUpperInvariant(),
            TargetRate = targetRate,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        item.RaiseDomainEvent(new CurrencyWatchItemCreatedDomainEvent(item.Id, item.BaseCurrency, item.TargetCurrency, item.TargetRate));
        return Result.Success(item);
    }
}
```

---

## 4. Guard Rails de Dados e EF Core (`optimizing-ef-core-queries`)

1. **Leitura Pura com `.AsNoTracking()`**: Sempre use `.AsNoTracking()` em queries de leitura (queries CQRS).
2. **Projeções Diretas**: Projete para DTOs via `.Select()` antes de materializar com `.ToListAsync(ct)`. Nunca carregue a entidade inteira se precisar de 3 colunas.
3. **Sem Cartesian Explosion**: Quando houver mais de um `.Include()`, adicione `.AsSplitQuery()` para evitar junções cartesianas gigantescas no PostgreSQL.
4. **Rastreabilidade**: Adicione `.TagWith("MarketPulse.GetActiveWatchlist")` nas queries para identificá-las no pg_stat_statements e APM.
5. **Propagação de Cancelamento**: Passe sempre `CancellationToken ct` para métodos de banco (`ToListAsync(ct)`, `FirstOrDefaultAsync(ct)`, `SaveChangesAsync(ct)`).

---

## 5. ASP.NET Core Minimal APIs Limpas (.NET 9+)

1. **Sem Swashbuckle**: Utilize OpenAPI nativo do .NET 9 (`builder.Services.AddOpenApi()` + `app.MapOpenApi()`).
2. **`TypedResults` Estritos**: Mapeie endpoints usando tipos concretos para gerar schemas estáticos perfeitos sem necessidade de filtros reflexivos pesados:

```csharp
public sealed class WatchlistEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/watchlist")
            .WithTags("Watchlist")
            .RequireAuthorization();

        group.MapPost("/", CreateItemAsync)
            .WithName("CreateWatchItem")
            .WithSummary("Registers a new currency alert item.");
    }

    private static async Task<Results<CreatedAtRoute<WatchItemResponse>, ProblemHttpResult>> CreateItemAsync(
        CreateWatchItemRequest request,
        ICreateWatchItemUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure)
        {
            return TypedResults.Problem(
                statusCode: result.Error.Type switch
                {
                    ErrorType.Validation => StatusCodes.Status400BadRequest,
                    ErrorType.Conflict   => StatusCodes.Status409Conflict,
                    ErrorType.NotFound   => StatusCodes.Status404NotFound,
                    _                    => StatusCodes.Status500InternalServerError
                },
                title: result.Error.Code,
                detail: result.Error.Description);
        }

        return TypedResults.CreatedAtRoute(result.Value, "GetWatchItemById", new { id = result.Value.Id });
    }
}
```

---

## 6. Anti-Padrão: Proibição de Sync-Over-Async (`csharp-async`)

- **NUNCA** chame:
  - `task.Result`
  - `task.GetAwaiter().GetResult()`
  - `task.Wait()`
- **Sempre utilize `await` e retorne `Task` / `Task<T>`** (ou `ValueTask<T>` em caminhos com alto índice de conclusão síncrona).
- Propague o parâmetro `CancellationToken ct` até o cliente HTTP, broker e banco de dados para evitar consumo inútil de recursos quando o cliente encerra a conexão.

---

## 7. Mensageria MassTransit e Transactional Outbox

- **Nunca publique diretamente no RabbitMQ fora da transação**:
  ```csharp
  // Registre o EntityFramework Outbox no MassTransit
  services.AddMassTransit(x =>
  {
      x.AddEntityFrameworkOutbox<AppDbContext>(o =>
      {
          o.UsePostgres();
          o.UseBusOutbox();
      });

      x.UsingRabbitMq((context, cfg) =>
      {
          cfg.Host(configuration.GetConnectionString("RabbitMq"));
          cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
          cfg.ConfigureEndpoints(context);
      });
  });
  ```
- **Consumidores Idempotentes**: Mensagens podem ser reentregues em redes instáveis. Armazene o `EventId` ou utilize o Inbox do MassTransit para garantir execução exatamente uma vez.

---

## 8. Bateria de Testes Automatizados (xUnit + Testcontainers)

1. **Arquitetura (`App.ArchTests`)**:
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
2. **Integração Real (`App.IntegrationTests`)**:
   - Utilize containers Docker gerenciados via `Testcontainers.PostgreSql` e `Testcontainers.RabbitMq`.
   - Inicialize containers em fixtures compartilhadas (`IAsyncLifetime`) para evitar overhead de restart por teste.
