using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Eto.Forms;
using EtoTest.Model;
using EtoTest.S3Files;

namespace EtoTest.ViewModel
{
    internal class S3FileGridPresenter
    {

        public S3FileGridPresenter(S3Uploader s3Uploader)
        {
            S3Files = new ObservableCollection<S3File>();
            Grid = new Lazy<GridView>(() => CreateS3FilesGrid(s3Uploader, S3Files));
            UploadToS3 = new Lazy<Func<Func<Control>, Button>>(() => (control => CreateUploadToS3Button(s3Uploader, control, S3Files)));
            RefreshS3Files = new Lazy<Button>(() => CreateRefreshS3FilesButton(Grid, s3Uploader, S3Files));
            DeleteS3Files = new Lazy<Button>(() => CreateDeleteS3FilesButton(Grid, s3Uploader, S3Files));
        }

        private ObservableCollection<S3File> S3Files { get; }
        public Lazy<GridView> Grid { get; }
        public Lazy<Func<Func<Control>, Button>> UploadToS3 { get; }
        public Lazy<Button> RefreshS3Files { get; }
        public Lazy<Button> DeleteS3Files { get; }

        private static GridView CreateS3FilesGrid(S3Uploader s3Uploader, ObservableCollection<S3File> s3Files)
        {
            var s3FilesGrid = new GridView {DataStore = s3Files, AllowColumnReordering = true};
            s3FilesGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell {Binding = Binding.Property<S3File, string>(r => r.Key)},
                HeaderText = "Key"
            });
            s3FilesGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell {Binding = Binding.Property<S3File, String>(r => r.AgeInDaysString)},
                HeaderText = "Age"
            });

            s3FilesGrid.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell {Binding = Binding.Property<S3File, bool?>(r => r.Check)},
                HeaderText = "Check"
            });
            s3FilesGrid.CellClick += (sender, args) =>
            {
                if (args.Column == 2)
                {
                    ((S3File)args.Item).Check = !((S3File)args.Item).Check;
                }
                else
                {
                    using (Clipboard clipboard = new Clipboard())
                    {
                        clipboard.Text = s3Uploader.GetPublicUri(((S3File)args.Item).Key).ToString();
                    }
                }
            };

            return s3FilesGrid;
        }

        private static Button CreateUploadToS3Button(S3Uploader s3Uploader, Func<Control> parent, ObservableCollection<S3File> s3Files)
        {
            var uploadFileToS3 = new Button {Text = "Upload"};
            uploadFileToS3.Click += (sender, args) =>
            {
                using (var ofd = new OpenFileDialog() {CheckFileExists = true})
                {
                    var result = ofd.ShowDialog(parent());
                    if (result == DialogResult.Ok)
                    {
                        if (File.Exists(ofd.FileName))
                        {
                            var s3 = s3Uploader.GetS3Client();
                            using (var fs = File.OpenRead(ofd.FileName))
                            {
                                var uploadPath = S3Uploader.StoreForLaterAccess(s3, fs, Path.GetFileName(ofd.FileName));
                                s3Files.Add(uploadPath);
                            }
                        }
                    }
                }
            };
            return uploadFileToS3;
        }

        private static Button CreateRefreshS3FilesButton(Lazy<GridView> uploader, S3Uploader s3Uploader, ObservableCollection<S3File> s3Files)
        {
            var refreshS3Files = new Button {Text = "Refresh"};
            refreshS3Files.Click += (sender, args) =>
            {
                var gv = uploader.Value;
                var s = gv.SelectedItems.ToList();
                s3Uploader.RefreshFiles(s3Files);
            };
            return refreshS3Files;
        }


        private static Button CreateDeleteS3FilesButton(Lazy<GridView> uploader, S3Uploader s3Uploader, ObservableCollection<S3File> s3Files)
        {
            var deleteS3Files = new Button { Text = "Delete" };
            deleteS3Files.Click += (sender, args) =>
            {
                var gv = uploader.Value;
                var selectedItems = gv.SelectedItems.Cast<S3File>().ToList();
                s3Uploader.DeleteFiles(selectedItems);
                s3Uploader.RefreshFiles(s3Files);
            };
            return deleteS3Files;
        }

    }
}