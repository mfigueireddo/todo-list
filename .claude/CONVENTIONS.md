# AI Instructions - Personal Coding Habits

## Naming Conventions

### Method Names
- Use PascalCase for all method names
- Examples: `GetGamePanel()`, `UpdatePlayer()`, `RenderGraphics()`

### Variable and Field Names
- Use camelCase for local variables and method parameters
  - Examples: `gamePanel`, `playerPosition`, `enemyCount`
- Use `_camelCase` (leading underscore) for private and internal fields
  - Examples: `_gamePanel`, `_playerPosition`, `_enemyCount`
- Use PascalCase for public fields and properties
  - Examples: `GamePanel`, `PlayerPosition`, `EnemyCount`

### Constants
- Use PascalCase for all constants (`const` and `static readonly` fields)
- Examples: `MaxHealth`, `DefaultSpeed`, `MinWidth`

### Descriptive Names
- Avoid abbreviations - use full, descriptive names
- Examples:
  - ✅ `enemyHealth`, `maximumVelocity`
  - ❌ `enHlth`, `maxVel`
- Avoid single-letter names, even in loops
- Examples:
  - ✅ `for (int row = 0; row < height; row++)`
  - ✅ `for (int index = 0; index < list.size(); index++)`
  - ✅ `for (int column = 0; column < width; column++)`
  - ❌ `for (int i = 0; i < n; i++)`

## Singleton Pattern

- When creating a singleton class, place the `Instance` accessor first in the class
- Order: static instance field, `Instance` property, then constructor and other methods

## Variable Initialization

- Initialize member variables at declaration, not in the constructor
- Example: `private readonly Button _newGame = new("New Game");`
- Keep constructors focused on setup and configuration logic

## Magic Numbers

- Never use magic numbers directly in code
- Always create final variables with descriptive names for all numeric values
- This includes: dimensions, sizes, margins, padding, thickness, colors, etc.
- Examples:
  - `const int ButtonsWidth = 200;`
  - `const int ButtonsHeight = 40;`
  - `const int ButtonsBorderThickness = 10;`
  - `const int ButtonsMargin = 10;`
- Group related constants together in the code for clarity

## General Programming Principles

### Memory Management and Performance

#### Recursion
- **Never use recursion**
- Prevents being stuck in infinite recursion and wasting memory

#### Heap Usage
- **Avoid heap usage when possible**
- Reduces the risk of memory leaks

#### Pointer Ownership (C++)
- **Use `std::unique_ptr` and similar smart pointers to move objects**
- Explicitly indicates ownership transfer
- Example: `std::unique_ptr<Player> player_ptr = std::make_unique<Player>();`

### Loop Safety

#### While Loop Iteration Limits
- **Always limit the number of iterations in while loops**
- This prevents the program from being stuck in an infinite loop if something goes wrong
- **Always create code to handle when this limitation is triggered**
- Example:
  ```cpp
  int max_iterations = 1000;
  int iteration_count = 0;
  while (condition && iteration_count < max_iterations) {
      // loop body
      iteration_count++;
  }
  if (iteration_count >= max_iterations) {
      // Handle loop limit reached
      logError("While loop exceeded maximum iterations");
  }
  ```

### Variable and Scope Management

#### Variable Scope
- **Limit variable access to the smallest scope possible**
- Makes the code easier to maintain
- Declare variables as close as possible to where they are used

#### Lambda Function Captures
- **Never use references in lambda function captures in any language**
- Prevents code from failing if a variable is removed, has its position switched, or can't be accessed when the lambda function is called
- Prefer capturing by value or using smart pointers
- Example:
  - ✅ `[](Health player_health) { return player_health > 0; }`
  - ❌ `[&]() { return player_health > 0; }`

#### Type Annotations (Dynamic Languages)
- **Always declare variable types in dynamically-typed languages**
- Even when the language does not require it, explicit types improve readability, catch bugs early, and serve as documentation
- Examples:
  - Python: use type hints (`x: int = 5`, `def process(name: str) -> bool:`)
  - JavaScript/TypeScript: use TypeScript annotations or JSDoc types
  - ✅ `player_health: int = 100`
  - ✅ `def getScore(player_id: int) -> float:`
  - ❌ `player_health = 100`
  - ❌ `def getScore(player_id):`

#### Type Inference (C++)
- **Use `auto` to make code cleaner and more readable**
- **Exception 1**: Never use `auto` for numeric types (int, float, double)
  - It's critically important to be explicit with numeric types to avoid rounding mistakes
- **Exception 2**: Never use `auto` when a function can return more than one data type
- Examples:
  - ✅ `auto game_panel = GamePanel::getInstance();`
  - ✅ `int player_score = 100;`
  - ❌ `auto player_score = 100;`
  - ❌ `auto payment = PaymentFactory::createPayment()`

### Code Quality and Debugging

