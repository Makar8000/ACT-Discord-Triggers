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
	public class MyPlugin : UserControl, IActPluginV1 {
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
			this.lblLabel = new System.Windows.Forms.Label();
			this.txtBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// lblLabel
			// 
			this.lblLabel.AutoSize = true;
			this.lblLabel.Location = new System.Drawing.Point(50, 34);
			this.lblLabel.Name = "lblLabel";
			this.lblLabel.Size = new System.Drawing.Size(47, 13);
			this.lblLabel.TabIndex = 0;
			this.lblLabel.Text = "MyLabel";
			// 
			// txtBox
			// 
			this.txtBox.Location = new System.Drawing.Point(53, 50);
			this.txtBox.Name = "txtBox";
			this.txtBox.Size = new System.Drawing.Size(100, 20);
			this.txtBox.TabIndex = 1;
			this.txtBox.Text = "This is a test.";
			// 
			// MyPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.txtBox);
			this.Controls.Add(this.lblLabel);
			this.Name = "MyPlugin";
			this.Size = new System.Drawing.Size(686, 384);
			this.Load += new System.EventHandler(this.MyPlugin_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#endregion
		public void PluginSample() {
			InitializeComponent();
		}

		Label lblStatus;    // The status label that appears in ACT's Plugin tab
		string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\ACT_DiscordTriggers.config.xml");
		private Label lblLabel;
		private TextBox txtBox;
		SettingsSerializer xmlSettings;

		#region IActPluginV1 Members
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
			pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
			this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
			xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
			//LoadSettings();

			// Create some sort of parsing event handler.  After the "+=" hit TAB twice and the code will be generated for you.
			ActGlobals.oFormActMain.AfterCombatAction += new CombatActionDelegate(oFormActMain_AfterCombatAction);

			lblStatus.Text = "Plugin Started";
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
			xmlSettings.AddControlSetting(txtBox.Name, txtBox);

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
