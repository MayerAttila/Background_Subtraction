using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Win32;



namespace Project
{
    public partial class Form1 : Form
    {
        VideoCapture videocapture;
        bool playing = false;
        int totalFrames;
        int currentFrameNumber;
        int FPS;
        int[,][] bcModellGray;

        int[,][] rgbBcModellRedValues;
        int[,][] rgbBcModellGreenValues;
        int[,][] rgbBcModellBlueValues;

        Mat currentFrameMatGray;
        Mat currentFrameMatRgb;
        Image<Gray, byte> convertedCurrentFrameMatGray;
        Image<Rgb, byte> convertedCurrentFrameMatRgb;

        int grayValInCurrentFramePixel;
        int redValInCurrentFramePixel;
        int greenValInCurrentFramePixel;
        int blueValInCurrentFramePixel;

        Mat outputMatGray;
        Mat outputMatRgb;
        Image<Gray, byte> convertedOutputMatGray;
        Image<Rgb, byte> convertedOutputMatRgb;

        Gray movingObjectColorGray = new Gray(255);
        Gray bcColorGray = new Gray(0);

        Rgb movingObjectColorRgb = new Rgb(255, 255, 255);
        Rgb bcColorRgb = new Rgb(0, 0, 0);



        Random random = new Random();


        const int RANGE = 30;
        const int MIN_MATCHES = 2;
        const int SAMPLES = 20;
        int totalPixels;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                videocapture = new VideoCapture(openFileDialog.FileName);
                totalFrames = Convert.ToInt32(videocapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount));
                FPS = Convert.ToInt32(videocapture.Get(Emgu.CV.CvEnum.CapProp.Fps));
                playing = true;
                currentFrameMatGray = new Mat();
                currentFrameMatRgb = new Mat();

