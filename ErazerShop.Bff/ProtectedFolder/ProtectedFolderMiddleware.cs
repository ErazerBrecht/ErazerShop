using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ErazerShop.Bff.ProtectedFolder
{
    public class ProtectedFolderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PathString _path;

        public ProtectedFolderMiddleware(RequestDelegate next, string path)
        {
            _next = next;
            _path = path;
        }

        public async Task Invoke(HttpContext httpContext, IAuthorizationService authorizationService)
        {
            if (httpContext.Request.Path.StartsWithSegments(_path))
            {
                if (!httpContext.User.Identity.IsAuthenticated)
                {
                    httpContext.Response.Redirect("/");
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}