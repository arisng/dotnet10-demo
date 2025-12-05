# How to Optimize Queries with EF Core Projections

This guide shows you how to use EF Core projections to improve the performance of your data access layer by fetching only the data you need.

## Prerequisites

- An existing EF Core `DbContext`.
- A defined Entity model (e.g., `Movie`, `Director`).

## Steps

### 1. Define a Read Model (DTO)

Create a simple class (Data Transfer Object) that represents exactly the data you want to display or return. This class should not be an Entity (no `[Key]` or database attributes usually needed).

```csharp
public class MovieSummaryDto
{
    public string Title { get; set; }
    public int ReleaseYear { get; set; }
    public string DirectorName { get; set; }
}
```

### 2. Write the Projected Query

Use the `.Select()` LINQ method to map your entity to the DTO.

```csharp
public async Task<List<MovieSummaryDto>> GetMovieSummariesAsync()
{
    using var context = new AppDbContext();

    var summaries = await context.Movies
        .AsNoTracking() // Optional but recommended for read-only queries
        .Select(m => new MovieSummaryDto
        {
            Title = m.Title,
            ReleaseYear = m.ReleaseYear,
            // Flattening the relationship directly in the query
            DirectorName = m.Director.Name 
        })
        .ToListAsync();

    return summaries;
}
```

### 3. Using Anonymous Types (Optional)

For internal logic where you don't need a reusable class, you can project to an anonymous type.

```csharp
var simpleList = context.Movies
    .Select(m => new { m.Title, m.ReleaseYear })
    .ToList();
```

## Key Considerations

- **Null Checks**: EF Core handles null checks in SQL generation for you (e.g., if `Director` is null, `Director.Name` will be null in the result without throwing a NullReferenceException during SQL execution).
- **IQueryable vs IEnumerable**: Ensure `.Select()` is called on the `IQueryable` (before `ToList` or `await`), otherwise the projection happens in memory after fetching all data.
- **Unsupported Methods**: Some .NET methods cannot be translated to SQL. If you get a runtime error, ensure your projection logic is simple enough for the database provider to understand.

## Related Documentation

- [About EF Core Projections](../../explanation/ef-core-projections.md)
