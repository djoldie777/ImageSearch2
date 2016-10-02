using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using OpenCvSharp;

namespace image_search_2
{
    public partial class Form1 : Form
    {
        private Bitmap bm;
        private bool isDown;
        private Pen p;
        private int penWidth = 3;
        private const int scaledWidth = 300;
        private const int scaledHeight = 300;
        private const int countOfSteps = 5;
        private const int widthStep = scaledWidth / countOfSteps;
        private const int heightStep = scaledHeight / countOfSteps;
        List<System.Drawing.Point> points;
        List<Bitmap> images;
        Dictionary<double, Bitmap> d;

        public Form1()
        {
            InitializeComponent();
            bm = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            getBitmapList();
            clear();
            isDown = false;
            points = new List<System.Drawing.Point>();
        }

        private void getBitmapList()
        {
            string[] files = System.IO.Directory.GetFiles(Application.StartupPath + "\\Images");
            images = new List<Bitmap>();

            for (int i = 0; i < files.Length; i++)
            {
                Bitmap btm = Bitmap.FromFile(files[i]) as Bitmap;

                if (btm.Width > pictureBox1.Width || btm.Height > pictureBox1.Height)
                {
                    Bitmap tmpBtm = new Bitmap(pictureBox1.Width, pictureBox1.Height);

                    using (Graphics g = Graphics.FromImage(tmpBtm))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(btm, 0, 0, pictureBox1.Width, pictureBox1.Height);
                    }

                    images.Add(tmpBtm);
                }
                else
                    images.Add(btm);
            }
        }

        private void clear()
        {
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.Clear(Color.White);
            }

            pictureBox1.Image = bm;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clear();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            isDown = true;

            if (e.Button == MouseButtons.Left)
            {
                using (Graphics g = Graphics.FromImage(bm))
                {
                    SolidBrush br = new SolidBrush(Color.Black);
                    p = new Pen(br, penWidth);

                    g.FillEllipse(br, e.X - penWidth / 2, e.Y - penWidth / 2, penWidth, penWidth);
                }

                pictureBox1.Refresh();
            }
            else
            {
                using (Graphics g = Graphics.FromImage(bm))
                {
                    SolidBrush br = new SolidBrush(Color.White);
                    p = new Pen(br, penWidth * 3);

                    g.FillEllipse(br, e.X - penWidth / 2, e.Y - penWidth / 2, penWidth, penWidth);
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDown)
            {
                points.Add(new System.Drawing.Point(e.X, e.Y));

                if (points.Count <= 1)
                    return;

                System.Drawing.Point[] pp = new System.Drawing.Point[points.Count];
                points.CopyTo(pp, 0);

                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.DrawCurve(p, pp);
                }
                
                pictureBox1.Refresh();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDown = false;

            points.Clear();
        }

        private Bitmap getContourImage(Bitmap btm)
        {
            Mat originalImage = btm.ToMat();
            Mat grayScale = new Mat(new OpenCvSharp.CPlusPlus.Size(scaledWidth, scaledHeight), originalImage.Type());

            Cv2.Resize(originalImage, grayScale, grayScale.Size(), 0, 0, Interpolation.Cubic);
            Cv2.CvtColor(grayScale, grayScale, ColorConversion.BgraToGray);
            Cv2.Canny(grayScale, grayScale, 110, 150, 3, false);
            Cv2.GaussianBlur(grayScale, grayScale, new OpenCvSharp.CPlusPlus.Size(21, 21), 0);

            Mat contourImage = new Mat(grayScale.Size(), grayScale.Type());
            contourImage.SetTo(Cv.ScalarAll(0));

            OpenCvSharp.CPlusPlus.Point[][] contours;
            HierarchyIndex[] hierarchy;

            Cv2.FindContours(grayScale, out contours, out hierarchy, ContourRetrieval.Tree, ContourChain.ApproxSimple, new OpenCvSharp.CPlusPlus.Point(0, 0));
            Cv2.DrawContours(contourImage, contours, -1, Cv.ScalarAll(255), 1, LineType.Link8, hierarchy, 1, null);

            return contourImage.ToBitmap();
        }

        private double getCorrelationValue(Bitmap b1, Bitmap b2)
        {
            if ((b1.Width != b2.Width) || (b1.Height != b2.Height))
                return -1.0;

            double res = 0;

            Mat originalImage = b1.ToMat();
            Mat filterImage = b2.ToMat();
            Mat correlationResult = new Mat(new OpenCvSharp.CPlusPlus.Size(widthStep, heightStep), MatType.CV_32FC1);

            CvRect originalRect, filterRect;

            originalRect.Width = widthStep;
            originalRect.Height = heightStep;
            filterRect.Width = widthStep;
            filterRect.Height = heightStep;

            double alpha = 1.0 / 255;
            int shiftX = (countOfSteps / 2 + 1) * widthStep;
            int shiftY = (countOfSteps / 2 + 1) * heightStep;

            for (int i = 0; i < filterImage.Height; i += heightStep)
                for (int j = 0; j < filterImage.Width; j += widthStep)
                {
                    filterRect.X = j;
                    filterRect.Y = i;

                    Mat filterROI = filterImage.SubMat(filterRect);
                    filterROI.ConvertScaleAbs(alpha, 0).ConvertTo(filterROI, MatType.CV_32FC1);

                    int minX = ((minX = j - shiftX) < 0) ? 0 : minX;
                    int maxX = ((maxX = j + shiftX) > b1.Width - 1) ? b1.Width - 1 : maxX;

                    int minY = ((minY = i - shiftY) < 0) ? 0 : minY;
                    int maxY = ((maxY = i + shiftY) > b1.Height - 1) ? b1.Height - 1 : maxY;

                    double max = Double.MinValue;

                    for (int y = minY; y < maxY; y += heightStep)
                        for (int x = minX; x < maxX; x += widthStep)
                        {
                            double minValue, maxValue;

                            originalRect.X = x;
                            originalRect.Y = y;

                            Mat originalROI = originalImage.SubMat(originalRect);
                            Cv2.Filter2D(originalROI, correlationResult, MatType.CV_32FC1, filterROI);
                            Cv2.MinMaxLoc(correlationResult, out minValue, out maxValue);

                            if (maxValue > max)
                                max = maxValue;
                        }

                    res += max;
                }

            return res;
        }

        private void goToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap b = getContourImage(bm);
            d = new Dictionary<double, Bitmap>();

            for (int i = 0; i < images.Count; i++)
            {
                Bitmap tmp = getContourImage(images[i]);

                double correlation = getCorrelationValue(tmp, b);

                d.Add(correlation, images[i]);
            }

            double max = Double.MinValue;

            foreach (double k in d.Keys)
                if (k > max)
                    max = k;

            pictureBox1.Image = d[max];
        }
    }
}
