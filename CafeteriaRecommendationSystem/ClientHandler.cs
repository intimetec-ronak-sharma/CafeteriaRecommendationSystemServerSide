using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace CafeteriaRecommendationSystem.ClientHandler
{ 
    internal class ClientHandler
    {
        public static void HandleClient(object obj)
            {

                TcpClient client = (TcpClient)obj;
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[4096];
                int byteCount;

                try
                {
                    while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        string request = Encoding.ASCII.GetString(buffer, 0, byteCount);
                        Console.WriteLine("Received: " + request);
                        string[] requestParts = request.Trim().Split(':');
                        string email = requestParts[0];
                        string action = requestParts.Length > 1 ? requestParts[1] : "";
                        string parameters = requestParts.Length > 2 ? requestParts[2] : "";

                        LoginResult result = Server.LoginUser(email);
                        string response;

                        if (result.Success)
                        {
                            response = $"Login successful as {result.Role} with UserId {result.UserId}. ";
                            response += Server.ExecuteRoleBasedFunctionality(result.Role, action, parameters);
                        }
                        else
                        {
                            response = "Login failed";
                        }

                        byte[] responseData = Encoding.ASCII.GetBytes(response);
                        stream.Write(responseData, 0, responseData.Length);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client Disconnected");
                }
            }
        }
    }
