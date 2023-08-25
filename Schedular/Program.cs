using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Schedular
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }
        static async Task RunAsync()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            using (HttpClient client = new HttpClient(clientHandler))
            {
                /* UAT Env
                string endpoint = "http://192.168.26.23:82/api/MESPASEnquiry/GenerateJson";*/
                /* Prod Env 
                string endpoint = "http://192.168.26.20:82/api/MESPASEnquiry/GenerateJson";*/
                /* Local Env*/
                string endpoint = "https://localhost:44314/api/MESPASEnquiry/GenerateJson";
                var data = new
                {
                    sourceType = "MESPAS"

                };
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                using (var Response = await client.PostAsync(endpoint, content))
                {
                    if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var result = Response.Content.ReadAsStringAsync().Result;
                    }
                }
            }
        }
    }
}