using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiTestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string baseUrl = "http://localhost:5177";
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

            var loginData = new { email = "super_admin@finveda.com", password = "Admin@123" };
            var loginResponse = await client.PostAsJsonAsync("/api/v1/Auth/login", loginData);
            var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var loanId = "0d762f74-46f1-4e1e-9bf7-c66b81d114aa"; // Explicitly defined from earlier query
            
            Console.WriteLine($"Generating Installments for Loan: {loanId}");
            var resGen = await client.PostAsync($"/api/v1/Installments/generate/{loanId}?count=12", null);
            Console.WriteLine($"Generate Status: {resGen.StatusCode}");
            Console.WriteLine(await resGen.Content.ReadAsStringAsync());
            
            var resCheck = await client.GetAsync("/api/Recovery/overdue");
            Console.WriteLine($"Recovery Call Status: {resCheck.StatusCode}");
            Console.WriteLine(await resCheck.Content.ReadAsStringAsync());
        }
    }
}
