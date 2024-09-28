using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Converters;

namespace CasketChatBot.Models
{
    class GuessesModel : ModelBase
    {
        private string _userName {  get; set; }
        private string _msgId {  get; set; }
        private Int64 _guess {  get; set; }
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
        public string MsgId
        {
            get
            {
                return _msgId;
            }
            set
            {
                _msgId = value;
                OnPropertyChanged();
            }
        }
       public Int64 Guess
        {
            get
            {
                return _guess;
            }
            set
            {
                _guess = value;
                OnPropertyChanged();
            }
        }

        public GuessesModel(string name, Int64 guess, string msgId)
        {
            Guess = guess;
            UserName = name;
            MsgId = msgId;
        }
    }
}
