using Eto.Drawing;
using Eto.Forms;
using EtoTest.Interfaces;
using EtoTest.S3Files;
using EtoTest.ViewModel;

namespace EtoTest.Dialogs
{
    sealed class SecurePartsLayout : Form
    {

        public SecurePartsLayout()
        {
            ISecretKeyProvider secretKeyProvider = new SecretKeyProvider();
            var s3Files = new S3FileGridPresenter(new S3Uploader(secretKeyProvider));
            var credentials = new CredentialGridPresenter(this, secretKeyProvider);

            var layout = new DynamicLayout();

            layout.BeginVertical(); // create a fields section
            layout.BeginHorizontal();
            layout.Add(new Label { Text = "Search" });
            layout.Add(credentials.SearchBox.Value, true);
            layout.EndHorizontal();
            layout.EndVertical();

            layout.BeginVertical(null, null, true, true);
            layout.BeginHorizontal(true);
            layout.Add(credentials.CredentialGrid.Value, true, true);
            layout.EndHorizontal();
            layout.EndVertical();

            layout.BeginVertical(null, null, true, false); // buttons section
            layout.BeginHorizontal(true);
            layout.Add(null, true, true);
            layout.EndHorizontal();
            layout.BeginHorizontal(false);
            layout.Add(null, true, false);
            layout.Add(credentials.NewButton.Value, false, false);
            layout.Add(credentials.LoadButton.Value, false, false);
            layout.Add(credentials.SaveButton.Value, false, false);
            layout.EndHorizontal();
            layout.EndVertical();


            var s3Uploader = new DynamicLayout();
            s3Uploader.BeginVertical(null, null, true, true); // buttons section
            s3Uploader.BeginHorizontal(true);
            s3Uploader.Add(s3Files.Grid.Value, true, true);
            s3Uploader.EndHorizontal();
            s3Uploader.EndVertical();

            s3Uploader.BeginVertical(null, null, true, false); // buttons section
            s3Uploader.BeginHorizontal(true);
            s3Uploader.Add(null, true, true);
            s3Uploader.EndHorizontal();

            s3Uploader.BeginHorizontal(false);
            s3Uploader.Add(null, true, false);
            s3Uploader.Add(s3Files.UploadToS3.Value(()=>layout), false, false);
            s3Uploader.Add(s3Files.RefreshS3Files.Value, false, false);
            s3Uploader.Add(s3Files.DeleteS3Files.Value, false, false);
            s3Uploader.EndHorizontal();
            s3Uploader.EndVertical();

            var tabControl = new TabControl();
            tabControl.Pages.Add(new TabPage(layout) { Text = "Passwords" });
            tabControl.Pages.Add(new TabPage(s3Uploader) { Text = "Sharing" });

            Content = tabControl;
            Size = new Size(500, 300);

            Title = "Secure Things";
        }


    }
}