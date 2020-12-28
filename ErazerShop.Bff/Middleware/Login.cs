using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using ErazerShop.Bff.Model;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ErazerShop.Bff.Middleware
{
    public static class Login
    {
        public static void UseLogin(this IApplicationBuilder app, IHostEnvironment _env)
        {
            if (_env.IsDevelopment())
            {
                app.Map("/login/dev", login =>
                {
                    login.UseWhen(x => x.Request.Method == HttpMethods.Post, (post) => post.Run(async context =>
                    {
                        var req = context.Request;
                        var res = context.Response;

                        var model = await req.ReadFromJsonAsync<DevLoginModel>();

                        if (model == null || string.IsNullOrWhiteSpace(model.UserName) ||
                            string.IsNullOrEmpty(model.Password))
                        {
                            res.StatusCode = 400;
                            await res.WriteAsJsonAsync(new {Error = "missing username and/or password"});
                            return;
                        }

                        var client = new HttpClient();
                        var disco = await client.GetDiscoveryDocumentAsync("https://localhost:7000");
                        var tokenRequest = new PasswordTokenRequest
                        {
                            Address = disco.TokenEndpoint,
                            ClientId = "erazershop.dev",
                            ClientSecret = "209839E0-6F18-4283-A798-A1458CD1F875",
                            Password = model.Password,
                            UserName = model.UserName
                        };

                        var tokenResponse = await client.RequestPasswordTokenAsync(tokenRequest);

                        if (tokenResponse.IsError)
                        {
                            res.StatusCode = 400;
                            res.ContentType = MediaTypeNames.Application.Json;
                            await context.SignOutAsync();
                            await res.WriteAsync(tokenResponse.Raw);
                        }
                        else
                        {
                            var expire = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokenResponse.ExpiresIn;
                            var userInfoRequest = new UserInfoRequest
                            {
                                Address = disco.UserInfoEndpoint,
                                Token = tokenResponse.AccessToken
                            };

                            var userInfoResponse = await client.GetUserInfoAsync(userInfoRequest);
                            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(userInfoResponse.Claims, "cookies", JwtClaimTypes.Name, JwtClaimTypes.Role));
                            var authenticationToken = new AuthenticationToken {Name = OpenIdConnectParameterNames.AccessToken, Value = tokenResponse.AccessToken};
                            var expireToken = new AuthenticationToken
                            {
                                Name = "expires_at",
                                Value = DateTimeOffset.FromUnixTimeSeconds(expire).ToString("o", CultureInfo.InvariantCulture)
                            };
                            var properties = new AuthenticationProperties();
                            properties.StoreTokens(new List<AuthenticationToken> {authenticationToken, expireToken});
                            await context.SignInAsync(claimsPrincipal, properties);

                            res.StatusCode = 204;
                        }
                    }));
                });
            }

            app.Map("/login", login =>
            {
                login.Run(async (context) =>
                {
                    var req = context.Request;
                    var res = context.Response;

                    var hasRedirectQueryParam = req.Query.TryGetValue("redirect", out var redirectQueryParam);
                    var hasPromptQueryParam = req.Query.TryGetValue("prompt", out var promptQueryParam);
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

                    res.Redirect(hasRedirectQueryParam ? $"/admin{redirectQueryParam}" : "/admin");
                });
            });
        }
    }
}