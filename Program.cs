using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;

namespace ZoomScreenSaver
{
    public class ScreenSaverForm : Form
    {
        private System.Windows.Forms.Timer renderTimer;
        private string[] imageFiles;
        private int currentImageIndex = 0;
        private readonly Random random = new Random();
        private string photoPath;
        private float zoomSpeed=0.01f;
        private PointF currentPosition;
        private float currentZoom;
        private float panSpeedX, panSpeedY;
        private Point lastMousePosition;
        //private PictureBox pictureBox;
        private Image? currentImage;
        private Stopwatch? stopwatch;
        private readonly float zoomInLimit = 1.2f; //1.5f;// Maximum zoom limit
        private readonly float zoomOutLimit = 1.0f; //1.0f; // Minimum zoom limit
        private int imageDisplayDuration = 5000; // Display duration for each image in milliseconds
        private float fadeStep = 0.05f; // Opacity change per step
        private bool fadingOut = false;
        private System.Windows.Forms.Timer fadeTimer;
        public ScreenSaverForm()
        {
            renderTimer = new System.Windows.Forms.Timer();
            imageFiles = Array.Empty<string>();
            currentImage=null;
            photoPath = string.Empty;
            zoomSpeed = 0.001f;
            currentZoom = 1.0f; // Start at normal size
            currentPosition = new PointF(0, 0); // Reset panning position
            panSpeedX = 0.001f; // Set pan speed in the X direction (adjust as needed)
            panSpeedY = 0.001f; // Set pan speed in the Y direction (adjust as needed)

            // Initialize fade timer
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 50; // Adjust fade speed
            fadeTimer.Tick += FadeTimer_Tick;

            LoadSettings();
            InitializeComponents();
            LoadImages();
            ResetAnimation();
            StartRenderLoop();
        }

        private void LoadSettings()
        {
            photoPath = ConfigurationManager.AppSettings["PhotoPath"] ?? "D:\\ScreenSaver";
            zoomSpeed =float.Parse(ConfigurationManager.AppSettings["ZoomSpeed"] ?? "0.001");
            
        }

        private void InitializeComponents()
        {
            this.WindowState = FormWindowState.Maximized;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.Size = new Size(1920, 1080);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Application.Exit(); };
            this.MouseMove += MainForm_MouseMove;
            this.MouseClick += (s, e) => Application.Exit();
            this.KeyPress += (s, e) => Application.Exit();
            Cursor.Hide();  // Hide the mouse cursor
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (lastMousePosition != Point.Empty && (lastMousePosition != e.Location))
            {
                Application.Exit();
            }
            lastMousePosition = e.Location;
        }
        private void LoadImages()
        {
            if (Directory.Exists(photoPath))
            {
                imageFiles = Directory.GetFiles(photoPath, "*.jpg");
                ShuffleArray(imageFiles); // Shuffle the image files
                if (imageFiles.Length > 0)
                {
                    //currentImage = Image.FromFile(imageFiles[currentImageIndex]);
                    DisplayImage(imageFiles[0]);
                }
            }
            else
            {
                imageFiles = new string[0];
            }
        }



