using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CasketChatBot.Core
{
    class WSClient
    {
        #region Properties
        private string _clientId = "";
        private string _clientSecret = "";
        private string _refreshToken = "";
        private string? _accessToken = "";
        private Uri URI = new Uri("wss://irc-ws.chat.twitch.tv:443");
        private string BotName = "";
        private string ChannelName = "";
        private ClientWebSocket? _client;
        #endregion

        #region Delegates
        public delegate void ClientConnectedHandler(WebSocketState state);
        public event ClientConnectedHandler? ClientConnected;

        public delegate void MessageReceivedHandler(string message);
        public event MessageReceivedHandler? MessageReceived;

        public delegate void CommandReceivedHandler(string command, string message);
        public event CommandReceivedHandler? CommandReceived;
        #endregion

        private string[] _commands = ["?start", "?end", "?botcheck", "?winner"];


        public async void Start()
        {
            if (_client != null)
            {
                Console.WriteLine("Disconnecting websocket and attempting to reconnect...");
                try
                {
                    await CloseClient();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }
            }
            
            await ConnectWebSocketAsync(URI);
            //ClientConnected?.Invoke(WebSocketState.Open);

        }
        private async Task<string?> RefreshToken()
        {
            using HttpClient client = new();
            var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/x-www-form-urlencoded"}
                };

            var body = new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", _refreshToken }

                };
            var content = new FormUrlEncodedContent(body);

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                
                Dictionary<string, object>? values = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                //MessageReceived?.Invoke(await response.Content.ReadAsStringAsync());
                return values?["access_token"].ToString();
            }
            else
            {
                Console.WriteLine("Error refreshing token");
                return null;
            }
        }
        public async Task BanUser(string username, int duration)
        {
            string? userId = await GetUserId(username);
            Uri banAPI = new Uri("https://api.twitch.tv/helix/moderation/bans?broadcaster_id=&moderator_id=");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", _clientId);
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            if (userId != null)
            {
                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
                {
                    {"user_id", userId},
                    {"duration", duration }
                };

                string json = await Task.Run(() => JsonConvert.SerializeObject(data));
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.twitch.tv/helix/moderation/bans?broadcaster_id=41783686&moderator_id=793352775", httpContent);

            }
        }
        public async Task<string?> GetUserId(string username)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", _clientId);            

            var response = await client.GetAsync($"https://api.twitch.tv/helix/users?login={username}");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Dictionary<string, object> values = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                var vals = JsonValue.Parse(values["data"].ToString());
                return vals[0]["id"].ToString();
            } else
            {
                Console.WriteLine("Error getting user ID");
                return null;
            }
            
        }
        public async Task<List<string>> GetChatters()
        {
            using HttpClient client = new();
            //string accessToken = await RefreshToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            client.DefaultRequestHeaders.Add("Client-Id", _clientId);
            List<string> chat = new List<string>();
            var response = await client.GetAsync($"https://api.twitch.tv/helix/chat/chatters?broadcaster_id=&moderator_id=");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Dictionary<string, object> values = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                var vals = JsonValue.Parse(values["data"].ToString()).AsArray();
                foreach (var item in vals)
                {
                    chat.Add(item["user_name"].ToString());
                }

            }
            return chat;

        }
        private async Task ConnectWebSocketAsync(Uri serverUri)
        {
            _accessToken = await RefreshToken();
            using (_client = new ClientWebSocket())
            {
                _client.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                try
                {
                    // Connect to the WebSocket server
                    await _client.ConnectAsync(serverUri, CancellationToken.None);
                    ClientConnected?.Invoke(_client.State);
                    // send required params to twitch
                    string reqMessage = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
                    byte[] reqMessaeBytes = Encoding.UTF8.GetBytes(reqMessage);
                    await _client.SendAsync(new ArraySegment<byte>(reqMessaeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    string passMessage = $"PASS oauth:{_accessToken}";
                    byte[] passMessageBytes = Encoding.UTF8.GetBytes(passMessage);
                    await _client.SendAsync(new ArraySegment<byte>(passMessageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    string nickMessage = $"NICK {BotName}";
                    byte[] nickMessageBytes = Encoding.UTF8.GetBytes(nickMessage);
                    await _client.SendAsync(new ArraySegment<byte>(nickMessageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    string joinMessage = $"JOIN #{ChannelName}";
                    byte[] joinMessageBytes = Encoding.UTF8.GetBytes(joinMessage);
                    await _client.SendAsync(new ArraySegment<byte>(joinMessageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine($"Connected to Twitch on channel: #{ChannelName}");
                    Task.Run(() => PingServer());
                    //MessageReceived?.Invoke("@badge-info=subscriber/20;badges=broadcaster/1,subscriber/0;color=#B22222;display-name=DoomerCreatine;emotes=302642909:7-16;first-msg=0;flags=;id=f6378f35-411d-42e2-a50e-cc9247d3cea1;mod=0;returning-chatter=0;room-id=127661354;subscriber=1;tmi-sent-ts=1727388324597;turbo=0;user-id=127661354;user-type= :doomercreatine!doomercreatine@doomercreatine.tmi.twitch.tv PRIVMSG #doomercreatine :123456 jaseCasket");
                    await ReceiveMessagesAsync(_client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        private async Task ReceiveMessagesAsync(ClientWebSocket webSocket)
        {
            byte[] buffer = new byte[1024 * 4]; // 4KB buffer size

            while (webSocket.State == WebSocketState.Open)
            {                
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.Aborted || result.Count == 0)
                {
                    ClientConnected?.Invoke(webSocket.State);
                    await CloseClient();
                    webSocket = null;
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (message.Contains("PRIVMSG"))
                    {
                        MessageReceived?.Invoke(message);
                    } 
                    
                }
            }
        }
        public async Task SendMessagesAsync(string message, string? msgId)
        {
            string msg = $"@reply-parent-msg-id={msgId} PRIVMSG #{ChannelName} :{message}";
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            await _client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async Task CloseClient()
        {
            Console.WriteLine($"Websocket: {_client.State}. Attempting to close.");
            if (_client.State == WebSocketState.Aborted)
            {
                await _client.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
            }
            else
            {
                await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            Console.WriteLine($"Websocket: {_client.State}. After close attempt.");
            ClientConnected?.Invoke(_client.State);
            await ConnectWebSocketAsync(URI);
        }
        private async Task ParseCommand(string message)
        {
            string? command = _commands.FirstOrDefault(x => x == message.Split(' ')[0]);
            if (command != null)
            {
                CommandReceived?.Invoke(command, message);
            }
        }
        private async void PingServer()
        {
            //var stopwatch = Stopwatch.StartNew();
            var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (_client.State == WebSocketState.Open && await periodicTimer.WaitForNextTickAsync())
            {
                //Console.WriteLine($"Sent PING {DateTime.Now}");
                await _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PING :tmi.twitch.tv")), WebSocketMessageType.Text, true, CancellationToken.None);
                
            }
        }
    }
}
