using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProxyKit;

namespace ErazerShop.Bff
{
    public class Startup
    {
        public Startup()
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxy();
            services.AddAccessTokenManagement();

            services.AddControllers();
            services.AddDistributedMemoryCache();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("cookies", options =>
                {
                    options.Cookie.Name = "ErazerShop.Bff";
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(1500);
                    options.SlidingExpiration = false;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://localhost:7000";
                    options.ClientId = "erazershop";
                    options.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";

                    options.ResponseType = "code";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.SignedOutCallbackPath = "https://localhost:9999";

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("erazershop.api");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUI(x =>
                {
                    x.SwaggerEndpoint("https://localhost:5000/swagger/v1/swagger.json", "API");
                });
            }
            
            app.UseAuthentication();

            app.Map("/login", login =>
            {
                login.Run(async (context) =>
                {
                    var hasRedirectQueryParam =
                        context.Request.Query.TryGetValue("redirect", out var redirectQueryParam);
                    var hasPromptQueryParam = 
                        context.Request.Query.TryGetValue("prompt", out var promptQueryParam);
                    var prompt = hasPromptQueryParam && promptQueryParam == "login";
                    
                    if (!context.User.Identity.IsAuthenticated || prompt)
                    {
                        var props = new OpenIdConnectChallengeProperties
                        {
                            Prompt = prompt ? "login" : null,
                            RedirectUri = hasRedirectQueryParam
                                ? $"/login?redirect={redirectQueryParam.First()}"
                                : "/login"
                        };

                        await context.ChallengeAsync(props);
                        return;
                    }

                    context.Response.Redirect(hasRedirectQueryParam ? $"/admin{redirectQueryParam}" : "/admin");
                });
            });

            app.Map("/api", api =>
            {
                api.RunProxy(async context =>
                {
                    var forwardContext = context.ForwardTo("https://localhost:5000");

                    var token = await context.GetUserAccessTokenAsync();
                    forwardContext.UpstreamRequest.SetBearerToken(token);

                    var result = await forwardContext.Send();

                    if (result.StatusCode != HttpStatusCode.Unauthorized)
                        return result;

                    await context.SignOutAsync("cookies");
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                });
            });

            app.UseProtectedStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireAuthorization();
            });
        }
    }
}