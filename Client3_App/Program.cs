using System;
using System.ServiceModel;
using System.Text;

class Program
{
    static void Main()
    {
        string serviceAddress = "http://localhost:8000/RobotService";
        var binding = new BasicHttpBinding();
        var endpoint = new EndpointAddress(serviceAddress);
        var factory = new ChannelFactory<RobotServer.IRobotService>(binding, endpoint);
        var channel = factory.CreateChannel();

        string apiKey = "KEY_CLIENT_3_789";
        byte[] sharedKey = Encoding.UTF8.GetBytes("ThisIsA16ByteKey");

        Send("ROTATE", apiKey, sharedKey, channel);
        Console.WriteLine("Enter za izlaz..."); Console.ReadLine();
    }

    static void Send(string command, string apiKey, byte[] sharedKey, RobotServer.IRobotService channel)
    {
        Console.WriteLine($"Šaljem: {command}");
        var enc = RobotServer.AesHelper.Encrypt(command, sharedKey);
        var msg = new RobotServer.CommandMessage { ApiKey = apiKey, EncryptedPayloadBase64 = Convert.ToBase64String(enc) };
        var res = channel.SendCommand(msg);
        Console.WriteLine($"Success={res.Success}, Msg={res.Message}, Pos=({res.State.X},{res.State.Y}) Rot={res.State.RotationDeg}");
    }
}