        private void DisplayImage(string imagePath)
        {
            currentImage = Image.FromFile(imagePath);
            Console.WriteLine("currentImage width and height: " + currentImage.Width + " " + currentImage.Height);             // Calculate initial zoom factor to fit the image within the form's client area
        }
         private void StartRenderLoop()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            //renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval =16; //16; // Approximately 60 FPS
            renderTimer.Tick += (s, e) => RenderFrame();
            renderTimer.Start();
        }

        private void RenderFrame()
        {
             float deltaTime = 0.016f; //0.016f; // Default to 60 FPS
            if (stopwatch != null)
            {
                deltaTime = (float)stopwatch.Elapsed.TotalMilliseconds / 1000.0f; // Convert to seconds
                stopwatch.Restart();
            }

            // Update the pan and zoom
            UpdateZoomAndPan(deltaTime);
            // Update the image display time
            UpdateImageDisplayTime(deltaTime);

            // Redraw the form
            Invalidate();
        }

        private void UpdateZoomAndPan(float deltaTime)
        {
            // Update zoom
            currentZoom += zoomSpeed * deltaTime;
            if (currentZoom >= zoomInLimit || currentZoom <= zoomOutLimit)
            {
                zoomSpeed = -zoomSpeed; // Reverse zoom direction
            }

            // Update pan position
            currentPosition.X += panSpeedX * deltaTime;
            currentPosition.Y += panSpeedY * deltaTime;

            // If we reach the edge, reverse direction
            if(currentImage == null) return;
            if (currentPosition.X <= 0 || currentPosition.X >= currentImage.Width * currentZoom - this.ClientSize.Width)
                panSpeedX = -panSpeedX;

            if (currentPosition.Y <= 0 || currentPosition.Y >= currentImage.Height * currentZoom - this.ClientSize.Height)
                panSpeedY = -panSpeedY;
        }

        private void UpdateImageDisplayTime(float deltaTime)
        {
            imageDisplayDuration -= (int)(deltaTime * 1000); // Reduce display time
            if (imageDisplayDuration <= 0)
            {
                StartFadeOut(); // Start fade-out effect
                //SwitchToNextImage();
            }
        }
        private void StartFadeOut()
        {
            if (!fadingOut)
            {
                fadingOut = true; // Begin fading out
                fadeTimer.Start(); // Start the fade timer
            }
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            // Fade out
            if (fadingOut)
            {
                this.Opacity -= fadeStep; // Decrease opacity

                if (this.Opacity <= 0.9) // 
                {
                    fadeTimer.Stop();
                    SwitchToNextImage(); // Change image after fade-out
                    this.Opacity = 0.8; // Ensure opacity 
                    StartFadeIn(); // Start fade-in effect
                }
            }
            else // Fade in
            {
                this.Opacity += fadeStep; // Increase opacity

                if (this.Opacity >= 1) // 
                {
                    fadeTimer.Stop();
                    fadingOut = false; // Reset fade out state
                }
            }
        }

        private void StartFadeIn()
        {
            fadingOut = false; // Set flag for fading in
            this.Opacity = 0.9; // Reset to fully transparent
            fadeTimer.Start(); // Start fade timer for fading in
        }


        private void SwitchToNextImage()
        {
            currentImage?.Dispose();

            // Move to the next image
            currentImageIndex = (currentImageIndex + 1) % imageFiles.Length;
            currentImage = Image.FromFile(imageFiles[currentImageIndex]);
            
            // Randomly set initial position
            RandomizePosition();
            ResetAnimation();
        }
        private void RandomizePosition()
        {
            // Randomize the current position to keep the image on screen
            // Ensure the image stays within the bounds of the window
            if(currentImage == null) return;
            float maxPosX = (currentImage.Width * currentZoom) - this.ClientSize.Width;
            float maxPosY = (currentImage.Height * currentZoom) - this.ClientSize.Height;

            currentPosition.X = (float)(random.NextDouble() * Math.Max(0, maxPosX));
            currentPosition.Y = (float)(random.NextDouble() * Math.Max(0, maxPosY));
        }

        private void ResetAnimation()
        {
            currentZoom = 1.0f;//zoomOutLimit;
            zoomSpeed = 0.01f; // Adjust zoom speed
            panSpeedX = 0.01f; // Set pan speed in X direction (adjustable)
            panSpeedY = 0.01f; // Set pan speed in Y direction (adjustable)
            imageDisplayDuration = 5000; // Reset display time for new image
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (currentImage == null) return;

            // Calculate destination rectangle for drawing
            float destWidth = currentImage.Width * currentZoom;
            float destHeight = currentImage.Height * currentZoom;

            var destRect = new RectangleF(
                -(currentPosition.X * currentZoom),
                -(currentPosition.Y * currentZoom),
                destWidth,
                destHeight
            );

            e.Graphics.DrawImage(currentImage, destRect);
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Cursor.Show(); // Show the cursor when the application is closing
            currentImage?.Dispose(); // Dispose of the current image
            renderTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void ShuffleArray(string[] array)
        {
            Random random = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1); // Get a random index from 0 to i
                // Swap array[i] with the element at the random index
                string temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }


    }

    public class SettingsForm : Form
    {
        private TextBox photoPathTextBox;
        private NumericUpDown zoomSpeedNumericUpDown;
        private Button saveButton;

        public SettingsForm()
        {
            photoPathTextBox = new TextBox();
            zoomSpeedNumericUpDown = new NumericUpDown();
            saveButton = new Button();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Screen Saver Settings";
            this.Size = new Size(400, 200);

            var photoPathLabel = new Label() { Text = "Photo Path:", Location = new Point(10, 20) };
            photoPathTextBox = new TextBox() { Location = new Point(100, 20), Width = 250 };
            var zoomSpeedLabel = new Label() { Text = "Zoom Speed:", Location = new Point(10, 60) };
            zoomSpeedNumericUpDown = new NumericUpDown() { Location = new Point(100, 60), Minimum = 1, Maximum = 100, Value = 10 };

            saveButton = new Button() { Text = "Save", Location = new Point(150, 100) };
            saveButton.Click += SaveButton_Click;

            this.Controls.Add(photoPathLabel);
            this.Controls.Add(photoPathTextBox);
            this.Controls.Add(zoomSpeedLabel);
            this.Controls.Add(zoomSpeedNumericUpDown);
            this.Controls.Add(saveButton);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["PhotoPath"].Value = photoPathTextBox.Text;
            config.AppSettings.Settings["ZoomSpeed"].Value = zoomSpeedNumericUpDown.Value.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            MessageBox.Show("Settings saved. Please restart the screen saver.");
        }

    }

static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ScreenSaverForm());
        }
    }
}