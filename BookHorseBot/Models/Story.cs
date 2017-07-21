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
        }

        public class RootobjectSingle
        {
            public Datum data { get; set; }
            public Included[] included { get; set; }
        }

        public class Links
        {
            public string first { get; set; }
            public string prev { get; set; }
            public string next { get; set; }
            public string self { get; set; }
        }

        public class Datum
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
            public Links links { get; set; }
            public Meta meta { get; set; }
        }

        public class Attributes
        {
            public string name { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public string short_description { get; set; }
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
            public Datum[] data { get; set; }
        }


        public class Meta
        {
            public string old_id { get; set; }
            public string url { get; set; }
        }

        public class Included
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes attributes { get; set; }
            public Meta meta { get; set; }
            public Links links { get; set; }
        }
    }
}
