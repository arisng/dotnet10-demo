# Monthly Changelog: November 2025

> **Coverage:** Weeks 45-48 (November 03-30)
> **Generated:** December 05, 2025

## Executive Summary

This month saw significant progress in the .NET 10 demo series, with the completion of multiple demo projects and enhancements to authentication diagnostics and configuration. Key highlights include finishing foundational identity demos, advancing Blazor InteractiveAuto features, and integrating Microsoft Entra ID capabilities.

---

## Details by Area

### 🆔 Identity Foundation

**New Features:**

- Completed the foundational identity demo project

### 📱 Dual Mode Handoff

**New Features:**

- Added interactive timeline and delay controls to authentication state probe for enhanced diagnostics and educational visibility
- Enhanced demo README with detailed goals, diagnostics, and passkey implementation notes
- Improved application configuration with response compression and static web assets support for non-development environments

### 🔐 BFF RBAC

**New Features:**

- Applied new .NET 10 Blazor feature to resolve UI flicker using PersistentState attribute on user properties
- Migrated solution file to the new .slnx format
- Completed the BFF RBAC demo project
- Added comprehensive README covering BFF APIs and permission-based role access control

**Improvements:**

- Removed authorization attribute from AuthStateProbe page to better demonstrate authentication state refresh behavior
- Refactored client components to use CascadingParameter for improved reactivity and state management

### ☁️ Entra ID Integration

**New Features:**

- Initialized the Entra ID integration demo project
- Completed demo project with full Microsoft Entra ID integration capabilities

### 🔗 Downstream API

**New Features:**

- Initialized the downstream API demo project

**Improvements:**

- Removed CORS configuration from internal API to enforce BFF pattern and improve security for server-to-server communications

---

*This summary covers completed weeks only. Additional updates may be added as the month progresses.*

