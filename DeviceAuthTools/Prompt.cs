using System;
using System.Windows.Forms;

namespace DeviceAuthGenerator
{
    public static class Prompt
    {
        public static string ShowDialog(string caption, string defaultValue = "")
        {
            Form prompt = new Form
            {
                Width = 300,
                Height = 100,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            TextBox textBox = new TextBox
            {
                Text = defaultValue,
                Left = 43,
                Top = 10,
                Width = 200
            };
            Button confirmation = new Button
            {
                Text = "Ok",
                Left = 113,
                Width = 60,
                Top = 35,
                DialogResult = DialogResult.OK
            };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "\x00";
        }

        public static string Authorization(string caption, string clientId)
        {
            Form prompt = new Form
            {
                Width = 325,
                Height = 135,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            TextBox textBox = new TextBox
            {
                Top = 10,
                Width = 225
            };
            textBox.Left = (prompt.Width - textBox.Width) / 2 - 6;

            Button confirmation = new Button
            {
                Text = "Ok",
                Width = 85,
                Top = 65,
                DialogResult = DialogResult.OK
            };
            confirmation.Left = (prompt.Width - confirmation.Width) / 2 - 6;
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            LinkLabel clickHere = new LinkLabel
            {
                Text = "Generate one",
                Top = 32,
            };
            clickHere.Left = (prompt.Width - clickHere.Width) / 2 + 8;
            clickHere.Click += delegate (object clickSender, EventArgs clickEventArgs) {
                System.Diagnostics.Process.Start($"https://www.epicgames.com/id/api/redirect?clientId={clientId}&responseType=code");
            };

            prompt.Controls.Add(clickHere);

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "\x00";
        }
    }
}
