using Client;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Client2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var ip = IPAddress.Loopback;
            var port = 27001;
            var ep = new IPEndPoint(ip, port);
            Console.WriteLine("Client");
            var client = new TcpClient();
            var bytes = new byte[1024];


            try
            {
                client.Connect(ep);
                while (true)
                {
                    var msg = client.Client.Receive(bytes);
                    var str = Encoding.UTF8.GetString(bytes, 0, msg);
                    

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
