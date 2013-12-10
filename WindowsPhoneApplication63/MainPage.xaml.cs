using System;
using System.IO;
using System.Windows.Media.Imaging;
using ExifLib;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;

namespace WindowsPhoneApplication63
{
    public partial class MainPage : PhoneApplicationPage
    {
        Stream capturedImage;
        int _width;
        int _height;
        ExifLib.ExifOrientation _orientation;
        int _angle;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += new System.Windows.RoutedEventHandler(MainPage_Loaded);
            OrientationChanged += new EventHandler<OrientationChangedEventArgs>(MainPage_OrientationChanged);
        }

        void MainPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            //PostedUri.Text = this.Orientation.ToString();
        }

        void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            PostedUri.Text = this.Orientation.ToString();
        }

        void OnCameraCaptureCompleted(object sender, PhotoResult e)
        {
            // figure out the orientation from EXIF data
            e.ChosenPhoto.Position = 0;
            JpegInfo info = ExifReader.ReadJpeg(e.ChosenPhoto, e.OriginalFileName);

            _width = info.Width;
            _height = info.Height;
            _orientation = info.Orientation;

            PostedUri.Text = info.Orientation.ToString();

            switch (info.Orientation)
            {
                case ExifOrientation.TopLeft:
                case ExifOrientation.Undefined:
                    _angle = 0;
                    break;
                case ExifOrientation.TopRight:
                    _angle = 90;
                    break;
                case ExifOrientation.BottomRight:
                    _angle = 180;
                    break;
                case ExifOrientation.BottomLeft:
                    _angle = 270;
                    break;
            }

            if (_angle > 0d)
            {
                capturedImage = RotateStream(e.ChosenPhoto, _angle);
            }
            else
            {
                capturedImage = e.ChosenPhoto;
            }

            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(capturedImage);

            ChosenPicture.Source = bmp;
        }

        private Stream RotateStream(Stream stream, int angle)
        {
            stream.Position = 0;
            if (angle % 90 != 0 || angle < 0) throw new ArgumentException();
            if (angle % 360 == 0) return stream;

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            WriteableBitmap wbSource = new WriteableBitmap(bitmap);

            WriteableBitmap wbTarget = null;
            if (angle % 180 == 0)
            {
                wbTarget = new WriteableBitmap(wbSource.PixelWidth, wbSource.PixelHeight);
            }
            else
            {
                wbTarget = new WriteableBitmap(wbSource.PixelHeight, wbSource.PixelWidth);
            }

            for (int x = 0; x < wbSource.PixelWidth; x++)
            {
                for (int y = 0; y < wbSource.PixelHeight; y++)
                {
                    switch (angle % 360)
                    {
                        case 90:
                            wbTarget.Pixels[(wbSource.PixelHeight - y - 1) + x * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 180:
                            wbTarget.Pixels[(wbSource.PixelWidth - x - 1) + (wbSource.PixelHeight - y - 1) * wbSource.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 270:
                            wbTarget.Pixels[y + (wbSource.PixelWidth - x - 1) * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                    }
                }
            }
            MemoryStream targetStream = new MemoryStream();
            wbTarget.SaveJpeg(targetStream, wbTarget.PixelWidth, wbTarget.PixelHeight, 0, 100);
            return targetStream;
        }

        private void OnMenuTakeClicked(object sender, EventArgs e)
        {
            CameraCaptureTask cam = new CameraCaptureTask();
            cam.Completed += new EventHandler<PhotoResult>(OnCameraCaptureCompleted);
            cam.Show();
        }

        private void OnMenuChooseClicked(object sender, EventArgs e)
        {
            PhotoChooserTask pix = new PhotoChooserTask();
            pix.Completed += new EventHandler<PhotoResult>(OnCameraCaptureCompleted);
            pix.ShowCamera = true;
            pix.Show();
        }

        private void OnPostClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            capturedImage = null;
        }

        private void OnSavePictureClicked(object sender, EventArgs e)
        {
            if (capturedImage != null)
            {
                capturedImage.Seek(0, 0);

                MediaLibrary ml = new MediaLibrary();
                try
                {
                    Picture p = ml.SavePicture(Guid.NewGuid().ToString(), capturedImage);
                    PostedUri.Text += ":" + p.Name;
                }
                catch (Exception ex)
                {
                    PostedUri.Text = ex.Message;
                }
            }
        }
    }
}