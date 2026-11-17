/*
This file is part of the OpenQMS.net project(https://github.com/C-realize/OpenQMS).
Copyright(C) 2022 - 2024  C - realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Data;
using OpenQMS.Models;
using OpenQMS.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var address in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (!IPAddress.TryParse(address, out var proxyAddress))
        {
            throw new InvalidOperationException($"Forwarded proxy address '{address}' is not a valid IP address.");
        }

        options.KnownProxies.Add(proxyAddress);
    }
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseSqlServer(connectionString));
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, ConfiguredEmailSender>();
builder.Services.Configure<Microsoft.AspNetCore.Identity.DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(15));
if (builder.Environment.IsProduction())
{
    builder.Services.AddOptions<EmailOptions>()
        .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
        .Validate(options => options.IsValidForProduction(),
            "Production email sign-in requires a sender, SMTP host, HTTPS public base URL, valid port, and TLS.")
        .ValidateOnStart();
}
builder.Services.AddControllersWithViews(options =>
    options.Conventions.Add(new FirstReleaseControllerConvention()));
builder.Services.AddRazorPages();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("email-sign-in", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddScoped<DocumentAuthorizationService>();
builder.Services.AddScoped<DocumentWorkflowService>();
builder.Services.AddHostedService<DocumentEffectivityWorker>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Authorization handlers.
builder.Services.AddScoped<IAuthorizationHandler,
                      AuthorAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler,
                      AdministratorAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler,
                      ManagerAuthorizationHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    await SeedData.Initialize(services, builder.Configuration["BootstrapAdmin:Email"]);
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
        name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();

public partial class Program { }