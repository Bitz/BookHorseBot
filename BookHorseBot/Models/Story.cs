using System;
using System.Diagnostics.CodeAnalysis;

namespace BookHorseBot.Models
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    class Story
    {

        public class Rootobject
        {
            public Datum[] data { get; set; }
            public Included[] included { get; set; }
            public Links links { get; set; }
            public string uri { get; set; }
            public string method { get; set; }
            public Debug debug { get; set; }
        }

        public class Links
        {
            public string first { get; set; }
            public string prev { get; set; }
            public string next { get; set; }
        }

        public class Debug
        {
            public string duration { get; set; }
        }

        public class Datum
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
            public Links1 links { get; set; }
            public Meta meta { get; set; }
        }

        public class Attributes
        {
            public string title { get; set; }
            public string description { get; set; }
            public DateTime date_published { get; set; }
            public int total_num_views { get; set; }
            public int num_words { get; set; }
            public int rating { get; set; }
            public string completion_status { get; set; }
            public string content_rating { get; set; }
        }

        public class Relationships
        {
            public Author author { get; set; }
            public Tags tags { get; set; }
        }

        public class Author
        {
            public Data data { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
        }

        public class Tags
        {
            public Datum1[] data { get; set; }
        }

        public class Datum1
        {
            public string type { get; set; }
            public string id { get; set; }
        }

        public class Links1
        {
            public string self { get; set; }
        }

        public class Meta
        {
            public string url { get; set; }
        }

        public class Included
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes1 attributes { get; set; }
            public Meta1 meta { get; set; }
            public Links2 links { get; set; }
        }

        public class Attributes1
        {
            public string name { get; set; }
            public string type { get; set; }
        }

        public class Meta1
        {
            public string old_id { get; set; }
            public string url { get; set; }
        }

        public class Links2
        {
            public string self { get; set; }
        }


    }
}
