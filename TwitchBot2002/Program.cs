using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Models.Client;
using System.Speech.Synthesis;
using System.Speech;
using vJoyInterfaceWrap;
using System.IO;
using System.Speech.AudioFormat;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace TwitchBot2002
{
    class Program
    {

        static SpeechSynthesizer synth;

        static vJoy joystick;
        static vJoy.JoystickState iReport;
        static uint id = 1;

        static BufferedWaveProvider bwp;

        static WaveInEvent waveIn;
        static WaveOut waveOut;

        static List<string> messages = new List<string>();
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            #region Audio

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities WOC = WaveOut.GetCapabilities(i);
                Console.WriteLine($"{i + 1}: {WOC.ProductName}");
            }

            Console.WriteLine("Enter Audio Output Device Number: ");
            var deviceNumString = Console.ReadLine();
            var deviceInt = 0;
            try
            {
                deviceInt = Convert.ToInt32(deviceNumString) - 1;
                if (deviceInt < 0 || deviceInt > WaveOut.DeviceCount - 1) return;
            }
            catch (Exception)
            {
                return;
            }

            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.DeviceNumber = deviceInt;
        
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            #endregion

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

            var authTokens = File.ReadLines("tokens.txt").ToArray();

            // Bot Username, API Key, Channel to join.
            var client = new TwitchClient(new ConnectionCredentials(authTokens[0], authTokens[1]), authTokens[2]);
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.Connect();
            Console.WriteLine("Client Started! Use CTRL-Z to quit");
            Console.WriteLine("Enter Message Number");
            while(true)
            {
                var numString = Console.ReadLine();
                try
                {
                    var num = Convert.ToInt32(numString);
                    Speak(messages[num - 1]);
                }
                catch (Exception)
                {
                    // TODO: Show error message
                    // Bigger TODO: Will probably never do that.
                }
            }
        }

        private static void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            joystick.SetBtn(false, id, 1);
        }

        private static void Speak(string message)
        {
            joystick.SetBtn(true, id, 1);
            WaveFileReader reader = new WaveFileReader(new MemoryStream(TextToBytes(message)));
            waveOut.Init(reader);
            waveOut.Play();
        }

        public static byte[] TextToBytes(string textToSpeak)
        {
            byte[] byteArr = null;

            var t = new System.Threading.Thread(() =>
            {
                SpeechSynthesizer ss = new SpeechSynthesizer();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    ss.SetOutputToWaveStream(memoryStream);
                    ss.Speak(textToSpeak);
                    byteArr = memoryStream.ToArray();
                }
            });
            t.Start();
            t.Join();
            return byteArr;
        }

        private static async void Client_OnMessageReceived(object sender, TwitchLib.Events.Client.OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits <= 0) return;

            messages.Add(e.ChatMessage.Message);
            // voice message!
            Console.WriteLine($"New Message #{messages.Count}: {e.ChatMessage.Username} ({e.ChatMessage.Bits}): {e.ChatMessage.Message}");
        }

        private static async void Client_OnChatCommandReceived(object sender, TwitchLib.Events.Client.OnChatCommandReceivedArgs e)
        {
            if (e.Command.ChatMessage.IsBroadcaster)
            {
                switch(e.Command.Command)
                {
                    case "speak":
                        Console.WriteLine($"Broadcaster override! {e.Command.ArgumentsAsString}");
                        Speak(e.Command.ArgumentsAsString);
                        break;
                }
            }
        }
    }
}
