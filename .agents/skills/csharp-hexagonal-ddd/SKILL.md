---
name: csharp-hexagonal-ddd
description: >-
  Use this skill whenever architecting, creating, or modifying Backend C# code (.NET 8/9+).
  Provides the exact structure, clean code patterns (KISS / Clean Code) for Minimal APIs,
  Hexagonal Architecture (Ports & Adapters), Domain-Driven Design (DDD), Result Pattern,
  FluentValidation, PostgreSQL (EF Core), RabbitMQ (MassTransit Outbox), HybridCache,
  Supabase JWT authentication, Microsoft .NET Team guardrails (dotnet/skills), and automated testing.
---

# Backend C# Standards (.NET 9+ Minimal API + Hexagonal Architecture + DDD + RabbitMQ)

Follow these rigorous standards and official guardrails aligned with **Microsoft .NET Team (`dotnet/skills`)**, **LazySkills C# standards**, and **Clean Code**.

---

## 1. Solution Structure and Strict Dependencies

```text
src/backend/
├── Directory.Build.props      # <Nullable>enable</Nullable>, <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
├── src/
│   ├── App.Domain/           # Zero external dependencies! Pure C# only.
│   ├── App.Application/      # References only: App.Domain + FluentValidation
│   ├── App.Infrastructure/   # References: App.Domain, App.Application + EF Core Npgsql, MassTransit, HybridCache, JwtBearer
│   └── App.Api/              # References: App.Application, App.Infrastructure (solely for DI composition)
└── tests/
    ├── App.UnitTests/        # xUnit, FluentAssertions, NSubstitute
    ├── App.ArchTests/        # NetArchTest.Rules
    └── App.IntegrationTests/ # WebApplicationFactory, Testcontainers (PostgreSQL, RabbitMQ)
```

---

## 2. Strict C# 12/13 (.NET 9) Language Guardrails

### 2.1. Rules for DTOs and API Contracts
- **Mandatory `public sealed record`**: Request and Response DTOs must be `sealed record` with positional properties or `{ get; init; }`. Guarantees immutability, value equality, and JIT devirtualization.
- **Standard Naming**: `Create{Entity}Request`, `Update{Entity}Request`, `{Entity}Response`, `{Entity}ListResponse`.
- **Exclusive `DateTimeOffset`**: Never use naive `DateTime` in API contracts or domain events. Always use `DateTimeOffset` for UTC precision and ISO 8601 compliance.
- **XML Docstrings**: All DTOs must contain `<summary>` tags on their parameters to automatically enrich generated OpenAPI documentation.

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

### 2.2. Modern Idiomatic C# 12/13 Syntax
- **Collection Expressions**: Use `[...]` for collections, empty lists, or literals:
  ```csharp
  List<string> symbols = [];
  ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
  ```
- **Primary Constructors**: Adopt concise primary constructors in services and records:
  ```csharp
  public sealed class CurrencyAlertConsumer(ILogger<CurrencyAlertConsumer> logger) : IConsumer<CurrencyAlertEvent>
  ```
- **`System.Threading.Lock` (.NET 9)**: Replace `lock(new object())` with the dedicated `System.Threading.Lock` type:
  ```csharp
  private readonly System.Threading.Lock _syncLock = new();
  // ...
  lock (_syncLock) { /* optimized critical section */ }
  ```
- **Raw String Literals**: Use `"""` for multi-line JSON, SQL, or templates without escape characters.

---

## 3. Domain and Application Patterns (Hexagonal & DDD)

### 3.1. Result Pattern & Exception-Free Flow (`App.Domain/Common/Result.cs`)
Never throw exceptions for validations or expected domain errors. Return `Result<T>`:

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

### 3.2. Protected Aggregate Roots & Invariants
Entities protect internal state with private constructors and static factory methods:

```csharp
namespace App.Domain.MarketPulse;

public sealed class CurrencyWatchItem : AggregateRoot<Guid>
{
    private CurrencyWatchItem() { } // Required for EF Core

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

## Deep-Dive References (load on demand)

- `references/ef-core-and-minimal-api.md`: EF Core Query Guardrails (`optimizing-ef-core-queries`), Clean ASP.NET Core Minimal APIs (.NET 9+), Anti-Pattern: Prohibition of Sync-Over-Async (`csharp-async`).
- `references/messaging-rabbitmq-outbox.md`: MassTransit Messaging, RabbitMQ & Transactional Outbox.
- `references/testing-battery.md`: Automated Test Battery (xUnit + Testcontainers).
- `references/llm-and-agentic-systems.md`: Multi-LLM Provider & Native Agentic Systems Architecture (.NET 9+).
