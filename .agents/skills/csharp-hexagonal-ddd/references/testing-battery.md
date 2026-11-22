# Automated Test Battery (xUnit + Testcontainers)

Reference for the `csharp-hexagonal-ddd` skill. Load only when the task touches this area.

## 8. Automated Test Battery (xUnit + Testcontainers)

1. **Architecture Tests (`App.ArchTests`)**:
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
2. **Real Integration Tests (`App.IntegrationTests`)**:
   - Utilize Docker containers managed via `Testcontainers.PostgreSql` and `Testcontainers.RabbitMq`.
   - Initialize containers in shared fixtures (`IAsyncLifetime`) to eliminate per-test restart overhead.

---
