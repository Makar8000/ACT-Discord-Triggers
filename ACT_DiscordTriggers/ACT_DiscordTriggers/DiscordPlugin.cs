using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using System.Speech.Synthesis;
using System.Diagnostics;
using System.Speech.AudioFormat;

namespace ACT_Plugin {
	public class DiscordPlugin : UserControl, IActPluginV1 {
		#region Designer Created Code (Avoid editing)
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.lblBotTok = new System.Windows.Forms.Label();
			this.txtToken = new System.Windows.Forms.TextBox();
			this.lblName = new System.Windows.Forms.Label();
			this.txtUserID = new System.Windows.Forms.TextBox();
			this.logBox = new System.Windows.Forms.TextBox();
			this.lblLog = new System.Windows.Forms.Label();
			this.btnJoin = new System.Windows.Forms.Button();
			this.btnLeave = new System.Windows.Forms.Button();
			this.btnConnect = new System.Windows.Forms.Button();
			this.btnDisconnect = new System.Windows.Forms.Button();
			this.lblServer = new System.Windows.Forms.Label();
			this.lblChannel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// lblBotTok
			// 
			this.lblBotTok.AutoSize = true;
			this.lblBotTok.Location = new System.Drawing.Point(28, 23);
			this.lblBotTok.Name = "lblBotTok";
			this.lblBotTok.Size = new System.Drawing.Size(96, 13);
			this.lblBotTok.TabIndex = 0;
			this.lblBotTok.Text = "Discord Bot Token";
			// 
			// txtToken
			// 
			this.txtToken.Location = new System.Drawing.Point(31, 39);
			this.txtToken.Name = "txtToken";
			this.txtToken.Size = new System.Drawing.Size(207, 20);
			this.txtToken.TabIndex = 1;
			this.txtToken.UseSystemPasswordChar = true;
			// 
			// lblName
			// 
			this.lblName.AutoSize = true;
			this.lblName.Location = new System.Drawing.Point(28, 88);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(57, 13);
			this.lblName.TabIndex = 2;
			this.lblName.Text = "Discord ID";
			// 
			// txtUserID
			// 
			this.txtUserID.Location = new System.Drawing.Point(31, 104);
			this.txtUserID.Name = "txtUserID";
			this.txtUserID.Size = new System.Drawing.Size(207, 20);
			this.txtUserID.TabIndex = 3;
			// 
			// logBox
			// 
			this.logBox.Location = new System.Drawing.Point(31, 176);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.ReadOnly = true;
			this.logBox.Size = new System.Drawing.Size(458, 180);
			this.logBox.TabIndex = 4;
			// 
			// lblLog
			// 
			this.lblLog.AutoSize = true;
			this.lblLog.Location = new System.Drawing.Point(28, 151);
			this.lblLog.Name = "lblLog";
			this.lblLog.Size = new System.Drawing.Size(60, 13);
			this.lblLog.TabIndex = 5;
			this.lblLog.Text = "Debug Log";
			// 
			// btnJoin
			// 
			this.btnJoin.Enabled = false;
			this.btnJoin.Location = new System.Drawing.Point(271, 102);
			this.btnJoin.Name = "btnJoin";
			this.btnJoin.Size = new System.Drawing.Size(106, 23);
			this.btnJoin.TabIndex = 6;
			this.btnJoin.Text = "Join Channel";
			this.btnJoin.UseVisualStyleBackColor = true;
			this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
			// 
			// btnLeave
			// 
			this.btnLeave.Enabled = false;
			this.btnLeave.Location = new System.Drawing.Point(383, 102);
			this.btnLeave.Name = "btnLeave";
			this.btnLeave.Size = new System.Drawing.Size(106, 23);
			this.btnLeave.TabIndex = 7;
			this.btnLeave.Text = "Leave Channel";
			this.btnLeave.UseVisualStyleBackColor = true;
			this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
			// 
			// btnConnect
			// 
			this.btnConnect.Location = new System.Drawing.Point(271, 37);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(106, 23);
			this.btnConnect.TabIndex = 8;
			this.btnConnect.Text = "Connect";
			this.btnConnect.UseVisualStyleBackColor = true;
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// btnDisconnect
			// 
			this.btnDisconnect.Enabled = false;
			this.btnDisconnect.Location = new System.Drawing.Point(383, 37);
			this.btnDisconnect.Name = "btnDisconnect";
			this.btnDisconnect.Size = new System.Drawing.Size(106, 23);
			this.btnDisconnect.TabIndex = 9;
			this.btnDisconnect.Text = "Disconnect";
			this.btnDisconnect.UseVisualStyleBackColor = true;
			this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
			// 
			// lblServer
			// 
			this.lblServer.AutoSize = true;
			this.lblServer.Location = new System.Drawing.Point(268, 21);
			this.lblServer.Name = "lblServer";
			this.lblServer.Size = new System.Drawing.Size(77, 13);
			this.lblServer.TabIndex = 10;
			this.lblServer.Text = "Server Options";
			// 
			// lblChannel
			// 
			this.lblChannel.AutoSize = true;
			this.lblChannel.Location = new System.Drawing.Point(268, 86);
			this.lblChannel.Name = "lblChannel";
			this.lblChannel.Size = new System.Drawing.Size(85, 13);
			this.lblChannel.TabIndex = 11;
			this.lblChannel.Text = "Channel Options";
			// 
			// DiscordPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.lblChannel);
			this.Controls.Add(this.lblServer);
			this.Controls.Add(this.btnDisconnect);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.btnLeave);
			this.Controls.Add(this.btnJoin);
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.logBox);
			this.Controls.Add(this.txtUserID);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "DiscordPlugin";
			this.Size = new System.Drawing.Size(540, 384);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#endregion
		public DiscordPlugin() {
			InitializeComponent();
		}

		Label lblStatus;    // The status label that appears in ACT's Plugin tab
		string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\ACT_DiscordTriggers.config.xml");
		private Label lblBotTok;
		private TextBox txtToken;
		private Label lblName;
		private TextBox txtUserID;
		private TextBox logBox;
		private Label lblLog;
		SettingsSerializer xmlSettings;
		private Button btnJoin;
		private Button btnLeave;
		private Button btnConnect;
		private Button btnDisconnect;
		private DiscordSocketClient bot;
		private IAudioClient audioClient;
		private SpeechAudioFormatInfo formatInfo;
		private AudioOutStream voiceStream;
		private Label lblServer;
		private Label lblChannel;
		private bool botReady;

		#region IActPluginV1 Members
		public async void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			//ACT Stuff
			lblStatus = pluginStatusText;   
			pluginScreenSpace.Controls.Add(this);  
			pluginScreenSpace.Text = "Discord Triggers";
			this.Dock = DockStyle.Fill; 
			xmlSettings = new SettingsSerializer(this);
			LoadSettings();

			//Discord Bot Stuff
			botReady = false;
			voiceStream = null;
			logBox.Text = "";
			formatInfo = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
			bot = new DiscordSocketClient();
			await bot.LoginAsync(TokenType.Bot, txtToken.Text);
			bot.Ready += Bot_Ready;
			bot.Connected += Bot_Connected;
			bot.Disconnected += Bot_Disconnected;

			//More ACT Stuff
			ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;
			lblStatus.Text = "Plugin Started";
			logBox.AppendText("Plugin loaded.\n");
		}

		private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo) {
			if (!botReady || audioClient == null || audioClient.ConnectionState != Discord.ConnectionState.Connected)
				return;
			//logBox.AppendText(logInfo.logLine + "\n");
			foreach (CustomTrigger trig in ActGlobals.oFormActMain.CustomTriggers.Values) {
				if (trig.Active && trig.RegEx.IsMatch(logInfo.logLine)) {
					if (trig.SoundType == 1)
						speak("beep");
					if (trig.SoundType == 2) 
						speak(trig.SoundData);
					if (trig.SoundType == 3)
						speakFile(trig.SoundData);
					break;
				}
			}
		}

		private void speak(string text) {
			SpeechSynthesizer tts = new SpeechSynthesizer();
			MemoryStream ms = new MemoryStream();
			tts.SetOutputToAudioStream(ms, formatInfo);
			if(voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920);
			tts.SpeakAsync(text);
			tts.SpeakCompleted += async (a, b) => {
				ms.Seek(0, SeekOrigin.Begin);
				await ms.CopyToAsync(voiceStream);
				await voiceStream.FlushAsync();
				logBox.AppendText("Completed speaking\n");
			};
		}

		private void speakFile(string filename) {
			SpeechSynthesizer tts = new SpeechSynthesizer();
			MemoryStream ms = new MemoryStream();
			//tts.SelectVoice("Microsoft Zira Desktop");
			tts.SetOutputToAudioStream(ms, formatInfo);
			if (voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920);
			tts.SpeakAsync(filename);
			tts.SpeakCompleted += async (a, b) => {
				ms.Seek(0, SeekOrigin.Begin);
				await ms.CopyToAsync(voiceStream);
				await voiceStream.FlushAsync();
				//logBox.AppendText("Completed speaking\n");
			};
		}

		public void DeInitPlugin() {
			// Unsubscribe from any events you listen to when exiting!
			ActGlobals.oFormActMain.OnLogLineRead -= OFormActMain_OnLogLineRead;
			SaveSettings();
			bot.StopAsync();
			lblStatus.Text = "Plugin Exited";
		}
		#endregion

		private async void btnJoin_Click(object sender, EventArgs e) {
			if (botReady == false || bot.ConnectionState != Discord.ConnectionState.Connected)
				return;
			btnJoin.Enabled = false;
			btnDisconnect.Enabled = false;
			ulong uid;
			if (!UInt64.TryParse(txtUserID.Text, out uid))
				return;
			SocketVoiceChannel chan = null;
			foreach (SocketGuild g in bot.Guilds) {
				if (chan != null)
					break;
				foreach (SocketVoiceChannel v in g.VoiceChannels) {
					if (chan != null)
						break;
					foreach (SocketGuildUser u in v.Users) {
						if (u.Id == uid) {
							chan = bot.GetGuild(g.Id).GetVoiceChannel(v.Id);
							break;
						}
					}
				}
			}
			if (chan != null) {
				audioClient = await chan.ConnectAsync();
				logBox.AppendText("Joined channel: " + chan.Name + "\n");
				btnLeave.Enabled = true;
			}
			else {
				logBox.AppendText("Either you are not in a discord channel, or the bot does not have access to the channel you are connected to.\n");
				btnJoin.Enabled = true;
				btnDisconnect.Enabled = true;
			}
		}

		private void btnLeave_Click(object sender, EventArgs e) {
			if (!botReady || bot.ConnectionState != Discord.ConnectionState.Connected)
				return;
			if (audioClient != null && audioClient.ConnectionState == Discord.ConnectionState.Connected) {
				if(voiceStream != null)
					voiceStream.Close();
				audioClient.StopAsync();
				btnJoin.Enabled = true;
				btnLeave.Enabled = false;
				btnDisconnect.Enabled = true;
				logBox.AppendText("Left channel.\n");
			}
		}

		private async void btnConnect_Click(object sender, EventArgs e) {
			if (bot.ConnectionState != Discord.ConnectionState.Disconnected)
				return;
			await bot.StartAsync();
		}

		private async void btnDisconnect_Click(object sender, EventArgs e) {
			if (!botReady || bot.ConnectionState != Discord.ConnectionState.Connected)
				return;
			await bot.StopAsync();
		}

		private Task Bot_Disconnected(Exception arg) {
			logBox.AppendText("Bot is disconnected.\n");
			btnConnect.Enabled = true;
			btnDisconnect.Enabled = false;
			btnJoin.Enabled = false;
			btnLeave.Enabled = false;
			return Task.CompletedTask;
		}

		private Task Bot_Connected() {
			logBox.AppendText("Bot is connected.\n");
			btnConnect.Enabled = false;
			btnDisconnect.Enabled = true;
			btnJoin.Enabled = true;
			return Task.CompletedTask;
		}

		private async Task Bot_Ready() {
			botReady = true;
			await bot.SetGameAsync("with ACT Triggers");
			logBox.AppendText("Bot is now ready.\n");
		}

		public void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
			xmlSettings.AddControlSetting(txtUserID.Name, txtUserID);

			if (File.Exists(settingsFile)) {
				FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				XmlTextReader xReader = new XmlTextReader(fs);

				try {
					while (xReader.Read()) {
						if (xReader.NodeType == XmlNodeType.Element) {
							if (xReader.LocalName == "SettingsSerializer") {
								xmlSettings.ImportFromXml(xReader);
							}
						}
					}
				} catch (Exception ex) {
					lblStatus.Text = "Error loading settings: " + ex.Message;
				}
				xReader.Close();
			}
		}
		public void SaveSettings() {
			FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
			xWriter.Formatting = Formatting.Indented;
			xWriter.Indentation = 1;
			xWriter.IndentChar = '\t';
			xWriter.WriteStartDocument(true);
			xWriter.WriteStartElement("Config");  
			xWriter.WriteStartElement("SettingsSerializer");  
			xmlSettings.ExportToXml(xWriter);  
			xWriter.WriteEndElement(); 
			xWriter.WriteEndElement(); 
			xWriter.WriteEndDocument(); 
			xWriter.Flush();    
			xWriter.Close();
		}
	}
}
