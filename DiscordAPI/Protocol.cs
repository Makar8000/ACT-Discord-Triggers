using System.Text.Json.Serialization;

namespace DiscordBridge.Protocol {

    public static class ProtocolConstants {
        public const int Version = 1;
    }

    public static class Op {
        public const string Hello = "Hello";
        public const string HelloResult = "HelloResult";
        public const string Init = "Init";
        public const string InitResult = "InitResult";
        public const string Deinit = "Deinit";
        public const string DeinitResult = "DeinitResult";
        public const string IsConnected = "IsConnected";
        public const string IsConnectedResult = "IsConnectedResult";
        public const string GetServers = "GetServers";
        public const string GetServersResult = "GetServersResult";
        public const string GetChannels = "GetChannels";
        public const string GetChannelsResult = "GetChannelsResult";
        public const string SetGame = "SetGame";
        public const string SetGameResult = "SetGameResult";
        public const string JoinChannel = "JoinChannel";
        public const string JoinChannelResult = "JoinChannelResult";
        public const string LeaveChannel = "LeaveChannel";
        public const string LeaveChannelResult = "LeaveChannelResult";
        public const string SpeakPcm = "SpeakPcm";
        public const string SpeakFile = "SpeakFile";
        public const string SpeakResult = "SpeakResult";
        public const string Shutdown = "Shutdown";

        public const string BotReady = "BotReady";
        public const string Log = "Log";
        public const string Disconnected = "Disconnected";
    }

    public class HelloRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Hello;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("protocolVersion")] public int ProtocolVersion { get; set; }
    }

    public class DeinitRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Deinit;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
    }

    public class IsConnectedRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.IsConnected;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
    }

    public class GetServersRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.GetServers;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
    }

    public class LeaveChannelRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.LeaveChannel;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
    }

    public class ShutdownRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Shutdown;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
    }

    public class HelloResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.HelloResult;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("bridgeVersion")] public string BridgeVersion { get; set; } = "";
        [JsonPropertyName("error")] public string Error { get; set; } = "";
    }

    public class InitRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Init;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("token")] public string Token { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "";
    }

    public class GetChannelsRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.GetChannels;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("server")] public string Server { get; set; } = "";
    }

    public class GetChannelsResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.GetChannelsResult;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("channels")] public string[] Channels { get; set; } = new string[0];
    }

    public class GetServersResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.GetServersResult;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("servers")] public string[] Servers { get; set; } = new string[0];
    }

    public class IsConnectedResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.IsConnectedResult;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("connected")] public bool Connected { get; set; }
    }

    public class SetGameRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.SetGame;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }

    public class JoinChannelRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.JoinChannel;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("server")] public string Server { get; set; } = "";
        [JsonPropertyName("channel")] public string Channel { get; set; } = "";
    }

    public class JoinChannelResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.JoinChannelResult;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("error")] public string Error { get; set; } = "";
    }

    // SpeakPcm is sent as a length-prefixed BINARY frame, not JSON.
    // See PipeClient.SendSpeakPcmAsync / pipe-server.ts _handleBinarySpeakPcm.
    // Layout (after the outer 4-byte LE length): [0x01][reqId u32 LE][sampleRate u32 LE][bits u8][channels u8][raw PCM...]
    // Response stays JSON: { op:"SpeakResult", reqId, ok, error }.

    public class SpeakFileRequest {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.SpeakFile;
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("path")] public string Path { get; set; } = "";
    }

    public class OkResponse {
        [JsonPropertyName("op")] public string Op { get; set; } = "";
        [JsonPropertyName("reqId")] public int? ReqId { get; set; }
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("error")] public string Error { get; set; } = "";
    }

    public class LogNotification {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Log;
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("level")] public string Level { get; set; } = "Info";
    }

    public class BotReadyNotification {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.BotReady;
    }

    public class DisconnectedNotification {
        [JsonPropertyName("op")] public string Op { get; set; } = Protocol.Op.Disconnected;
        [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    }
}
