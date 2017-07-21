using System;

namespace BookHorseBot.Models
{
    class Misc
    {

        public class FfAuthorization
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
        }


        //Will eventually group each request with their respective responses to help group data
        public class UserRequest
        {
            public string Request { get; set; }
            public RequestType Type { get; set; }
            public RequestResult Result { get; set; } = RequestResult.NothingHappened;
            public string Response { get; set; }
            public int SortOrder { get; set; } = 0;


            public enum RequestResult
            {
                Success,
                Fail,
                NotFound,
                NothingHappened
            }

            public enum RequestType
            {
                Search_Name,
                Search_ID,
                Search_URL
            }
        }
    }
}
