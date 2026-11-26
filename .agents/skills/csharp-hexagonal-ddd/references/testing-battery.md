# Automated Test Battery (xUnit + Testcontainers)

Reference for the `csharp-hexagonal-ddd` skill. Load only when the task touches tests. Rules here are enforced by review (`qa-security-reviewer`) and, where possible, by `scripts/validate-all.ps1`.

---

## 1. Test Levels and Where Each One Belongs

Favor fast unit tests for business rules, keep integration tests for adapters that touch real infrastructure, and reserve end-to-end tests for the few journeys that must never break. The more a test resembles production, the slower and more fragile it is: balance the levels, do not stack everything on one.

| Level | Project | Proves | Doubles allowed |
| :--- | :--- | :--- | :--- |
| Unit | `App.UnitTests` | Domain invariants, Value Objects, handlers, validators | Outbound ports only |
| Architecture | `App.ArchTests` | Layer dependency rules | none |
| Integration | `App.IntegrationTests` | Adapters against real Postgres/RabbitMQ, HTTP pipeline, Outbox | none for the infrastructure under test |
| End-to-end | Playwright | Critical user journeys (auth, create alert, error path) | none |

An adapter that is "just a query plus a call" (repository, HTTP client) needs a real-infrastructure integration test, not a mock-based unit test. TDD is mandatory for domain logic and algorithms; for thin adapters it is acceptable to write the integration test right after the adapter, but never to skip it.

## 2. Designing the Test Cases (before writing code)

1. **Equivalence classes**: for every input condition of a business rule, list the valid and invalid classes, then write **one** test per class. Two tests whose inputs belong to the same class are duplicates: delete one. Use `[Theory]` + `[InlineData]` only when each row is a different class or boundary, never to pile up similar values.
2. **Boundary values**: for ranges and thresholds, test the last valid and first invalid value on each edge (for `0 < rate <= 1_000_000` test `0`, a minimal positive value, the maximum and `max + 1`). Mutation testing (`Stryker.NET`) flips exactly these comparisons.
3. **Combinations**: with three or more interacting conditions use a decision table; with many independent parameters use pairwise selection instead of the full cartesian product.
4. **Failure paths**: for each success scenario list the failure scenarios (validation error, conflict, dependency down, timeout, duplicate message).
5. Record the classes and boundaries in the Spec test plan (`docs/templates/SPEC_TEMPLATE.md`, section 11) so QA can check coverage against a list, not a feeling.

## 3. Writing the Test

- **Name = behavior**, in the existing convention `Subject_WhenCondition_ShouldOutcome`. The name must describe the class of input, not concrete values: `Create_WhenTargetRateIsNotPositive_ShouldFail`, not `Create_With0_ShouldFail`. A name that embeds literal values invites more tests of the same class.
- **One behavior per test**, Arrange / Act / Assert. A test that asserts on several different objects is a smell that the production code mixes responsibilities.
- **Assert the whole outcome**: for a returned object compare the object (record equality or `BeEquivalentTo`), not one property; for a list assert the count and the content of each element.
- **Expected first, actual second** when the assertion library requires an order, so failure messages read correctly.
- **No logic in tests** (no `if`, loops over expected values, or recomputing the production formula).
- **Setup without hiding the scenario**: use a Test Data Builder or an object-mother factory for entities with many required fields; keep the values that matter for the scenario visible in the test body. Shared setup belongs in the constructor/fixture, but each test must still read on its own.
- **Do not test private methods.** If a private method seems to need its own test, it is probably a separate responsibility: extract it into its own type and test that. The same applies when a test needs a very large scenario or when the test class keeps growing with "for X" variants: the production class has too many responsibilities.
- **Skip pure noise**: generated or trivial members (auto-properties, pass-through constructors) do not need dedicated tests, and the coverage tooling should exclude them. A one-line method with a business rule is not noise: test it.
- **Look at the failure before trusting a test**: a new test must be seen failing for the expected reason (red) and then passing (green), otherwise the test itself is unproven.

## 4. Test Doubles Policy

