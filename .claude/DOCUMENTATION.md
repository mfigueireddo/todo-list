# AI Instructions - Code Documentation Model

When documenting functions, methods, or any callable unit of code, follow the structured model below. This ensures that every piece of logic is well-understood by both humans and AI assistants, reducing ambiguity and facilitating maintenance.

Not every item is mandatory for every function — use **only the items that are relevant**. For example, a simple getter with a self-explanatory name may only need **Descrição** and **Retornos Esperados**, while a complex business-logic method should include all seven items.

## Documentation Items

### 1. Objetivo
- **When to include**: When the function's name alone does not fully convey all of its goals or side effects.
- **Purpose**: Clarify the broader intent behind the function — what it aims to achieve beyond what the name suggests.
- **Guideline**: If the function name is already fully descriptive (e.g., `getPlayerHealth()`), this item can be omitted. Include it when the function has secondary goals, orchestrates multiple operations, or its name is intentionally generic.

**Example:**
```
=== <b>Objetivo</b> ===

<para>
Initializes the game session by loading saved data, resetting the score,
and preparing the rendering pipeline for the first frame.
</para>
```

### 2. Descrição
- **When to include**: Always — this is the core of the documentation.
- **Purpose**: Explain **how** the function works internally, step by step.
- **Guideline**: Describe the logical flow, the sequence of operations, and any important decisions or branches the function takes. This should read as a walkthrough of the function's body without being a line-by-line code translation.

**Example:**
```
=== <b>Descrição</b> ===

<para>
Reads the save file from disk using the FileManager utility.
</para>

<para>
Parses the JSON content into a PlayerData object.
</para>

<para>
If no save file exists, creates a default PlayerData with initial values.
</para>

<para>
Assigns the PlayerData to the active session and triggers a UI refresh.
</para>
```

### 3. Parâmetros
- **When to include**: When the function has parameters whose names do not fully explain their meaning, expected format, valid ranges, or role in the function's logic.
- **Purpose**: Remove all ambiguity about what should be passed in, including types, valid values, units, and edge cases.
- **Guideline**: If a parameter is named `player_name` and it clearly represents a player's name as a string, there is no need to document it. Focus on parameters that carry implicit expectations (e.g., a `speed` parameter that must be positive, or a `direction` parameter that only accepts specific values).

**Example:**
```
Parâmetros:
- tile_size: The size of each tile in pixels. Must be a positive integer and a power of 2
  (e.g., 16, 32, 64). Used to calculate the rendering grid dimensions.
- scale_factor: A multiplier applied to tile_size for high-DPI displays.
  A value of 1.0 means no scaling. Accepted range: 0.5 to 4.0.
```

### 4. Retornos Esperados
- **When to include**: When the function returns a value, especially if there are multiple return paths or the return value carries specific meaning.
- **Purpose**: Describe every possible return outcome and the conditions (streams) that lead to each one.
- **Guideline**: Document each distinct return path as a separate stream, clarifying what triggers it and what the caller should expect. For void methods, this item can be omitted or replaced with a note about side effects.

**Example:**
```
=== <b>Retornos</b> ===

<para>
Returns the loaded PlayerData object when the save file is successfully read and parsed.
</para>

<para>
Returns a new PlayerData with default values when no save file is found on disk.
</para>

<para>
Returns null when the save file exists but contains corrupted or unreadable data.
</para>
```

### 5. Assertivas de Entrada (Pré-condições)
- **When to include**: When the function depends on specific conditions being true **before** it is called — covering both parameter expectations and the overall system state.
- **Purpose**: Define the contract that callers must satisfy. If these conditions are not met, the function's behavior is undefined or unreliable.
- **Guideline**: Think about what the function takes for granted. Does it assume a file exists? That a connection is open? That a parameter is non-null? Document all such assumptions explicitly.

**Example:**
```
=== <b>Assertivas de Entrada<b> ===

<para>
- The GameEngine must be fully initialized (i.e., GameEngine.getInstance() returns a non-null value).
</para>

<para>
- The file_path parameter must point to an existing, readable file on disk.
</para>

<para>
- The player_id parameter must correspond to a player already registered in the active session.
</para>
```

### 6. Assertivas de Saída (Pós-condições)
- **When to include**: When the function modifies state, and the caller or the system relies on specific conditions being true **after** the function completes.
- **Purpose**: Define what the function guarantees upon successful completion — covering both output values and changes to system state.
- **Guideline**: Think about what has changed after this function runs. Has an object been mutated? Has a file been written? Has a flag been set? Document the guaranteed state of the world after execution.

**Example:**
```
=== <b>Assertivas de Saída</b> ===

<para>
The active session's PlayerData is non-null and fully populated.
</para>

<para>
The UI has been refreshed to reflect the loaded player data.
</para>

<para>
If a new default PlayerData was created, it has NOT been persisted to disk yet —
the caller is responsible for saving it if needed.
</para>
```

### 7. Restrições
- **When to include**: When there are business rules, technical constraints, or design decisions that dictate **how** the function must operate, beyond its pure logic.
- **Purpose**: Make explicit any rules that constrain the implementation — such as data storage choices, performance requirements, external dependencies, or deliberate limitations.
- **Guideline**: This is the place for "why it's done this way" notes. If the function writes to a `.txt` file instead of a database, or must avoid using a certain library, or has a specific threading requirement, document it here.

**Example:**
```
=== <b>Restrições</b> ===

<para>
Player data must be persisted in a local .txt file, not in a database,
due to the project's offline-first requirement.
<para>
  
<para> 
This function must not be called from a background thread, as it directly
updates Blazor component state, which must run on the renderer's synchronization
context (use `InvokeAsync` when triggered from another thread).
</para>

<para>
The save file format must remain backward-compatible with version 1.0 saves.
</para>
```

## Quick Reference Template

```csharp
/// <summary>
/// 
/// === <b>Objetivo</b> ===
/// 
/// <para>
/// [Broader intent, if the name is not self-explanatory]
/// </para>
///
/// === <b>Descrição</b> ===
/// 
/// <para>
/// [Step one]
/// </para>
/// 
/// <para>
/// [Step two]
/// </para>
/// 
/// <para>
/// [Step three]
/// </para>
/// 
/// </summary>
/// 
/// <param name="param_name">[Meaning, format, valid range, or special expectations]</param>
/// 
/// <remarks>
/// 
/// === <b>Assertivas de Entrada</b> ===
/// 
/// <para>
/// [Precondition about parameters or system state]
/// <para>
///
/// === <b>Assertivas de Saída</b> ===
/// 
/// <para>
/// [Postcondition about outputs or system state]
/// </para>
///
/// === <b>Restrições</b> ===
/// 
/// <para>
/// [Business rule or technical constraint]
/// </para>
/// 
/// === <b>Retornos</b> ===
/// 
/// <para>
/// Returns [value/type] when [condition].
/// <para>
/// 
/// <para>
/// Returns [value/type] when [condition].
/// </para>
/// 
/// </remarks>
```
