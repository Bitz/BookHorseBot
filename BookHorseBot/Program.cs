using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using BookHorseBot.Models;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;

using static BookHorseBot.Models.Misc;
using static BookHorseBot.Properties.Settings;

namespace BookHorseBot
{
    class Program
    {
        private static readonly HttpClient BotClient = new HttpClient();

        static void Main()
        {
            Console.Title = "BookHorseBot";
            List<string> ignoredUsers = new List<string> {"nightmirrormoon"};

            if (string.IsNullOrEmpty(Default.FF_Token))
            {
                string receiveStream = FimFictionGetAuthToken();
                FfAuthorization authorization = JsonConvert.DeserializeObject<FfAuthorization>(receiveStream);
                Default.FF_Token = authorization.access_token;
                Default.Save();
            }

            BotClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            BotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                Default.FF_Token);
            BotWebAgent webAgent = new BotWebAgent(Default.R_Username,
                Default.R_Password,
                Default.R_client_id,
                Default.R_client_secret,
                "https://google.com");

            Reddit reddit = new Reddit(webAgent, true);
            reddit.LogIn(Default.R_Username, Default.R_Password);
            string redditName = reddit.User.FullName;
            if (redditName.ToLower() == Default.R_Username.ToLower())
            {
                Console.WriteLine("Logged in!");
            }

            Subreddit subreddit = reddit.GetSubreddit("mylittlepony");


            IEnumerable<Comment> comments =
                subreddit.CommentStream.Where(c => !ignoredUsers.Contains(c.AuthorName.ToLower())
                                                   && c.CreatedUTC >= DateTime.UtcNow.AddMinutes(-5)
                );

            foreach (Comment comment in comments)
            {
                if (!comment.Body.Contains("{") && !comment.Body.Contains("}"))
                {
                    continue;
                }
                Comment qualifiedComment = reddit.GetComment(new Uri(comment.Shortlink));
                if (qualifiedComment.Comments.All(x => x.AuthorName != redditName))
                {
                    List<string> list = ExtractStoryNames(comment);
                    if (list.Count > 0)
                    {
                        List<Story.Rootobject> stories = GetPostText(list);
                        string postReplyBody = GeneratePostBody(stories);
                        comment.Reply(postReplyBody);
                        Console.WriteLine($"Reply posted to {comment.AuthorName}!");
                    }
                }
            }
        }

        private static List<string> ExtractStoryNames(Comment comment)
        {
            List<string> list = new List<string>();
            MatchCollection matches = Regex.Matches(comment.Body, @"(?<=\{)[^}]*(?=\})", RegexOptions.None);
            foreach (Match match in matches)
            {
                list.Add(Regex.Replace(match.Value.Trim(), @"[^a-zA-Z0-9 -]", ""));
            }
            return list;
        }

        private static string GeneratePostBody(List<Story.Rootobject> rootList)
        {
            string template = "";
            foreach (var root in rootList)
            {
                if (root.data.Length == 0)
                {
                    continue;
                }
                Story.Datum story = root.data.First();
                if (story.attributes.content_rating == "mature")
                {
                    template += Constants.NotAllowed;
                }
                else
                {
                    template += "\r\n [](/twibeam) \r\n" +
                                $"#[{story.attributes.title}]({story.meta.url})\r\n" +
                                $"*by [{GetUsername(root)}](https://www.fimfiction.net/user/{story.relationships.author.data.id}) " +
                                $"| {story.attributes.date_published:dd MMM yy} " +
                                $"| Views: {FormatNumber(story.attributes.total_num_views)} " +
                                $"| {FormatNumber(story.attributes.num_words)} Words " +
                                $"| Status: `{UppercaseFirst(story.attributes.completion_status)}` " +
                                $"| Rating: `{(double) story.attributes.rating}%`*\r\n\r\n" +
                                $"{GenerateDescription(story.attributes.description)}" +
                                "\r\n\r\n" +
                                $"**Tags**: {GenerateTags(root)}";
                }
                template += "\r\n \r\n" +
                            "-----";
            }
            if (rootList.All(x => x.data.Length == 0) || rootList.Count == 0)
            {
                template = Constants.NoResults;
            }

            template += Constants.Footer;

            return template;
        }

