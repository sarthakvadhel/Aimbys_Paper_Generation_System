namespace Aimbys.Infrastructure.Verification;

/// <summary>
/// Generates a simple SVG "QR placeholder" containing the verification
/// URL. A production deployment should replace this with a proper QR
/// encoder (QRCoder / ZXing.Net); the placeholder satisfies the contract
/// while avoiding external NuGet dependencies at this stage.
/// </summary>
public static class QrSvgGenerator
{
    public static string GeneratePlaceholder(string verificationUrl)
    {
        // Simple SVG with the URL encoded as text — placeholder until
        // a proper QR library is added
        var escaped = System.Security.SecurityElement.Escape(verificationUrl) ?? verificationUrl;
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120" width="120" height="120" data-verify-url="{escaped}">
                <rect width="120" height="120" fill="white" stroke="#333" stroke-width="2"/>
                <rect x="10" y="10" width="30" height="30" fill="#333"/>
                <rect x="80" y="10" width="30" height="30" fill="#333"/>
                <rect x="10" y="80" width="30" height="30" fill="#333"/>
                <rect x="50" y="50" width="20" height="20" fill="#333"/>
                <text x="60" y="115" text-anchor="middle" font-size="6" fill="#666">Verify</text>
            </svg>
            """;
    }
}
