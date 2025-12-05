# Vertical Slice Architecture: Where Does the Shared Logic Live?

This explanation explores the conceptual challenges of managing shared logic in Vertical Slice Architecture (VSA), a modern approach that organizes code by features rather than technical layers. Unlike traditional architectures like Clean Architecture, which provide strict guidelines for code placement, VSA offers flexibility but requires careful discipline to avoid reintroducing unwanted coupling.

## The Shift from Guardrails to Flexibility

Traditional layered architectures, such as Clean Architecture, act as guardrails: entities in the domain layer, interfaces in application, implementations in infrastructure. This structure prevents mistakes but can hinder rapid development when shortcuts are appropriate.

Vertical Slice Architecture removes these guardrails, emphasizing organization by feature over technical concern. This promotes speed and adaptability but places the responsibility for good design on the developer. The key question becomes: when logic is shared across features, where should it reside without compromising the architecture's benefits?

## Avoiding the "Common" Junk Drawer

A common pitfall is creating a centralized "Shared," "Common," or "Utils" project or folder. This often becomes a dumping ground for unrelated code, coupling features that should remain independent. For example, an `OrderCalculationService` mixing cart totals, revenue reporting, and invoice formatting creates dependencies between features with different change frequencies.

Such a structure reintroduces the coupling VSA aims to eliminate, turning the architecture into a tangled web.

## Decision Framework for Sharing

When encountering potential shared logic, evaluate it with three questions:

1. **Is this infrastructural or domain?** Infrastructure code (e.g., logging, database connections) is almost always shared. Domain logic requires more scrutiny.

2. **How stable is this concept?** Stable concepts that change infrequently can be shared; volatile ones that evolve with each feature should stay local.

3. **Am I past the "Rule of Three"?** Duplicate code once or twice is acceptable. Only abstract when you have three genuine, identical usages.

## Three Tiers of Sharing

Instead of a binary choice between shared and not shared, consider three tiers based on the logic's nature:

### Tier 1: Technical Infrastructure (Share Freely)

Centralize pure plumbing that affects all features equally: logging adapters, database factories, authentication middleware, result patterns, and validation pipelines. This rarely changes due to business needs and can live in a `Shared.Kernel` or `Infrastructure` project.

Example: A `Result` type for functional error handling.

### Tier 2: Domain Concepts (Share and Push Logic Down)

Business rules belong in domain entities and value objects. This prevents scattering logic across slices and ensures consistency.

Example: An `Order` entity with `CanBeCancelled()` and `Cancel()` methods, used by multiple order-related slices.

Different slices can share the same domain model, promoting reuse without coupling.

### Tier 3: Feature-Specific Logic (Keep It Local)

For logic shared only within a feature family, create a `Shared` subfolder inside the feature directory. This keeps related code together and ensures it disappears if the feature is removed.

Example: `Features/Orders/Shared/OrderValidator.cs` and `OrderPricingService.cs`, used by `CreateOrder`, `UpdateOrder`, etc.

## Cross-Feature Sharing

When logic spans unrelated features, first verify if sharing is necessary. Often, it's disguised data access: each slice queries the database directly without calling other features.

For genuine cross-feature logic:

- **Domain logic**: Place in `Domain/Services`, e.g., a `TaxCalculator` used by orders and invoices.
- **Infrastructure**: Place in `Infrastructure/Services`, e.g., external API clients or formatting utilities.

If side effects are needed across features, use messaging and events or expose a facade via a public API.

## When Duplication Is Preferable

Not all similar code should be shared. Duplication can be cheaper than incorrect abstraction. For instance, identical response DTOs for `GetOrder` and `CreateOrder` might diverge later (e.g., adding a tracking URL to one), making a shared DTO problematic.

## Practical Project Structure

A mature VSA project might look like:

```text
src/
├── Features/
│   ├── Orders/
│   │   ├── CreateOrder/
│   │   ├── UpdateOrder/
│   │   └── Shared/          # Feature-local sharing
│   ├── Customers/
│   └── Invoices/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Services/            # Cross-feature domain logic
├── Infrastructure/
│   ├── Persistence/
│   └── Services/
└── Shared/
    └── Behaviors/           # Cross-cutting concerns
```

Features own their request/response models. Domain houses shared business logic. Infrastructure handles technical concerns.

## Key Rules

1. Features own their models; no shared DTOs across features.
2. Push business logic into domain entities and value objects.
3. Keep feature-family sharing local within the feature's `Shared` folder.
4. Share infrastructure by default.
5. Apply the Rule of Three before extracting shared code.

## Why This Matters

VSA asks, "What feature does this belong to?" Shared logic challenges this when it belongs to multiple features. The approach acknowledges that some concepts span features but assigns them homes based on nature—domain, infrastructure, or behavior—while resisting over-sharing.

The goal isn't eliminating duplication but enabling easy changes as requirements evolve, which they always do.

For related learning, see tutorials on getting started with Vertical Slice Architecture. For practical guides, refer to how-to articles on implementing VSA in .NET projects.

## References

https://www.milanjovanovic.tech/blog/vertical-slice-architecture-where-does-the-shared-logic-live