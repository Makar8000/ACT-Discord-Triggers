﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace DiscordAPI {
  public static class DiscordClient {
    private static DiscordSocketClient bot;
    private static IAudioClient audioClient;
    private static AudioOutStream voiceStream;
    private static SocketVoiceChannel voiceChannel;
    private static string statusMsg;

    public delegate void BotLoaded();
    public static BotLoaded BotReady;

    public delegate void BotMessage(string message);
    public static BotMessage Log;

    public static async void InIt(string logintoken, string botstatus) {
      try {
        var config = new DiscordSocketConfig {
          GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
        };
        bot = new DiscordSocketClient(config);
      } catch (NotSupportedException ex) {
        Log?.Invoke("Unsupported Operating System.");
        Log?.Invoke(ex.Message);
      }

      try {
        bot.Log += Bot_Log;
        bot.Ready += Bot_Ready;
        statusMsg = botstatus;
        await bot.LoginAsync(TokenType.Bot, logintoken);
        await bot.StartAsync();
      } catch (Exception ex) {
        Log?.Invoke(ex.Message);
        Log?.Invoke("Error connecting to Discord.");
      }
    }

    public static async Task deInIt() {
      bot.Ready -= Bot_Ready;
      bot.Log -= Bot_Log;
      if (audioClient?.ConnectionState == ConnectionState.Connected) {
        LeaveChannel();
      }
      await bot.StopAsync();
      await bot.LogoutAsync();
    }

    public static bool IsConnected() {
      return bot?.ConnectionState == ConnectionState.Connected;
    }

    private static Task Bot_Log(LogMessage arg) {
      if (arg.Message.Equals("Unknown OpCode (Hello)"))
        return Task.CompletedTask;
      Log?.Invoke($"[{arg.Source}] {arg.Message}");
      return Task.CompletedTask;
    }

    private static async Task Bot_Ready() {
      await SetGameAsync(statusMsg);
      Log?.Invoke("Bot in ready state. Populating servers...");
      BotReady?.Invoke();
    }

    public static string[] getServers() {
      List<string> servers = new List<string>();

      try {
        foreach (SocketGuild g in bot.Guilds)
          servers.Add(g.Name);
      } catch (Exception ex) {
        Log?.Invoke("Error loading servers in DiscordAPI#getServers().");
        Log?.Invoke(ex.Message);
      }

      return servers.ToArray();
    }

    public static string[] getChannels(string server) {
      List<string> discordchannels = new List<string>();

      foreach (SocketGuild g in bot.Guilds) {
        if (g.Name == server) {
          var channels = new List<SocketVoiceChannel>(g.VoiceChannels);
          channels.Sort((x, y) => x.Position.CompareTo(y.Position));
          foreach (SocketVoiceChannel channel in channels)
            discordchannels.Add(channel.Name);
          break;
        }
      }

      return discordchannels.ToArray();
    }

    private static SocketVoiceChannel[] getSocketChannels(string server) {
      List<SocketVoiceChannel> discordchannels = new List<SocketVoiceChannel>();

      foreach (SocketGuild g in bot.Guilds) {
        if (g.Name == server) {
          var channels = new List<SocketVoiceChannel>(g.VoiceChannels);
          channels.Sort((x, y) => x.Position.CompareTo(y.Position));
          foreach (SocketVoiceChannel channel in channels)
            discordchannels.Add(channel);
          break;
        }
      }

      return discordchannels.ToArray();
    }

    public static async Task SetGameAsync(string text) {
      await bot?.SetGameAsync(string.IsNullOrWhiteSpace(text)
                              ? "Playing with ACT Triggers"
                              : text.Trim(),
                              null,
                              ActivityType.CustomStatus );
    }

    public static async Task<bool> JoinChannel(string server, string channel) {
      SocketVoiceChannel chan = null;

      foreach (SocketVoiceChannel vchannel in getSocketChannels(server)) {
        if (vchannel.Name == channel) {
          chan = vchannel;
          break;
        }
      }

      if (chan != null) {
        try {
          audioClient = await chan.ConnectAsync();
          voiceChannel = chan;
          Log?.Invoke("Joined channel: " + chan.Name);
        } catch (Exception ex) {
          Log?.Invoke("Error joining channel.");
          Log?.Invoke(ex.Message);
          return false;
        }
      }
      return true;
    }

    public static async void LeaveChannel() {
      voiceStream?.Close();
      voiceStream = null;
      await audioClient.StopAsync();
      await voiceChannel.DisconnectAsync();
    }

    private static object speaklock = new object();
    private static SpeechAudioFormatInfo formatInfo = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);

    public static void Speak(string text, string voice, int vol, int speed) {
      lock (speaklock) {
        if (voiceStream == null)
          voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 128 * 1024);
        SpeechSynthesizer tts = new SpeechSynthesizer();
        tts.SelectVoice(voice);
        tts.Volume = vol * 5;
        tts.Rate = speed - 10;
        MemoryStream ms = new MemoryStream();
        tts.SetOutputToAudioStream(ms, formatInfo);

        tts.Speak(text);
        ms.Seek(0, SeekOrigin.Begin);
        ms.CopyTo(voiceStream);
        voiceStream.Flush();
      }
    }

    public static void SpeakFile(string path) {
      lock (speaklock) {
        if (voiceStream == null)
          voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 128 * 1024);
        try {
          WaveFileReader wav = new WaveFileReader(path);
          WaveFormat waveFormat = new WaveFormat(48000, 16, 2);
          WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(wav);
          WaveFormatConversionStream output = new WaveFormatConversionStream(waveFormat, pcm);
          output.CopyTo(voiceStream);
          voiceStream.Flush();
        } catch (Exception ex) {
          Log?.Invoke("Unable to read file: " + ex.Message);
        }
      }
    }
  }
}
