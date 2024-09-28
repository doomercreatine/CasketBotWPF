using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CasketChatBot.Models
{
    class ClientModel : ModelBase
    {
        private string _connectionStatus;
        public string ConnectionStatus 
        { 
            get 
            { 
                return _connectionStatus;  
            } 
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        private string _guessingStatus;
        public string GuessingStatus
        {
            get
            {
                return _guessingStatus;
            }
            set
            {
                _guessingStatus = value;
                OnPropertyChanged();
            }
        }
    }
}
