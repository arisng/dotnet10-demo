---
date: 2025-12-15
type: Bug
severity: High
status: Investigating
author: GitHub Copilot
tags:
  - demo5
  - downstream-api
  - di-registration
  - claims-flow
  - documentation-gap
---

# Demo5 Implementation Gaps: Missing DI Registration & Claims Flow

## Problem

Demo5's README.md contains multiple documentation claims that do not align with the actual implementation, creating significant confusion for developers following the workshop. These gaps undermine the educational value of the demo and may lead to incorrect assumptions about .NET authentication patterns.

## Root Cause

The README was written based on intended implementation rather than actual code, resulting in:
- Overstated feature claims (e.g., dual downstream APIs when only one is implemented)
- Missing code examples that don't exist in the codebase
- Inconsistent guidance on architectural decisions (e.g., CORS requirements)
- Incomplete claims flow demonstration

## Solution

### 1. IDownstreamApi DI Registration Gap

**Current State:** Despite README claiming "Service Registration: `AddDownstreamApi("WeatherApi", config)` in Program.cs", the code actually works without explicit registration due to framework defaults.

**Impact:** Developers may assume manual registration is required, leading to unnecessary configuration attempts.

**Fix:** Update README to clarify that registration happens automatically or provide accurate code snippets.

### 2. MicrosoftGraph Integration Omission

**Current State:** README Glossary states demo5 demonstrates calling TWO downstream APIs (MicrosoftGraph + WeatherApi), but only WeatherApi is implemented in the UI components.

**Impact:** Misleads developers about the demo's scope and multi-API capabilities.

**Fix:** Either implement MicrosoftGraph calls in the demo or update documentation to reflect single API focus.

### 3. Claims Flow Documentation Inconsistency

**Current State:** README promises "Permission claims added in demo4 are attached to the user's token when sent to the downstream API" and "Downstream API can inspect `roles` claim to validate the user's identity", but WeatherApi only validates `Forecast.Read` scope, not role-based claims.

**Impact:** Creates false expectation that RBAC claims propagate through OBO flow to downstream APIs.

**Fix:** Either implement claims validation in WeatherApi or clarify that scope-based auth is separate from RBAC.

### 4. Missing DI Registration Code Examples

**Current State:** "What's New > Client Implementation" documents best practices for `AddDownstreamApi()` and `EnableTokenAcquisitionToCallDownstreamApi()` that aren't explicitly shown in the code.

**Impact:** Developers can't find the promised examples to learn from.

**Fix:** Add code comments or dedicated sections showing the registration patterns.

### 5. CORS Documentation Contradiction

**Current State:** Section states "CORS is intentionally NOT configured to enforce server-to-server calls" but architecture table shows "Downstream pattern requires 'CORS: Required'".

**Impact:** Confusing guidance on when CORS is needed for downstream APIs.

**Fix:** Clarify that CORS is not required for server-to-server calls, update table accordingly.

### 6. [RequiredScope] Attribute Not Used

**Current State:** Documentation mentions scope validation but WeatherApi uses manual implementation instead of `[RequiredScope]` attribute pattern.

**Impact:** Misses opportunity to demonstrate declarative authorization patterns.

**Fix:** Update WeatherApi to use `[RequiredScope("Forecast.Read")]` for consistency with Microsoft.Identity.Web best practices.

### 7. Downstream API Project Naming Inconsistency

**Current State:** README refers to the project with inconsistent terminology:
- Referred to as "Demo5 Protected API" in Entra ID Configuration section
- Configuration key shown as `"WeatherApi"` in Step 4
- Glossary mentions "Demo5.DownstreamApi.WeatherApi"
- Glossary section shows registration of TWO APIs (MicrosoftGraph + WeatherApi) but nomenclature is unclear

**Impact:** Developers may be confused about whether the API is called "Protected API" or "WeatherApi" or what the actual project name is.

**Fix:** Standardize naming across all documentation sections to use consistent terminology (recommend: "WeatherApi").

## Implementation Status Summary

| Feature                                       | README Claims               | Actual Code                  | Gap Severity |
| --------------------------------------------- | --------------------------- | ---------------------------- | ------------ |
| WeatherApi (protected API)                    | ✅ Fully implemented         | ✅ Exists and functional      | ✅ **No gap** |
| IDownstreamApi injection                      | ✅ Works as documented       | ✅ Functional endpoint        | ✅ **No gap** |
| `/api/downstream-weather` endpoint            | ✅ Returns weather data      | ✅ Implemented                | ✅ **No gap** |
| ApiArchitectureComparison.razor               | ✅ Side-by-side UI           | ✅ Component exists           | ✅ **No gap** |
| DownstreamWeatherFetcher.razor                | ✅ Calls downstream API      | ✅ Component exists           | ✅ **No gap** |
| MicrosoftGraph downstream API                 | ✅ Claims dual APIs          | ❌ Not implemented            | 🔴 **HIGH**   |
| `AddDownstreamApi()` registration             | ✅ Claims in Program.cs      | ❌ Code doesn't show it       | 🟡 **MEDIUM** |
| `EnableTokenAcquisitionToCallDownstreamApi()` | ✅ Claims in Program.cs      | ❌ Code doesn't show it       | 🟡 **MEDIUM** |
| Claims flow to downstream API                 | ✅ Documents role validation | ⚠️ Only scope validation      | 🟡 **MEDIUM** |
| [RequiredScope] attribute usage               | ✅ Mentioned as pattern      | ❌ Manual implementation used | 🟡 **MEDIUM** |
| Consistent API naming                         | ✅ Claims consistency        | ❌ Mixed terminology          | 🟡 **MEDIUM** |

## Lessons Learned

- Documentation should be validated against actual implementation before publishing
- Workshop demos should prioritize clarity and accuracy over ambitious feature claims
- Inconsistencies between docs and code erode trust in educational materials

## Prevention

- [ ] Implement automated checks to validate README claims against codebase
- [ ] Require code review of documentation alongside implementation changes
- [ ] Add "Implementation Status" sections to READMEs indicating what's actually working vs. planned