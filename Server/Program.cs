using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var ip = IPAddress.Loopback;
            var port = 27001;
            var ep = new IPEndPoint(ip, port);

            var clients = new List<ClientInfo>();

            

            var listener = new TcpListener(ep);
            Console.WriteLine("Server");

            try
            {
                listener.Start();
                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    ClientInfo clientInfo = new ClientInfo();
                    clientInfo.IpAdress = client.Client.RemoteEndPoint;
                    clients.Add(clientInfo);
                    _ = Task.Run(() =>
                    {
                        Console.WriteLine($"{client.Client.RemoteEndPoint} is connected");
                        var json = JsonSerializer.Serialize(clients);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        client.Client.Send(bytes);
                    });
                }




            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }



        }
    }
}
