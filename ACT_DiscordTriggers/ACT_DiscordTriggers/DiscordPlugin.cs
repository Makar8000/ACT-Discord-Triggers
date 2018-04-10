using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Speech.Synthesis;
using System.Reflection;
using DiscordAPI;

namespace ACT_DiscordTriggers {
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
			this.logBox = new System.Windows.Forms.TextBox();
			this.chkAutoConnect = new System.Windows.Forms.CheckBox();
			this.discordConnectbtn = new System.Windows.Forms.Button();
			this.sliderTTSSpeed = new System.Windows.Forms.TrackBar();
			this.lblTTSSpeed = new System.Windows.Forms.Label();
			this.sliderTTSVol = new System.Windows.Forms.TrackBar();
			this.lblTTSVol = new System.Windows.Forms.Label();
			this.cmbChan = new System.Windows.Forms.ComboBox();
			this.lblChan = new System.Windows.Forms.Label();
			this.cmbServer = new System.Windows.Forms.ComboBox();
			this.lblServer = new System.Windows.Forms.Label();
			this.cmbTTS = new System.Windows.Forms.ComboBox();
			this.lblTTS = new System.Windows.Forms.Label();
			this.btnLeave = new System.Windows.Forms.Button();
			this.btnJoin = new System.Windows.Forms.Button();
			this.lblLog = new System.Windows.Forms.Label();
			this.txtToken = new System.Windows.Forms.TextBox();
			this.lblBotTok = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).BeginInit();
			this.SuspendLayout();
			// 
			// logBox
			// 
			this.logBox.BackColor = System.Drawing.SystemColors.Control;
			this.logBox.Location = new System.Drawing.Point(251, 41);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.ReadOnly = true;
			this.logBox.Size = new System.Drawing.Size(436, 348);
			this.logBox.TabIndex = 44;
			// 
			// chkAutoConnect
			// 
			this.chkAutoConnect.AutoSize = true;
			this.chkAutoConnect.Location = new System.Drawing.Point(120, 64);
			this.chkAutoConnect.Name = "chkAutoConnect";
			this.chkAutoConnect.Size = new System.Drawing.Size(91, 17);
			this.chkAutoConnect.TabIndex = 59;
			this.chkAutoConnect.Text = "Auto Connect";
			this.chkAutoConnect.UseVisualStyleBackColor = true;
			// 
			// discordConnectbtn
			// 
			this.discordConnectbtn.Location = new System.Drawing.Point(21, 60);
			this.discordConnectbtn.Name = "discordConnectbtn";
			this.discordConnectbtn.Size = new System.Drawing.Size(93, 23);
			this.discordConnectbtn.TabIndex = 58;
			this.discordConnectbtn.Text = "Connect";
			this.discordConnectbtn.UseVisualStyleBackColor = true;
			this.discordConnectbtn.Click += new System.EventHandler(this.discordConnectbtn_Click);
			// 
			// sliderTTSSpeed
			// 
			this.sliderTTSSpeed.Location = new System.Drawing.Point(21, 363);
			this.sliderTTSSpeed.Maximum = 20;
			this.sliderTTSSpeed.Name = "sliderTTSSpeed";
			this.sliderTTSSpeed.Size = new System.Drawing.Size(193, 45);
			this.sliderTTSSpeed.TabIndex = 57;
			this.sliderTTSSpeed.Value = 10;
			// 
			// lblTTSSpeed
			// 
			this.lblTTSSpeed.AutoSize = true;
			this.lblTTSSpeed.Location = new System.Drawing.Point(22, 345);
			this.lblTTSSpeed.Name = "lblTTSSpeed";
			this.lblTTSSpeed.Size = new System.Drawing.Size(62, 13);
			this.lblTTSSpeed.TabIndex = 56;
			this.lblTTSSpeed.Text = "TTS Speed";
			// 
			// sliderTTSVol
			// 
			this.sliderTTSVol.Location = new System.Drawing.Point(21, 296);
			this.sliderTTSVol.Maximum = 20;
			this.sliderTTSVol.Name = "sliderTTSVol";
			this.sliderTTSVol.Size = new System.Drawing.Size(193, 45);
			this.sliderTTSVol.TabIndex = 55;
			this.sliderTTSVol.Value = 10;
			// 
			// lblTTSVol
			// 
			this.lblTTSVol.AutoSize = true;
			this.lblTTSVol.Location = new System.Drawing.Point(18, 276);
			this.lblTTSVol.Name = "lblTTSVol";
			this.lblTTSVol.Size = new System.Drawing.Size(66, 13);
			this.lblTTSVol.TabIndex = 54;
			this.lblTTSVol.Text = "TTS Volume";
			// 
			// cmbChan
			// 
			this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbChan.FormattingEnabled = true;
			this.cmbChan.Location = new System.Drawing.Point(21, 159);
			this.cmbChan.Name = "cmbChan";
			this.cmbChan.Size = new System.Drawing.Size(193, 21);
			this.cmbChan.TabIndex = 53;
			// 
			// lblChan
			// 
			this.lblChan.AutoSize = true;
			this.lblChan.Location = new System.Drawing.Point(18, 141);
			this.lblChan.Name = "lblChan";
			this.lblChan.Size = new System.Drawing.Size(46, 13);
			this.lblChan.TabIndex = 52;
			this.lblChan.Text = "Channel";
			// 
			// cmbServer
			// 
			this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbServer.FormattingEnabled = true;
			this.cmbServer.Location = new System.Drawing.Point(21, 113);
			this.cmbServer.Name = "cmbServer";
			this.cmbServer.Size = new System.Drawing.Size(193, 21);
			this.cmbServer.TabIndex = 51;
			this.cmbServer.SelectedValueChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
			// 
			// lblServer
			// 
			this.lblServer.AutoSize = true;
			this.lblServer.Location = new System.Drawing.Point(18, 99);
			this.lblServer.Name = "lblServer";
			this.lblServer.Size = new System.Drawing.Size(38, 13);
			this.lblServer.TabIndex = 50;
			this.lblServer.Text = "Server";
			// 
			// cmbTTS
			// 
			this.cmbTTS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbTTS.FormattingEnabled = true;
			this.cmbTTS.Location = new System.Drawing.Point(21, 242);
			this.cmbTTS.Name = "cmbTTS";
			this.cmbTTS.Size = new System.Drawing.Size(193, 21);
			this.cmbTTS.TabIndex = 49;
			// 
			// lblTTS
			// 
			this.lblTTS.AutoSize = true;
			this.lblTTS.Location = new System.Drawing.Point(18, 227);
			this.lblTTS.Name = "lblTTS";
			this.lblTTS.Size = new System.Drawing.Size(58, 13);
			this.lblTTS.TabIndex = 48;
			this.lblTTS.Text = "TTS Voice";
			// 
			// btnLeave
			// 
			this.btnLeave.Enabled = false;
			this.btnLeave.Location = new System.Drawing.Point(120, 186);
			this.btnLeave.Name = "btnLeave";
			this.btnLeave.Size = new System.Drawing.Size(94, 23);
			this.btnLeave.TabIndex = 47;
			this.btnLeave.Text = "Leave Channel";
			this.btnLeave.UseVisualStyleBackColor = true;
			this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
			// 
			// btnJoin
			// 
			this.btnJoin.Enabled = false;
			this.btnJoin.Location = new System.Drawing.Point(21, 185);
			this.btnJoin.Name = "btnJoin";
			this.btnJoin.Size = new System.Drawing.Size(93, 23);
			this.btnJoin.TabIndex = 46;
			this.btnJoin.Text = "Join Channel";
			this.btnJoin.UseVisualStyleBackColor = true;
			this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
			// 
			// lblLog
			// 
			this.lblLog.AutoSize = true;
			this.lblLog.Location = new System.Drawing.Point(248, 18);
			this.lblLog.Name = "lblLog";
			this.lblLog.Size = new System.Drawing.Size(60, 13);
			this.lblLog.TabIndex = 45;
			this.lblLog.Text = "Debug Log";
			// 
			// txtToken
			// 
			this.txtToken.Location = new System.Drawing.Point(21, 34);
			this.txtToken.Name = "txtToken";
			this.txtToken.Size = new System.Drawing.Size(193, 20);
			this.txtToken.TabIndex = 43;
			this.txtToken.UseSystemPasswordChar = true;
			// 
			// lblBotTok
			// 
			this.lblBotTok.AutoSize = true;
			this.lblBotTok.Location = new System.Drawing.Point(18, 18);
			this.lblBotTok.Name = "lblBotTok";
			this.lblBotTok.Size = new System.Drawing.Size(96, 13);
			this.lblBotTok.TabIndex = 42;
			this.lblBotTok.Text = "Discord Bot Token";
			// 
			// DiscordPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.logBox);
			this.Controls.Add(this.chkAutoConnect);
			this.Controls.Add(this.discordConnectbtn);
			this.Controls.Add(this.sliderTTSSpeed);
			this.Controls.Add(this.lblTTSSpeed);
			this.Controls.Add(this.sliderTTSVol);
			this.Controls.Add(this.lblTTSVol);
			this.Controls.Add(this.cmbChan);
			this.Controls.Add(this.lblChan);
			this.Controls.Add(this.cmbServer);
			this.Controls.Add(this.lblServer);
			this.Controls.Add(this.cmbTTS);
			this.Controls.Add(this.lblTTS);
			this.Controls.Add(this.btnLeave);
			this.Controls.Add(this.btnJoin);
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "DiscordPlugin";
			this.Size = new System.Drawing.Size(722, 439);
			((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#endregion
		public DiscordPlugin() {
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			InitializeComponent();
			var tts = new SpeechSynthesizer();
			foreach (InstalledVoice v in tts.GetInstalledVoices())
				cmbTTS.Items.Add(v.VoiceInfo.Name);
			cmbTTS.SelectedIndex = 0;
		}

		Label lblStatus;
		string settingsFile;
		SettingsSerializer xmlSettings;
		private TextBox logBox;
		private CheckBox chkAutoConnect;
		private Button discordConnectbtn;
		private TrackBar sliderTTSSpeed;
		private Label lblTTSSpeed;
		private TrackBar sliderTTSVol;
		private Label lblTTSVol;
		private ComboBox cmbChan;
		private Label lblChan;
		private ComboBox cmbServer;
		private Label lblServer;
		private ComboBox cmbTTS;
		private Label lblTTS;
		private Button btnLeave;
		private Button btnJoin;
		private Label lblLog;
		private TextBox txtToken;
		private Label lblBotTok;
		private Random ran = new Random();

		#region IActPluginV1 Members
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			//ACT Stuff
			lblStatus = pluginStatusText;
			settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\ACT_DiscordTriggers.config.xml");
			pluginScreenSpace.Controls.Add(this);
			pluginScreenSpace.Text = "Discord Triggers";
			Dock = DockStyle.Fill;
			xmlSettings = new SettingsSerializer(this);
			LoadSettings();

			//Discord Bot Stuff
			DiscordClient.BotReady += BotReady;
			DiscordClient.Log += Log;

			if (chkAutoConnect.Checked)
				discordConnectbtn_Click(null, EventArgs.Empty);

			lblStatus.Text = "Plugin Started";
		}

		public async void DeInitPlugin() {
			ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
			ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
			SaveSettings();
			try {
				await DiscordClient.deInIt();
			} catch (Exception ex) {
				ActGlobals.oFormActMain.WriteExceptionLog(ex, "Error with DeInit of Discord Plugin.");
			}
			lblStatus.Text = "Plugin Exited";
		}

		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			try {
				var asm = new AssemblyName(args.Name);
				var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);
				string file;
				if (plugin != null) {
					file = plugin.pluginFile.DirectoryName;
					file = Path.Combine(file, asm.Name + ".dll");
					if (File.Exists(file)) {
						return Assembly.LoadFile(file);
					}
				}
				file = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Plugins\\Discord");
				file = Path.Combine(file, asm.Name + ".dll");
				if (File.Exists(file)) {
					return Assembly.LoadFrom(file);
				}
			} catch (Exception ex) {
				ActGlobals.oFormActMain.WriteExceptionLog(ex, "Error with loading an assembly for Discord Plugin.");
			}
			return null;
		}
		#endregion

		private object speaklock = new object();

		#region Discord Methods
		private void speak(string text) {
			DiscordClient.Speak(text, cmbTTS.SelectedItem.ToString(), sliderTTSVol.Value, sliderTTSSpeed.Value);
		}

		private void speakFile(string path, int volume) {
			DiscordClient.SpeakFile(path);
		}

		private void BotReady() {
			btnJoin.Enabled = true;
			DiscordClient.SetGameAsync("with ACT Triggers");
			populateServers();
		}


		private void populateServers() {
			try {
				string[] servers = DiscordClient.getServers();

				cmbServer.Items.Clear();
				cmbChan.Items.Clear();

				foreach (string server in servers)
					cmbServer.Items.Add(server);

				if (cmbServer.Items.Count > 0)
					cmbServer.SelectedIndex = 0;
			} catch (Exception ex) {
				Log("Error populating servers.");
				Log(ex.Message);
			}
		}

		private void populateChannels(string server) {
			try {
				cmbChan.Items.Clear();
				cmbChan.Items.AddRange(DiscordClient.getChannels(server));
				if (cmbChan.Items.Count > 0)
					cmbChan.SelectedIndex = 0;
			} catch (Exception ex) {
				Log("Error populating channels.");
				Log(ex.Message);
			}
		}
		#endregion

		#region UI Events
		private async void btnJoin_Click(object sender, EventArgs e) {

			btnJoin.Enabled = false;
			if (await DiscordClient.JoinChannel(cmbServer.SelectedItem.ToString(), cmbChan.SelectedItem.ToString())) {
				btnLeave.Enabled = true;
				ActGlobals.oFormActMain.PlayTtsMethod = speak;
				ActGlobals.oFormActMain.PlaySoundMethod = speakFile;
			}
			else {
				Log("Unable to join channel. Does your bot have permission to join this channel?");
				btnJoin.Enabled = true;
				populateServers();
			}
		}

		private void btnLeave_Click(object sender, EventArgs e) {
			btnLeave.Enabled = false;
			try {
				DiscordClient.LeaveChannel();
				btnJoin.Enabled = true;
				btnLeave.Enabled = false;
				Log("Left channel.");
				ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
				ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
				btnJoin.Enabled = true;
			} catch (Exception ex) {
				Log("Error leaving channel. Possible connection issue.");
				btnLeave.Enabled = true;
				Log(ex.Message);
			}
		}

		private void cmbServer_SelectedIndexChanged(object sender, EventArgs e) {
			populateChannels(cmbServer.SelectedItem.ToString());
		}
		#endregion

		#region Discord Events
		public void Log(string text) {
			logBox.AppendText(text + "\n");
		}
		#endregion

		#region Settings
		public void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
			xmlSettings.AddControlSetting(sliderTTSVol.Name, sliderTTSVol);
			xmlSettings.AddControlSetting(sliderTTSSpeed.Name, sliderTTSSpeed);
			xmlSettings.AddControlSetting(chkAutoConnect.Name, chkAutoConnect);
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

		public bool SaveSettings() {
			try {
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
			} catch {
				return false;
			}
			return true;
		}
		#endregion

		private void discordConnectbtn_Click(object sender, EventArgs e) {

			if (DiscordClient.IsConnected()) {
				Log("Already connected to Discord.");
				return;
			}
			if (DiscordClient.InIt(txtToken.Text)) 
				Log("Connected to Discord.");
			else
				Log("Error connecting to Discord. Discord may be down or key is incorrect.");
		}
	}
}