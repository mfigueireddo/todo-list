# AI Instructions - Code Documentation Model

When documenting functions, methods, or any callable unit of code, follow the structured model below. This ensures that every piece of logic is well-understood by both humans and AI assistants, reducing ambiguity and facilitating maintenance.

Not every item is mandatory for every function — use **only the items that are relevant**. For example, a simple getter with a self-explanatory name may only need **Description** and **Expected Returns**, while a complex business-logic method should include all seven items.

## Documentation Items

### 1. Objective
- **When to include**: When the function's name alone does not fully convey all of its goals or side effects.
- **Purpose**: Clarify the broader intent behind the function — what it aims to achieve beyond what the name suggests.
- **Guideline**: If the function name is already fully descriptive (e.g., `getPlayerHealth()`), this item can be omitted. Include it when the function has secondary goals, orchestrates multiple operations, or its name is intentionally generic.

**Example:**
```
Objective: Initializes the game session by loading saved data, resetting the score,
and preparing the rendering pipeline for the first frame.
```

### 2. Description
- **When to include**: Always — this is the core of the documentation.
- **Purpose**: Explain **how** the function works internally, step by step.
- **Guideline**: Describe the logical flow, the sequence of operations, and any important decisions or branches the function takes. This should read as a walkthrough of the function's body without being a line-by-line code translation.

**Example:**
```
Description:
1. Reads the save file from disk using the FileManager utility.
2. Parses the JSON content into a PlayerData object.
3. If no save file exists, creates a default PlayerData with initial values.
4. Assigns the PlayerData to the active session and triggers a UI refresh.
```

### 3. Parameters
- **When to include**: When the function has parameters whose names do not fully explain their meaning, expected format, valid ranges, or role in the function's logic.
- **Purpose**: Remove all ambiguity about what should be passed in, including types, valid values, units, and edge cases.
- **Guideline**: If a parameter is named `player_name` and it clearly represents a player's name as a string, there is no need to document it. Focus on parameters that carry implicit expectations (e.g., a `speed` parameter that must be positive, or a `direction` parameter that only accepts specific values).

**Example:**
```
Parameters:
- tile_size: The size of each tile in pixels. Must be a positive integer and a power of 2
  (e.g., 16, 32, 64). Used to calculate the rendering grid dimensions.
- scale_factor: A multiplier applied to tile_size for high-DPI displays.
  A value of 1.0 means no scaling. Accepted range: 0.5 to 4.0.
```

### 4. Expected Returns
- **When to include**: When the function returns a value, especially if there are multiple return paths or the return value carries specific meaning.
- **Purpose**: Describe every possible return outcome and the conditions (streams) that lead to each one.
- **Guideline**: Document each distinct return path as a separate stream, clarifying what triggers it and what the caller should expect. For void methods, this item can be omitted or replaced with a note about side effects.

**Example:**
```
Expected Returns:
- Returns the loaded PlayerData object when the save file is successfully read and parsed.
- Returns a new PlayerData with default values when no save file is found on disk.
- Returns null when the save file exists but contains corrupted or unreadable data.
```

### 5. Assertives of Entrance (Preconditions)
- **When to include**: When the function depends on specific conditions being true **before** it is called — covering both parameter expectations and the overall system state.
- **Purpose**: Define the contract that callers must satisfy. If these conditions are not met, the function's behavior is undefined or unreliable.
- **Guideline**: Think about what the function takes for granted. Does it assume a file exists? That a connection is open? That a parameter is non-null? Document all such assumptions explicitly.

**Example:**
```
Assertives of Entrance:
- The GameEngine must be fully initialized (i.e., GameEngine.getInstance() returns a non-null value).
- The file_path parameter must point to an existing, readable file on disk.
- The player_id parameter must correspond to a player already registered in the active session.
```

### 6. Assertives of Departure (Postconditions)
- **When to include**: When the function modifies state, and the caller or the system relies on specific conditions being true **after** the function completes.
- **Purpose**: Define what the function guarantees upon successful completion — covering both output values and changes to system state.
- **Guideline**: Think about what has changed after this function runs. Has an object been mutated? Has a file been written? Has a flag been set? Document the guaranteed state of the world after execution.

