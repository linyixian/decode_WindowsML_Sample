using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

//
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.AI.MachineLearning.Preview;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.Media.FaceAnalysis;
using Windows.UI;
using Windows.UI.Core;
using Windows.Storage;



// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace decode_WindowsML_Sample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private MediaCapture mediaCapture;
        private FaceTracker faceTracker;
        private ThreadPoolTimer timer;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private LearningModelPreview model;                                 //モデルオブジェクト
        private const string ModelFileName = "face.onnx";                   //モデルファイル名
        private ImageVariableDescriptorPreview inputImageDescription;       
        private MapVariableDescriptorPreview outputMapDescription;
        private TensorVariableDescriptorPreview outputTensorDescription;
        private int lossNum = 2;                                            //CustomVisionで定義したTagの数
        private IDictionary<string,float> loss { get; set; }                //推定結果を受け取る

        /// <summary>
        /// 
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Modelの読み込み
            await LoadModelAsysnc();

            //FaceTrackerオブジェクトの作成
            if (faceTracker == null)
            {
                faceTracker = await FaceTracker.CreateAsync();
            }

            //カメラの初期化
            await InitCameraAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task InitCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");

            try
            {
                //mediaCaptureオブジェクトが有効な時は一度Disposeする
                if (mediaCapture != null)
                {
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                //キャプチャーの設定
                var captureInitSettings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = "",
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                //カメラデバイスの取得
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                if (cameraDevices.Count() == 0)
                {
                    Debug.WriteLine("No Camera");
                    return;
                }
                else if (cameraDevices.Count() == 1)
                {
                    captureInitSettings.VideoDeviceId = cameraDevices[0].Id;
                }
                else
                {
                    captureInitSettings.VideoDeviceId = cameraDevices[1].Id;
                }

                //キャプチャーの準備
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(captureInitSettings);

                var resolutions = GetPreviewResolusions(mediaCapture);

                VideoEncodingProperties vp = new VideoEncodingProperties();

                vp.Height = 240;
                vp.Width = 320;
                vp.Subtype = "YUY2";

                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, vp);

                capture.Source = mediaCapture;

                //キャプチャーの開始
                await mediaCapture.StartPreviewAsync();

                Debug.WriteLine("Camera Initialized");

                //15FPS毎にタイマーを起動する。
                TimeSpan timerInterval = TimeSpan.FromMilliseconds(66);
                timer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(CurrentVideoFrame), timerInterval);

            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="timer"></param>
        private async void CurrentVideoFrame(ThreadPoolTimer timer)
        {
            //追跡動作中の場合は処理をしない
            if (!semaphore.Wait(0))
            {
                return;
            }

            try
            {
                IList<DetectedFace> faces = null;

                //FaceTrackingではNv12フォーマットのみ対応
                using (VideoFrame previewFrame = new VideoFrame(BitmapPixelFormat.Nv12, 320, 240))
                //CustomVisionで出力したモデルの入力は224x224サイズでフォーマットはBGRA8となる
                using (VideoFrame inputFrame = new VideoFrame(BitmapPixelFormat.Bgra8, 224, 224))
                {
                    //ビデオフレームの取得
                    //フォーマットが違うので別々にフレームを取得
                    await mediaCapture.GetPreviewFrameAsync(previewFrame);
                    await mediaCapture.GetPreviewFrameAsync(inputFrame);

                    
                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        //顔認識の実行
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);
                    }
                    else
                    {
                        throw new NotSupportedException("PixelFormat 'Nv12' is not supported by FaceDetector");
                    }

                    //FaceTrackingで顔が見つかった場合のみ顔認証を行っている。
                    if (faces.Count > 0)
                    {
                        //認識に使ったフレームのサイズ取得
                        var previewFrameSize = new Size(previewFrame.SoftwareBitmap.PixelWidth, previewFrame.SoftwareBitmap.PixelHeight);

                        //顔追跡はUIスレッドとは別スレッドなので顔の位置表示のためにUIスレッドに切り替え
                        var ignored = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            FaceDraw(previewFrameSize, faces, previewFrame.SoftwareBitmap);
                        });

                        //顔認証
                        await EvaluateVideoFrameAsync(inputFrame);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LoadModelAsysnc()
        {
            if (model != null) return;

            try
            {
                //モデルの読み込み
                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{ModelFileName}"));
                model = await LearningModelPreview.LoadModelFromStorageFileAsync(modelFile);

                
                List<ILearningModelVariableDescriptorPreview> inputFeatures = model.Description.InputFeatures.ToList();
                List<ILearningModelVariableDescriptorPreview> outputFeatures = model.Description.OutputFeatures.ToList();

                //入力データの形式を設定
                inputImageDescription = inputFeatures.FirstOrDefault(feature => feature.ModelFeatureKind == LearningModelFeatureKindPreview.Image) as ImageVariableDescriptorPreview;

                //出力データの形式を設定
                outputMapDescription = outputFeatures.FirstOrDefault(feature => feature.ModelFeatureKind == LearningModelFeatureKindPreview.Map) as MapVariableDescriptorPreview;
                outputTensorDescription = outputFeatures.FirstOrDefault(feature => feature.ModelFeatureKind == LearningModelFeatureKindPreview.Tensor) as TensorVariableDescriptorPreview;

                //オプション
                model.InferencingOptions.ReclaimMemoryAfterEvaluation = true;
                
                //GPUが利用できる環境ではコメントを外すと処理速度がアップする
                //model.InferencingOptions.PreferredDeviceKind = LearningModelDeviceKindPreview.LearningDeviceGpu;

                //Outputを受け取るためにあらかじめ記録領域を用意する必要がある。
                //CustomVisionから出力したモデルの場合はTagの数だけ用意しておく。
                loss = new Dictionary<string, float>();
                
                for (var i = 0; i < lossNum; i++)
                {
                    loss.Add(i.ToString(), float.NaN);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="previewFrame"></param>
        /// <returns></returns>
        private async Task EvaluateVideoFrameAsync(VideoFrame previewFrame)
        {
            if (previewFrame != null)
            {
                try
                {
                    //一番確率の高い結果のラベル（CustomVisionのTag）が入る
                    IList<string> classLabel = new List<string>();

                    LearningModelBindingPreview binding = new LearningModelBindingPreview(model as LearningModelPreview);
                    
                    //入力データをモデルにバインド
                    binding.Bind(inputImageDescription.Name, previewFrame);

                    //出力データをモデルにバインド（Tag毎の推論値が入る）
                    binding.Bind(outputMapDescription.Name, loss);
                    //出力データをモデルにバインド（一番推論値の高いラベル）
                    binding.Bind(outputTensorDescription.Name, classLabel);

                    var stopwatch = Stopwatch.StartNew();

                    //推論の実行
                    LearningModelEvaluationResultPreview results = await model.EvaluateAsync(binding, string.Empty);

                    stopwatch.Stop();

                    //推論の結果を表示
                    //これ以降はアプリケーションの目的ごとに内容を考える
                    //このサンプルでは処理速度の計測結果とTag毎の推論値、一番確率の高いTagの表示を行っている
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        fpsTBlock.Text = $"{1000f / stopwatch.ElapsedMilliseconds,4:f1} fps";
                        string txt = "";
                        
                        foreach(KeyValuePair<string,float> item in loss)
                        {
                            txt =txt+ item.Key.ToString() + " : " + item.Value.ToString("0.00%") + "\n\n";
                        }

                        txt = txt + "-----------------------------------------------------\n\n";
                        txt = txt + classLabel[0]+"さんを認識しました。";
                        statusTBlock.Text = txt;

                    });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FramePixelSize"></param>
        /// <param name="foundFaces"></param>
        private void FaceDraw(Size FramePixelSize, IList<DetectedFace> foundFaces, SoftwareBitmap previewBitmap)
        {
            //Canvasをクリア
            canvas.Children.Clear();

            double actualWidth = canvas.ActualWidth;
            double actualHeight = canvas.ActualHeight;

            if (foundFaces != null && actualWidth != 0 && actualHeight != 0)
            {
                double widthScale = FramePixelSize.Width / actualWidth;
                double heightScale = FramePixelSize.Height / actualHeight;

                //見つかった顔の位置に四角を描画
                foreach (DetectedFace face in foundFaces)
                {
                    Rectangle box = new Rectangle();
                    box.Width = (uint)(face.FaceBox.Width / widthScale);
                    box.Height = (uint)(face.FaceBox.Height / heightScale);
                    box.Fill = new SolidColorBrush(Colors.Transparent);
                    box.Stroke = new SolidColorBrush(Colors.Yellow);
                    box.StrokeThickness = 2.0;
                    box.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale), 0, 0);

                    this.canvas.Children.Add(box);

                    Ellipse ellipse = new Ellipse();
                    ellipse.Stroke = new SolidColorBrush(Colors.Yellow);
                    ellipse.Fill = new SolidColorBrush(Colors.Yellow);
                    ellipse.Width = 10;
                    ellipse.Height = 10;
                    ellipse.Margin = new Thickness((uint)((face.FaceBox.X / widthScale) + box.Width / 2 - 5), (uint)((face.FaceBox.Y / heightScale) + box.Height / 2 - 5), 0, 0);
                    this.canvas.Children.Add(ellipse);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="capture"></param>
        /// <returns></returns>
        private List<VideoEncodingProperties> GetPreviewResolusions(MediaCapture capture)
        {
            IReadOnlyList<IMediaEncodingProperties> ret;
            ret = capture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);

            if (ret.Count <= 0)
            {
                return new List<VideoEncodingProperties>();
            }


            //接続しているカメラの対応解像度やSubtypeを確認するときはコメントを外す
            /*
            foreach (VideoEncodingProperties vp in ret)
            {
                var frameRate = (vp.FrameRate.Numerator / vp.FrameRate.Denominator);
                Debug.WriteLine("{0}: {1}x{2} {3}fps", vp.Subtype, vp.Width, vp.Height, frameRate);
            }
            */

            return ret.Select(item => (VideoEncodingProperties)item).ToList();
        }
    }
}

// © 2018　株式会社リンシステムズ All rights reserved.
