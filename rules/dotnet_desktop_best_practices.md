# .NET Desktop Application Best Practices

# 1. General .NET Development Guidelines

## Naming Conventions

Follow Microsoft naming conventions:

### Classes

-   PascalCase
-   Nouns or noun phrases
-   Examples:
    -   FileTransferService
    -   MainViewModel
    -   TransferSession

### Methods

-   PascalCase
-   Verb or verb phrase
-   Examples:
    -   StartTransfer()
    -   CalculateChecksum()
    -   ValidateInput()

### Private fields

-   camelCase with underscore prefix
-   Example:
    -   \_fileService
    -   \_logger

### Interfaces

-   Prefix with I
-   Example:
    -   IFileTransferService
    -   ILoggerService

### Async methods

-   Suffix with Async
-   Example:
    -   StartTransferAsync()
    -   LoadFilesAsync()

## Class Structure Best Practices

### Single Responsibility Principle

Each class should have one reason to change.

Bad example: - A class that handles UI, networking, and file validation.

Good example: - FileTransferService → networking only - FileValidator →
validation only - TransferViewModel → UI state only

## Access Modifiers

-   Use the most restrictive modifier possible.
-   Default to private.
-   Use internal for same-assembly usage.
-   Avoid unnecessary public APIs.

## Immutability

Prefer immutable objects when possible:

-   Use readonly fields.
-   Use init setters.
-   Avoid mutable global state.

## Exception Design

-   Throw specific exceptions.
-   Avoid catching Exception unless necessary.
-   Use custom exception types for domain errors.

Example: TransferFailedException

## Logging Standards

-   Log at appropriate levels:
    -   Trace
    -   Debug
    -   Information
    -   Warning
    -   Error
    -   Critical
-   Do not log sensitive information.
-   Include contextual data.

# 2. Architecture Principles

## Use MVVM (Model-View-ViewModel)

MVVM is recommended for WPF, WinUI, MAUI, Avalonia.

-   Model: Business logic and data
-   View: XAML UI
-   ViewModel: Presentation logic

### MVVM Best Practices

-   No business logic in Views
-   Use data binding
-   Implement INotifyPropertyChanged
-   Use ObservableCollection`<T>`{=html}
-   Use ICommand instead of click handlers
-   Keep ViewModels testable

# 3. Project Structure

Recommended structure:

/Models /ViewModels /Views /Services /Repositories /Infrastructure
/Helpers /Resources

Separate UI, business logic, and infrastructure layers.

# 4. Dependency Injection

Use Microsoft.Extensions.DependencyInjection.

Best practices: - Constructor injection - Avoid Service Locator
pattern - Prefer interfaces - Register services in one place

# 5. Asynchronous Programming

Use async/await for:

-   File operations
-   Network operations
-   Database calls

Best practices: - Never block UI thread - Avoid .Result and .Wait() -
Use CancellationToken - Propagate async all the way

# 6. Large File Handling (10GB+)

-   Use streaming (FileStream)
-   Use buffered operations
-   Avoid loading entire file in memory
-   Implement progress reporting
-   Add resume capability
-   Validate with hashes (SHA256)

# 7. Error Handling & Logging

-   Centralized exception handling
-   User-friendly error messages
-   Technical details only in logs
-   Global unhandled exception handler

# 8. Configuration Management

-   Use appsettings.json
-   Use strongly-typed configuration classes
-   Separate dev/prod configs

# 9. Security

-   Validate all input
-   Use TLS
-   Protect sensitive data
-   Never hardcode secrets
-   Use secure file permissions

# 10. Testing

## Unit Testing

Use xUnit or NUnit with a mocking library (Moq, NSubstitute).

### What to Test

-   **ViewModels**: Commands, property changes, validation, error handling
-   **Services**: Business logic, calculations, validation rules
-   **Helpers/Utilities**: Pure functions, formatting, parsing

### Best Practices

-   **Arrange–Act–Assert**: Structure each test clearly
-   **One assertion focus**: Prefer one logical assertion per test; use multiple asserts only when testing one behavior
-   **Mock external dependencies**: Use interfaces (IFileService, ILogger) and mock in tests
-   **Test edge cases**: Null, empty, boundary values, cancellation
-   **Test INotifyPropertyChanged**: Verify properties raise change notifications when expected
-   **Test ICommand**: Verify CanExecute and Execute behavior, including when disabled
-   **Avoid testing the framework**: Don’t test WPF/UI binding mechanics; test ViewModel logic
-   **Meaningful names**: Use `MethodName_Scenario_ExpectedResult` or similar
-   **No logic in tests**: No conditionals or loops; tests should be obvious and stable

