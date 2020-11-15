using Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LAMIS_NMRS.Utils
{
    public class APIHelper
    {
        readonly string baseDataURL;

        HttpClient ApiClient = null;

        public APIHelper(string baseUrl)
        {
            baseDataURL = baseUrl;
            ApiClient = new HttpClient();
            ApiClient.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Basic",
               Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:Admin123")));
        }

        public async Task<string> PostMessage(string urlPart, string jsonMsg)
        {
            string url = baseDataURL + urlPart;

            StringContent stringContent = new StringContent(jsonMsg, Encoding.UTF8, "application/json");
            var response = await ApiClient.PostAsync(url, stringContent);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<T> PostMessage<T>(string urlPart, string jsonString)
        {
            string url = baseDataURL + urlPart;
            var response = await ApiClient.PostAsJsonAsync(url,
                                    new StringContent(jsonString, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsAsync<T>();
        }

        public async Task<string> PostMessage<T>(string urlPart, T data)
        {
            string url = baseDataURL + urlPart;
            var response = await ApiClient.PostAsJsonAsync(url, data);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<ApiResponse> SendData<T, U>(string urlPart, U data)
        {
            try
            {
                string url = baseDataURL + urlPart;
                var response = await ApiClient.PostAsJsonAsync(url, data);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsAsync<ApiResponse>();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: {0}", Environment.NewLine);
                    Console.ForegroundColor = ConsoleColor.White;                    
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                    Console.WriteLine("An error was encountered with the following message: {0}", Environment.NewLine);
                    Console.WriteLine(await response.Content.ReadAsStringAsync());                    
                    return new ApiResponse { uuid = string.Empty };
                }
            }
            catch(Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered with the following message:" + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message + Environment.NewLine);
                return new ApiResponse { uuid = string.Empty };
            }
        }

        public async Task<string> GetMessage(string urlPart)
        {
            string url = baseDataURL + urlPart;
            try
            {
                var response = await ApiClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> GetData(string urlPart)
        {
            try
            {
                string url = baseDataURL + urlPart;
                var response = await ApiClient.GetAsync(url);
                return await response.Content.ReadAsAsync<dynamic>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<string> Delete(string urlPart)
        {
            string url = baseDataURL + urlPart;
            try
            {
                var response = await ApiClient.DeleteAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}