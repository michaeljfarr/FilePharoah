using System;
using Eto.Forms;

namespace EtoTest.Dialogs
{
    public static class PasswordInputBox
    {
        public static string GetPassword(Control owner, string dialogTitle)
        {
            using (var dialog = new Dialog {Title = dialogTitle})
            {
                String passwordOverride = null;
                var passwordBox = new PasswordBox();
                passwordBox.TextInput += (sender, args) =>
                {
                    passwordOverride = null;
                };
                passwordBox.KeyDown += (sender, args) =>
                {
                    if (args.KeyData == (Keys.V | Keys.Control))
                    {
                        using (var clipboard = new Clipboard())
                        {
                            passwordOverride = clipboard.Text;
                        }
                    }
                };
                var result = DialogResult.Cancel;

                var buttonOk = new Button {Text = "Ok"};
                buttonOk.Click += (sender, args) =>
                {
                    result = DialogResult.Ok;
                    dialog.Close();
                };

                var buttonCancel = new Button {Text = "Cancel"};
                buttonCancel.Click += (sender, args) =>
                {
                    result = DialogResult.Cancel;
                    dialog.Close();
                };

                dialog.DefaultButton = buttonOk;
                dialog.AbortButton = buttonCancel;

                var layout = new DynamicLayout();

                layout.BeginVertical(); // fields section
                layout.AddRow(new Label {Text = "Password"});
                layout.AddRow(passwordBox);
                layout.EndVertical();

                layout.BeginVertical(); // buttons section
                // passing null in AddRow () creates a scaled column
                layout.AddRow(null, buttonOk, buttonCancel);
                layout.EndVertical();

                dialog.Content = layout;
                dialog.Topmost = true;

                var center = Screen.PrimaryScreen.WorkingArea.Center;
                center.X -= 80;
                center.Y -= 40;

                //dialog.Location = new Point(center);
                //owner.Location

                dialog.ShowModal(owner);
                dialog.Visible = true;

                return result == DialogResult.Ok ? passwordOverride ?? passwordBox.Text : null;
            }
        }
    }
}
