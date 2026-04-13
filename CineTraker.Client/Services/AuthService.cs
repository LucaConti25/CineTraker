using System.Net.Http.Json;
using Blazored.LocalStorage;
using CineTraker.Shared; // Asegurate que acá esté tu clase Login

namespace CineTraker.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<bool> Login(Login loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/account/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result != null)
                {
                    // Guardamos el token para que el JwtHandler lo use después
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    return true;
                }
            }
            return false;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
        }
    }

    public class LoginResult
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}