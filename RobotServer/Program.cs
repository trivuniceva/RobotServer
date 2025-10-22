using RobotServer.Data;
using RobotServer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;

namespace RobotServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                Console.WriteLine("🔹 Initializing database...");
                Database.SetInitializer(new CreateDatabaseIfNotExists<RobotDbContext>());
                using (var db = new RobotDbContext())
                {
                    db.Database.Initialize(false);
                }
                Console.WriteLine(" Database initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database initialization failed: " + ex.Message);
            }

            string baseAddress = "http://localhost:8000/RobotService";
            using (ServiceHost host = new ServiceHost(typeof(RobotService), new Uri(baseAddress)))
            {
                var smb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (smb == null) {
                    host.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
                } else {
                    smb.IncludeExceptionDetailInFaults = true;
                }

                host.AddServiceEndpoint(typeof(IRobotService), new BasicHttpBinding(), "");
                try
                {
                    host.Open();
                    Console.WriteLine("RobotService is running at " + baseAddress);
                    Console.WriteLine("Press Enter to stop...");
                    Console.ReadLine();
                    host.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to start service: " + ex);
                    Console.WriteLine("Press Enter to exit..."); Console.ReadLine();
                }
            }
        }
    }

    public class RobotService : IRobotService
    {
        private static readonly byte[] SharedKey = Encoding.UTF8.GetBytes("ThisIsA16ByteKey"); // 16 bytes
        private static readonly object ExecLock = new object();
        private static RobotState CurrentState = new RobotState() { X = 2, Y = 2, RotationDeg = 0 };
        private const int GRID = 5;

        
        /*
        private static readonly System.Collections.Generic.Dictionary<string, int> ClientPriority = new System.Collections.Generic.Dictionary<string, int>()
        {
            {"KEY_CLIENT_1_123", 1},
            {"KEY_CLIENT_2_456", 2},
            {"KEY_CLIENT_3_789", 2}
        };
        */

        public OperationResult SendCommand(CommandMessage msg)
        {
            var timestamp = DateTime.UtcNow;
            string apiKey = msg?.ApiKey ?? "";
            string payload;

            var clientType = GetClientType(apiKey);

            try
            {
                payload = DecryptIfNeeded(msg?.EncryptedPayloadBase64);
            }
            catch(Exception ex)
            {
                LogAttempt(apiKey, msg?.EncryptedPayloadBase64 ?? "", false, "Decrypt error: " + ex.Message, timestamp);
                return new OperationResult { Success = false, Message = "Decrypt error" };
            }

            if (clientType == ClientType.Unknown)
            {
                LogAttempt(apiKey, payload, false, "Invalid apiKey", timestamp);
                return new OperationResult { Success = false, Message = "Invalid apiKey", State = new RobotStateDto { X = CurrentState.X, Y = CurrentState.Y, RotationDeg = CurrentState.RotationDeg } };
            }

            if (!IsAllowed(clientType, payload))
            {
                LogAttempt(apiKey, payload, false, "Operation not allowed", timestamp);
                return new OperationResult { Success = false, Message = "Operation not allowed", State = new RobotStateDto { X = CurrentState.X, Y = CurrentState.Y, RotationDeg = CurrentState.RotationDeg } };
            }

            lock (ExecLock)
            {
                bool applied = TryApplyCommand(payload, out string message);
                LogAttempt(apiKey, payload, applied, message, timestamp);
                return new OperationResult
                {
                    Success = applied,
                    Message = message,
                    State = new RobotStateDto { X = CurrentState.X, Y = CurrentState.Y, RotationDeg = CurrentState.RotationDeg }
                };
            }
        }

        /*
        private bool IsOperationAllowed(string apiKey, string payload)
        {
            if(apiKey == "KEY_CLIENT_1_123") return true;
            if(apiKey == "KEY_CLIENT_2_456") return payload != null && payload.StartsWith("MOVE_");
            if(apiKey == "KEY_CLIENT_3_789") return payload == "ROTATE";
            return false;
        }
        */

        private enum ClientType { Unknown = 0, Client1 = 1, Client2 = 2, Client3 = 3 }

        private static readonly Dictionary<string, ClientType> ApiKeyMap =
            new Dictionary<string, ClientType>(StringComparer.Ordinal)
            {
                ["KEY_CLIENT_1"] = ClientType.Client1,
                ["KEY_CLIENT_1_123"] = ClientType.Client1,

                ["KEY_CLIENT_2"] = ClientType.Client2,
                ["KEY_CLIENT_2_123"] = ClientType.Client2,

                ["KEY_CLIENT_3"] = ClientType.Client3,
                ["KEY_CLIENT_3_123"] = ClientType.Client3
            };

        private static ClientType GetClientType(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return ClientType.Unknown;
            return ApiKeyMap.TryGetValue(apiKey, out var type) ? type : ClientType.Unknown;
        }

        private static bool IsAllowed(ClientType ct, string command)
        {
            command = (command ?? "").Trim().ToUpperInvariant();

            switch (ct)
            {
                case ClientType.Client1:
                    return command == "MOVE_LEFT" || command == "MOVE_RIGHT" ||
                           command == "MOVE_UP" || command == "MOVE_DOWN" ||
                           command == "ROTATE";
                case ClientType.Client2:
                    return command.StartsWith("MOVE_");
                case ClientType.Client3:
                    return command == "ROTATE";
                default:
                    return false;
            }
        }


        private bool TryApplyCommand(string cmd, out string message)
        {
            int x = CurrentState.X;
            int y = CurrentState.Y;
            int rot = CurrentState.RotationDeg;

            switch(cmd)
            {
                case "MOVE_LEFT":
                    if(x - 1 < 0) { message = "Out of bounds"; return false; }
                    x -= 1; break;
                case "MOVE_RIGHT":
                    if(x + 1 >= GRID) { message = "Out of bounds"; return false; }
                    x += 1; break;
                case "MOVE_UP":
                    if(y - 2 < 0) { message = "Out of bounds"; return false; }
                    y -= 2; break;
                case "MOVE_DOWN":
                    if(y + 2 >= GRID) { message = "Out of bounds"; return false; }
                    y += 2; break;
                case "ROTATE":
                    rot = (rot + 90) % 360; break;
                default:
                    message = "Unknown command"; return false;
            }

            CurrentState.X = x;
            CurrentState.Y = y;
            CurrentState.RotationDeg = rot;
            message = "OK";
            return true;
        }

        private string DecryptIfNeeded(string base64)
        {
            if(string.IsNullOrEmpty(base64)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(base64);
                var plain = AesHelper.Decrypt(data, SharedKey);
                return plain;
            }
            catch
            {
                return base64;
            }
        }

        private void LogAttempt(string apiKey, string payload, bool success, string message, DateTime time)
        {
            try
            {
                using (var db = new RobotDbContext())
                {
                    db.OperationLogs.Add(new OperationLog
                    {
                        ApiKey = apiKey,
                        Payload = payload,
                        Success = success,
                        Message = message,
                        Timestamp = time
                    });
                    db.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                try
                {
                    string line = $"{time:O}\t{apiKey}\t{payload}\t{success}\t{message}\tDB_ERR:{ex.Message}\r\n";
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "operations.log");
                    File.AppendAllText(path, line);
                }
                catch { }
            }
        }

        private class RobotState { public int X; public int Y; public int RotationDeg; }
    }
}