**Example:**
```
Assertives of Departure:
- The active session's PlayerData is non-null and fully populated.
- The UI has been refreshed to reflect the loaded player data.
- If a new default PlayerData was created, it has NOT been persisted to disk yet —
  the caller is responsible for saving it if needed.
```

### 7. Restrictions
- **When to include**: When there are business rules, technical constraints, or design decisions that dictate **how** the function must operate, beyond its pure logic.
- **Purpose**: Make explicit any rules that constrain the implementation — such as data storage choices, performance requirements, external dependencies, or deliberate limitations.
- **Guideline**: This is the place for "why it's done this way" notes. If the function writes to a `.txt` file instead of a database, or must avoid using a certain library, or has a specific threading requirement, document it here.

**Example:**
```
Restrictions:
- Player data must be persisted in a local .txt file, not in a database,
  due to the project's offline-first requirement.
- This function must not be called from a background thread, as it directly
  updates Blazor component state, which must run on the renderer's synchronization
  context (use `InvokeAsync` when triggered from another thread).
- The save file format must remain backward-compatible with version 1.0 saves.
```

### 8. Attributes
- **When to include**: **Always, whenever a type or member is decorated with one or more attributes** (e.g., `[ApiController]`, `[Route]`, `[HttpGet]`, `[Authorize]`, `[FromBody]`, or any custom attribute). This item is mandatory in this repository for every attribute used.
- **Purpose**: Make explicit what each attribute *implies* about the class/method, because attributes are not interpreted by the C# compiler — they are metadata read at runtime by a framework/SDK (ASP.NET Core, the serializer, etc.). Without this note, the behavior an attribute introduces is invisible from the code alone.
- **Guideline**: List each attribute applied to the unit being documented and, for each, describe (a) **what it does** — the concrete behavior or contract it activates — and (b) **who interprets it** when relevant (the framework, the serializer, your own code). If an attribute takes arguments or special tokens (e.g., the `[controller]` token in a route template), explain how they are resolved. Document the attributes on the member where they appear: type-level attributes in the type's doc comment, member-level attributes in the member's doc comment.

**Example:**
```
Attributes:
- [ApiController]: marks the class as a REST API controller and enables ASP.NET Core
  conventions (automatic model validation returning 400, parameter source inference,
  ProblemDetails error responses). Read by the framework at runtime, not by the C# compiler.
- [Route("[controller]")]: defines this controller's URL template. The [controller] token is
  replaced by the routing layer with the class name minus the "Controller" suffix.
- [HttpGet]: maps the action to HTTP GET requests; with no argument it inherits the controller's
  route. It is this attribute — not the method name — that drives routing.
```

## Quick Reference Template

```csharp
/// <summary>
/// Objective: [Broader intent, if the name is not self-explanatory]
///
/// Description:
/// 1. [Step one]
/// 2. [Step two]
/// 3. [Step three]
/// </summary>
/// <param name="param_name">[Meaning, format, valid range, or special expectations]</param>
/// <returns>
/// - Returns [value/type] when [condition].
/// - Returns [value/type] when [condition].
/// </returns>
/// <remarks>
/// Attributes:
/// - [AttributeName]: [what it does and who interprets it] — required for every attribute used.
///
/// Assertives of Entrance:
/// - [Precondition about parameters or system state]
///
/// Assertives of Departure:
/// - [Postcondition about outputs or system state]
///
/// Restrictions:
/// - [Business rule or technical constraint]
/// </remarks>
```

## When to Apply Full vs. Partial Documentation

| Function Complexity | Recommended Items |
|---|---|
| Simple getter/setter | Description only (or omit entirely if trivial) |
| Utility/helper method | Description + Parameters + Expected Returns |
| Business logic method | All 8 items |
| Public API method | All 8 items |
| Private internal method | Description + Assertives (entrance/departure) as needed |
| Constructor | Description + Parameters + Assertives of Departure |

> **Note:** The **Attributes** item is independent of complexity — whenever a type or member is decorated with attributes, document them, even on an otherwise trivial unit.
