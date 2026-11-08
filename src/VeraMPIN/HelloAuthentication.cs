using Windows.Security.Credentials.UI;

namespace VeraMPIN;

internal static class HelloAuthentication
{
    public static async Task VerifyAsync()
    {
        var availability = await UserConsentVerifier.CheckAvailabilityAsync();
        if (availability != UserConsentVerifierAvailability.Available)
        {
            throw new InvalidOperationException($"Windows Hello verification is unavailable for this account or device ({availability}). Use VeraCrypt's native password prompt or configure Windows Hello first.");
        }

        var result = await UserConsentVerifier.RequestVerificationAsync("Approve using the saved VeraCrypt password.");
        if (result != UserConsentVerificationResult.Verified)
        {
            throw new OperationCanceledException($"Windows Hello did not approve the request ({result}).");
        }
    }
}