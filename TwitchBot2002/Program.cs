using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Models.Client;
using System.Speech.Synthesis;
using vJoyInterfaceWrap;

namespace TwitchBot2002
{
    class Program
    {

        static SpeechSynthesizer synth;

        static public vJoy joystick;
        static public vJoy.JoystickState iReport;
        static public uint id = 1;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            #region Setup VJoy

            joystick = new vJoy();
            iReport = new vJoy.JoystickState();

            if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
                id = Convert.ToUInt32(args[0]);

            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);

            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return;
            }

            joystick.ResetVJD(id);
            joystick.ResetButtons(id);

            #endregion

            synth = new SpeechSynthesizer();
            synth.SpeakStarted += Synth_SpeakStarted;
            synth.SpeakCompleted += Synth_SpeakCompleted;

            synth.SetOutputToDefaultAudioDevice();

            var authTokens = System.IO.File.ReadLines("tokens.txt").ToArray();
            // Bot Username, API Key, Channel to join.
            var client = new TwitchClient(new ConnectionCredentials(authTokens[0], authTokens[1]), "");
            client.OnMessageReceived += Client_OnMessageReceived;
            client.Connect();
            while(true)
            {
                var userMessage = Console.ReadLine();
                SpeakAsync(userMessage);
            }
        }

        private static async void SpeakAsync(string message)
        {
            synth.SpeakAsync(message);
        }

        private static void Synth_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            joystick.SetBtn(true, id, 1);
        }

        private static void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            joystick.SetBtn(false, id, 1);
        }

        private static async void Client_OnMessageReceived(object sender, TwitchLib.Events.Client.OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits > 0)
            {
                // voice message!
                SpeakAsync(e.ChatMessage.Message);
                Console.WriteLine($"Voice: {e.ChatMessage.Username}: {e.ChatMessage.Message}");
            }
            else
            {
                Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");
            }
        }
    }
}