        private static string GenerateDescription(string description)
        {
            if (description.Length > 500)
            {
                description = description.Substring(0, 500);
            }
            if (description.Contains("[url=") && !description.Contains("[/url]"))
            {
                //If we stripped part of the body and didn't keep the closing tag for this.
                description = Regex.Replace(description, @"\[url=(.+?)\]", "");
            }
            //Bold
            description = Regex.Replace(description, Constants.RegexBbCode("b"), match => "**" + match.Groups[1] + "**",
                RegexOptions.Multiline & RegexOptions.IgnoreCase);
            //Italics
            description = Regex.Replace(description, Constants.RegexBbCode("i"), match => "*" + match.Groups[1] + "*",
                RegexOptions.Multiline & RegexOptions.IgnoreCase);
            //Strike-through
            description = Regex.Replace(description, Constants.RegexBbCode("s"), match => "~~" + match.Groups[1] + "~~",
                RegexOptions.Multiline & RegexOptions.IgnoreCase);
            //URL
            description = Regex.Replace(description, @"\[url=(.+?)\]((?:.|\n)+?)\[\/url\]",
                match => $"[{match.Groups[2]}]({match.Groups[1]})", RegexOptions.Multiline & RegexOptions.IgnoreCase);
            //Everything else
            description = Regex.Replace(description, @"[[\/\!]*?[^\[\]]*?](?!\()", "",
                RegexOptions.Multiline & RegexOptions.IgnoreCase);
            return description;
        }

        private static string GetUsername(Story.Rootobject s)
        {
            string authorId = s.data.First().relationships.author.data.id;

            var authorName = s.included.First(x => x.id == authorId && x.type == "user").attributes.name;
            return authorName;
        }

        private static string GenerateTags(Story.Rootobject relationshipsTags)
        {
            List<string> tagIds =
                relationshipsTags.data.First().relationships.tags.data.Select(datum1 => datum1.id).ToList();
            List<string> tagNames = new List<string>();
            foreach (string tagId in tagIds)
            {
                string tagName =
                    relationshipsTags.included.First(x => x.id == tagId && x.type == "story_tag").attributes.name;
                tagNames.Add(tagName);
            }

            string builtTagLineContent = string.Join("`, `", tagNames);
            builtTagLineContent = "`" + builtTagLineContent + "`";
            return builtTagLineContent;
        }

        private static List<Story.Rootobject> GetPostText(List<string> sanitizedNames)
        {
            List<Story.Rootobject> resultCollection = new List<Story.Rootobject>();
            foreach (var sanitizedName in sanitizedNames)
            {
                string queryUrl = "https://www.fimfiction.net/api/v2/stories" +
                                  "?include=characters,tags,author" +
                                  "&sort=-relevance" +
                                  "&page[size]=1" +
                                  "&fields[user]=name" +
                                  "&fields[story]=title,description,date_published,total_num_views,num_words,rating,completion_status,tags,content_rating,author" +
                                  "&fields[story_tag]=name,type" +
                                  $"&query={sanitizedName}";
                string res =
                    BotClient.GetStringAsync(queryUrl)
                        .Result;
                Story.Rootobject searchResult = JsonConvert.DeserializeObject<Story.Rootobject>(res);
                resultCollection.Add(searchResult);
            }
            return resultCollection;
        }

        private static string FimFictionGetAuthToken()
        {
            var values = new Dictionary<string, string>
            {
                {"client_id", Default.FF_client_id},
                {"client_secret", Default.FF_client_secret},
                {"grant_type", "client_credentials"}
            };

            HttpContent content = new FormUrlEncodedContent(values);
            HttpResponseMessage response =
                BotClient.PostAsync("https://www.fimfiction.net/api/v2/token", content).Result;
            string receiveStream = response.Content.ReadAsStringAsync().Result;
            return receiveStream;
        }


        static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        private static string FormatNumber(long num)
        {
            long i = (long) Math.Pow(10, (int) Math.Max(0, Math.Log10(num) - 2));
            num = num / i * i;

            if (num >= 1000000000)
                return (num / 1000000000D).ToString("0.##") + "B";
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "M";
            if (num >= 1000)
                return (num / 1000D).ToString("0.##") + "K";

            return num.ToString("#,0");
        }
    }
}
