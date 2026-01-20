# Demo4 Research Notes

This folder houses the investigation and evidence behind the patterns we applied in Demo4. Each file links back to the catalog entry in `.docs/reference/patterns/` and contains links and code snippets that informed implementation decisions. Architecture visuals now live in `.docs/diagrams/` (see `architecture-c4-model-diagrams.md`), so reference diagrams from here when you need the visuals that support a research narrative.

| File                                | Focus                                                                          | Highlights                                                                                                                                                             |
| ----------------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `microsoft-identity-web.md`         | Microsoft.Identity.Web library configuration, token caching, and authentication patterns | Covers fluent authorization, distributed token cache requirements, claims serialization, and telemetry best practices that feed `guidance/implementation-patterns.md`. |
| `graph-integration.md`              | Microsoft Graph API integration with IDownstreamApi and Graph SDK              | Details OBO flow, scopes, error handling, and retry policies for secure Graph API calls.                                                                              |
| `hybrid-auth-identity.md`           | Entra ID authentication in Blazor Web Apps and hybrid identity scenarios       | Explores BFF pattern with YARP, claims mapping, IClaimsTransformation, and account linking strategies.                                                                |
| `security-and-metrics.md`           | .NET 10 authentication/authorization features and security best practices     | Documents fluent authorization builder, authentication metrics, production security requirements, and deprecated patterns.                                           |
| `AUTO_PROVISIONING_RESEARCH.md`     | Deep dive on provisioning Entra users                                          | Discusses ClaimsTransformation trade-offs, `oid` vs `sub` mismatches, transaction safety, and the recommended refactor to OIDC events.                                 |

**How to add new research**:
1. Copy the relevant pattern or topic into a new markdown file (prefix with the date if needed).
2. Document the evidence (links, quotes, config snippets) and cite the catalog entry.
3. Update this index with a short summary so reviewers know where the evidence lives.
4. Link the research file from `guidance/implementation-patterns.md` or the demo README when the pattern is mentioned.
