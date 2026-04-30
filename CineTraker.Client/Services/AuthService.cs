using Blazored.LocalStorage;
using CineTraker.Shared; // Asegurate que acá esté tu clase Login
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace CineTraker.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> Login(Login loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/account/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result != null)
                {
                    // 1. Guardamos el token en el storage para persistencia (F5)
                    await _localStorage.SetItemAsync("authToken", result.Token);

                    // 2. Le pasamos el token DIRECTAMENTE al Provider 
                    // Esto hace que el menú cambie sin lag y sin trabar el navegador
                    if (_authStateProvider is JwtAuthStateProvider jwtProvider)
                    {
                        jwtProvider.NotifyAuthChanged(result.Token);
                    }

                    return true;
                }
            }
            return false;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");

            // Avisamos que salimos (sin pasarle nada)
            if (_authStateProvider is JwtAuthStateProvider jwtProvider)
            {
                jwtProvider.NotifyAuthChanged();
            }
        }

        public async Task<RegisterResult> Register(RegisterRequest registerModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/account/register", registerModel);

            if (response.IsSuccessStatusCode)
            {
                return new RegisterResult { Succeeded = true };
            }

            // Si falla (ej: usuario duplicado), leemos los errores del backend
            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
            return result ?? new RegisterResult { Succeeded = false, Errors = new List<string> { "Error desconocido" } };
        }
    }

    public class LoginResult
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }



}