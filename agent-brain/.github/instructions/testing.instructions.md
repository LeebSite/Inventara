---
applyTo: "**/*Tests*/**/*.cs"
---

# Testing

- Framework: xUnit. Mocking: Moq (only if already in the project). Assertions: FluentAssertions (only if already in the project).
- Test naming: `MethodName_Condition_ExpectedResult`.
- Arrange / Act / Assert, one assertion concept per test. Test the business rule, not the framework.
- Every new Application service method gets at least: happy path + validation failure + not-found / conflict case.
- Mock the repository / external dependency, never the DbContext directly. Never hit a real database in a unit test.
- Do not edit a test to make it pass. If a test fails, fix the code — or report the test as stale with evidence.
- Before finishing: `dotnet test` for the touched test project must pass.
