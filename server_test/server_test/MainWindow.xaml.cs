using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics; //TCP
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net; //TCP
using System.Net.Sockets; //TCP
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//OpenCV
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Threading;
using System.Threading;

namespace WPF
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        TcpListener server = null;
        //List<Stream> clntList = new List<Stream>();
        Dictionary<Stream, string> clntList = new Dictionary<Stream, string>();
        List<Factory> ft = null;

        List<string> colorShape = new List<string>() { "REDTriangle", "REDSquare", "REDPentagon", "REDHexagon",
                                                       "GREENTriangle", "GREENSquare", "GREENPentagon", "GREENHexagon",
                                                       "BLUETriangle","BLUESquare","BLUEPentagon","BLUEHexagon"};


        int count = 0;

        readonly object thisLock = new object();
        bool lockedCount = false;

        // 전역으로 현재 검사중인 색/도형 저장
        string color = null;
        string shape = null;

        public MainWindow()
        {
            InitializeComponent();
            totalCount.Content = 0;

            //서버 코드 작성
            string bindIP = "10.10.20.103";
            const int bindPort = 9090;

            try
            {
                /*IPEndPoint localAdr = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);*/
                IPEndPoint localAdr = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);//주소 정보 설정

                server = new TcpListener(localAdr); //TCPListener 객체 생성

                server.Start(); //서버 오픈

                Thread t1 = new Thread(new ThreadStart(ClientConnect));
                t1.Start();
            }
            catch (SocketException err) //소켓 오류 날때 예외처리
            {
                MessageBox.Show(err.ToString());
            }
            //finally
            //{
            //    server.Stop(); //서버 종료
            //}
        }


        // 스레드로 다중 클라이언트 연결
        private void ClientConnect()
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                count = 0;
                lock (thisLock)
                {
                    while (lockedCount == true)
                        Monitor.Wait(thisLock);
                    lockedCount = true;
                    count++;
                    clntList.Add(stream, count.ToString());
                    lockedCount = false;
                    Monitor.Pulse(thisLock);
                }

                //MessageBox.Show(Convert.ToString(clntList.Count));
                // stream에서 string으로 변환
                //StreamReader reader = new StreamReader(stream);
                //string test = reader.ReadToEnd();

                //MessageBox.Show(clntList[stream]);

                //ft = new List<Factory>();
                //ft.Add(new Factory() { client = clntList[stream] });
                //this.Dispatcher.Invoke(() =>
                //{
                //    //ClientView.ItemsSource = null;
                //    ClientView.ItemsSource = ft;
                //});


                //MessageBox.Show("클라연결");

                //MemoryStream ms = new MemoryStream();
                //clntList[0].CopyTo(ms);
                //byte[] bytes = ms.ToArray();

                //string test = Encoding.Default.GetString(bytes);
                //MessageBox.Show(test);

                FileThread(client, stream);
            }
        }
        int tri_count, rect_count, penta_count, hexa_count, remain_count;

        // 메시지 받음
        private async void FileThread(object c, object st)
        {
            int cc = 0;

            TcpClient client = (TcpClient)c;
            NetworkStream stream = (NetworkStream)st;
            //ft = new List<Factory>();

            // 텍스트랑 파일 분리해야함... 그럼...? 파일이랑 텍스트인 걸 알아야됨 파일? 인 걸? 어케 앎?
            // 우리가 받는 게 파일 크기 + 파일
            try
            {
                await Task.Run(async () =>
                {
                    while(true)
                    {
                        // 메세지를 받아야함
                        byte[] sendMsg = new byte[1024];
                        stream.Read(sendMsg, 0, sendMsg.Length);

                        string sendString = Encoding.Default.GetString(sendMsg, 0, sendMsg.Length);
                        string[] splitMsg = sendString.Split('^');

                        switch (splitMsg[0])
                        {
                            case "1":
                                color = splitMsg[1];
                                shape = splitMsg[2];
                                //foreach (string s in splitMsg)
                                //    MessageBox.Show(s);

                                //ft = new List<Factory>();


                                //IEnumerable<ListViewItem> lv = ClientView.Items.Cast<ListViewItem>();
                                //var generator1 = ClientView.ItemContainerGenerator;
                                //var container1 = generator1.ContainerFromItem(shape);
                                //int index1 = generator1.IndexFromContainer(container1);
                                //var generator2 = ClientView.ItemContainerGenerator;
                                //var container2 = generator2.ContainerFromItem(color);
                                //int index2 = generator2.IndexFromContainer(container2);
                                //if (index1 == -1 &&index2 ==-1 )
                                //{
                                //    Factory.GetInstance().Add(new Factory() { client = clntList[stream], Color = color, Shape = shape, Normal = 0, Defective = 0, Total = 0 });   
                                //}                           


                                Factory.GetInstance().Add(new Factory() { client = clntList[stream], Color = color, Shape = shape, Normal = 0, Defective = 0, Total = 0 });
                                this.Dispatcher.Invoke(() =>
                                {
                                    //ClientView.ItemsSource = null;
                                    ClientView.ItemsSource = Factory.GetInstance();
                                    choiceColor.Content = color;
                                    choiceShape.Content = shape;
                                });

                                await Task.Delay(100);
                                break;

                            case "2":
                                string print = "";
                                cc++;
                                //데이터 크기 수신
                                byte[] size = new byte[4];
                                stream.Read(size, 0, 4);
                                //MessageBox.Show("수신데이터 크기: " + BitConverter.ToInt32(size, 0).ToString());
                                int strLen = BitConverter.ToInt32(size, 0); //받아야할 데이터 크기

                                int len = 0, recvLen = 0; //임시 길이 저장, 전체 수신길이 저장
                                byte[] bytes = new byte[strLen]; //버퍼
                                recvLen = stream.Read(bytes, 0, bytes.Length); //데이터 1차 수신

                                List<byte> buf = new List<byte>();
                                buf.AddRange(bytes);
                                //string buffer = Encoding.Default.GetString(bytes, 0, bytes.Length);
                                while (recvLen < strLen)
                                {
                                    len = stream.Read(bytes, 0, bytes.Length);
                                    buf.AddRange(bytes);
                                    recvLen += len;
                                    len = 0;
                                }
                                //MessageBox.Show("최종 데이터 크기: " + recvLen.ToString());

                                
                                // 색과 도형을 검출했음
                                Mat frame = new Mat();
                                frame = Mat.FromImageData(buf.ToArray(), ImreadModes.AnyColor);
                                //Filecheck(frame);

                                Mat test_img = frame;
                                Mat gr_line_img = new Mat(); // 흑백으로 바꾸고 외곽선만 남긴 이미지 담을 Mat 초기화
                                Cv2.CvtColor(test_img, gr_line_img, ColorConversionCodes.BGR2GRAY); // BGR이미지 test_img를 GRAY이미지 gr_img로 변환
                                gr_line_img = gr_line_img.Canny(75, 200, 3, true); // 외곽선 추출 함수 | (최소 임계값, 최대 임계값, 소벨 연산 마스크 크기, L2그레디언트)
                                                                                   // 픽셀값이 최소 임계보다 낮으면 가장자리 X, 최대 임계보다 높으면 가장자리 O, 3이 일반적, 정확히 계산할 것인지
                                OpenCvSharp.Point[][] conto_p; // 윤곽선의 점 선언
                                HierarchyIndex[] conto_hierarchy; // 윤곽선의 계층 구조 저장

                                Cv2.FindContours(gr_line_img, out conto_p, out conto_hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                                for (int i = 0; i < conto_p.Length; i++)
                                {
                                    double length = Cv2.ArcLength(conto_p[i], true); // 윤곽선 전체 길이 함수 | (윤곽 혹은 곡선 담은 포인트, 곡선의 닫힘 정보<true = 시작점-끝점 연결 계산>)

                                    OpenCvSharp.Point[] pp = Cv2.ApproxPolyDP(conto_p[i], length * 0.02, true);
                                    // 윤곽의 근사함수 | (윤곽 혹은 곡선 담은 포인트, 근사치 최대 거리(근사치 정확도), 폐곡선 여부) | 근사치 최대 거리는 보통 전체 윤곽선 길이의 1~5% 값
                                    // 윤곽점 배열 conto_p[i]에서 근사치 최대 거리값으로 폐곡선(시작점-끝점 연결)인 다각형 근사(단순화) 진행

                                    RotatedRect rrect = Cv2.MinAreaRect(pp); // 윤곽선의 경계를 둘러싸는 사각형 계산 / RotatedRect 구조체 반환
                                    Moments moments = Cv2.Moments(conto_p[i], false);
                                    double cx = moments.M10 / moments.M00, cy = moments.M01 / moments.M00;
                                    OpenCvSharp.Point pnt = new OpenCvSharp.Point(cx, cy);
                                    int x = Convert.ToInt32(cx);
                                    int y = Convert.ToInt32(cy);
                                    Vec3b color = test_img.At<Vec3b>(x, y);
                                    if (color.Item0 > color.Item1 && color.Item0 > color.Item2)
                                    {
                                        string B = color.Item0.ToString();
                                        string G = color.Item1.ToString();
                                        string R = color.Item2.ToString();
                                        print = "blue";
                                        //MessageBox.Show(print);
                                    }
                                    else if (color.Item1 > color.Item0 && color.Item1 > color.Item2)
                                    {
                                        string B = color.Item0.ToString();
                                        string G = color.Item1.ToString();
                                        string R = color.Item2.ToString();
                                        print = "green";

                                        //MessageBox.Show(print);
                                    }
                                    else if (color.Item2 > color.Item0 && color.Item2 > color.Item1)
                                    {
                                        string B = color.Item0.ToString();
                                        string G = color.Item1.ToString();
                                        string R = color.Item2.ToString();
                                        print = "red";

                                        //MessageBox.Show(print);
                                    }
                                    if (pp.Length == 3)
                                    {
                                        Cv2.DrawContours(test_img, conto_p, i, Scalar.Red, 5, LineTypes.AntiAlias);
                                        // 윤곽선 그리기 함수 | (윤곽선 그릴 이미지, 윤곽 정보 담긴 Mat, 윤곽선 번호(-1일 때, 모든 윤곽선 그림), 색상, 두께, 선형 타입)
                                        tri_count++; // 삼각형 개수 추가
                                        
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            shapeTitle_Copy.Content = "삼각형 개수: " + tri_count+print;
                                        });
                                    }
                                    else if (pp.Length == 4)
                                    {
                                        Cv2.DrawContours(test_img, conto_p, i, Scalar.Orange, 5, LineTypes.AntiAlias);
                                        rect_count++;
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            shapeTitle_Copy.Content = "사각형 개수: " + rect_count + print;
                                        });
                                    }
                                    else if (pp.Length == 5)
                                    {
                                        Cv2.DrawContours(test_img, conto_p, i, Scalar.Yellow, 5, LineTypes.AntiAlias);
                                        penta_count++;
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            shapeTitle_Copy.Content = "오각형 개수: " + penta_count + print;
                                        });
                                    }
                                    else if (pp.Length == 6)
                                    {
                                        Cv2.DrawContours(test_img, conto_p, i, Scalar.Green, 5, LineTypes.AntiAlias);
                                        hexa_count++;
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            shapeTitle_Copy.Content = "육각형 개수: " + hexa_count + print;
                                        });
                                    }
                                    else
                                    {
                                        Cv2.DrawContours(test_img, conto_p, i, Scalar.Blue, 5, LineTypes.AntiAlias);
                                        remain_count++;
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            shapeTitle_Copy.Content = "나머지: " + remain_count+print;
                                        });
                                    }
                                }


                                //Mat src1 = new Mat();
                                //Mat src2 = new Mat();


                                //frame.CopyTo(src2);

                                //Cv2.CvtColor(src2, src1, ColorConversionCodes.BGR2GRAY);
                                //Cv2.GaussianBlur(src1, src1, new OpenCvSharp.Size(0, 0), 1, 1, BorderTypes.Replicate);
                                ////Cv2.BoxFilter(src1, src1, MatType.CV_8UC3, new OpenCvSharp.Size(9, 9), new OpenCvSharp.Point(-1, -1), true, BorderTypes.Default);
                                ////Cv2.Threshold(src1, src1, 170, 255, ThresholdTypes.Binary);

                                //Cv2.ImShow("k", src1);

                                //try
                                //{
                                //    // 윤곽선 잡기
                                //    Cv2.FindContours(src1, out OpenCvSharp.Point[][] contour1, out OpenCvSharp.HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                                //    //Cv2.CvtColor(src1, src1, ColorConversionCodes.GRAY2BGR);

                                //    //MessageBox.Show("들어옴1");
                                //    for (int i = 0; i < contour1.Length; i++)
                                //    {
                                //        List<OpenCvSharp.Point[]> new_contours = new List<OpenCvSharp.Point[]>();

                                //        //MessageBox.Show("들어옴2");

                                //        foreach (OpenCvSharp.Point[] p in contour1)
                                //        {
                                //            OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(p, Cv2.ArcLength(p, true) * 0.02, true);
                                //            OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(p);
                                //            double contourArea = Cv2.ContourArea(p);
                                //            double length = Cv2.ArcLength(p, true);
                                //            if (length > 100)
                                //            {
                                //                new_contours.Add(Cv2.ApproxPolyDP(contour1[i], 0.01 * 1, true));
                                //            }
                                //            bool convex = Cv2.IsContourConvex(p);
                                //            OpenCvSharp.Point[] hull = Cv2.ConvexHull(p, true);

                                //            // 중심점 찾기
                                //            Moments moments = Cv2.Moments(p, false);
                                //            double cx = moments.M10 / moments.M00, cy = moments.M01 / moments.M00;

                                //            // 중심점
                                //            OpenCvSharp.Point pnt = new OpenCvSharp.Point(cx, cy); //center point

                                //            // 특정 윤곽선
                                //            Cv2.DrawContours(src1, new OpenCvSharp.Point[][] { hull }, -1, Scalar.White, 2);
                                //            //MessageBox.Show("들어옴3");
                                //            double area = Cv2.ContourArea(p, true);
                                //            if (length > 100)
                                //            {

                                //                int x = Convert.ToInt32(cx);
                                //                int y = Convert.ToInt32(cy);
                                //                //MessageBox.Show("들어옴4");

                                //                // 색 추출 
                                //                Vec3b color = src2.At<Vec3b>(x, y);
                                //                if (color.Item0 > color.Item1 && color.Item0 > color.Item2)
                                //                {
                                //                    string B = color.Item0.ToString();
                                //                    string G = color.Item1.ToString();
                                //                    string R = color.Item2.ToString();
                                //                    print = B + ":" + G + ":" + R + "blue";
                                //                    //MessageBox.Show(print);
                                //                }
                                //                else if (color.Item1 > color.Item0 && color.Item1 > color.Item2)
                                //                {
                                //                    string B = color.Item0.ToString();
                                //                    string G = color.Item1.ToString();
                                //                    string R = color.Item2.ToString();
                                //                    print = B + ":" + G + ":" + R + "green";

                                //                    //MessageBox.Show(print);
                                //                }
                                //                else if (color.Item2 > color.Item0 && color.Item2 > color.Item1)
                                //                {
                                //                    string B = color.Item0.ToString();
                                //                    string G = color.Item1.ToString();
                                //                    string R = color.Item2.ToString();
                                //                    print = B + ":" + G + ":" + R + "red";

                                //                    //MessageBox.Show(print);                           
                                //                }
                                //                else
                                //                {
                                //                    print = "yeahfcuk";
                                //                    //MessageBox.Show(print);
                                //                }

                                //                //this.Dispatcher.Invoke(() =>
                                //                //{
                                //                //    Cam_1.Source = WriteableBitmapConverter.ToWriteableBitmap(frame);
                                //                //});

                                //            }
                                //        }
                                //    }
                                //}
                                //catch
                                //{
                                //    MessageBox.Show("튕김");
                                //}

                                //foreach (Stream Value in clntList.Keys)
                                //{
                                //    ft.Add(new Factory() { client = clntList[Value], Color = "RED", Shape = "Square", Normal = 0, Defective = 0, Total = 0 });
                                //}

                                //ft = new List<Factory>();
                                //ft.Add(new Factory() { client = clntList[stream], Color = color, Shape = shape, Normal = cc, Defective = 0, Total = 0 });

                                //ft = new List<Factory>();
                                //var newList = ClientView.Items.OfType<ListViewItem>().Select(item => item.Content.ToString()).ToList();
                                //int newList = ClientView.Items.IndexOf(((MainViewModel)DataContext).Items.FirstOrDefault(item => item.Name == searchText))
                                //int colorIndex = newList.IndexOf(color);
                                //int shapeIndex = newList.IndexOf(shape);

                                this.Dispatcher.Invoke(() =>
                                {
                                    Cam_1.Source = WriteableBitmapConverter.ToWriteableBitmap(test_img);
                                    //MessageBox.Show(print);
                                    //shapeTitle_Copy.Content = print;

                                    totalCount.Content = cc.ToString();

                                    //ClientView.ItemsSource = null;
                                    //ClientView.ItemsSource = ft;

                                    //ok.Content = colorIndex.ToString() + "^" + shapeIndex.ToString();

                                    Factory newft = Factory.GetInstance().ElementAt(0);
                                    newft.Normal = cc;
                                    ClientView.Items.Refresh();
                                    //choiceColor.Content = data.ToString();

                                });
                                await Task.Delay(100);
                                break;
                        }
                    }
                });
            }
            //
            catch
            {
                MessageBox.Show("클라연결 끊김ㅜ");
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }
        string print = "";
        private void Filecheck(object mat)
        {
            //MessageBox.Show("들어옴");

            Mat frame = (Mat)mat;

            Mat src1 = new Mat();
            frame.CopyTo(src1);

            Cv2.CvtColor(src1, src1, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(src1, src1, new OpenCvSharp.Size(0, 0), 1, 1, BorderTypes.Replicate);
            Cv2.BoxFilter(src1, src1, MatType.CV_8UC3, new OpenCvSharp.Size(9, 9), new OpenCvSharp.Point(-1, -1), true, BorderTypes.Default);
            Cv2.Threshold(src1, src1, 170, 255, ThresholdTypes.Binary);

            //Cv2.ImShow("k", src1);

            //try
            //{
            // 윤곽선 잡기
            Cv2.FindContours(src1, out OpenCvSharp.Point[][] contour1, out OpenCvSharp.HierarchyIndex[] hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            //Cv2.CvtColor(src1, src1, ColorConversionCodes.GRAY2BGR);
            //MessageBox.Show("들어옴1");
            for (int i = 0; i < contour1.Length; i++)
            {
                List<OpenCvSharp.Point[]> new_contours = new List<OpenCvSharp.Point[]>();

                //MessageBox.Show("들어옴2");

                foreach (OpenCvSharp.Point[] p in contour1)
                {
                    OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(p, Cv2.ArcLength(p, true) * 0.02, true);
                    OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(p);
                    double contourArea = Cv2.ContourArea(p);
                    double length = Cv2.ArcLength(p, true);
                    if (length > 100)
                    {
                        new_contours.Add(Cv2.ApproxPolyDP(contour1[i], 0.01 * 1, true));
                    }
                    bool convex = Cv2.IsContourConvex(p);
                    OpenCvSharp.Point[] hull = Cv2.ConvexHull(p, true);

                    // 중심점 찾기
                    Moments moments = Cv2.Moments(p, false);
                    double cx = moments.M10 / moments.M00, cy = moments.M01 / moments.M00;
                        
                    // 중심점
                    OpenCvSharp.Point pnt = new OpenCvSharp.Point(cx, cy); //center point

                    // 특정 윤곽선
                    Cv2.DrawContours(src1, new OpenCvSharp.Point[][] { hull }, -1, Scalar.White, 2);
                    //MessageBox.Show("들어옴3");
                    double area = Cv2.ContourArea(p, true);
                    if (length > 100)
                    {

                        int x = Convert.ToInt32(cx);
                        int y = Convert.ToInt32(cy);
                        //MessageBox.Show("들어옴4");
                        
                        // 색 추출 
                        Vec3b color =  frame.At<Vec3b>(x, y);
                        if (color.Item0 > color.Item1 && color.Item0 > color.Item2)
                        {
                            string B = color.Item0.ToString();
                            string G = color.Item1.ToString();
                            string R = color.Item2.ToString();
                            print = B + ":" + G + ":" + R + "blue";
                            //MessageBox.Show(print);
                        }
                        else if (color.Item1 > color.Item0 && color.Item1 > color.Item2)
                        {
                            string B = color.Item0.ToString();
                            string G = color.Item1.ToString();
                            string R = color.Item2.ToString();
                            print = B + ":" + G + ":" + R + "green";

                            //MessageBox.Show(print);
                        }
                        else if (color.Item2 > color.Item0 && color.Item2 > color.Item1)
                        {
                            string B = color.Item0.ToString();
                            string G = color.Item1.ToString();
                            string R = color.Item2.ToString();
                            print = B + ":" + G + ":" + R + "red";

                            //MessageBox.Show(print);                           
                        }
                        else
                        {
                            print = "yeahfcuk";
                            //MessageBox.Show(print);
                        }
                        
                        this.Dispatcher.Invoke(() =>
                        {
                            Cam_1.Source = WriteableBitmapConverter.ToWriteableBitmap(frame);
                        });

                    }
                }
            }
            MessageBox.Show(print);
            //}
            //catch
            //{
            //    MessageBox.Show(print);
            //}
            //finally
            //{

            //}
        }

        private void windows_loaded(object sender, RoutedEventArgs e)
        {

        }


        public class Factory
        {
            public string client { get; set; }
            public string Color { get; set; }
            public string Shape { get; set; }
            public int Normal { get; set; }
            public int Defective { get; set; }
            public int Total { get; set; }

            private static List<Factory> instance;

            public static List<Factory> GetInstance()
            {
                if (instance == null)
                    instance = new List<Factory>();
                return instance;
            }
        }

        //public class Factory : INotifyPropertyChanged
        //{
        //    public string client { get; set; }
        //    public string Color { get; set; }
        //    public string Shape { get; set; }

        //    private int nomal;
        //    public int Normal
        //    {
        //        get
        //        {
        //            return nomal;
        //        }
        //        set
        //        {
        //            nomal = value;
        //            OnPropertyChanged("Nomal");
        //        }
        //    }
        //    public int Defective { get; set; }
        //    public int Total { get; set; }

        //    public event PropertyChangedEventHandler PropertyChanged;
        //    protected void OnPropertyChanged(string prop)
        //    {
        //        if(PropertyChanged != null)
        //        {
        //            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        //        }

        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        //    }
        //}
    }
}