### Example Test Layout

```csharp
[Fact]
public void StartTransfer_WhenNotConnected_ReturnsFalse()
{
    var mockService = new Mock<IFileTransferService>();
    var vm = new TransferViewModel(mockService.Object);
    // Arrange, Act, Assert
}
```

### Recommended Libraries

-   xUnit or NUnit
-   Moq or NSubstitute
-   FluentAssertions (readable assertions)
-   AutoFixture (optional, for test data)

---

## Integration Testing

Test components together with real or near-real dependencies (e.g. file system, in-memory HTTP, test database).

### What to Test

-   **Service + repository**: File transfer with real streams or test files
-   **Network behavior**: Use HttpClient with mock handlers or test servers (e.g. WireMock, TestServer)
-   **File I/O**: Temporary directories, cleanup in teardown
-   **Cancellation**: CancellationToken propagation and cleanup
-   **Error paths**: Network failures, timeouts, disk full, permission errors

### Best Practices

-   **Isolated environment**: Use temp folders, in-memory or test DBs; never touch production
-   **Deterministic**: Avoid sleeps; use timeouts, events, or test doubles for timing
-   **Cleanup**: Dispose resources, delete temp files in finally or IDisposable
-   **Realistic data**: Use representative file sizes and content (e.g. 10GB scenario for large-file handling)
-   **Mark clearly**: Use `[Trait("Category", "Integration")]` and run separately from fast unit tests
-   **CI-friendly**: Integration tests should be runnable in CI; avoid UI or machine-specific assumptions where possible

### Example Scenarios

-   Transfer a file from disk to a test HTTP endpoint
-   Resume after simulated network failure
-   Cancel mid-transfer and verify cleanup
-   Handle invalid or corrupted file paths

### Recommended Libraries

-   xUnit/NUnit with collection fixtures for shared setup
-   Microsoft.AspNetCore.Mvc.Testing / TestServer for HTTP
-   System.IO.Abstractions for testable file I/O

---

## UI Testing

Automate real UI interaction for critical user flows (e.g. start transfer, cancel, open settings).

### When to Use

-   Smoke tests for main flows after deployment
-   Regression tests for hard-to-reach UI branches
-   Not a replacement for unit tests; use sparingly and keep suite small

### Best Practices

-   **Stable selectors**: Prefer AutomationId, accessible names, or stable IDs over raw coordinates or visual position
-   **Page/Object model**: Wrap screens and controls in test abstractions to reduce brittleness
-   **Short, focused tests**: One main user journey per test; avoid long scripts
-   **Isolated runs**: Start app in a known state (e.g. clean config or test profile); avoid depending on previous test runs
-   **Timeouts and retries**: Use explicit waits for controls to appear; avoid fixed sleeps
-   **Run in clean environment**: Dedicated test user or VM to avoid side effects from other apps

### Technologies (Windows desktop)

-   **FlaUI**: UIA-based automation for WPF/WinForms; good for in-process or local automation
-   **WinAppDriver**: Appium-compatible; useful for cross-platform or remote UI tests
-   **MAUI/UITest**: For MAUI, consider Microsoft.Maui.Automation or platform-specific UI test frameworks

### What to Automate

-   Login or main screen load
-   Starting and cancelling a transfer
-   Opening settings and changing one option
-   Handling an error dialog (e.g. “file not found”)

### What to Avoid

-   Testing every button; cover critical paths only
-   Relying on exact pixel positions or colors
-   Long, flaky sequences; prefer fewer, stable UI tests and more unit/integration coverage

# 11. Performance

-   Use virtualization in UI lists
-   Dispose resources
-   Avoid memory leaks
-   Profile regularly

# 12. Clean Code Principles

-   Follow SOLID
-   Avoid long methods
-   Avoid magic strings
-   Keep methods focused
-   Use meaningful naming

# 13. Deployment

-   Use MSIX (Windows)
-   Provide auto-update
-   Sign binaries
-   Test upgrade scenarios

# 14. Recommended Libraries

-   CommunityToolkit.Mvvm
-   Microsoft.Extensions.DependencyInjection
-   Microsoft.Extensions.Logging
-   Serilog
-   Polly
-   **Testing**: xUnit, Moq or NSubstitute, FluentAssertions, FlaUI (UI tests)

# Summary

A high-quality .NET desktop application:

-   Follows MVVM
-   Uses proper naming conventions
-   Has clean architecture
-   Is modular and testable
-   Uses async correctly
-   Handles large files efficiently
-   Is secure and maintainable
-   Has unit tests for ViewModels and services, integration tests for key flows, and selective UI tests for critical paths
