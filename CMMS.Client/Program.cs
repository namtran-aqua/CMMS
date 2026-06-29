using AquaSolution.Client.Common;
using Blazored.SessionStorage;
using CMMS.Client;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<CurrentUserInfo>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddTransient<AuthMessageHandler>();
builder.Services.AddHttpClient("CMMS.API", client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("CMMS.API"));


builder.Services.AddSingleton<FactoryStateService>();
builder.Services.AddAntDesign();
builder.Services.AddAuthorizationCore();
await builder.Build().RunAsync();
