# About EF Core Projections

EF Core projections are a technique for mapping database query results directly to a specific structure, rather than loading full entity instances. This document explains the conceptual benefits and trade-offs of using projections versus fetching full entities.

## What is a Projection?

In the context of Entity Framework Core, a **projection** is the process of selecting specific columns and data shapes from the database and mapping them to a destination type (usually a DTO or anonymous type) within the query itself.

Instead of executing `SELECT *` and hydrating a full tracked entity, a projection executes `SELECT Col1, Col2` and instantiates a simple object.

## The Cost of Non-Projected Queries

When you query entities directly (e.g., `context.Movies.ToList()`), several overheads occur:

1. **Over-fetching Data**: All columns are retrieved, even if you only need a few. For tables with many columns or large text/blob fields, this wastes significant I/O and memory.
2. **Change Tracking**: By default, EF Core tracks changes for all entities returned. This consumes CPU and memory to maintain the state manager, which is unnecessary for read-only operations.
3. **Serialization Overhead**: Passing full entities to an API response often requires complex serialization logic to handle circular references or hide sensitive fields.
4. **N+1 Risks**: Without careful `Include()` usage, accessing related data on full entities can trigger multiple database roundtrips (lazy loading).

## Benefits of Projections

Projections address these issues by shifting the work to the database engine:

### 1. Optimized SQL Generation

Projections generate SQL that selects only the requested columns.

```sql
-- Full Entity Fetch
SELECT "Id", "Title", "Description", "CreatedBy", "CreatedDate", ... FROM "Movies"

-- Projection
SELECT "Title", "ReleaseYear" FROM "Movies"
```

### 2. Reduced Memory Footprint

Because the Change Tracker is bypassed (projections are not tracked entities) and fewer data bytes are held in memory, the application's memory usage is significantly lower, especially for large lists.

### 3. Flattening Relationships

Projections can flatten related data in a single query, avoiding the complexity of joining full object graphs in memory.

**Example Concept:**
Instead of loading a `Movie` and its `Director` entity, a projection can simply return a `MovieSummary` with a `DirectorName` property.

## When to Use Projections

| Use Case                 | Approach        | Reason                                           |
| ------------------------ | --------------- | ------------------------------------------------ |
| **Read-only Lists**      | **Projection**  | Maximize performance, minimize memory.           |
| **API Responses**        | **Projection**  | Return exactly the shape the client needs (DTO). |
| **Updating Data**        | **Full Entity** | Need Change Tracking to persist modifications.   |
| **Complex Domain Logic** | **Full Entity** | Need the rich behavior of the domain model.      |

## Related Documentation

- [How to Optimize Queries with EF Core Projections](../how-to/data-access/optimize-queries-with-projections.md)
