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
			this.txtName = new System.Windows.Forms.TextBox();
			this.logBox = new System.Windows.Forms.TextBox();
			this.lblLog = new System.Windows.Forms.Label();
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
			this.txtToken.Size = new System.Drawing.Size(281, 20);
			this.txtToken.TabIndex = 1;
			this.txtToken.Text = "Enter your Discord Bot Token";
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
			// txtName
			// 
			this.txtName.Location = new System.Drawing.Point(31, 104);
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(281, 20);
			this.txtName.TabIndex = 3;
			this.txtName.Text = "Enter your Discord ID";
			// 
			// logBox
			// 
			this.logBox.Location = new System.Drawing.Point(31, 167);
			this.logBox.Multiline = true;
			this.logBox.Name = "logBox";
			this.logBox.Size = new System.Drawing.Size(588, 180);
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
			// MyPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.logBox);
			this.Controls.Add(this.txtName);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "MyPlugin";
			this.Size = new System.Drawing.Size(686, 384);
			this.Load += new System.EventHandler(this.MyPlugin_Load);
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
		private TextBox txtName;
		private TextBox logBox;
		private Label lblLog;
		SettingsSerializer xmlSettings;

		#region IActPluginV1 Members
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
			pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
			pluginScreenSpace.Text = "Discord Triggers";
			this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
			xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
			LoadSettings();

			// Create some sort of parsing event handler.  After the "+=" hit TAB twice and the code will be generated for you.
			ActGlobals.oFormActMain.AfterCombatAction += new CombatActionDelegate(oFormActMain_AfterCombatAction);
			ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;

			lblStatus.Text = "Plugin Started";
		}

		private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo) {
			foreach (KeyValuePair<string, CustomTrigger> trig in ActGlobals.oFormActMain.CustomTriggers) {
				if (trig.Value.RegEx.IsMatch(logInfo.logLine)) {
					logBox.AppendText("Found a match!\n");
					//this is where I would say something in discord
					break;
				}
			}
		}

		public void DeInitPlugin() {
			// Unsubscribe from any events you listen to when exiting!
			ActGlobals.oFormActMain.AfterCombatAction -= oFormActMain_AfterCombatAction;

			SaveSettings();
			lblStatus.Text = "Plugin Exited";
		}
		#endregion

		void oFormActMain_AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo) {
			throw new NotImplementedException();
		}

		void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
			xmlSettings.AddControlSetting(txtName.Name, txtName);

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
		void SaveSettings() {
			FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
			xWriter.Formatting = Formatting.Indented;
			xWriter.Indentation = 1;
			xWriter.IndentChar = '\t';
			xWriter.WriteStartDocument(true);
			xWriter.WriteStartElement("Config");    // <Config>
			xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
			xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
			xWriter.WriteEndElement();  // </SettingsSerializer>
			xWriter.WriteEndElement();  // </Config>
			xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
			xWriter.Flush();    // Flush the file buffer to disk
			xWriter.Close();
		}

		private void MyPlugin_Load(object sender, EventArgs e) {
			
		}
	}
}
