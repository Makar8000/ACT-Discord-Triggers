// Wire protocol mirror of DiscordAPI/Protocol.cs. Keep both sides in sync.
// PROTOCOL_VERSION here must match ProtocolConstants.Version on the C# side.
//
// Two frame shapes share the outer 4-byte LE length prefix; the first byte of
// the payload tells them apart:
//
//   0x7B ('{')  → UTF-8 JSON, dispatched by op string. Used for everything
//                 except SpeakPcm.
//   0x01        → Binary SpeakPcm, plugin → bridge only:
//                   [0x01]
//                   [reqId u32 LE]
//                   [sampleRate u32 LE]
//                   [bits u8]
//                   [channels u8]
//                   [raw PCM bytes...]   // remainder of payload
//                 Header is 11 bytes. Response is JSON `SpeakResult` with the
//                 matching reqId.
//
// SpeakFile is a normal JSON op carrying a file path; the bridge opens and
// streams the file itself (must be 48 kHz / 16-bit / stereo PCM WAV).

export const PROTOCOL_VERSION = 1 as const;
export const MAX_FRAME_BYTES = 64 * 1024 * 1024;

export const FRAME_JSON_MARKER = 0x7B; // '{'
export const FRAME_BINARY_SPEAK_PCM = 0x01;
export const BINARY_SPEAK_PCM_HEADER_BYTES = 11;

export const Op = {
    Hello: 'Hello', HelloResult: 'HelloResult',
    Init: 'Init', InitResult: 'InitResult',
    Deinit: 'Deinit', DeinitResult: 'DeinitResult',
    IsConnected: 'IsConnected', IsConnectedResult: 'IsConnectedResult',
    GetServers: 'GetServers', GetServersResult: 'GetServersResult',
    GetChannels: 'GetChannels', GetChannelsResult: 'GetChannelsResult',
    SetGame: 'SetGame', SetGameResult: 'SetGameResult',
    JoinChannel: 'JoinChannel', JoinChannelResult: 'JoinChannelResult',
    LeaveChannel: 'LeaveChannel', LeaveChannelResult: 'LeaveChannelResult',
    SpeakPcm: 'SpeakPcm',
    SpeakFile: 'SpeakFile',
    SpeakResult: 'SpeakResult',
    Shutdown: 'Shutdown',
    BotReady: 'BotReady', Log: 'Log', Disconnected: 'Disconnected',
} as const;

export type OpName = typeof Op[keyof typeof Op];

export type LogLevel = 'Info' | 'Warn' | 'Error';

export type ReqId = number | null;

export interface BaseRequest { op: OpName; reqId: ReqId }

export interface HelloRequest        extends BaseRequest { op: 'Hello'; protocolVersion: number }
export interface InitRequest         extends BaseRequest { op: 'Init'; token: string; status: string }
export interface DeinitRequest       extends BaseRequest { op: 'Deinit' }
export interface IsConnectedRequest  extends BaseRequest { op: 'IsConnected' }
export interface GetServersRequest   extends BaseRequest { op: 'GetServers' }
export interface GetChannelsRequest  extends BaseRequest { op: 'GetChannels'; server: string }
export interface SetGameRequest      extends BaseRequest { op: 'SetGame'; text: string }
export interface JoinChannelRequest  extends BaseRequest { op: 'JoinChannel'; server: string; channel: string }
export interface LeaveChannelRequest extends BaseRequest { op: 'LeaveChannel' }
export interface SpeakFileRequest    extends BaseRequest { op: 'SpeakFile'; path: string }
export interface ShutdownRequest     extends BaseRequest { op: 'Shutdown' }

export type Request =
    | HelloRequest | InitRequest | DeinitRequest | IsConnectedRequest
    | GetServersRequest | GetChannelsRequest | SetGameRequest
    | JoinChannelRequest | LeaveChannelRequest | SpeakFileRequest | ShutdownRequest;

export interface HelloResponse        { op: 'HelloResult';        reqId: ReqId; ok: boolean; bridgeVersion: string; error: string }
export interface InitResponse         { op: 'InitResult';         reqId: ReqId; ok: boolean; error: string }
export interface DeinitResponse       { op: 'DeinitResult';       reqId: ReqId; ok: true;    error: '' }
export interface IsConnectedResponse  { op: 'IsConnectedResult';  reqId: ReqId; connected: boolean }
export interface GetServersResponse   { op: 'GetServersResult';   reqId: ReqId; servers: string[] }
export interface GetChannelsResponse  { op: 'GetChannelsResult';  reqId: ReqId; channels: string[] }
export interface SetGameResponse      { op: 'SetGameResult';      reqId: ReqId; ok: true;    error: '' }
export interface JoinChannelResponse  { op: 'JoinChannelResult';  reqId: ReqId; ok: boolean; error: string }
export interface LeaveChannelResponse { op: 'LeaveChannelResult'; reqId: ReqId; ok: true;    error: '' }
export interface SpeakResponse        { op: 'SpeakResult';        reqId: ReqId; ok: boolean; error: string }

// Generic shape used for the catch-all error response in pipe-server. Matches
// C# OkResponse: any *Result op with reqId/ok/error fields parses as this.
export interface ErrorResponse { op: OpName; reqId: ReqId; ok: false; error: string }

export type Response =
    | HelloResponse | InitResponse | DeinitResponse | IsConnectedResponse
    | GetServersResponse | GetChannelsResponse | SetGameResponse
    | JoinChannelResponse | LeaveChannelResponse | SpeakResponse;

export interface BotReadyNotification     { op: 'BotReady' }
export interface LogNotification          { op: 'Log'; level: LogLevel; message: string }
export interface DisconnectedNotification { op: 'Disconnected'; reason: string }

export type Notification = BotReadyNotification | LogNotification | DisconnectedNotification;

export type OutboundFrame = Response | ErrorResponse | Notification;
