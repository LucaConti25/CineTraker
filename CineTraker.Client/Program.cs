using CineTraker.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using CineTraker.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

// 2. El interceptor (Peaje)
builder.Services.AddTransient<JwtHandler>();

// 3. El cliente HTTP configurado para usar el peaje
builder.Services.AddHttpClient("CineTraker.ServerAPI", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<JwtHandler>();

// 4. Inyección del HttpClient por defecto
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("CineTraker.ServerAPI"));

// 5. Tu servicio de Login
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
