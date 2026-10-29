using System.Drawing;
using System.Windows.Forms;

namespace WebBrowser
{
    internal sealed class KeywordForm : Form
    {
        private readonly TextBox keywordInput = new TextBox();
        public string Keyword { get { return keywordInput.Text.Trim(); } }

        public KeywordForm()
        {
            Text = "Add blocked keyword";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(360, 130);
            Label label = new Label { Text = "Keyword:", AutoSize = true, Location = new Point(16, 22) };
            keywordInput.Location = new Point(85, 18); keywordInput.Size = new Size(255, 24);
            Button ok = new Button { Text = "Add", DialogResult = DialogResult.OK, Location = new Point(184, 76), Size = new Size(75, 28) };
            Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(265, 76), Size = new Size(75, 28) };
            ok.Click += (s, e) => { if (Keyword.Length == 0) { DialogResult = DialogResult.None; MessageBox.Show(this, "Enter a keyword first.", "Keyword required", MessageBoxButtons.OK, MessageBoxIcon.Information); keywordInput.Focus(); } };
            Controls.AddRange(new Control[] { label, keywordInput, ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }
    }
}
