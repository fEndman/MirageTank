using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace MirageTank
{
    public partial class MainPage : ContentPage
    {
        private FileResult photoFile1 = null;
        private FileResult photoFile2 = null;

        public MemoryStream photoTankStream = new MemoryStream();

        private float photo1_K = 1.2f;
        private float photo2_K = 0.6f;
        private byte photoThreshold = 110;

        public MainPage()
        {
            InitializeComponent();
            this.BackgroundColor = Color.LightGray;
        }

        private async void Image1_Clicked(object sender, EventArgs e)
        {
            photoFile1 = await MediaPicker.PickPhotoAsync();
            if (photoFile1 == null)
                return;
            else
                Image1.Source = photoFile1.FullPath;
        }

        private async void Image2_Clicked(object sender, EventArgs e)
        {
            photoFile2 = await MediaPicker.PickPhotoAsync();
            if (photoFile2 == null)
                return;
            else
                Image2.Source = photoFile2.FullPath;
        }

        private async void Image5_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ImagePage(photoTankStream));
        }

        private void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            if (Switch1.IsToggled) Image5.BackgroundColor = Color.Black;
            else Image5.BackgroundColor = Color.White;
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            DoTankAssemble();
        }

        private async void Button_Clicked_2(object sender, EventArgs e)
        {
            if (photoTankStream.Length != 0)
            {
                string path = await DependencyService.Get<IAndroidService>().ImageSave(photoTankStream);
                await DisplayAlert("完成", "已保存至：\n" + path + "\n可在MirageTank相册中找到", "确认");
            }
            else
            {
                await DisplayAlert("警告", "请先生成图片！", "确认");
            }
        }

        private async void Button_Clicked_3(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }

        private void Entry1_Completed(object sender, EventArgs e)
        {
            float.TryParse(Entry1.Text, out photo1_K);
            if (photo1_K > 10f) photo1_K = 10f;
            if (photo1_K < 0.6f) photo1_K = 0.6f;
            Entry1.Text = photo1_K.ToString("F2");
        }

        private void Entry2_Completed(object sender, EventArgs e)
        {
            float.TryParse(Entry2.Text, out photo2_K);
            if (photo2_K > 1.4f) photo2_K = 1.4f;
            if (photo2_K < 0.01f) photo2_K = 0.01f;
            Entry2.Text = photo2_K.ToString("F2");
        }

        private void Entry3_Completed(object sender, EventArgs e)
        {
            byte.TryParse(Entry3.Text, out photoThreshold);
            if (photoThreshold > 200) photoThreshold = 200;
            if (photoThreshold < 50) photoThreshold = 50;
            Entry3.Text = photoThreshold.ToString("D3");
        }

        private async void DoTankAssemble()
        {
            int height = 0, width = 0;
            MemoryStream photo1Stream = new MemoryStream();
            MemoryStream photo2Stream = new MemoryStream();
            SKColor[] photo1ColorArray = null;
            SKColor[] photo2ColorArray = null;
            SKBitmap photo1 = null;
            SKBitmap photo2 = null;
            SKBitmap photoTank = null;

            if (photoFile1 == null ||
                photoFile2 == null ||
                string.IsNullOrEmpty(photoFile1.FullPath) ||
                string.IsNullOrEmpty(photoFile2.FullPath))
            {
                await DisplayAlert("警告", "缺少图片！", "确认");
                return;
            }
            else
            {
                photo1 = SKBitmap.Decode(photoFile1.FullPath);
                photo2 = SKBitmap.Decode(photoFile2.FullPath);
            }

            Image3.Source = null;
            Image4.Source = null;
            Image5.Source = null;

            ActivityIndicator1.IsRunning = true;
            ProgressBar1.Progress = 0f;
            ProgressBar1.ProgressColor = Color.DeepSkyBlue;

            await Task.Run(() =>
            {

                //灰度处理
                photo1ColorArray = photo1.Pixels;
                for (int i = 0; i < photo1ColorArray.Length; i++)
                {
                    if (i % 100 == 0)
                        ProgressBar1.Progress = ((float)i / (float)photo1ColorArray.Length) / 3f;
                    int tmpValue = (int)(GetGrayNumColor(photo1ColorArray[i]) * photo1_K);
                    if (tmpValue < photoThreshold + 1) tmpValue = photoThreshold + 1;
                    if (tmpValue > 254) tmpValue = 254;
                    photo1ColorArray[i] = new SKColor((byte)tmpValue, (byte)tmpValue, (byte)tmpValue);
                }
                photo1.Pixels = photo1ColorArray;
                photo1.Encode(photo1Stream, SKEncodedImageFormat.Png, 100);

                photo2ColorArray = photo2.Pixels;
                for (int i = 0; i < photo2ColorArray.Length; i++)
                {
                    if (i % 100 == 0)
                        ProgressBar1.Progress = 0.33333f + ((float)i / (float)photo2ColorArray.Length) / 3f;
                    int tmpValue = (int)(GetGrayNumColor(photo2ColorArray[i]) * photo2_K);
                    if (tmpValue > photoThreshold) tmpValue = photoThreshold;
                    photo2ColorArray[i] = new SKColor((byte)tmpValue, (byte)tmpValue, (byte)tmpValue);
                }
                photo2.Pixels = photo2ColorArray;
                photo2.Encode(photo2Stream, SKEncodedImageFormat.Png, 100);

                /*  /!/装配坦克/!/  */
                //调整图像大小
                if (photo1.Height != photo2.Height || photo1.Width != photo2.Width)
                {
                    if (photo1.Height > photo2.Height) height = photo1.Height;
                    else height = photo2.Height;
                    if (photo1.Width > photo2.Width) width = photo1.Width;
                    else width = photo2.Width;

                    photo1 = photo1.Resize(new SKSizeI(width, height), SKFilterQuality.High);
                    photo1ColorArray = photo1.Pixels;
                    photo2 = photo2.Resize(new SKSizeI(width, height), SKFilterQuality.High);
                    photo2ColorArray = photo2.Pixels;
                }
                else
                {
                    width = photo1.Width;
                    height = photo1.Height;
                }

                photoTank = new SKBitmap(width, height);
                SKColor[] photoTankColorArray = new SKColor[width * height];
                for (int i = 0; i < width * height; i++)
                {
                    if (i % 100 == 0)
                        ProgressBar1.Progress = 0.66667f + ((float)i / (float)(width * height)) / 3f;

                    int pixel1 = photo1ColorArray[i].Red;
                    int pixel2 = photo2ColorArray[i].Red;

                    int alpha = 255 - (pixel1 - pixel2);
                    int gray = (int)(255 * pixel2 / alpha);

                    photoTankColorArray[i] = new SKColor((byte)gray, (byte)gray, (byte)gray, (byte)alpha);
                }
                photoTank.Pixels = photoTankColorArray;
                photoTankStream = new MemoryStream();
                photoTank.Encode(photoTankStream, SKEncodedImageFormat.Png, 100);

            });

            Image3.Source = ImageSource.FromStream(() => new MemoryStream(photo1Stream.ToArray()));
            Image4.Source = ImageSource.FromStream(() => new MemoryStream(photo2Stream.ToArray()));
            Image5.Source = ImageSource.FromStream(() => new MemoryStream(photoTankStream.ToArray()));

            ProgressBar1.Progress = 1f;
            ProgressBar1.ProgressColor = Color.MediumSeaGreen;
            ActivityIndicator1.IsRunning = false;
        }

        private int GetGrayNumColor(SKColor codecolor)
        {
            //return (codecolor.Red * 19595 + codecolor.Green * 38469 + codecolor.Blue * 7472) >> 16;
            return (int)((float)codecolor.Red * 0.229f + (float)codecolor.Green * 0.587f + (float)codecolor.Blue * 0.114f);
        }
    }
    public interface IAndroidService
    {
        Task<string> ImageSave(MemoryStream stream);
    }
}
