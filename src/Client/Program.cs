using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("Here is the Information in /.well-known/openid-configuration in STS Press any key to continue...");
            Console.ReadLine();

            // discover endpoints from metadata
            var client = new HttpClient();

            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }
            Console.WriteLine(disco.Json);
            Console.WriteLine("\n\n");
            Console.WriteLine("Here is the Token from STS using Client Credential Grany Type. Press any key to continue...");
            Console.ReadLine();
            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",

                Scope = "default_api"
            });
            
            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            Console.WriteLine("using Client Credential Grany Type we can call another API. Press any key to continue...");
            Console.ReadLine();
            // call api
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await apiClient.GetAsync("https://localhost:10001/WeatherForecast");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }

            Console.WriteLine("But denied when User Information is requested for another API. Press any key to continue...");
            Console.ReadLine();


            // call api
            apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            response = await apiClient.GetAsync("https://localhost:10001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }


            Console.WriteLine("Here is the Token from STS using Resource Owner Password Grany Type. Press any key to continue...");
            Console.ReadLine();
            // request token
            tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                UserName = "bob",
                Password = "Pass123$",
                Scope = "default_api"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            Console.WriteLine("Calling User API using the Token should go through. Press any key to continue...");
            Console.ReadLine();

            // call api
            apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            response = await apiClient.GetAsync("https://localhost:10001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }

            Console.WriteLine("Calling Admin API Using User Role token, should be denied , Press any key...");
            Console.ReadLine();
            apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            response = await apiClient.GetAsync("https://localhost:10001/admin");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }


            Console.WriteLine("Here is the User Token in Admin Role from STS using Resource Owner Password Grany Type. Press any key to continue ...");
            Console.ReadLine();
            // request token
            tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                UserName = "alice",
                Password = "Pass123$",
                Scope = "default_api"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            response = await apiClient.GetAsync("https://localhost:10001/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }

            Console.WriteLine("Calling API Using Admin Role token, should go through , Press any key...");
            Console.ReadLine();

            // call api
            apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            response = await apiClient.GetAsync("https://localhost:10001/admin");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }


            Console.WriteLine("Press any key to End ...");
            Console.ReadLine();


            



        }
    }
}
