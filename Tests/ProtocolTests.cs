using DiscordBridge.Protocol;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ActDiscordTriggers.Tests {
    public class ProtocolTests {
        private static readonly JsonSerializerOptions opts = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        [Fact]
        public void HelloRequest_serializes_with_op_and_protocolVersion() {
            var req = new HelloRequest { ReqId = 1, ProtocolVersion = ProtocolConstants.Version };
            string json = JsonSerializer.Serialize(req, opts);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Hello", doc.RootElement.GetProperty("op").GetString());
            Assert.Equal(1, doc.RootElement.GetProperty("reqId").GetInt32());
            Assert.Equal(ProtocolConstants.Version, doc.RootElement.GetProperty("protocolVersion").GetInt32());
        }

        [Fact]
        public void HelloResponse_round_trips() {
            var resp = new HelloResponse { ReqId = 2, Ok = true, BridgeVersion = "1.0.0", Error = "" };
            string json = JsonSerializer.Serialize(resp, opts);
            var back = JsonSerializer.Deserialize<HelloResponse>(json);
            Assert.True(back.Ok);
            Assert.Equal(2, back.ReqId);
            Assert.Equal("1.0.0", back.BridgeVersion);
        }

        [Fact]
        public void Notification_omits_reqId_when_null() {
            var note = new BotReadyNotification();
            string json = JsonSerializer.Serialize(note, opts);
            Assert.DoesNotContain("reqId", json);
            Assert.Contains("\"op\":\"BotReady\"", json);
        }

        [Fact]
        public void LogNotification_carries_message_and_level() {
            var note = new LogNotification { Message = "boom", Level = "Error" };
            string json = JsonSerializer.Serialize(note, opts);
            var back = JsonSerializer.Deserialize<LogNotification>(json);
            Assert.Equal("boom", back.Message);
            Assert.Equal("Error", back.Level);
            Assert.Equal("Log", back.Op);
        }

        [Fact]
        public void SpeakFileRequest_serializes_with_path() {
            var req = new SpeakFileRequest { ReqId = 9, Path = @"C:\sounds\alert.wav" };
            string json = JsonSerializer.Serialize(req, opts);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("SpeakFile", doc.RootElement.GetProperty("op").GetString());
            Assert.Equal(9, doc.RootElement.GetProperty("reqId").GetInt32());
            Assert.Equal(@"C:\sounds\alert.wav", doc.RootElement.GetProperty("path").GetString());
        }

        [Fact]
        public void OkResponse_with_caller_supplied_op_keeps_op_in_json() {
            var resp = new OkResponse { Op = Op.DeinitResult, ReqId = 3, Ok = true };
            string json = JsonSerializer.Serialize(resp, opts);
            Assert.Contains("\"op\":\"DeinitResult\"", json);
            Assert.Contains("\"reqId\":3", json);
        }

        [Fact]
        public void GetChannelsResponse_with_empty_array_serializes_as_empty_array() {
            var resp = new GetChannelsResponse { ReqId = 5, Channels = new string[0] };
            string json = JsonSerializer.Serialize(resp, opts);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("channels").ValueKind);
            Assert.Equal(0, doc.RootElement.GetProperty("channels").GetArrayLength());
        }

        [Fact]
        public void JoinChannelResponse_with_error_carries_error_string() {
            var resp = new JoinChannelResponse { ReqId = 7, Ok = false, Error = "channel not found" };
            string json = JsonSerializer.Serialize(resp, opts);
            var back = JsonSerializer.Deserialize<JoinChannelResponse>(json);
            Assert.False(back.Ok);
            Assert.Equal("channel not found", back.Error);
        }

        [Fact]
        public void All_op_constants_are_distinct() {
            var ops = typeof(Op).GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null))
                .ToList();
            Assert.NotEmpty(ops);
            Assert.Equal(ops.Count, ops.Distinct().Count());
        }

        [Fact]
        public void Every_request_op_has_a_paired_result_op() {
            var ops = new System.Collections.Generic.HashSet<string>(
                typeof(Op).GetFields()
                    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                    .Select(f => (string)f.GetValue(null)));
            string[] requestsExpectingResult = {
                Op.Hello, Op.Init, Op.Deinit, Op.IsConnected,
                Op.GetServers, Op.GetChannels, Op.SetGame,
                Op.JoinChannel, Op.LeaveChannel,
            };
            foreach (var req in requestsExpectingResult) {
                Assert.Contains(req + "Result", ops);
            }
            // SpeakPcm and SpeakFile both reply with Op.SpeakResult, not "<Op>Result".
            Assert.Contains(Op.SpeakResult, ops);
            Assert.Contains(Op.SpeakPcm, ops);
            Assert.Contains(Op.SpeakFile, ops);
        }

        [Fact]
        public void ProtocolVersion_is_positive() {
            Assert.True(ProtocolConstants.Version > 0);
        }
    }
}
