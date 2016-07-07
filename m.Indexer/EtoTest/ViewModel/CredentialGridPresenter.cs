using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;
using EtoTest.Dialogs;
using EtoTest.Encrypt;
using EtoTest.Interfaces;
using EtoTest.IO;
using EtoTest.Model;
using EtoTest.SecureFiles;
using EtoTest.Serialisation;

namespace EtoTest.ViewModel
{
    class CredentialGridPresenter
    {
        private const string DataFileName = "muipw.enc";
        private readonly ISecretKeyProvider _secretKeyProvider;

        public Lazy<TextBox> SearchBox { get; }
        public Lazy<Button> SaveButton { get; }
        public Lazy<Button> LoadButton { get; }
        public Lazy<Button> NewButton { get; }

        public Lazy<GridView> CredentialGrid { get; }
        public FilterCollection<Credential> FilterCollection { get; }

        public CredentialGridPresenter(Control parent, ISecretKeyProvider secretKeyProvider)
        {
            _secretKeyProvider = secretKeyProvider;
            FilterCollection = new FilterCollection<Credential>();
            CredentialGrid = new Lazy<GridView>(() => CreateCredentialGrid(FilterCollection));

            SearchBox = new Lazy<TextBox>(CreateSearchBox);

            NewButton = new Lazy<Button>(CreateNewButton);

            LoadButton = new Lazy<Button>(() => CreateLoadButton(parent));

            SaveButton = new Lazy<Button>(() => CreateSaveButton(parent));
        }

        private Button CreateSaveButton(Control parent)
        {
            var b = new Button {Text = "Save"};
            b.Click += (sender, args) =>
            {
                if (FilterCollection.Count > 1)
                {
                    var result = PasswordInputBox.GetPassword(parent, "Master Password");
                    if (!string.IsNullOrEmpty(result))
                    {
                        using (var password = new SecureStringOrArray(Encoding.ASCII.GetBytes(result)))
                        {
                            var folderBasedFilePathProvider = new FolderBasedFilePathProvider(".");
                            var localSecureFileRepository = new LocalSecureFileRepository(folderBasedFilePathProvider, "muipw.iv", DataFileName, "muipw.ver", "muipw.salt");
                            var aesFormatter = new AesFormatter(localSecureFileRepository);

                            try
                            {
                                //just test that the password is correct by deserializing the current data.
                                var credentialSet = aesFormatter.DeserializeData<CredentialSet>(password);
                                _secretKeyProvider.Initialize(credentialSet.Credentials);
                                _secretKeyProvider.Initialize(FilterCollection);
                                var mSecretsSecretKey = _secretKeyProvider.GetSecret(SecretKeyConstants.MSecretsSecretKeyName);
                                if (mSecretsSecretKey == null)
                                {
                                    MessageBox.Show($"Missing {SecretKeyConstants.MSecretsSecretKeyName}!");
                                    return;
                                }
                                var fileBroker2 = SecureFileBroker.Create(folderBasedFilePathProvider, mSecretsSecretKey, "muipw.iv", DataFileName, "muipw.ver", "muipw.salt");
                                aesFormatter = new AesFormatter(fileBroker2);

                                var set = new CredentialSet();
                                foreach (var credential in FilterCollection)
                                {
                                    set.Credentials.Add(credential);
                                }
                                aesFormatter.SerializeData(password, set);
                            }
                            catch (Exception)
                            {
                                MessageBox.Show("Incorrect password!");
                            }
                        }
                    }
                }
            };
            return b;
        }

