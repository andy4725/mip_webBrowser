using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebBrowser
{
    internal sealed class BrowserForm : Form
    {
        private readonly SQLiteHandler database;
        private readonly List<string> blockedWords;
        private readonly System.Windows.Forms.WebBrowser browser = new System.Windows.Forms.WebBrowser();
        private readonly TextBox address = new TextBox();
        private readonly ComboBox keywords = new ComboBox();
        private readonly Button back = new Button();
        private readonly Button forward = new Button();
        private readonly Button home = new Button();
        private readonly Button go = new Button();
        private readonly Button add = new Button();
        private readonly Button remove = new Button();
        private readonly Label status = new Label();
        private readonly string logPath;
        private readonly object traceLock = new object();

        public BrowserForm()
        {
            Text = "WebBrowser"; Width = 1200; Height = 800; MinimumSize = new Size(800, 550);
            StartPosition = FormStartPosition.CenterScreen;
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebBrowser");
            Directory.CreateDirectory(appData);
            logPath = Path.Combine(appData, "browser_events.txt");
            database = new SQLiteHandler();
            blockedWords = database.GetAllKeywords();
            BuildInterface();
            RefreshKeywords();
            FormClosed += (s, e) => database.Dispose();
            LogEvent("Browser started.");
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(6) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 5) };
            ConfigureButton(back, "Back", 64); ConfigureButton(forward, "Forward", 76); ConfigureButton(home, "Home", 64); ConfigureButton(go, "Go", 56);
            address.Width = 430; address.Margin = new Padding(4, 7, 4, 3); address.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            address.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await NavigateFromAddressAsync(); } };
            go.Click += async (s, e) => await NavigateFromAddressAsync();
            back.Click += (s, e) => { if (browser.CanGoBack) { browser.GoBack(); LogEvent("Back navigation."); } };
            forward.Click += (s, e) => { if (browser.CanGoForward) { browser.GoForward(); LogEvent("Forward navigation."); } };
            home.Click += (s, e) => { browser.Navigate("https://www.google.com/"); LogEvent("Home navigation."); };
            keywords.DropDownStyle = ComboBoxStyle.DropDownList; keywords.Width = 145; keywords.Margin = new Padding(8, 7, 3, 3);
            ConfigureButton(add, "Add keyword", 100); ConfigureButton(remove, "Delete", 75);
            add.Click += AddKeyword_Click; remove.Click += DeleteKeyword_Click;
            toolbar.Controls.AddRange(new Control[] { back, forward, home, address, go, new Label { Text = "Blocked words", AutoSize = true, Margin = new Padding(10, 10, 2, 0) }, keywords, add, remove });
            browser.Dock = DockStyle.Fill; browser.ScriptErrorsSuppressed = true; browser.Navigated += (s, e) => address.Text = e.Url == null ? "" : e.Url.ToString();
            status.AutoSize = true; status.Text = "Ready"; status.Padding = new Padding(2, 5, 2, 2); status.ForeColor = Color.DimGray;
            root.Controls.Add(toolbar, 0, 0); root.Controls.Add(browser, 0, 1); root.Controls.Add(status, 0, 2);
            Controls.Add(root);
        }

        private static void ConfigureButton(Button button, string text, int width)
        {
            button.Text = text; button.Width = width; button.Height = 30; button.Margin = new Padding(3, 3, 3, 3); button.UseVisualStyleBackColor = true;
        }

        private async Task NavigateFromAddressAsync()
        {
            string destination = address.Text.Trim();
            if (destination.Length == 0) { status.Text = "Enter a web address."; return; }
            if (await ContainsBlockedWordAsync(destination))
            {
                status.Text = "Navigation blocked: the address contains a blocked keyword.";
                LogEvent("Blocked navigation attempt: " + destination);
                MessageBox.Show(this, "This address contains a blocked keyword.", "Navigation blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!destination.Contains("://")) destination = "https://" + destination;
            Uri uri;
            if (!Uri.TryCreate(destination, UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                status.Text = "Enter a valid HTTP or HTTPS address."; return;
            }
            address.Text = uri.ToString(); status.Text = "Loading " + uri.Host + "…";
            browser.Navigate(uri); LogEvent("Navigating to " + uri);
        }

        private Task<bool> ContainsBlockedWordAsync(string destination)
        {
            string[] snapshot = blockedWords.ToArray();
            return Task.Run(() => snapshot.Any(word => destination.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private void AddKeyword_Click(object sender, EventArgs e)
        {
            using (KeywordForm dialog = new KeywordForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string keyword = dialog.Keyword;
                if (blockedWords.Any(word => string.Equals(word, keyword, StringComparison.OrdinalIgnoreCase)))
                { MessageBox.Show(this, "That keyword is already in the list.", "Duplicate keyword", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                database.InsertKeyword(keyword); blockedWords.Add(keyword); RefreshKeywords();
                status.Text = "Added “" + keyword + "” to blocked words."; LogEvent("Blocked keyword added: " + keyword);
            }
        }

        private void DeleteKeyword_Click(object sender, EventArgs e)
        {
            string selected = keywords.SelectedItem as string;
            if (selected == null) { MessageBox.Show(this, "Choose a keyword to delete.", "No keyword selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            database.DeleteKeyword(selected); blockedWords.RemoveAll(word => string.Equals(word, selected, StringComparison.OrdinalIgnoreCase));
            RefreshKeywords(); status.Text = "Deleted “" + selected + "”."; LogEvent("Blocked keyword deleted: " + selected);
        }

        private void RefreshKeywords()
        {
            keywords.BeginUpdate(); keywords.Items.Clear(); keywords.Items.AddRange(blockedWords.OrderBy(word => word, StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray()); keywords.EndUpdate();
            remove.Enabled = keywords.Items.Count > 0;
        }

        private void LogEvent(string message)
        {
            Task.Run(() =>
            {
                lock (traceLock)
                {
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(new TextWriterTraceListener(logPath));
                    Trace.AutoFlush = true;
                    Trace.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " - " + message);
                    Trace.Flush();
                    Trace.Close();
                }
            });
        }
    }
}
