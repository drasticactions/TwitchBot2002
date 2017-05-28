using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Models.Client;
using System.Speech.Synthesis;

namespace TwitchBot2002
{
    class Program
    {

        static SpeechSynthesizer synth;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();

            var authTokens = System.IO.File.ReadLines("tokens.txt").ToArray();
            // Bot Username, API Key, Channel to join.
            var client = new TwitchClient(new ConnectionCredentials(authTokens[0], authTokens[1]), "");
            client.OnMessageReceived += Client_OnMessageReceived;
            client.Connect();
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

        private static async void Client_OnMessageReceived(object sender, TwitchLib.Events.Client.OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits > 0)
            {
                // voice message!
                synth.SpeakAsync(e.ChatMessage.Message);
                Console.WriteLine($"Voice: {e.ChatMessage.Username}: {e.ChatMessage.Message}");
            }
            else
            {
                Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");
            }
        }
    }
}
