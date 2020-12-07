using ErazerShop.Bff.ProtectedFolder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ProxyKit;

namespace ErazerShop.Bff
{
    public static class Extensions
    {
        public static IApplicationBuilder UseProtectedStaticFiles(this IApplicationBuilder app)
        {
            app.UseDefaultFiles();
            app.UseMiddleware<ProtectedFolderMiddleware>("/admin");
            app.UseStaticFiles();

            return app;
        }

        /// <summary>
        ///     Adds X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto and
        ///     X-Forwarded-PathBase headers to the forward request context. If
        ///     the headers already exist they will be appended otherwise they
        ///     will be added.
        /// </summary>
        /// <param name="forwardContext">The forward context.</param>
        /// <param name="endpoint">The endpoint that takes care of the proxy</param>
        /// <returns>The forward context.</returns>
        public static ForwardContext AddXForwardedHeaders(this ForwardContext forwardContext)
        {
            var headers = forwardContext.UpstreamRequest.Headers;
            var protocol = forwardContext.HttpContext.Request.Scheme;
            var @for = forwardContext.HttpContext.Connection.RemoteIpAddress;
            var host = forwardContext.HttpContext.Request.Headers["Host"];
            var hostString = HostString.FromUriComponent(host);
            var pathBase = forwardContext.HttpContext.Request.PathBase.Value;

            headers.ApplyXForwardedHeaders(@for, hostString, protocol, pathBase);
            
            if (!string.IsNullOrWhiteSpace(pathBase))
            {
                @headers.Add("X-Forwarded-Prefix", pathBase);
            }

            return forwardContext;
        }
    }
}