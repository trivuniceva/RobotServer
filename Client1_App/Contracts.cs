
using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace RobotServer
{
    [ServiceContract]
    public interface IRobotService
    {
        [OperationContract]
        OperationResult SendCommand(CommandMessage msg);
    }

    [DataContract]
    public class CommandMessage
    {
        [DataMember] public string ApiKey { get; set; }
        [DataMember] public string EncryptedPayloadBase64 { get; set; }
    }

    [DataContract]
    public class OperationResult
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public RobotStateDto State { get; set; }
    }

    [DataContract]
    public class RobotStateDto
    {
        [DataMember] public int X { get; set; }
        [DataMember] public int Y { get; set; }
        [DataMember] public int RotationDeg { get; set; }
    }
}
