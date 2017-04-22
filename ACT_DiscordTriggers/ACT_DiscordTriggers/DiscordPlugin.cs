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
			this.lblName.Location = new System.Drawing.Point(28, 62);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(57, 13);
			this.lblName.TabIndex = 2;
			this.lblName.Text = "Discord ID";
			// 
			// txtUserID
			// 
			this.txtUserID.Location = new System.Drawing.Point(31, 78);
			this.txtUserID.Name = "txtUserID";
			this.txtUserID.Size = new System.Drawing.Size(207, 20);
			this.txtUserID.TabIndex = 3;
			// 
			// logBox
			// 
			this.logBox.BackColor = System.Drawing.SystemColors.Control;
			this.logBox.Location = new System.Drawing.Point(31, 135);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.ReadOnly = true;
			this.logBox.Size = new System.Drawing.Size(337, 180);
			this.logBox.TabIndex = 4;
			// 
			// lblLog
			// 
			this.lblLog.AutoSize = true;
			this.lblLog.Location = new System.Drawing.Point(28, 110);
			this.lblLog.Name = "lblLog";
			this.lblLog.Size = new System.Drawing.Size(60, 13);
			this.lblLog.TabIndex = 5;
			this.lblLog.Text = "Debug Log";
			// 
			// btnJoin
			// 
			this.btnJoin.Enabled = false;
			this.btnJoin.Location = new System.Drawing.Point(262, 39);
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
			this.btnLeave.Location = new System.Drawing.Point(262, 68);
			this.btnLeave.Name = "btnLeave";
			this.btnLeave.Size = new System.Drawing.Size(106, 23);
			this.btnLeave.TabIndex = 7;
			this.btnLeave.Text = "Leave Channel";
			this.btnLeave.UseVisualStyleBackColor = true;
			this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
			// 
			// lblChannel
			// 
			this.lblChannel.AutoSize = true;
			this.lblChannel.Location = new System.Drawing.Point(259, 23);
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
			this.Controls.Add(this.btnLeave);
			this.Controls.Add(this.btnJoin);
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.logBox);
			this.Controls.Add(this.txtUserID);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "DiscordPlugin";
			this.Size = new System.Drawing.Size(402, 338);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#endregion
		public DiscordPlugin() {
			InitializeComponent();
		}

		Label lblStatus;
		string settingsFile;
		private Label lblBotTok;
		private TextBox txtToken;
		private Label lblName;
		private TextBox txtUserID;
		private TextBox logBox;
		private Label lblLog;
		SettingsSerializer xmlSettings;
		private Button btnJoin;
		private Button btnLeave;
		private DiscordSocketClient bot;
		private IAudioClient audioClient;
		private SpeechAudioFormatInfo formatInfo;
		private AudioOutStream voiceStream;
		private Label lblChannel;

		#region IActPluginV1 Members
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			//ACT Stuff
			lblStatus = pluginStatusText;
			settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\ACT_DiscordTriggers.config.xml");
			pluginScreenSpace.Controls.Add(this);
			pluginScreenSpace.Text = "Discord Triggers";
			this.Dock = DockStyle.Fill;
			xmlSettings = new SettingsSerializer(this);
			LoadSettings();

			//Discord Bot Stuff
			voiceStream = null;
			logBox.Text = "";
			formatInfo = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
			bot = new DiscordSocketClient();
			try {
				bot.LoginAsync(TokenType.Bot, txtToken.Text);
				bot.LoggedIn += Bot_LoggedIn;
				bot.Ready += Bot_Ready;
				logBox.AppendText("Plugin loaded successfully.\n");
			} catch (Exception ex) {
				logBox.Text = "Error connecting bot. Make sure your Bot Token is correct then restart the plugin (Go to \"Plugin Listing\" tab, uncheck \"Enabled\" and then check it again).";
			}
			lblStatus.Text = "Plugin Started";
		}

		public async void DeInitPlugin() {
			ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
			SaveSettings();
			bot.Ready -= Bot_Ready;
			bot.LoggedIn -= Bot_LoggedIn;
			await bot.StopAsync();
			await bot.LogoutAsync();
			lblStatus.Text = "Plugin Exited";
		}
		#endregion

		#region Discord Methods
		private void speak(string text) {
			SpeechSynthesizer tts = new SpeechSynthesizer();
			MemoryStream ms = new MemoryStream();
			tts.SetOutputToAudioStream(ms, formatInfo);
			if (voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920);
			tts.SpeakAsync(text);
			tts.SpeakCompleted += (a, b) => {
				ms.Seek(0, SeekOrigin.Begin);
				ms.CopyTo(voiceStream);
				voiceStream.Flush();
			};
		}

		//TODO:
		private void speakFile(string filename) {
			SpeechSynthesizer tts = new SpeechSynthesizer();
			MemoryStream ms = new MemoryStream();
			tts.SetOutputToAudioStream(ms, formatInfo);
			if (voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920);
			tts.SpeakAsync(filename);
			tts.SpeakCompleted += async (a, b) => {
				ms.Seek(0, SeekOrigin.Begin);
				await ms.CopyToAsync(voiceStream);
				await voiceStream.FlushAsync();
			};
		}

		private SocketVoiceChannel getUsersVoiceChannel(ulong uid) {
			foreach (SocketGuild g in bot.Guilds)
				foreach (SocketVoiceChannel v in g.VoiceChannels)
					foreach (SocketGuildUser u in v.Users)
						if (u.Id == uid)
							return bot.GetGuild(g.Id).GetVoiceChannel(v.Id);
			return null;
		}
		#endregion

		#region UI Events
		private async void btnJoin_Click(object sender, EventArgs e) {
			btnJoin.Enabled = false;
			ulong uid;
			if (!UInt64.TryParse(txtUserID.Text, out uid)) {
				logBox.AppendText("Invalid Discord ID.\n");
				btnJoin.Enabled = true;
				return;
			}
			SocketVoiceChannel chan = getUsersVoiceChannel(uid);
			if (chan != null) {
				try {
					audioClient = await chan.ConnectAsync();
				} catch (Exception ex) {
					logBox.AppendText("Unable to join channel. Does your bot have permission to join this channel?");
					btnJoin.Enabled = true;
					return;
				}
				logBox.AppendText("Joined channel: " + chan.Name + "\n");
				btnLeave.Enabled = true;
				ActGlobals.oFormActMain.PlayTtsMethod = speak;
			}
			else {
				logBox.AppendText("Unable to join channel. This could be due to any of the following reasons:\n");
				logBox.AppendText("* You are not in a voice channel.\n");
				logBox.AppendText("* The Discord ID you entered above is incorrect.\n");
				btnJoin.Enabled = true;
			}
		}

		private void btnLeave_Click(object sender, EventArgs e) {
			btnLeave.Enabled = false;
			try {
				if (voiceStream != null)
					voiceStream.Close();
				voiceStream = null;
				audioClient.StopAsync();
				btnJoin.Enabled = true;
				btnLeave.Enabled = false;
				logBox.AppendText("Left channel.\n");
				ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
				btnJoin.Enabled = true;
			} catch (Exception ex) {
				logBox.AppendText("Error leaving channel. Possible connection issue.\n");
				btnLeave.Enabled = true;
			}
		}
		#endregion

		#region Discord Events
		private async Task Bot_Ready() {
			btnJoin.Enabled = true;
			await bot.SetGameAsync("with ACT Triggers");
			logBox.AppendText("Bot is now ready.\n");
		}

		private async Task Bot_LoggedIn() {
			await bot.StartAsync();
		}
		#endregion

		#region Settings
		public void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
			xmlSettings.AddControlSetting(txtUserID.Name, txtUserID);

			if (File.Exists(settingsFile)) {
				FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				XmlTextReader xReader = new XmlTextReader(fs);

				try {
					while (xReader.Read())
						if (xReader.NodeType == XmlNodeType.Element)
							if (xReader.LocalName == "SettingsSerializer")
								xmlSettings.ImportFromXml(xReader);
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
		#endregion
	}
}