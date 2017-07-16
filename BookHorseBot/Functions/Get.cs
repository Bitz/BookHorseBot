using System;
using System.Collections.Generic;
using System.Net.Http;
using static BookHorseBot.Configuration;

namespace BookHorseBot.Functions
{
    class Get
    {
        public static bool IsMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static bool IsWindows()
        {
            return !IsMono();
        }

        public static string FimFictionGetAuthToken(HttpClient botClient)
        {
            var values = new Dictionary<string, string>
            {
                {"client_id", C.FimFiction.ClientId},
                {"client_secret", C.FimFiction.ClientSecret},
                {"grant_type", "client_credentials"}
            };

            HttpContent content = new FormUrlEncodedContent(values);
            HttpResponseMessage response =
                botClient.PostAsync("https://www.fimfiction.net/api/v2/token", content).Result;
            string receiveStream = response.Content.ReadAsStringAsync().Result;
            return receiveStream;
        }
    }
}