                currentFrameNumber = 0;
                PlayVideo();
            }
        }

        private async void PlayVideo()
        {
            if (videocapture == null)
            {
                return;
            }

            try
            {
                while (playing == true && currentFrameNumber < totalFrames)
                {
                    videocapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrameNumber);
                    videocapture.Read(currentFrameMatGray);
                    videocapture.Read(currentFrameMatRgb);

                    convertedCurrentFrameMatGray = currentFrameMatGray.ToImage<Gray, byte>();
                    currentFrameMatGray = convertedCurrentFrameMatGray.Mat;

                    convertedCurrentFrameMatRgb = currentFrameMatRgb.ToImage<Rgb, byte>();
                    currentFrameMatRgb = convertedCurrentFrameMatRgb.Mat;

                    pictureBox1.Image = currentFrameMatGray.ToBitmap();
                    pictureBox3.Image = currentFrameMatRgb.ToBitmap();

                    textBox1.Text = "CurrentFrameNumber: " + currentFrameNumber.ToString();

                    if (currentFrameNumber == 0)
                    {
                        InitalizeBcModellGray();
                        InitalizeBcModellRgb();
                    }
                    MyVibeAlgorithmForGray();
                    MyVibeAlgorithmForRgb();

                    currentFrameNumber += 1;

                    await Task.Delay(1000 / FPS);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void playbtn_Click(object sender, EventArgs e)
        {
            if (videocapture != null)
            {
                playing = true;
                PlayVideo();
            }
            else
            {
                playing = false;
            }
        }

        private void pausebtn_Click(object sender, EventArgs e)
        {
            playing = false;
        }

        void MyVibeAlgorithmForGray()
        {
            int currentMatches;
            convertedOutputMatGray = convertedCurrentFrameMatGray;
            int isUpdating;
            int countOfForegroundPixels = 0;

            for (int i = 0; i < currentFrameMatGray.Rows; i++)
            {
                for (int j = 0; j < currentFrameMatGray.Cols; j++)
                {
                    currentMatches = 0;
                    grayValInCurrentFramePixel = (int)convertedCurrentFrameMatGray[i, j].Intensity;
                    for (int k = 0; k < SAMPLES; k++)
                    {
                        if (IsPixelValInRange(grayValInCurrentFramePixel, bcModellGray[i, j][k]))
                        {
                            currentMatches++;
                        }
                    }
                    isUpdating = random.Next(1, 101);
                    if (currentMatches < MIN_MATCHES)
                    {
                        if (isUpdating <= 1) UpdateBcModellGray(i, j, grayValInCurrentFramePixel);
                        convertedOutputMatGray[i, j] = movingObjectColorGray;
                        countOfForegroundPixels++;
                    }
                    else
                    {
                        if (isUpdating <= 20) UpdateBcModellGray(i, j, grayValInCurrentFramePixel);
                        convertedOutputMatGray[i, j] = bcColorGray;
                    }
                }
            }
            m3ModuleForGray();
            outputMatGray = convertedOutputMatGray.Mat;
            pictureBox2.Image = outputMatGray.ToBitmap();

            if ((double)countOfForegroundPixels / totalPixels > 0.3)
            {
                InitalizeBcModellGray();
            }
        }

        void MyVibeAlgorithmForRgb()
        {
            int currentMatches;
            convertedOutputMatRgb = convertedCurrentFrameMatRgb;
            int isUpdating;
            int countOfForegroundPixels = 0;

            for (int i = 0; i < currentFrameMatRgb.Rows; i++)
            {
                for (int j = 0; j < currentFrameMatRgb.Cols; j++)
                {
                    currentMatches = 0;
                    redValInCurrentFramePixel = (int)convertedCurrentFrameMatRgb[i, j].Red;
                    blueValInCurrentFramePixel = (int)convertedCurrentFrameMatRgb[i, j].Blue;
                    greenValInCurrentFramePixel = (int)convertedCurrentFrameMatRgb[i, j].Green;

                    for (int k = 0; k < SAMPLES; k++)
                    {
                        if (IsPixelValInRange(redValInCurrentFramePixel, rgbBcModellRedValues[i, j][k]) && IsPixelValInRange(greenValInCurrentFramePixel, rgbBcModellGreenValues[i, j][k]) && IsPixelValInRange(blueValInCurrentFramePixel, rgbBcModellBlueValues[i, j][k]))
                        {
                            currentMatches++;
                        }
                    }
                    isUpdating = random.Next(1, 101);
                    if (currentMatches < MIN_MATCHES)
                    {
                        if (isUpdating <= 1) UpdateBcModellRgb(i, j, redValInCurrentFramePixel, greenValInCurrentFramePixel, blueValInCurrentFramePixel);
                        convertedOutputMatRgb[i, j] = movingObjectColorRgb;
                        countOfForegroundPixels++;
                    }
                    else
                    {
                        if (isUpdating <= 20) UpdateBcModellRgb(i, j, redValInCurrentFramePixel, greenValInCurrentFramePixel, blueValInCurrentFramePixel);
                        convertedOutputMatRgb[i, j] = bcColorRgb;
                    }
                }
            }
            m3ModuleForRgb();
            outputMatRgb = convertedOutputMatRgb.Mat;
            pictureBox4.Image = outputMatRgb.ToBitmap();

            if ((double)countOfForegroundPixels / totalPixels > 0.3)
            {
                InitalizeBcModellRgb();
            }
        }

        void m3ModuleForRgb()
        {
            int isUpdating;
            int redVal;
            int greenVal;
            int blueVal;

            for (int i = 1; i < convertedOutputMatRgb.Rows - 1; i++)
            {
                for (int j = 1; j < convertedOutputMatRgb.Cols - 1; j++)
                {
                    if (convertedOutputMatRgb[i, j].Red == 255)
                    {
                        for (int r = -1; r < 2; r++)
                        {
                            for (int c = -1; c < 2; c++)
                            {
                                if (convertedOutputMatRgb[i + r, j + c].Red == 0)
                                {
                                    isUpdating = random.Next(1, 101);
                                    if (isUpdating < 10)
                                    {
                                        redVal = (int)convertedCurrentFrameMatRgb[i + r, j + c].Red;
                                        greenVal = (int)convertedCurrentFrameMatRgb[i + r, j + c].Green;
                                        blueVal = (int)convertedCurrentFrameMatRgb[i + r, j + c].Blue;
                                        UpdateBcModellRgb(i, j, redVal, greenVal, blueVal);
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        void m3ModuleForGray()
        {
            //convertedOutputMatRgb
            int isUpdating;
            int grayVal;

            for (int i = 1; i < convertedOutputMatGray.Rows - 1; i++)
            {
                for (int j = 1; j < convertedOutputMatGray.Cols - 1; j++)
                {
                    if (convertedOutputMatGray[i, j].Intensity == 255)
                    {
                        for (int r = -1; r < 2; r++)
                        {
                            for (int c = -1; c < 2; c++)
                            {
                                if (convertedOutputMatGray[i + r, j + c].Intensity == 0)
                                {
                                    isUpdating = random.Next(1, 101);
                                    if (isUpdating < 10)
                                    {
                                        grayVal = (int)convertedOutputMatGray[i + r, j + c].Intensity;
                                        UpdateBcModellGray(i, j, grayVal);
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        bool IsPixelValInRange(int valueOfCurrentPixel, int valueOfModelPixel)
        {
            if (valueOfCurrentPixel > valueOfModelPixel - RANGE && valueOfCurrentPixel < valueOfModelPixel + RANGE) return true;
            else return false;
        }

        void InitalizeBcModellGray()
        {
            bcModellGray = new int[currentFrameMatGray.Rows, currentFrameMatGray.Cols][];

            List<int> intGrayValuesInRadius = new List<int>();

            int randomGraySample;

            int randomRow;
            int randomRowRange1;
            int randomRowRange2;

            int randomCol;
            int randomColRange1;
            int randomColRange2;

            totalPixels = currentFrameMatGray.Rows * currentFrameMatGray.Cols;

            for (int i = 0; i < currentFrameMatGray.Rows; i++)
            {
                for (int j = 0; j < currentFrameMatGray.Cols; j++)
                {
                    if (i == 0)
                    {
                        if (j == 0)
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                        else if (j == currentFrameMatGray.Cols - 1)
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = -1;
                            randomColRange2 = 1;
                        }
                        else
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = -1;
                            randomColRange2 = 2;
                        }
                    }
                    else if (j == 0)
                    {
                        if (i == currentFrameMatGray.Rows - 1)
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                        else
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 2;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                    }
                    else if (i == currentFrameMatGray.Rows - 1)
                    {
                        if (j == currentFrameMatGray.Cols - 1)
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = -1;
                            randomColRange2 = 1;
                        }
                        else
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = -1;
                            randomColRange2 = 2;
                        }
                    }
                    else if (j == currentFrameMatGray.Cols - 1)
                    {
                        randomRowRange1 = -1;
                        randomRowRange2 = 2;
                        randomColRange1 = -1;
                        randomColRange2 = 1;
                    }
                    else
                    {
                        randomRowRange1 = -1;
                        randomRowRange2 = 2;
                        randomColRange1 = -1;
                        randomColRange2 = 2;
                    }

                    intGrayValuesInRadius.Clear();

                    for (int s = 0; s < SAMPLES; s++)
                    {

                        randomRow = random.Next(i + randomRowRange1, i + randomRowRange2);
                        randomCol = random.Next(j + randomColRange1, j + randomColRange2);

                        randomGraySample = (int)convertedCurrentFrameMatGray[randomRow, randomCol].Intensity;
                        intGrayValuesInRadius.Add(randomGraySample);
                    }
                    bcModellGray[i, j] = intGrayValuesInRadius.ToArray();
                }
            }
        }

        void InitalizeBcModellRgb()
        {
            rgbBcModellRedValues = new int[currentFrameMatGray.Rows, currentFrameMatGray.Cols][];
            rgbBcModellGreenValues = new int[currentFrameMatGray.Rows, currentFrameMatGray.Cols][];
            rgbBcModellBlueValues = new int[currentFrameMatGray.Rows, currentFrameMatGray.Cols][];

            List<int> intRedValuesInRadius = new List<int>();
            List<int> intGreenValuesInRadius = new List<int>();
            List<int> intBlueValuesInRadius = new List<int>();

            int randomRedSample;
            int randomGreenSample;
            int randomBlueSample;

            int randomRow;
            int randomRowRange1;
            int randomRowRange2;

            int randomCol;
            int randomColRange1;
            int randomColRange2;

            totalPixels = currentFrameMatGray.Rows * currentFrameMatGray.Cols;

            for (int i = 0; i < currentFrameMatGray.Rows; i++)
            {
                for (int j = 0; j < currentFrameMatGray.Cols; j++)
                {
                    if (i == 0)
                    {
                        if (j == 0)
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                        else if (j == currentFrameMatGray.Cols - 1)
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = -1;
                            randomColRange2 = 1;
                        }
                        else
                        {
                            randomRowRange1 = 0;
                            randomRowRange2 = 2;
                            randomColRange1 = -1;
                            randomColRange2 = 2;
                        }
                    }
                    else if (j == 0)
                    {
                        if (i == currentFrameMatGray.Rows - 1)
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                        else
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 2;
                            randomColRange1 = 0;
                            randomColRange2 = 2;
                        }
                    }
                    else if (i == currentFrameMatGray.Rows - 1)
                    {
                        if (j == currentFrameMatGray.Cols - 1)
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = -1;
                            randomColRange2 = 1;
                        }
                        else
                        {
                            randomRowRange1 = -1;
                            randomRowRange2 = 1;
                            randomColRange1 = -1;
                            randomColRange2 = 2;
                        }
                    }
                    else if (j == currentFrameMatGray.Cols - 1)
                    {
                        randomRowRange1 = -1;
                        randomRowRange2 = 2;
                        randomColRange1 = -1;
                        randomColRange2 = 1;
                    }
                    else
                    {
                        randomRowRange1 = -1;
                        randomRowRange2 = 2;
                        randomColRange1 = -1;
                        randomColRange2 = 2;
                    }

                    intRedValuesInRadius.Clear();
                    intGreenValuesInRadius.Clear();
                    intBlueValuesInRadius.Clear();

                    for (int s = 0; s < SAMPLES; s++)
                    {

                        randomRow = random.Next(i + randomRowRange1, i + randomRowRange2);
                        randomCol = random.Next(j + randomColRange1, j + randomColRange2);

                        randomRedSample = (int)convertedCurrentFrameMatRgb[randomRow, randomCol].Red;
                        intRedValuesInRadius.Add(randomRedSample);

                        randomGreenSample = (int)convertedCurrentFrameMatRgb[randomRow, randomCol].Green;
                        intGreenValuesInRadius.Add(randomGreenSample);

                        randomBlueSample = (int)convertedCurrentFrameMatRgb[randomRow, randomCol].Blue;
                        intBlueValuesInRadius.Add(randomBlueSample);


                    }
                    rgbBcModellRedValues[i, j] = intRedValuesInRadius.ToArray();
                    rgbBcModellGreenValues[i, j] = intGreenValuesInRadius.ToArray();
                    rgbBcModellBlueValues[i, j] = intBlueValuesInRadius.ToArray();
                }
            }
        }


        void UpdateBcModellGray(int row, int col, int valueOfCurrentPixel)
        {
            int randomIndex = random.Next(0, 20);
            bcModellGray[row, col][randomIndex] = valueOfCurrentPixel;
        }

        void UpdateBcModellRgb(int row, int col, int redValueOfCurrentPixel, int greenValueOfCurrentPixel, int blueValueOfCurrentPixel)
        {
            int randomIndex = random.Next(0, 20);
            rgbBcModellRedValues[row, col][randomIndex] = redValueOfCurrentPixel;
            rgbBcModellGreenValues[row, col][randomIndex] = greenValueOfCurrentPixel;
            rgbBcModellBlueValues[row, col][randomIndex] = blueValueOfCurrentPixel;
        }
    }
}


