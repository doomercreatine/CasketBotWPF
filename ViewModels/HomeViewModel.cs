using CasketChatBot.Core;
using CasketChatBot.Models;
using NSwag.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CasketChatBot.ViewModels
{
    class HomeViewModel : ViewModelBase
    {
        private ObservableDictionary<string, Int64> _guesses;
        public ObservableDictionary<string, Int64> Guesses { get { return _guesses; } set { _guesses = value; OnPropertyChanged(); } }
        private ObservableDictionary<string, int> _winners;
        public ObservableDictionary<string, int> Winners { get { return _winners; } set { _winners = value; OnPropertyChanged(); } }

        private Dictionary<string, int> _numGuesses = new Dictionary<string, int>();
        public RelayCommand EnableGuessing { get; set; }
        public RelayCommand DisableGuessing { get; set; }
        public RelayCommand SelectWinner { get; set; }
        private string _guessing { get; set; } = "Closed";
        public string Guessing
        {
            get
            {
                return _guessing;
            }
            set
            {
                _guessing = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand Ban5min { get; set; }
        public RelayCommand UpdateTodayRelay { get; set; }
        public RelayCommand SendUpdateToday { get; set; }
        public RelayCommand TestBot { get; set; }
        public RelayCommand ReconnectBot { get; set; }
        public delegate void TodayCommandHandler(string todayCommand);
        private string _todayCommandText { get; set; } = "!editcom !today Nothing yet jaseDespair";
        public string TodayCommandText
        {
            get
            {
                return _todayCommandText;
            }
            set
            {
                _todayCommandText = value;
                OnPropertyChanged();
            }
        }
        private TwitchClient twitchClient;
        private WebSocketState _state;
        public WebSocketState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnPropertyChanged();                
            }
        }
        public HomeViewModel()        
        {
            Guesses = new ObservableDictionary<string, long>();
            Winners = new ObservableDictionary<string, int>();
            twitchClient = new TwitchClient();
            EnableGuessing = new RelayCommand(o =>
            {
                Guesses.Clear();
                _numGuesses.Clear();
                twitchClient.StartGuessing();
                Guessing = "Enabled";
            });
            UpdateTodayRelay = new RelayCommand(o =>
            {
                UpdateToday();
            });
            SendUpdateToday = new RelayCommand(o =>
            {
                twitchClient.Send(Convert.ToString(o));
            });
            DisableGuessing = new RelayCommand(o =>
            {
                twitchClient.StopGuessing();
                Guessing = "Closed";
            });

            SelectWinner = new RelayCommand(o =>
            {   
                FindWinnerCommand(o);
            });

            Ban5min = new RelayCommand(o =>
            {
                BanUser(o);
            });
            TestBot = new RelayCommand(o =>
            {
                twitchClient.Send("I'm here!!!");
            });
            ReconnectBot = new RelayCommand(o =>
            {
                twitchClient.Start();
            });

            twitchClient.ClientConnected += OnStateChanged;
            twitchClient.GuessReceived += OnGuessReceived;
            twitchClient.Start();     
        }
        private ObservableCollection<SimpleClass> _casketValues;
        public ObservableCollection<SimpleClass> CasketValues
        {

            get { return _casketValues ?? (_casketValues = new ObservableCollection<SimpleClass>()); }

            set { _casketValues = value; OnPropertyChanged(); }
        }
        private ObservableCollection<SimpleClass> _specialLoot;
        public ObservableCollection<SimpleClass> SpecialLoot
        {

            get { return _specialLoot ?? (_specialLoot = new ObservableCollection<SimpleClass>()); }

            set { _specialLoot = value; OnPropertyChanged(); }
        }
        public void UpdateToday()
        {
            List<KeyValuePair<Int64, string>> caskets = new List<KeyValuePair<Int64, string>>();

            foreach (SimpleClass sClass in CasketValues)
            {
                if (!string.IsNullOrEmpty(sClass.Quantity)) { caskets.Add(new KeyValuePair<Int64, string>(Convert.ToInt64(sClass.Quantity), sClass.ItemName)); }
            }
            string prefix = "!editcom !today";
            int numCaskets = caskets.Count;
            List<Int64> casketValuesInt = (from kvp in caskets select kvp.Key).ToList();
            List<string> casketValues = new List<string>();
            foreach (KeyValuePair<Int64, string> casket in caskets)
            {
                string v = "";
                if (casket.Key > 1e6 && casket.Key < 1e9)
                {
                    v = string.Format("{0:0.0}m", (float)casket.Key / 1e6);
                }
                else if (casket.Key > 1e9)
                {
                    v = string.Format("{0:0.0}b", (float)casket.Key / 1e9);
                }
                else
                {
                    v = string.Format("{0:0}k", (float)casket.Key / 1e3);
                }
                if (!string.IsNullOrEmpty(casket.Value))
                {
                    v = $"{v} ({casket.Value})";
                }
                casketValues.Add(v);
            }
            List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
            foreach (SimpleClass sClass2 in SpecialLoot) { 

                if (!string.IsNullOrEmpty(sClass2.ItemName)) items.Add(new KeyValuePair<string, int>(sClass2.ItemName, Convert.ToInt32(sClass2.Quantity)));
            }
            List<string> specialItems = new List<string>();
            foreach (KeyValuePair<string, int> item in items)
            {
                if (string.IsNullOrEmpty(item.Key)) { continue; }
                string v = $"{item.Value}x {item.Key}";
                specialItems.Add(v);
            }

            string todayCommand = $"{prefix} Nothing yet jaseDespair";
            if (items.Count == 0 && casketValues.Count > 0)
            {
                todayCommand = $"{prefix} {numCaskets}x Master clues ({System.String.Join(", ", casketValues)}) jaseHappy";
            }
            else if (items.Count > 0 && casketValues.Count > 0)
            {
                todayCommand = $"{prefix} {numCaskets}x Master clues ({System.String.Join(", ", casketValues)}), {System.String.Join(", ", specialItems)} jaseHappy";
            }
            else if (items.Count > 0 && casketValues.Count == 0)
            {
                todayCommand = $"{prefix} {System.String.Join(", ", specialItems)} jaseHappy";
            }
            
            TodayCommandText = todayCommand;
        }
    
    
        private void BanUser(object username)
        {
            twitchClient.Ban(Convert.ToString(username), 21600);
        }
        private async void FindWinnerCommand(object o)
        {
            if (string.IsNullOrEmpty(o.ToString()))
            {
                MessageBox.Show("Please enter a casket value");
                return;
            }
            Int64 casketValue = Convert.ToInt64(o);
            if (twitchClient.GuessingEnabled)
            {
                MessageBox.Show("You haven't ended guessing yet.");
                return;
            }
            if (Guesses.Count == 0)
            {
                MessageBox.Show("There were no guesses logged.");
                return;
            }
            else
            {
                
                var keyR = Guesses.Min(kvp => Math.Abs(kvp.Value - casketValue));
                var winners = Guesses.Where(kvp => Math.Abs(kvp.Value - casketValue) == keyR);
                List<string> winnerNames = new List<string>();
                int diff = 0;
                List<int> wins = new List<int>();
                foreach (var kvp in winners)
                {
                    winnerNames.Add(kvp.Key);
                    Console.WriteLine(string.Join(", ", winnerNames));
                    diff = Convert.ToInt32(Math.Abs(Guesses[kvp.Key] - casketValue));
                    if (Winners.Keys.Contains(kvp.Key))
                    {
                        Winners[kvp.Key]++;
                    }
                    else
                    {
                        Winners[kvp.Key] = 1;
                    }
                    wins.Add(DataBaseManager.GetWinCounts(kvp.Key));
                    DataBaseManager.InsertGuesses(Guesses, casketValue, kvp.Key);
                }
                List<string> guessAmounts = new List<string>();
                foreach (var kvp in winners)
                {
                    guessAmounts.Add($"{kvp.Value:n0}");
                }
                


                SimpleClass casket = new SimpleClass();
                casket.Quantity = casketValue.ToString();
                CasketValues.Add(casket);
                UpdateToday();
                await twitchClient.Send($"Winner was {string.Join(", ", winnerNames)} Clap [difference: {diff:n0}]. Wins: {string.Join(", ", wins)}");
                
            }
        }
        private async void OnGuessReceived(GuessesModel guess)
        {
            Console.WriteLine(guess.UserName);
            if (Guesses.ContainsKey(guess.UserName))
            {
                if (_numGuesses[guess.UserName] == 1)
                {
                    await twitchClient.Send($"You already guessed {Guesses[guess.UserName]:n0}, if you want to keep {guess.Guess:n0} send it again with AreYouSerious at the end.", guess.MsgId);
                    _numGuesses[guess.UserName]++;
                }
                else if (_numGuesses[guess.UserName] > 1)
                {
                    return;
                }
            }
            else
            {
                Guesses.Add(guess.UserName, guess.Guess);
                _numGuesses.Add(guess.UserName, 1);
            }

        }
        private void OnStateChanged(WebSocketState state)
        {
            State = state;
            //Console.WriteLine($"{state} from HomeViewModel");
        }
    }
public class SimpleClass
{
    public string ItemName { get; set; }
    public string Quantity { get; set; }
    public SimpleClass()
    {

    }
}
}