#### Debug Logging
- **Never commit code with debug logs**
- This includes functions like: `print()`, `printf()`, `console.log()`, `Console.WriteLine()`, `System.out.println()`, etc.
- **Exception**: This rule can be ignored if the project explicitly uses the terminal/console for communication with the user

#### Function Return Values
- **Always check the return value of a function call**
- If the function returns void, make this explicit in code
- Example in C: `(void)some_function();`

### Compilation and Build Standards

#### Compiler Warnings
- **Always compile code with warnings activated**
- **Prefer compilation settings that treat warnings as errors**
- Examples:
  - C/C++: Use `-Wpedantic` and `-Werror`
  - C#/.NET: Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, enable `<Nullable>enable</Nullable>`, and raise the analysis level (`<AnalysisLevel>latest-Recommended</AnalysisLevel>`) in the `.csproj`
- This ensures code quality and catches potential issues early

#### Code Formatting
- **Guarantee that indentation is uniform across all code**
- **Configure the IDE's TAB behavior to use 4 spaces**
- Maintain consistency across all files in the project

#### Line Wrapping (Markdown files and code comments)
- **Never hard-wrap a line at an arbitrary column width.** Let the editor soft-wrap long text.
- This applies to **both Markdown files** (`.md`) **and code comments** (C# `//` and `///` XML doc comments).
- **Only start a new line in one of these three situations:**
  - A sentence just ended (a `.`).
  - You are starting a new bullet point (`-`).
  - You are starting a new enumerated item (`1.`, `2.`, etc.).
- **Never split a single sentence across multiple lines.** A colon (`:`), a dash (`—`), or simply running out of "width" are **not** reasons to break.
- ❌ Wrong — the sentence is broken across lines for no reason:
  ```
  `DbContext` do EF Core que representa a sessão com o SQL Server. **Deliberadamente vazio** (sem
  `DbSet`) nesta etapa: serve para configurar/validar a conectividade e como base para as entidades e
  o ASP.NET Core Identity que virão depois.
  - **Usage**: Injetado por requisição (scoped) nos controllers que acessam o banco — hoje, o
    `DatabaseHealthController`.
  ```
- ✅ Correct — break only after a sentence-ending `.` and before a bullet; each sentence stays on one line:
  ```
  `DbContext` do EF Core que representa a sessão com o SQL Server.
  **Deliberadamente vazio** (sem `DbSet`) nesta etapa: serve para configurar/validar a conectividade e como base para as entidades e o ASP.NET Core Identity que virão depois.
  - **Usage**: Injetado por requisição (scoped) nos controllers que acessam o banco — hoje, o `DatabaseHealthController`.
  ```

#### Multi-line Expression Indentation (C#)
When a C# expression must span multiple lines, follow these rules:
- When you break the line **right after an opening parenthesis** `(`, put the matching **closing `)` on its own line**, indented to align with **the line that opened the call** (the statement/operator holding the opening function), **not** with the column of the `(`.
- Put the statement terminator **`;` on its own line**, aligned with the **first token of the statement** (e.g., the `var` of the declaration).
- In a **fluent method chain**, align every chained `.Method()` call with the **first** method call in the chain (align at the `.`).
- Canonical example — wrapped call with `??`/`throw`, hanging `)` aligned with the opener and hanging `;` aligned with `var`:
  ```csharp
  var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionName)
      ?? throw new InvalidOperationException(
          $"A connection string '{DatabaseConnectionName}' não foi configurada. " +
          "Defina-a em appsettings.json (ConnectionStrings) ou via user-secrets/variáveis de ambiente."
      )
  ;
  ```
- Canonical example — fluent chain inside a lambda; chained methods aligned with the first method (`.WithOrigins`):
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddPolicy(WebClientCorsPolicy, policy =>
          policy.WithOrigins(Routes.Web.HttpsBaseUrl, Routes.Web.HttpBaseUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
          )
      ;
  });
  ```

#### Macro Usage
- **Avoid using macros as conditions or flags**
- Macro flags exponentially increase the test cases necessary to cover the whole system's behavior
- Prefer const variables, enums, or configuration classes instead

#### Compile-Time Constants
- **Use language resources like `inline constexpr` when creating variables that have their value known at the beginning of the program and won't be modified**
- This is a great substitution for MACROS
- Provides type safety and better debugging compared to preprocessor macros
- Examples:
  - C++: `inline constexpr int file_path = "src/images/tree.png";`
  - C++: `inline constexpr double PI = 3.14159265359;`
- Benefits: type checking, scoping rules, and debugger visibility

### Object-Oriented Design

#### Parent Classes
- **When creating a parent class, make it abstract**
- This makes the code's behavior more explicit
- Forces intentional design decisions about which classes should be instantiated

#### Member Access
- **Always use `this.` when accessing any class member (fields, methods, properties)**
- Applies to all member accesses within a class, without exception
- Examples:
  - ✅ `this.PlayerHealth = 100;`
  - ✅ `this.UpdatePlayer();`
  - ❌ `PlayerHealth = 100;`
  - ❌ `UpdatePlayer();`
