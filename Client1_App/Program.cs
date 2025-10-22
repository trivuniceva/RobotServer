using System;
using System.ServiceModel;
using System.Text;

namespace Client1_App
{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceAddress = "http://localhost:8000/RobotService";
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(serviceAddress);
            var factory = new ChannelFactory<RobotServer.IRobotService>(binding, endpoint);
            var channel = factory.CreateChannel();

            string apiKey = "KEY_CLIENT_1_123"; 
            byte[] sharedKey = Encoding.UTF8.GetBytes("ThisIsA16ByteKey");

            try
            {
                Send("MOVE_LEFT", apiKey, sharedKey, channel);
                Send("MOVE_UP", apiKey, sharedKey, channel);
                Send("ROTATE", apiKey, sharedKey, channel);

                Console.WriteLine("Gotovo. Pritisni Enter za izlaz...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška pri pozivu: " + ex.Message);
                Console.ReadLine();
            }
        }

        static void Send(string command, string apiKey, byte[] sharedKey, RobotServer.IRobotService channel)
        {
            Console.WriteLine($"Šaljem komandu: {command}");
            byte[] enc = RobotServer.AesHelper.Encrypt(command, sharedKey);
            string base64 = Convert.ToBase64String(enc);
            var msg = new RobotServer.CommandMessage { ApiKey = apiKey, EncryptedPayloadBase64 = base64 };
            var res = channel.SendCommand(msg);
            Console.WriteLine($"Success={res.Success}, Msg={res.Message}, Pos=({res.State.X},{res.State.Y}) Rot={res.State.RotationDeg}");
        }
    }
}