- **Do not double**: entities, aggregates, Value Objects, pure helpers, and the unit under test. Use the real object; a bug in it should fail the test.
- **Double**: outbound ports that reach infrastructure (repositories, message bus, HTTP gateways, e-mail) and interfaces with several implementations.
- **Verify outcomes, not choreography**: prefer asserting the returned `Result`, the persisted state or the published event over `Received()` counts. Verifying a call is allowed only when the call *is* the observable behavior (e.g. the event must be published exactly once). A suite that only asserts on doubles is testing the mock.
- **Smells to treat as design feedback**: more than three doubles in one test, doubles that only some tests in the class use, repeating the same stubbing in many tests, a test that needs a long chain of calls. Fix the production design (split the class, introduce a cohesive abstraction) instead of adding more setup.

## 5. Time, Randomness, and Other Ambient Inputs

Static or ambient inputs (`DateTime.UtcNow`, `DateTimeOffset.UtcNow`, `Guid.NewGuid()`, `Random`, environment variables) make logic untestable. Inject them:

- Time: inject `TimeProvider` (register `TimeProvider.System` in DI) and use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` in tests. Never call `UtcNow` inside Domain or Application code.
- Identifiers: inject an ID generator abstraction where tests need deterministic values.
- Weekend/holiday/timezone rules need at least one test per class of day (weekday, weekend, month end, DST transition when relevant).

## 6. Waiting, Flakiness, and Test Isolation

- **No fixed sleeps.** `Task.Delay(500)` before an assertion is either too slow or too short. Poll a condition with a hard timeout (`WaitForConditionAsync`) and fail with a clear message.
- **Proving a negative** (e.g. an uncommitted Outbox message is never dispatched) legitimately needs a bounded wait. Name the constant, keep it as small as the broker latency allows, and comment why.
- Tests never depend on execution order or on state left by another test.
- **Reset state between tests** that share infrastructure: rollback, `TRUNCATE`, or a per-test schema/ID. Prefer creating scenario data in code (builders) over large SQL seed scripts that must be edited whenever a column changes.
- Infrastructure a test claims to use must fail loudly when it is missing (see `learnedLessons.md`, lesson 12).
- A flaky test is a bug: find the root cause (timing, shared state, ordering) instead of adding retries.

## 7. Architecture Tests (`App.ArchTests`)

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

Add a rule when a new layering decision is made (for example "Domain and Application never read the system clock").

## 8. Real Integration Tests (`App.IntegrationTests`)

- Use Docker containers through `Testcontainers.PostgreSql` and `Testcontainers.RabbitMq`.
- Initialize containers once in shared fixtures (`IAsyncLifetime`) to avoid per-test restart cost.
- Controllers/endpoints are adapters: test them through `WebApplicationFactory` for status codes, `ProblemDetails`, authentication and authorization (including BOLA), not for business rules (those belong to unit tests).

## 9. Testing AI-Backed Features (advisor, agents)

LLM output is probabilistic, so test the deterministic parts rigorously and the probabilistic parts by evaluation:

1. **Unit**: tools, prompt assembly, history trimming, fallback engine, validators.
2. **Golden set**: a versioned list of prompts with the expected behavior class (in scope, refusal, grounded answer). Run it on every change of system prompt, model or fallback rules. Compare by semantic or rule-based checks, not exact strings.
3. **Adversarial corpus**: prompt-injection, data-leakage, jailbreak, and persuasion attempts ("ignore previous instructions", "reveal your system prompt", destructive commands, SQL fragments). Assertions: the reply never contains system-prompt text or secrets, never confirms an out-of-scope action, and the application never executes model output as a command.
4. **Agent integration**: include the failure paths (tool error, empty result, repeated identical calls) and a hard cap on steps and spend.
5. Provider unavailable or key missing must degrade to the deterministic engine without changing the contract.

## 10. Mutation Testing as the Judge

Coverage says code ran, mutation score says tests would notice a change. When Stryker.NET reports a surviving mutant, add the missing boundary or assertion; if no test can reasonably kill it, the code is probably dead and should be deleted.
