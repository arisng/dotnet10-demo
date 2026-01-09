# Demo 5.1: Distributed Modular Monolith with Aspire

## Overview

This demo evolves the **Downstream API** pattern from Demo 5 into a full **Distributed Modular Monolith** architecture using .NET Aspire and YARP.

## Architecture

| Component    | Responsibility             | Project              |
| ------------ | -------------------------- | -------------------- |
| **AppHost**  | Orchestration (Aspire)     | `Demo5_1.AppHost`    |
| **Frontend** | BFF (UI + Proxy)           | `Demo5_1.Web`        |
| **Backend**  | Modular Monolith API       | `Demo5_1.ApiService` |
| **Proxy**    | YARP (Frontend -> Backend) | `Demo5_1.Web`        |

## Key Changes from Demo 5

1.  **Topology**: Instead of manually running two projects, `AppHost` runs everything.
2.  **YARP**: The Frontend no longer needs specific Controllers to call the backend. It uses **YARP Reverse Proxy** to forward `/api/*` requests.
3.  **Identity Ownership**: The Identity Logic (Database, User Management) has moved from the Frontend to the **Backend (`ApiService`)**.
4.  **Modular Monolith**: The `ApiService` is designed to hold multiple feature slices (Identity, Weather) in one host.

## How to Run

1.  Open `Demo5_1.sln`.
2.  Set `Demo5_1.AppHost` as startup project.
3.  Run (F5).
