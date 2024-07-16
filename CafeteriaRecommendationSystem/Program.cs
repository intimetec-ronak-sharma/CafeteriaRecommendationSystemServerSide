using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CafeteriaRecommendationSystem
{
    internal class Program
    {
        public static TcpListener listener;

        public static void Main()
        {
            listener = new TcpListener(IPAddress.Any, 5001);
            listener.Start();
            Console.WriteLine("Server started on port 5001");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client Connected");
                Thread clientThread = new Thread(ClientHandler.ClientHandler.HandleClient);
                clientThread.Start(client);
            }
        }
    }
}
