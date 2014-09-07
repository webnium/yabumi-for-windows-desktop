using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace yabumi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>

    public partial class MainWindow : Window
    {
        System.Windows.Point start;
        System.Windows.Point end;
        int status = 0; // 0: 開始時 1:選択中 2: 選択後
        Bitmap bmp;

        public MainWindow()
        {
            InitializeComponent();

            // カレントディレクトリを exe と同じ場所に設定
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            // 自身のディレクトリを取得する
            string CurrentDirectory = System.IO.Directory.GetCurrentDirectory();

            // Drag & dropされた時の処理
            string[] cmds = System.Environment.GetCommandLineArgs();

            if (cmds.Length > 1)
            {
                //ドロップされたファイルのパスをすべて表示
                Console.WriteLine("次のファイルがドロップされました");
                for (int i = 1; i < cmds.Length; i++)
                {
                    Console.WriteLine(cmds[i]);

                    if (isPng(cmds[i]) || isJpg(cmds[i]) || isGif(cmds[i]) || isSvg(cmds[i]) || isPdf(cmds[i]))
                    {
                        // PNG はそのままupload
                        uploadFile(0, cmds[i]);
                    }
                    else
                    {
                        // 上位機以外のファイルの場合PNG 形式に変換してアップロード
                        // 変換できない場合はメッセージを出して終了

                        /*
                                    TCHAR tmpDir[MAX_PATH], tmpFile[MAX_PATH];
                                    GetTempPath(MAX_PATH, tmpDir);
                                    GetTempFileName(tmpDir, _T("gya"), 0, tmpFile);

                                    if (convertPNG(tmpFile, __targv[1])) {
                                        //アップロード
                                        uploadFile(NULL, tmpFile);
                                    } else {
                                        // PNGに変換できなかった...
                                        MessageBox(NULL, _T("Cannot convert this image"), szTitle, 
                                            MB_OK | MB_ICONERROR);
                                    }
                                    DeleteFile(tmpFile);
                                }
                                return TRUE;
                         * */
                    }
                }
            } else {
                Console.WriteLine("ドロップされたファイルはありませんでした");
                // Screenshot取る

                //Bitmapの作成
                bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height);
                //Graphicsの作成
                Graphics g = Graphics.FromImage(bmp);
                //画面全体をコピーする
                g.CopyFromScreen(
                    new System.Drawing.Point(0, 0),
                    new System.Drawing.Point(0, 0),
                    bmp.Size);

                //解放
                g.Dispose();


                //表示


                using (Stream st = new MemoryStream())
                {
                    bmp.Save(st, System.Drawing.Imaging.ImageFormat.Bmp);

                    st.Seek(0, SeekOrigin.Begin);
                    image1.Source = BitmapFrame.Create(st, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }


                // Windowsの整形
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
//                this.Topmost = true;

                // 領域指定させる
                // カーソル変更
                image1.Cursor =
                    this.Cursor = System.Windows.Input.Cursors.Cross;
                // 画像アップロードする
            }
        }

        public void convertPNG(string tmpFile, string srcFile)
        {
//            Image b = new Image(srcFile, 0);
        }
        public string uploadFile(int hwnd, string filename)
        {
            string imageurl = "";
            const string UPLOAD_SERVER = @"direct.yabumi.cc";
            const string UPLOAD_PATH = @"/api/images.txt";
            //送信するファイルのパス

            //送信先のURL
            string url = "https://" + UPLOAD_SERVER + UPLOAD_PATH;
            //送信するファイルのパス
            
            //文字コード
            System.Text.Encoding enc = Encoding.UTF8;

            //区切り文字列
            string boundary = System.Environment.TickCount.ToString();

            //WebRequestの作成
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)
                System.Net.WebRequest.Create(url);
            req.UserAgent = @"YabumiUploaderForWindowsDesktop/1.1 Gyazowin/1.0";
            //メソッドにPOSTを指定
            req.Method = "POST";
            //ContentTypeを設定
            req.ContentType = "multipart/form-data; boundary=" + boundary;

            //POST送信するデータを作成
            string postData = "";
            postData = "--" + boundary + "\r\n" +
                "Content-Disposition: form-data; name=\"imagedata\"; filename=\"" +
                    filename + "\"\r\n" +
                "Content-Type: application/octet-stream\r\n" +
                "Content-Transfer-Encoding: binary\r\n\r\n";
            //バイト型配列に変換
            byte[] startData = enc.GetBytes(postData);
            postData = "\r\n--" + boundary + "--\r\n";
            byte[] endData = enc.GetBytes(postData);

            //送信するファイルを開く
            System.IO.FileStream fs = new System.IO.FileStream(
                filename, System.IO.FileMode.Open,
                System.IO.FileAccess.Read);

            //POST送信するデータの長さを指定
            req.ContentLength = startData.Length + endData.Length + fs.Length;

            //データをPOST送信するためのStreamを取得
            System.IO.Stream reqStream = req.GetRequestStream();
            //送信するデータを書き込む
            reqStream.Write(startData, 0, startData.Length);
            //ファイルの内容を送信
            byte[] readData = new byte[0x1000];
            int readSize = 0;
            while (true)
            {
                readSize = fs.Read(readData, 0, readData.Length);
                if (readSize == 0)
                    break;
                reqStream.Write(readData, 0, readSize);
            }
            fs.Close();
            reqStream.Write(endData, 0, endData.Length);
            reqStream.Close();

            //サーバーからの応答を受信するためのWebResponseを取得
            System.Net.HttpWebResponse res =
                (System.Net.HttpWebResponse)req.GetResponse();
            //応答データを受信するためのStreamを取得
            System.IO.Stream resStream = res.GetResponseStream();
            //受信して表示
            System.IO.StreamReader sr =
                new System.IO.StreamReader(resStream, enc);
            imageurl = sr.ReadToEnd();
            //閉じる
            sr.Close();
            return imageurl;
        }

        // ヘッダを見て PNG 画像かどうか(一応)チェック
        public bool isPng(string filename)
        {
            byte[] pngHead = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
	        byte[] readHead = new byte[8];
            try
            {
                // ファイル読み込み
                System.IO.FileStream fs = new System.IO.FileStream(
                    filename,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                fs.Read(readHead, 0, 8);
                //閉じる
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
 
            // compare
            for (int i = 0; i < 8; i++)
            {
                if (pngHead[i] != readHead[i])
                {
                    return false;
                }
            }
            return true;
        }
        // ヘッダを見て JPG 画像かどうか(一応)チェック
        public bool isJpg(string filename)
        {
            byte[] jpgHead = { 0xff, 0xd8 };
	        byte[] readHead = new byte[2];
            try
            {
                // ファイル読み込み
                System.IO.FileStream fs = new System.IO.FileStream(
                    filename,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                fs.Read(readHead, 0, 2);
                //閉じる
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
 
            // compare
            for (int i = 0; i < 2; i++)
            {
                if (jpgHead[i] != readHead[i])
                {
                    return false;
                }
            }
            return true;
        }
        // ヘッダを見て SVG かどうか(一応)チェック
        public bool isSvg(string filename)
        {
            byte[] svgHead = { 0x3c };
            byte[] readHead = new byte[1];
            try
            {
                // ファイル読み込み
                System.IO.FileStream fs = new System.IO.FileStream(
                    filename,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                fs.Read(readHead, 0, 1);
                //閉じる
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            // compare
            for (int i = 0; i < 1; i++)
            {
                if (svgHead[i] != readHead[i])
                {
                    return false;
                }
            }
            return true;
        }
        // ヘッダを見て PDF かどうか(一応)チェック
        public bool isPdf(string filename)
        {
            byte[] pdfHead = { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e };
            byte[] readHead = new byte[7];
            try
            {
                // ファイル読み込み
                System.IO.FileStream fs = new System.IO.FileStream(
                    filename,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                fs.Read(readHead, 0, 7);
                //閉じる
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            // compare
            for (int i = 0; 7 < 1; i++)
            {
                if (pdfHead[i] != readHead[i])
                {
                    return false;
                }
            }
            return true;
        }
        // ヘッダを見て PDF かどうか(一応)チェック
        public bool isGif(string filename)
        {
            byte[] gifHead = { 0x47, 0x49, 0x46 };
            byte[] readHead = new byte[7];
            try
            {
                // ファイル読み込み
                System.IO.FileStream fs = new System.IO.FileStream(
                    filename,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read);
                fs.Read(readHead, 0, 3);
                //閉じる
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            // compare
            for (int i = 0; 3 < 1; i++)
            {
                if (gifHead[i] != readHead[i])
                {
                    return false;
                }
            }
            return true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.status != 0)
            {
                return;
            }
            this.start = e.GetPosition(this);
            Console.WriteLine("Start " + this.start.X + ":" + this.start.Y);
            this.status = 1;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.status != 1)
            {
                return;
            }

            string imageurl = "";
            this.status = 2;

            // ウインドウ閉じる
            this.Close();

            // 選択範囲決定
            Console.WriteLine("End " + this.end.X + ":" + this.end.Y);

            // 画像切りだし
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(
                (int)this.start.X,
                (int)this.start.Y,
                (int)this.end.X - (int)this.start.X,
                (int)this.end.Y - (int)this.start.Y);
            Bitmap bmpNew = bmp.Clone(rect, bmp.PixelFormat);

            // 画像をPNG形式で保存
            string newFilePath = @"tmp.png";
            bmpNew.Save(newFilePath, System.Drawing.Imaging.ImageFormat.Png);

            // 画像リソースを解放
            bmpNew.Dispose();
            // アップロード
            imageurl = uploadFile(0, newFilePath);

            // ブラウザで開く
            Console.WriteLine(imageurl);
            System.Diagnostics.Process.Start(imageurl);

        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.status != 1)
            {
                return;
            }
            
            this.end = e.GetPosition(this);
            // 選択範囲矩形描画
            rect.Width = this.end.X - this.start.X;
            rect.Height = this.end.Y - this.start.Y;
            Canvas.SetLeft(rect, this.start.X);
            Canvas.SetTop(rect, this.start.Y);
            
            // 選択範囲サイズ描画
            rectWidth.Text = (this.end.X - this.start.X).ToString();
            rectHeight.Text = (this.end.Y - this.start.Y).ToString();
            Canvas.SetLeft(rectWidth, this.end.X - 30);
            Canvas.SetTop(rectWidth, this.end.Y - 30);

            Canvas.SetLeft(rectHeight, this.end.X - 30);
            Canvas.SetTop(rectHeight, this.end.Y - 20);


            Console.WriteLine("Change " + this.end.X + ":" + this.end.Y);
            
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // ESCで終了
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }

    }



}
