using CasketChatBot.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Interop;

namespace CasketChatBot.Core
{    
    class TwitchClient : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private WSClient _client = new WSClient();
        
        public delegate void ClientConnectedHandler(WebSocketState state);
        public event ClientConnectedHandler? ClientConnected;
        private List<string> chatters = new List<string>();
        public delegate void GuessHandler(GuessesModel guess);
        public event GuessHandler? GuessReceived;

        public bool GuessingEnabled = false;
        public TwitchClient()
        {
            _client.ClientConnected += OnClientConnected;
            _client.MessageReceived += OnMessageReceived;            
        }
        public async void StartGuessing()
        {
            GuessingEnabled = true;
            chatters = await _client.GetChatters();
            //Console.WriteLine("Guessing started");
            await Send("Guessing for Master Casket value is now OPEN! POGGIES");
        }
        public async void StopGuessing()
        {
            GuessingEnabled = false;
            await Send("]=-[]=-[]=-[]=-[]=-[]=-[]=-[]=-[]=-[]=-[");
            //Console.WriteLine("Guessing stopped");
        }
        public void Start()
        {
            _client.Start();
        }
        public async Task Send(string message, string? msgId = null)
        {
            await _client.SendMessagesAsync(message, msgId);
        }
        public async Task Ban(string username, int length)
        {
            //await _client.BanUser(username, length);
            TimeSpan span = TimeSpan.FromSeconds(length);
            string len = string.Format("{0}hr {1}min {2}sec",
                            (int)span.TotalHours,
                                    span.Minutes,
                                    span.Seconds);

            Console.WriteLine($"Banned {username} for {len}");
        }
        #region Message parsing
        private async Task ExtractGuess(string message)
        {
            TwitchMessage twitchMessage = new TwitchMessage(message, chatters);
            if (twitchMessage.guess != null)
            {
                GuessReceived?.Invoke(new GuessesModel(twitchMessage.username, (long)twitchMessage.guess, twitchMessage.msgId));
            }
        }
        #endregion


        #region Event Handlers
        private void OnClientConnected(WebSocketState state)
        {
            this.ClientConnected?.Invoke(state);
        }

        private async void OnMessageReceived(string message)
        {
            if (GuessingEnabled)
            {
                await ExtractGuess(message);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
