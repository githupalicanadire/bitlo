var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add HttpClient
builder.Services.AddHttpClient();

// Add HttpContextAccessor and UserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

// Add Authentication with Identity Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = "ToyShop.Auth";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = builder.Configuration["IdentityServer:BaseUrl"];
    options.ClientId = "shopping.web";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false; // Only for development

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("shopping.web");
    options.Scope.Add("catalog.api");
    options.Scope.Add("basket.api");
    options.Scope.Add("ordering.api");

    options.GetClaimsFromUserInfoEndpoint = true;

    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
    {
        OnAccessDenied = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            // Log the error for debugging
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Authentication failed: {Error}", context.Exception.Message);

            context.HandleResponse();
            context.Response.Redirect($"/?error={Uri.EscapeDataString(context.Exception.Message)}");
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            // Log the error for debugging
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Failure, "Remote authentication failure: {Error}", context.Failure?.Message);

            context.HandleResponse();
            context.Response.Redirect($"/?error={Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error")}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddRefitClient<ICatalogService>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
    })
    .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

builder.Services.AddRefitClient<IBasketService>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
    })
    .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

builder.Services.AddRefitClient<IOrderingService>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
    })
    .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add custom middleware to handle unauthorized access to protected pages
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

    // Protected paths that require authentication
    var protectedPaths = new[] { "/cart", "/checkout", "/orderlist", "/orderdetail" };

    if (!isAuthenticated && protectedPaths.Any(p => path?.StartsWith(p) == true))
    {
        // Store the original URL to redirect after login
        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return;
    }

    await next();
});

app.MapRazorPages();

app.Run();
