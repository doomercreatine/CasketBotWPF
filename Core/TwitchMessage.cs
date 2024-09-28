using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CasketChatBot.Core
{ 
    public class TwitchMessage
    {
        private static string[] emoteList = File.ReadAllLines("emotes.txt");

        public static Dictionary<string, double> tens = new() { { "k", 1e3 }, { "m", 1e6 }, { "b", 1e9 } };

        private List<string> chatters;
        public string username { get; set; }
        public Int64? guess { get; set; } = null;
        public string msgId { get; set; }

        private string? message;
        private bool returnGuess { get; set; }

        public TwitchMessage(string rawMessage, List<string> chatters)
        {
            this.chatters = chatters;
            var dict = ParseText(rawMessage);
            this.username = dict[0];
            this.msgId = dict[1];
            
            

        }
        private List<string> ParseText(string rawText)
        {
            string[] components = rawText.Split("PRIVMSG");
            string[] tags = components[0].Split(';');
            Dictionary<string, string> metadata =
                      tags.Select(item => item.Split('=')).ToDictionary(s => s[0], s => s[1]);
            string message = components[1].Split(':')[1].Trim();
            foreach (string chatter in chatters)
            {
                message.Replace(chatter, "");
            }
            this.message = message;
            ParseGuess(message);
            return [metadata["display-name"], metadata["id"]];
        }

        private void ParseGuess(string message)
        {
            //Console.WriteLine(message);
            var regex = new Regex(@"(?<![\/\\aAcCdDeEfFgGhHiIjJlLnNoOpPqQrRsStTuUvVwWxXyYzZ][0-9]{0,5})[0-9\s,.]+(?![\/\\aAcCdDeEfFgGhHiIjJlLnNoOpPqQrRsStTuUvVwWxXyYzZ]+\b)\s*[,.]*[kKmMbB]{0,1}\s*[0-9]*");
            var results = regex.Match(message).Value;
            if (results != null)
            {
                try
                {
                    var formatted = results.Replace(",", ".").ToLower();
                    foreach (string r in emoteList)
                    {
                        formatted = formatted.Replace(r, "");
                    }
                    formatted = formatted.Trim();
                    if (new[] { "k", "m", "b" }.Any(formatted.Contains))
                    {
                        double guess = Convert.ToDouble(formatted.Substring(0, formatted.Length - 1));
                        char unit = formatted[^1];
                        this.guess = Convert.ToInt64(guess * tens[$"{unit}"]);
                    }
                    else
                    {
                        if (formatted != "")
                        {
                            this.guess = Convert.ToInt64(formatted.Replace(".", ""));
                        }

                    }
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing guess {this.message}");

                }
            }
        }
    }
}
