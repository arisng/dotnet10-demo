namespace Demo4.EntraIntegration.Shared.Models;

/// <summary>
/// Exception thrown when a user needs to undergo an interactive challenge (e.g., for incremental consent).
/// This is a shared alternative to MicrosoftIdentityWebChallengeUserException that can be used on both server and client (WASM).
/// </summary>
public class ChallengeRequiredException : Exception
{
    public ChallengeRequiredException() : base("Interactive challenge required.") { }
    public ChallengeRequiredException(string message) : base(message) { }
    public ChallengeRequiredException(string message, Exception innerException) : base(message, innerException) { }
}
