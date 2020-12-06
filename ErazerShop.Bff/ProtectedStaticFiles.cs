using ErazerShop.Bff.ProtectedFolder;
using Microsoft.AspNetCore.Builder;

namespace ErazerShop.Bff
{
    public static class ProtectedStaticFiles
    {
        public static IApplicationBuilder UseProtectedStaticFiles(this IApplicationBuilder app)
        {
            app.UseDefaultFiles();
            app.UseMiddleware<ProtectedFolderMiddleware>("/admin");
            app.UseStaticFiles();

            return app;
        }
    }
}