        private Button CreateLoadButton(Control parent)
        {
            var b = new Button {Text = "Load"};
            b.Click += (sender, args) =>
            {
                var folderBasedFilePathProvider = new FolderBasedFilePathProvider(".");
                var localSecureFileRepository = new LocalSecureFileRepository(folderBasedFilePathProvider,
                    "muipw.iv", DataFileName, "muipw.ver", "muipw.salt");
                var aesFormatter = new AesFormatter(localSecureFileRepository);
                var password = PasswordInputBox.GetPassword(parent, "Master Password");
                try
                {
                    var credentialSet =
                        aesFormatter.DeserializeData<CredentialSet>(
                            new SecureStringOrArray(Encoding.ASCII.GetBytes(password)));
                    _secretKeyProvider.Initialize(credentialSet.Credentials);
                    var mSecretsSecretKey = _secretKeyProvider.GetSecret(SecretKeyConstants.MSecretsSecretKeyName);
                    if (mSecretsSecretKey == null)
                    {
                        MessageBox.Show(parent, $"Missing {SecretKeyConstants.MSecretsSecretKeyName}!  Can't sync with S3.");
                        FilterCollection.Clear();
                        FilterCollection.AddRange(credentialSet.Credentials);
                        return;
                    }
                    var fileBroker2 = SecureFileBroker.Create(new FolderBasedFilePathProvider("."), mSecretsSecretKey,
                        "muipw.iv", DataFileName, "muipw.ver", "muipw.salt");
                    aesFormatter = new AesFormatter(fileBroker2);
                    credentialSet =
                        aesFormatter.DeserializeData<CredentialSet>(
                            new SecureStringOrArray(Encoding.ASCII.GetBytes(password)));

                    FilterCollection.Clear();
                    FilterCollection.AddRange(credentialSet.Credentials);
                }
                catch (Exception e)
                {
                    var dataFilePath =Path.GetFullPath($"./{DataFileName}");
                    var dataFileStart = string.Join(",", localSecureFileRepository.GetDataFile().Take(3).Select(a => ((int)a).ToString()).ToArray());
                    var salt = string.Join(",", localSecureFileRepository.GetSalt().Take(3).Select(a => ((int)a).ToString()).ToArray());
                    var iv = string.Join(",", localSecureFileRepository.GetIv().Take(3).Select(a => ((int)a).ToString()).ToArray());
                    var pw = string.Join(",", Encoding.ASCII.GetBytes(password).Take(3).Select(a => ((int)a).ToString()).ToArray());
                    
                    MessageBox.Show(parent, $"Incorrect Password For {dataFilePath}, {dataFileStart} {salt} {iv} {pw}.");
                }
            };
            return b;
        }

        private Button CreateNewButton()
        {
            var b = new Button {Text = "New"};
            b.Click += (sender, args) =>
            {
                FilterCollection.Add(new Credential()
                {
                    Url = "new",
                });
            };
            return b;
        }

        private TextBox CreateSearchBox()
        {
            var tb = new TextBox();
            tb.TextChanged += (sender, args) =>
            {
                FilterCollection.Filter = poco =>
                {
                    if (string.IsNullOrEmpty(SearchBox.Value.Text))
                        return true;
                    else
                    {
                        return poco.Url.IndexOf(SearchBox.Value.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                };
            };
            return tb;
        }

        private static GridView CreateCredentialGrid(FilterCollection<Credential> filterCollection)
        {
            var credentialGrid = new GridView { DataStore = filterCollection };
            credentialGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<Credential, string>(r => r.Url) },
                HeaderText = "Url",
                Editable = true
            });
            credentialGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<Credential, string>(r => r.UserId) },
                HeaderText = "UserId",
                Editable = true
            });
            credentialGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Delegate((Func<Credential, string>)(r => "Password")) },
                HeaderText = "Password"
            });
            var semaphore = new WeakSemaphore();
            credentialGrid.CellClick += (sender, args) =>
            {
                semaphore.Release();
                StartDelayedSTATask(400, () =>
                    {
                        if(!semaphore.Access())
                        {
                            return 0;
                        }
                        Application.Instance.Invoke(() =>
                        {
                            var item = args.Item as Credential;
                            if (item == null) return;
                            switch (args.Column)
                            {
                                case 0:
                                    SetClipboardValue(item.Url);
                                    break;
                                case 1:
                                    SetClipboardValue(item.UserId);
                                    break;
                                case 2:
                                    SetClipboardValue(item.GetPasswordAsString());
                                    break;
                            }
                        });
                        return 1;
                    });
            };
            credentialGrid.CellDoubleClick += (sender, args) =>
            {
                if (!semaphore.Access())
                {
                    return;
                }
                var item = args.Item as Credential;
                if (item == null) return;
                switch (args.Column)
                {
                    case 0://url
                        credentialGrid.BeginEdit(args.Row, args.Column);
                        break;
                    case 1://userid
                        credentialGrid.BeginEdit(args.Row, args.Column);
                        break;
                    case 2:
                        var password = PasswordInputBox.GetPassword(credentialGrid, "Password Entry");
                        if (!string.IsNullOrEmpty(password))
                        {
                            item.SetPassword(password);
                        }
                        break;
                }
            };
            return credentialGrid;
        }
        private static Task StartDelayedSTATask<T>(int msDelay, Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    Thread.Sleep(msDelay);
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
        private static void SetClipboardValue(string value)
        {
            using (Clipboard clipboard = new Clipboard())
            {
                if (String.IsNullOrEmpty(value))
                {
                    //clipboard.Clear();
                }
                else
                {
                    clipboard.Text = value;
                }
            }
        }
    }
}
