using IdentityServer4;

namespace Identity.API.Configuration;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResource("roles", "User roles", new[] { "role" })
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope("catalog.api", "Catalog API"),
            new ApiScope("basket.api", "Basket API"),
            new ApiScope("ordering.api", "Ordering API"),
            new ApiScope("discount.grpc", "Discount gRPC"),
            new ApiScope("shopping.web", "Shopping Web"),
            new ApiScope("gateway.api", "Gateway API")
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new ApiResource("catalog.api", "Catalog API")
            {
                Scopes = { "catalog.api" }
            },
            new ApiResource("basket.api", "Basket API")
            {
                Scopes = { "basket.api" }
            },
            new ApiResource("ordering.api", "Ordering API")
            {
                Scopes = { "ordering.api" }
            },
            new ApiResource("discount.grpc", "Discount gRPC")
            {
                Scopes = { "discount.grpc" }
            },
            new ApiResource("shopping.web", "Shopping Web")
            {
                Scopes = { "shopping.web" }
            },
            new ApiResource("gateway.api", "Gateway API")
            {
                Scopes = { "gateway.api" }
            }
        };

    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            // Shopping Web Application
            new Client
            {
                ClientId = "shopping.web",
                ClientName = "Shopping Web Application",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,
                AllowOfflineAccess = true,

                RedirectUris =
                {
                    "http://localhost:5000/signin-oidc",
                    "https://localhost:5000/signin-oidc",
                    "http://localhost:6005/signin-oidc",
                    "https://localhost:6005/signin-oidc",
                    "http://shopping.web:8080/signin-oidc",
                    "https://shopping.web:8080/signin-oidc"
                },

                PostLogoutRedirectUris =
                {
                    "http://localhost:5000/signout-callback-oidc",
                    "https://localhost:5000/signout-callback-oidc",
                    "http://localhost:6005/signout-callback-oidc",
                    "https://localhost:6005/signout-callback-oidc",
                    "http://shopping.web:8080/signout-callback-oidc",
                    "https://shopping.web:8080/signout-callback-oidc"
                },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "roles",
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.grpc",
                    "shopping.web"
                },

                AccessTokenLifetime = 3600,
                RefreshTokenUsage = TokenUsage.ReUse
            },

            // Swagger UI
            new Client
            {
                ClientId = "swagger.ui",
                ClientName = "Swagger UI",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = true,

                RedirectUris =
                {
                    "http://localhost:6000/swagger/oauth2-redirect.html",
                    "http://localhost:6001/swagger/oauth2-redirect.html",
                    "http://localhost:6003/swagger/oauth2-redirect.html",
                    "http://localhost:6004/swagger/oauth2-redirect.html"
                },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.grpc",
                    "gateway.api"
                }
            }
        };
}
