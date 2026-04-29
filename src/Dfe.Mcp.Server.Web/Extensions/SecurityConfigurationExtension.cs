using Microsoft.AspNetCore.HttpOverrides;

namespace Dfe.Mcp.Server.Web.Extensions;

public static class SecurityConfigurationExtension
{
    /// <summary>
    /// Add API key based authentication
    /// </summary>
    /// <param name="app"></param>
    /// <returns>An instance of <see cref="IApplicationBuilder"/></returns>
    public static IApplicationBuilder AddSecurity(this IApplicationBuilder app)
    {
        app.AddForwardedHeaders();
        app.AddSecurityHeaders();
        app.UseHttpsRedirection();
        return app;
    }
    private static IApplicationBuilder AddForwardedHeaders(this IApplicationBuilder app)
    {
        var forwardOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All,
            RequireHeaderSymmetry = false
        };
        forwardOptions.KnownIPNetworks.Clear();
        forwardOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardOptions);

        return app;
    }

    private static IApplicationBuilder AddSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseSecurityHeaders(options =>
        {
            options.AddFrameOptionsDeny()
                .AddXssProtectionDisabled()
                .AddContentTypeOptionsNoSniff()
                .RemoveServerHeader()
                .AddContentSecurityPolicy(builder =>
                {
                    builder.AddDefaultSrc().Self();
                    builder.AddStyleSrc().Self().WithNonce();
                    builder.AddScriptSrc().Self().WithNonce();
                })
                .AddPermissionsPolicy(builder =>
                {
                    builder.AddAccelerometer().None();
                    builder.AddAutoplay().None();
                    builder.AddCamera().None();
                    builder.AddEncryptedMedia().None();
                    builder.AddFullscreen().None();
                    builder.AddGeolocation().None();
                    builder.AddGyroscope().None();
                    builder.AddMagnetometer().None();
                    builder.AddMicrophone().None();
                    builder.AddMidi().None();
                    builder.AddPayment().None();
                    builder.AddPictureInPicture().None();
                    builder.AddSyncXHR().None();
                    builder.AddUsb().None();
                });
        });
        return app;
    }
}
