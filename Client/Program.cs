using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Threading.Tasks;

namespace Client
{
    //readlineler duzgun islemir  yazanda 2 defe yazmaq lazimdir
    //taxta 0 dan basdiyir
    internal class Program
    {
        static bool inGame = false; 

        static void Main(string[] args)
        {
            var ip = IPAddress.Loopback;
            var port = 27001;
            var ep = new IPEndPoint(ip, port);

            var client = new TcpClient();
            Console.WriteLine("Client");

            try
            {
                client.Connect(ep);
                var stream = client.GetStream();
                _ = Task.Run(() => ListenForMessages(stream));

                var ownPort = ((IPEndPoint)client.Client.LocalEndPoint).Port;

                while (true)
                {
                    if (inGame)
                    {
                        Console.Write("Enter position (0-8) to place your mark: ");
                        if (int.TryParse(Console.ReadLine(), out int position) && position >= 0 && position < 9)
                        {
                            SendMove(stream, ownPort, position);
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Enter a position between 0 and 8.");
                        }
                    }
                    else
                    {
                        Console.Write("Enter port for playing (don’t enter your own port): ");
                        if (int.TryParse(Console.ReadLine(), out int selectedPort))
                        {
                            if (selectedPort == ownPort)
                            {
                                Console.WriteLine("You cannot invite yourself. Try again.");
                            }
                            else
                            {
                                SendGameInvitation(stream, ownPort, selectedPort);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid input, please enter a valid port.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void SendGameInvitation(NetworkStream stream, int invitingPort, int targetPort)
        {
            var invitationMessage = $"INVITE:{invitingPort}:{targetPort}";
            var bytes = Encoding.UTF8.GetBytes(invitationMessage);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            Console.WriteLine($"Invitation sent to client on port {targetPort}. Waiting for acceptance...");
        }

        static void SendMove(NetworkStream stream, int playerPort, int position)
        {
            var moveMessage = $"MOVE:{playerPort}:{position}";
            var bytes = Encoding.UTF8.GetBytes(moveMessage);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        static void ListenForMessages(NetworkStream stream)
        {
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine(message);

                        if (message.Contains("Type 'yes' to accept"))
                        {
                            Console.Write("Type 'yes' to accept the game invitation: ");
                            if (Console.ReadLine().Trim().ToLower() == "yes")
                            {
                                stream.Write(Encoding.UTF8.GetBytes("yes"), 0, 3);
                                stream.Flush();
                            }
                        }
                        else if (message.Contains("Game started"))
                        {
                            inGame = true;
                            Console.WriteLine("Game has started! You can now place your moves.");
                        }
                        else if (message.Contains("wins!") || message.Contains("draw"))
                        {
                            inGame = false;
                            Console.WriteLine("Game over!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }
    }
}
