using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using NAudio.Wave;
using Discord.Net.Providers.WS4Net;
using Discord.Net.Providers.UDPClient;
using System.Reflection;

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
			this.logBox = new System.Windows.Forms.TextBox();
			this.lblLog = new System.Windows.Forms.Label();
			this.btnJoin = new System.Windows.Forms.Button();
			this.btnLeave = new System.Windows.Forms.Button();
			this.lblTTS = new System.Windows.Forms.Label();
			this.cmbTTS = new System.Windows.Forms.ComboBox();
			this.cmbServer = new System.Windows.Forms.ComboBox();
			this.lblServer = new System.Windows.Forms.Label();
			this.cmbChan = new System.Windows.Forms.ComboBox();
			this.lblChan = new System.Windows.Forms.Label();
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
			this.txtToken.Size = new System.Drawing.Size(193, 20);
			this.txtToken.TabIndex = 1;
			this.txtToken.UseSystemPasswordChar = true;
			// 
			// logBox
			// 
			this.logBox.BackColor = System.Drawing.SystemColors.Control;
			this.logBox.Location = new System.Drawing.Point(272, 49);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.ReadOnly = true;
			this.logBox.Size = new System.Drawing.Size(381, 189);
			this.logBox.TabIndex = 4;
			// 
			// lblLog
			// 
			this.lblLog.AutoSize = true;
			this.lblLog.Location = new System.Drawing.Point(269, 23);
			this.lblLog.Name = "lblLog";
			this.lblLog.Size = new System.Drawing.Size(60, 13);
			this.lblLog.TabIndex = 5;
			this.lblLog.Text = "Debug Log";
			// 
			// btnJoin
			// 
			this.btnJoin.Enabled = false;
			this.btnJoin.Location = new System.Drawing.Point(31, 165);
			this.btnJoin.Name = "btnJoin";
			this.btnJoin.Size = new System.Drawing.Size(93, 23);
			this.btnJoin.TabIndex = 6;
			this.btnJoin.Text = "Join Channel";
			this.btnJoin.UseVisualStyleBackColor = true;
			this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
			// 
			// btnLeave
			// 
			this.btnLeave.Enabled = false;
			this.btnLeave.Location = new System.Drawing.Point(130, 165);
			this.btnLeave.Name = "btnLeave";
			this.btnLeave.Size = new System.Drawing.Size(94, 23);
			this.btnLeave.TabIndex = 7;
			this.btnLeave.Text = "Leave Channel";
			this.btnLeave.UseVisualStyleBackColor = true;
			this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
			// 
			// lblTTS
			// 
			this.lblTTS.AutoSize = true;
			this.lblTTS.Location = new System.Drawing.Point(28, 200);
			this.lblTTS.Name = "lblTTS";
			this.lblTTS.Size = new System.Drawing.Size(58, 13);
			this.lblTTS.TabIndex = 12;
			this.lblTTS.Text = "TTS Voice";
			// 
			// cmbTTS
			// 
			this.cmbTTS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbTTS.FormattingEnabled = true;
			this.cmbTTS.Location = new System.Drawing.Point(31, 217);
			this.cmbTTS.Name = "cmbTTS";
			this.cmbTTS.Size = new System.Drawing.Size(193, 21);
			this.cmbTTS.TabIndex = 13;
			// 
			// cmbServer
			// 
			this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbServer.FormattingEnabled = true;
			this.cmbServer.Location = new System.Drawing.Point(31, 88);
			this.cmbServer.Name = "cmbServer";
			this.cmbServer.Size = new System.Drawing.Size(193, 21);
			this.cmbServer.TabIndex = 15;
			this.cmbServer.SelectedIndexChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
			// 
			// lblServer
			// 
			this.lblServer.AutoSize = true;
			this.lblServer.Location = new System.Drawing.Point(28, 71);
			this.lblServer.Name = "lblServer";
			this.lblServer.Size = new System.Drawing.Size(38, 13);
			this.lblServer.TabIndex = 14;
			this.lblServer.Text = "Server";
			// 
			// cmbChan
			// 
			this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbChan.FormattingEnabled = true;
			this.cmbChan.Location = new System.Drawing.Point(31, 138);
			this.cmbChan.Name = "cmbChan";
			this.cmbChan.Size = new System.Drawing.Size(193, 21);
			this.cmbChan.TabIndex = 17;
			// 
			// lblChan
			// 
			this.lblChan.AutoSize = true;
			this.lblChan.Location = new System.Drawing.Point(28, 121);
			this.lblChan.Name = "lblChan";
			this.lblChan.Size = new System.Drawing.Size(46, 13);
			this.lblChan.TabIndex = 16;
			this.lblChan.Text = "Channel";
			// 
			// DiscordPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.cmbChan);
			this.Controls.Add(this.lblChan);
			this.Controls.Add(this.cmbServer);
			this.Controls.Add(this.lblServer);
			this.Controls.Add(this.cmbTTS);
			this.Controls.Add(this.lblTTS);
			this.Controls.Add(this.btnLeave);
			this.Controls.Add(this.btnJoin);
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.logBox);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "DiscordPlugin";
			this.Size = new System.Drawing.Size(701, 296);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#endregion
		public DiscordPlugin() {
			//Required for assembly references
			AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
				try {
					var asm = new AssemblyName(e.Name);
					var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);
					if (plugin != null) {
						var thisDirectory = plugin.pluginFile.DirectoryName;
						var path1 = Path.Combine(thisDirectory, asm.Name + ".dll");
						if (File.Exists(path1)) {
							return Assembly.LoadFrom(path1);
						}
					}
					var pluginDirectory = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						@"Advanced Combat Tracker\Plugins");
					var path = Path.Combine(pluginDirectory, asm.Name + ".dll");
					if (File.Exists(path)) {
						return Assembly.LoadFrom(path);
					}
				} catch (Exception ex) {
					ActGlobals.oFormActMain.WriteExceptionLog(
						ex,
						"Uh oh.");
				}
				return null;
			};
			InitializeComponent();
			var tts = new SpeechSynthesizer();
			foreach (InstalledVoice v in tts.GetInstalledVoices())
				cmbTTS.Items.Add(v.VoiceInfo.Name);
			cmbTTS.SelectedIndex = 0;
		}

		Label lblStatus;
		string settingsFile;
		private Label lblBotTok;
		private TextBox txtToken;
		private TextBox logBox;
		private Label lblLog;
		SettingsSerializer xmlSettings;
		private Button btnJoin;
		private Button btnLeave;
		private DiscordSocketClient bot;
		private IAudioClient audioClient;
		private SpeechAudioFormatInfo formatInfo;
		private AudioOutStream voiceStream;
		private Label lblTTS;
		private ComboBox cmbServer;
		private Label lblServer;
		private ComboBox cmbChan;
		private Label lblChan;
		private ComboBox cmbTTS;

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
			formatInfo = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
			try {
				bot = new DiscordSocketClient();
			} catch (PlatformNotSupportedException) {
				bot = new DiscordSocketClient(new DiscordSocketConfig {
					WebSocketProvider = WS4NetProvider.Instance,
					UdpSocketProvider = UDPClientProvider.Instance,
				});
			}
			try {
				bot.LoggedIn += Bot_LoggedIn;
				bot.Ready += Bot_Ready;
				bot.LoginAsync(TokenType.Bot, txtToken.Text).GetAwaiter().GetResult();
				logBox.AppendText("Plugin loaded successfully.\n");
			} catch (Exception ex) {
				logBox.AppendText("Error connecting bot. Make sure your Bot Token is correct then restart the plugin (Go to \"Plugin Listing\" tab, uncheck \"Enabled\" and then check it again).\n");
				logBox.AppendText(ex.Message + "\n");
			}
			lblStatus.Text = "Plugin Started";
		}

		public async void DeInitPlugin() {
			ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
			ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
			SaveSettings();
			try {
				bot.Ready -= Bot_Ready;
				bot.LoggedIn -= Bot_LoggedIn;
				if (audioClient?.ConnectionState == ConnectionState.Connected) {
					voiceStream?.Close();
					await audioClient.StopAsync();
				}
				await bot.StopAsync();
				await bot.LogoutAsync();
			} catch (Exception ex) {
				//nothing to see here
			}
			lblStatus.Text = "Plugin Exited";
		}
		#endregion

		#region Discord Methods
		private void speak(string text) {
			SpeechSynthesizer tts = new SpeechSynthesizer();
			tts.SelectVoice((string) cmbTTS.SelectedItem);
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

		private void speakFile(string path, int volume) {
			if (voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 1920);
			try {
				WaveFileReader wav = new WaveFileReader(path);
				WaveFormat waveFormat = new WaveFormat(48000, 16, 2);
				WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(wav);
				WaveFormatConversionStream output = new WaveFormatConversionStream(waveFormat, pcm);
				output.CopyTo(voiceStream);
				voiceStream.Flush();
			} catch (Exception ex) {
				logBox.AppendText("Unable to read file.\n" + ex.Message + "\n");
			}
		}

		private void populateServers() {
			try {
				cmbServer.Items.Clear();
				cmbChan.Items.Clear();
				foreach (SocketGuild g in bot.Guilds)
					cmbServer.Items.Add(g);
				if (cmbServer.Items.Count > 0)
					cmbServer.SelectedIndex = 0;
			} catch (Exception ex) {
				logBox.AppendText("Error populating servers.\n");
				logBox.AppendText(ex.Message + "\n");
			}
		}

		private void populateChannels(SocketGuild g) {
			try {
				cmbChan.Items.Clear();
				foreach (SocketVoiceChannel v in g.VoiceChannels)
					cmbChan.Items.Add(v);
				if (cmbChan.Items.Count > 0)
					cmbChan.SelectedIndex = 0;
			} catch (Exception ex) {
				logBox.AppendText("Error populating channels.\n");
				logBox.AppendText(ex.Message + "\n");
			}
		}
		#endregion

		#region UI Events
		private async void btnJoin_Click(object sender, EventArgs e) {
			btnJoin.Enabled = false;
			SocketVoiceChannel chan = (SocketVoiceChannel) cmbChan.SelectedItem;
			try {
				audioClient = await chan.ConnectAsync();
				logBox.AppendText("Joined channel: " + chan.Name + "\n");
				btnLeave.Enabled = true;
				ActGlobals.oFormActMain.PlayTtsMethod = speak;
				ActGlobals.oFormActMain.PlaySoundMethod = speakFile;
			} catch (Exception ex) {
				logBox.AppendText("Unable to join channel. Does your bot have permission to join this channel?\n");
				btnJoin.Enabled = true;
				populateServers();
				logBox.AppendText(ex.Message + "\n");
				return;
			}
		}

		private void btnLeave_Click(object sender, EventArgs e) {
			btnLeave.Enabled = false;
			try {
				bot.SetStatusAsync(UserStatus.Offline);
				voiceStream?.Close();
				voiceStream = null;
				audioClient.StopAsync();
				btnJoin.Enabled = true;
				btnLeave.Enabled = false;
				logBox.AppendText("Left channel.\n");
				ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
				ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
				btnJoin.Enabled = true;
			} catch (Exception ex) {
				logBox.AppendText("Error leaving channel. Possible connection issue.\n");
				btnLeave.Enabled = true;
				logBox.AppendText(ex.Message + "\n");
			}
		}

		private void cmbServer_SelectedIndexChanged(object sender, EventArgs e) {
			populateChannels((SocketGuild) cmbServer.SelectedItem);
		}
		#endregion

		#region Discord Events
		private async Task Bot_Ready() {
			btnJoin.Enabled = true;
			await bot.SetGameAsync("with ACT Triggers");
			populateServers();
			logBox.AppendText("Bot is now ready.\n");
		}

		private async Task Bot_LoggedIn() {
			try {
				await bot.StartAsync();
				logBox.AppendText("Bot started successfully.\n");
			} catch (Exception ex) {
				logBox.AppendText("Unable to start. Error:\n" + ex.Message + "\n");
			}
		}
		#endregion

		#region Settings
		public void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
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