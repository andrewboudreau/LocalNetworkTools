using System.Net;
using System.Net.Sockets;
using System.Text;

using Spectre.Console;

var mode = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select mode:")
        .AddChoices("Server", "Client"));

if (mode == "Server")
{
    RunServer();
}
else
{
    RunClient();
}

void RunServer()
{
    int port = 13000;
    TcpListener server = new(IPAddress.Any, port);
    server.Start();

    Console.WriteLine("Server is running...");
    Console.WriteLine("Waiting for a connection...");

    // Broadcast presence
    Task.Run(() =>
    {
        using UdpClient udpClient = new();
        IPEndPoint endPoint = new(IPAddress.Broadcast, port);

        while (true)
        {
            byte[] broadcastMessage = Encoding.UTF8.GetBytes("DISCOVER_SERVER");
            udpClient.Send(broadcastMessage, broadcastMessage.Length, endPoint);
            Thread.Sleep(1000);
        }
    });

    while (true)
    {
        using var client = server.AcceptTcpClient();
        using var stream = client.GetStream();
        byte[] buffer = new byte[256];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine("Received: {0}", message);
    }
}

void RunClient()
{
    int port = 13000;
    string serverIp = DiscoverServer(port);

    if (string.IsNullOrEmpty(serverIp))
    {
        Console.WriteLine("Server not found.");
        return;
    }

    using TcpClient client = new(serverIp, port);
    using var stream = client.GetStream();

    Console.WriteLine("Connected to server.");
    Console.WriteLine("Enter a message to send, or type 'exit' to quit:");

    string? message;
    while ((message = Console.ReadLine()) != null && message.ToLower() != "exit")
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", message);
    }

    client.Close();
}

string DiscoverServer(int port)
{
    using UdpClient udpClient = new(port);
    IPEndPoint endPoint = new(IPAddress.Any, port);

    Console.WriteLine("Looking for server...");

    udpClient.Client.ReceiveTimeout = 5000;

    try
    {
        while (true)
        {
            byte[] receivedData = udpClient.Receive(ref endPoint);
            string receivedMessage = Encoding.UTF8.GetString(receivedData);

            if (receivedMessage == "DISCOVER_SERVER")
            {
                return endPoint.Address.ToString();
            }
        }
    }
    catch (SocketException ex)
    {
        return $"Exception: {ex.Message}";
    }
}
