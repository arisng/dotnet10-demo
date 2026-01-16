# Feature Flags (Feature Management)


**Introduced:** demo7 (Planned)  
**Category:** Feature Management  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Runtime toggles that control feature visibility or behavior. Allow deploying code without enabling features; useful for A/B testing, gradual rollouts, and kill switches.

**Use Cases:**
- Gradual feature rollout (dark launch)
- A/B testing new features
- Emergency kill switches
- Subscription tier features (premium only)
- Canary deployments

**Implementation (Microsoft.FeatureManagement):**
```csharp
if (await featureManager.IsEnabledAsync("PremiumReports"))
{
    // Show premium report features
}
```

**Integration with Azure AppConfig:**
- Centralized flag management
- Real-time updates (no redeploy)
- Per-environment flags
- Per-tenant feature overrides

**Strengths:**
- ✅ Quick enable/disable without deploy
- ✅ Per-tenant customization
- ✅ A/B testing support
- ✅ Gradual rollout capability

**Weaknesses:**
- ❌ Flag proliferation without cleanup
- ❌ Testing complexity (many combinations)
- ❌ Operational overhead

**Related Patterns:**
- [Multi-Tenancy](mt-finbuckle.md)

**Demo References:**
- demo7: Feature flags for premium features
- demo6+: Per-tenant feature toggles

