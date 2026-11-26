# Test Playbook: Isolated Behavioral Test Generation

> **Golden Rule**: Tests must verify observable behavior, never implementation details or mock call counts.
> To prevent **Confirmation Bias**, generate tests in an isolated session (`/clear` or clean subagent) rather than the conversational window where the code was written.

---

## 1. Test Session Metadata

| Attribute | Details |
| :--- | :--- |
| **Target Component** | `[e.g., OrderAggregate / CurrencyExchangeHandler / JwtValidator]` |
| **Layer / Scope** | `[Domain Unit / Application Use Case / Integration with Testcontainers]` |
| **Test Frameworks** | `xUnit + FluentAssertions + NSubstitute (Unit) / WebApplicationFactory + Testcontainers (Integration)` |

---

## 2. Test Design: Equivalence Partitioning & Boundary Value Analysis (BVA)

Every business rule must be tested across valid partitions, invalid partitions, and precise boundaries:

| ID | Test Scenario | Input Partition / Boundary | Expected Outcome | Method Name |
| :---: | :--- | :--- | :--- | :--- |
| `TC-01` | Valid nominal case | Typical valid inputs | `Result.Success` with expected state | `Given_ValidInput_When_Executing_Then_ReturnsSuccess` |
| `TC-02` | Lower boundary | Minimum allowed value (e.g., Amount = 0.01) | Valid execution | `Given_MinimumAmount_When_Creating_Then_Succeeds` |
| `TC-03` | Off-boundary invalid | Value below minimum (e.g., Amount = 0.00) | `Result.Failure` with Validation Error | `Given_ZeroAmount_When_Creating_Then_ReturnsValidationError` |
| `TC-04` | Upper boundary | Maximum allowed value | Valid execution | `Given_MaximumAmount_When_Creating_Then_Succeeds` |
| `TC-05` | Unauthorized actor | User does not own the resource (BOLA/IDOR) | `Result.Failure` with Forbidden/NotFound | `Given_NonOwnerUser_When_Accessing_Then_ReturnsForbidden` |
| `TC-06` | Illegal state transition | Status transition not permitted by aggregate | `Result.Failure` with Domain Conflict Error | `Given_CompletedOrder_When_Cancelling_Then_ReturnsConflict` |

---

## 3. Test Structure: Given / When / Then Pattern

All test methods must strictly adhere to the Arrange-Act-Assert (`Given_When_Then`) convention:

```csharp
[Fact]
public void Given_NonOwnerUserId_When_CheckingOwnership_Then_ReturnsFalse()
{
    // Arrange (Given)
    var ownerId = UserId.New();
    var otherUserId = UserId.New();
    var orderResult = Order.Create(ownerId, Money.Create(100m, "USD").Value);
    var order = orderResult.Value;

    // Act (When)
    var isOwned = order.IsOwnedBy(otherUserId);

    // Assert (Then)
    isOwned.Should().BeFalse();
}
```

---

## 4. Anti-Coverage Theater Verification (Mutation Resilience)

A test suite that passes 100% when methods return `default` is invalid (Coverage Theater).
- [ ] **Observable Assertions**: Assertions check state changes and outputs, not intermediate internal fields.
- [ ] **Mutation Testing**: Run Stryker.NET on the target assembly to ensure all injected mutants are killed:
  ```powershell
  dotnet stryker --project src/App.Domain.csproj
  ```
- [ ] **Mutation Score**: Mutation score $\ge 80\%$.
