using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static List<TcpClient> tcpClients = new List<TcpClient>();
        static List<ClientInfo> connectedClients = new List<ClientInfo>();
        static Dictionary<int, int> activeGames = new Dictionary<int, int>();
        static char[] board = Enumerable.Repeat(' ', 9).ToArray();
        static int currentPlayer = 0; 
        static int[] gamePorts = new int[2]; 

        static void Main(string[] args)
        {
            var ip = IPAddress.Loopback;
            var port = 27001;
            var ep = new IPEndPoint(ip, port);

            var listener = new TcpListener(ep);
            Console.WriteLine("Server");

            try
            {
                listener.Start();

                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    var clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                    lock (connectedClients)
                    {
                        tcpClients.Add(client);
                        connectedClients.Add(new ClientInfo(clientEndPoint.Address.ToString(), clientEndPoint.Port));
                    }

                    Console.WriteLine($"{clientEndPoint.Address}:{clientEndPoint.Port} is connected");

                    _ = Task.Run(() => HandleClient(client));
                    _ = Task.Run(() => BroadcastClientList());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (receivedData.StartsWith("INVITE:"))
                    {
                        var parts = receivedData.Split(':');
                        int invitingPort = int.Parse(parts[1]);
                        int targetPort = int.Parse(parts[2]);
                        SendInvitationToClient(targetPort, invitingPort);
                    }
                    else if (receivedData == "yes")
                    {
                        StartGame();
                    }
                    else if (receivedData.StartsWith("MOVE:"))
                    {
                        var parts = receivedData.Split(':');
                        int playerPort = int.Parse(parts[1]);
                        int position = int.Parse(parts[2]);
                        ProcessMove(playerPort, position);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client disconnected: {ex.Message}");
                tcpClients.Remove(client);
            }
        }

        static void SendInvitationToClient(int targetPort, int invitingPort)
        {
            lock (tcpClients)
            {
                var targetClient = tcpClients.FirstOrDefault(c =>
                    ((IPEndPoint)c.Client.RemoteEndPoint).Port == targetPort);

                if (targetClient != null)
                {
                    var message = $"Client on port {invitingPort} is inviting you to a game! Type 'yes' to accept.";
                    var bytes = Encoding.UTF8.GetBytes(message);

                    try
                    {
                        NetworkStream stream = targetClient.GetStream();
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();
                        Console.WriteLine($"Invitation sent from {invitingPort} to {targetPort}");

                        activeGames[invitingPort] = targetPort;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send invitation: {ex.Message}");
                        tcpClients.Remove(targetClient);
                    }
                }
                else
                {
                    Console.WriteLine($"Client on port {targetPort} not found.");
                }
            }
        }

        static void StartGame()
        {
            board = Enumerable.Repeat(' ', 9).ToArray();

            if (activeGames.Count >= 1)
            {
                var firstGame = activeGames.First();
                gamePorts[0] = firstGame.Key;
                gamePorts[1] = firstGame.Value;
                currentPlayer = 0;

                BroadcastToGameClients("Game started! Here's the initial board:");
                BroadcastBoard();
            }
        }

        static void ProcessMove(int playerPort, int position)
        {
            if (playerPort == gamePorts[currentPlayer] && board[position] == ' ')
            {
                board[position] = currentPlayer == 0 ? 'X' : 'O';
                BroadcastBoard();

                if (CheckWin())
                {
                    BroadcastToGameClients($"Player {currentPlayer + 1} ({(currentPlayer == 0 ? 'X' : 'O')}) wins!");
                    ResetGame();
                }
                else if (CheckDraw())
                {
                    BroadcastToGameClients("The game is a draw!");
                    ResetGame();
                }
                else
                {
                    currentPlayer = 1 - currentPlayer; 
                    BroadcastToGameClients($"Player {currentPlayer + 1}'s turn!");
                }
            }
        }

        static void BroadcastBoard()
        {
            var boardState = $"Board:\n" +
                             $"{board[0]} | {board[1]} | {board[2]}\n" +
                             $"---------\n" +
                             $"{board[3]} | {board[4]} | {board[5]}\n" +
                             $"---------\n" +
                             $"{board[6]} | {board[7]} | {board[8]}";
            BroadcastToGameClients(boardState);
        }

        static void BroadcastToGameClients(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            foreach (var tcpClient in tcpClients.Where(c => gamePorts.Contains(((IPEndPoint)c.Client.RemoteEndPoint).Port)))
            {
                try
                {
                    NetworkStream stream = tcpClient.GetStream();
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting to client: {ex.Message}");
                    tcpClients.Remove(tcpClient);
                }
            }
        }

        static bool CheckWin()
        {
            int[][] winningCombinations = new int[][]
            {
                new int[] {0, 1, 2}, new int[] {3, 4, 5}, new int[] {6, 7, 8},
                new int[] {0, 3, 6}, new int[] {1, 4, 7}, new int[] {2, 5, 8},
                new int[] {0, 4, 8}, new int[] {2, 4, 6}
            };

            return winningCombinations.Any(combo =>
                board[combo[0]] != ' ' &&
                board[combo[0]] == board[combo[1]] &&
                board[combo[1]] == board[combo[2]]);
        }

        static bool CheckDraw() => board.All(cell => cell != ' ');

        static void ResetGame()
        {
            activeGames.Remove(gamePorts[0]);
            activeGames.Remove(gamePorts[1]);
            gamePorts = new int[2];
            board = Enumerable.Repeat(' ', 9).ToArray();
            currentPlayer = 0;
        }

        static void BroadcastClientList()
        {
            var clientList = string.Join(", ", connectedClients.Select(c => $"{c.IPAddress}:{c.Port}"));
            var bytes = Encoding.UTF8.GetBytes($"Connected clients: {clientList}");
            lock (tcpClients)
            {
                foreach (var tcpClient in tcpClients.ToList())
                {
                    try
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        tcpClients.Remove(tcpClient);
                    }
                }
            }
        }
    }
}
