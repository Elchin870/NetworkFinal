namespace Server;
public class ClientInfo
{
    public string IPAddress { get; set; }
    public int Port { get; set; }

    public ClientInfo(string ipAddress, int port)
    {
        IPAddress = ipAddress;
        Port = port;
    }

    public override string ToString()
    {
        return $"{IPAddress}:{Port}";
    }
}
