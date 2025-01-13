using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;
using System.Drawing.Imaging;

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
        private readonly float zoomInLimit = 1.5f; //1.5f;// Maximum zoom limit
        private readonly float zoomOutLimit = 1.0f; //1.0f; // Minimum zoom limit
        private int imageDisplayDuration = 15000; // Display duration for each image in milliseconds
        private float fadeStep = 0.1f; // Opacity change per step
        private bool fadingOut = false;
        private float imageOpacity = 1.0f; // Opacity of the current image (1.0 = fully visible, 0.0 = fully transparent)
        private TextureBrush? cachedTextureBrush; // Cached TextureBrush for the current image

        private System.Windows.Forms.Timer fadeTimer;
        public ScreenSaverForm()
        {
            renderTimer = new System.Windows.Forms.Timer();
            imageFiles = Array.Empty<string>();
            currentImage=null;
            photoPath = string.Empty;

            zoomSpeed = 0.01f;
            currentZoom = 1.0f; // Start at normal size
            currentPosition = new PointF(0, 0); // Reset panning position
            panSpeedX = 0.001f; // Set pan speed in the X direction (adjust as needed)
            panSpeedY = 0.001f; // Set pan speed in the Y direction (adjust as needed)

            LoadSettings();
            InitializeComponents();
            LoadImages();
            ResetAnimation();
            RandomizePosition();
            StartRenderLoop();

            // Initialize fade timer
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 50; // Adjust fade speed
            fadeTimer.Tick += FadeTimer_Tick;            
        }

        private void LoadSettings()
        {
            photoPath = ConfigurationManager.AppSettings["PhotoPath"] ?? "D:\\ScreenSaver";
            zoomSpeed =float.Parse(ConfigurationManager.AppSettings["ZoomSpeed"] ?? "0.01");
            
        }

        private void InitializeComponents()
        {
            this.WindowState = FormWindowState.Maximized;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            //this.TopMost = true;
            if (Screen.PrimaryScreen != null)
            {
                this.Size = Screen.PrimaryScreen.Bounds.Size;
            }
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.KeyDown += (s, e) => Application.Exit(); 
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
            // Calculate initial zoom factor to fit the image within the form's client area
            float zoomX = (float)this.ClientSize.Width / currentImage.Width;
            float zoomY = (float)this.ClientSize.Height / currentImage.Height;
            currentZoom = Math.Min(zoomX, zoomY); // Use the smaller zoom factor to fit the image
        }
         private void StartRenderLoop()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            //renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval =24; //16; // Approximately 60 FPS
            renderTimer.Tick += (s, e) => RenderFrame();
            renderTimer.Start();
        }

        private void RenderFrame()
        {
            float deltaTime = 0.024f; //0.016f; // Default to 60 FPS
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
             // Ensure zoom stays within limits
        if (currentZoom >= zoomInLimit)
        {
            currentZoom = zoomInLimit;
            zoomSpeed = -Math.Abs(zoomSpeed); // Reverse zoom direction
        }
        else if (currentZoom <= zoomOutLimit)
        {
            currentZoom = zoomOutLimit;
            zoomSpeed = Math.Abs(zoomSpeed); // Reverse zoom direction
        }
            // if (currentZoom >= zoomInLimit || currentZoom <= zoomOutLimit)
            // {   
            //     zoomSpeed = -zoomSpeed; // Reverse zoom direction
            // }
            // Update pan position
            currentPosition.X += panSpeedX * deltaTime;
            currentPosition.Y += panSpeedY * deltaTime;
            // If we reach the edge, reverse direction
            if(currentImage == null) return;
            if (currentPosition.X <= 0 || currentPosition.X >= currentImage.Width * currentZoom - (Screen.PrimaryScreen?.Bounds.Size.Width ?? this.ClientSize.Width))
                panSpeedX = -panSpeedX;

            if (currentPosition.Y <= 0 || currentPosition.Y >= currentImage.Height * currentZoom - (Screen.PrimaryScreen?.Bounds.Size.Height ?? this.ClientSize.Height))
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
                imageOpacity -= fadeStep; // Decrease opacity
                
                if (imageOpacity <= 0) // 
                {
                    fadeTimer.Stop();
                    SwitchToNextImage(); // Change image after fade-out
                    imageOpacity = 0; // Ensure opacity 
                    StartFadeIn(); // Start fade-in effect
                }
            }
            else // Fade in
            {
                imageOpacity += fadeStep; // Increase opacity
                if (imageOpacity >= 1) // 
                {
                    fadeTimer.Stop();
                    fadingOut = false; // Reset fade out state
                }
            }
        }

        private void StartFadeIn()
        {
            fadingOut = false; // Set flag for fading in
            imageOpacity= 0; // Reset to fully transparent
            fadeTimer.Start(); // Start fade timer for fading in
        }

        private void SwitchToNextImage()
        {
            currentImage?.Dispose();

            // Move to the next image
            currentImageIndex = (currentImageIndex + 1) % imageFiles.Length;
            currentImage = Image.FromFile(imageFiles[currentImageIndex]);
            
            // Create and cache a TextureBrush for the new image
            if (currentImage != null)
            {
                // Create a color matrix to apply opacity to the image
                ColorMatrix colorMatrix = new ColorMatrix();
                colorMatrix.Matrix33 = imageOpacity; // Set the opacity (alpha channel)

                // Create an image attributes object to apply the color matrix
                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                // Create a TextureBrush with the image and apply the ImageAttributes
                cachedTextureBrush = new TextureBrush(currentImage, new Rectangle(0, 0, currentImage.Width, currentImage.Height), imageAttributes);
            }
            // Reset the display time for the new image
            ResetAnimation();
            // Randomly set initial position
            RandomizePosition();
            // Reset image opacity for fade-in effect
            imageOpacity = 0; // Start fully transparent

        }
        private void RandomizePosition()
        {
            // Randomize the current position to keep the image on screen
            // Ensure the image stays within the bounds of the window
            if(currentImage == null) return;
            float maxPosX = (currentImage.Width * currentZoom)  - (Screen.PrimaryScreen?.Bounds.Size.Width ?? this.ClientSize.Width);
            float maxPosY = (currentImage.Height * currentZoom) - (Screen.PrimaryScreen?.Bounds.Size.Height ?? this.ClientSize.Height);;


            currentPosition.X = (float)(random.NextDouble() * Math.Max(0, maxPosX));
            currentPosition.Y = (float)(random.NextDouble() * Math.Max(0, maxPosY));

            // Randomize the pan direction
            panSpeedX = (float)(random.NextDouble() * 2 - 1) * 20.0f; // Random speed between -0.5 and 0.5
            panSpeedY = (float)(random.NextDouble() * 2 - 1) * 20.0f; // Random speed between -0.5 and 0.5

            zoomSpeed = GetRandomBoolean() ? -Math.Abs(zoomSpeed) : Math.Abs(zoomSpeed); // Randomize the zoom direction
            panSpeedX = GetRandomBoolean() ? -Math.Abs(panSpeedX) : Math.Abs(panSpeedX); // Randomize the pan direction	
            panSpeedY = GetRandomBoolean() ? -Math.Abs(panSpeedY) : Math.Abs(panSpeedY); // Randomize the pan direction
        }

        private void ResetAnimation()
        {
            currentZoom = zoomOutLimit;
            zoomSpeed = 0.01f; // Adjust zoom speed
            panSpeedX = 0.01f; // Set pan speed in X direction (adjustable)
            panSpeedY = 0.01f; // Set pan speed in Y direction (adjustable)
            imageDisplayDuration = 15000; // Reset display time for new image

        }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Fill the background with black
        e.Graphics.Clear(Color.Black);

        if (currentImage == null) return;

        // Enable high-quality rendering
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // Ensure smooth rendering

        // Create a transformation matrix for panning and zooming
        using (System.Drawing.Drawing2D.Matrix transform =  new System.Drawing.Drawing2D.Matrix())
        {
            // Apply zoom
            transform.Scale(currentZoom, currentZoom);

            // Apply panning
            transform.Translate(-currentPosition.X, -currentPosition.Y, System.Drawing.Drawing2D.MatrixOrder.Append);

            // Apply the transformation to the Graphics object
            e.Graphics.Transform = transform;
        }

        // Create a color matrix to apply opacity to the image
        ColorMatrix colorMatrix = new ColorMatrix();
        colorMatrix.Matrix33 = imageOpacity; // Set the opacity (alpha channel)

        // Create an image attributes object to apply the color matrix
        ImageAttributes imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        // Draw the image at its original size (scaling and panning are handled by the transformation matrix)
        e.Graphics.DrawImage(
            currentImage,
            new Rectangle(0, 0, currentImage.Width, currentImage.Height), // Draw at original size
            0, 0, currentImage.Width, currentImage.Height, // Source rectangle
            GraphicsUnit.Pixel,
            imageAttributes
        );

        // Reset the transformation matrix
        e.Graphics.ResetTransform();
    }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Cursor.Show(); // Show the cursor when the application is closing
            currentImage?.Dispose(); // Dispose of the current image
            renderTimer.Dispose();
            cachedTextureBrush?.Dispose(); // Dispose of the cached TextureBrush
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
        
        private bool GetRandomBoolean()
        {
            return random.Next(2) == 0; // Generates 0 or 1, returns true if 0, false if 1
        }
    }

    public class SettingsForm : Form
    {
        private TextBox photoPathTextBox;
        private NumericUpDown zoomSpeedNumericUpDown;
        private Button saveButton;
        private string configFilePath=string.Empty;
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
            this.Size = new Size(550, 250);

            var photoPathLabel = new Label() { Text = "Photo Path:", Location = new Point(10, 20) };
            photoPathTextBox = new TextBox() { Location = new Point(120, 20), Width = 250 };

            var selectFolderButton = new Button() { Location =new Point(380,20), Width=80, Text = "Folder" };             selectFolderButton.Click += SelectFolderButton_Click; 

            var zoomSpeedLabel = new Label() { Text = "Zoom Speed(0.01 - 1):", Width= 80,Location = new Point(10, 60) };
            zoomSpeedNumericUpDown = new NumericUpDown() { Location = new Point(120, 60), Minimum = 0.01M, Maximum = 10M, Value = 0.01M };

            saveButton = new Button() { Text = "Save", Location = new Point(150, 100) };
            saveButton.Click += SaveButton_Click;

            this.Controls.Add(photoPathLabel);
            this.Controls.Add(photoPathTextBox);
            this.Controls.Add(selectFolderButton);
            this.Controls.Add(zoomSpeedLabel);
            this.Controls.Add(zoomSpeedNumericUpDown);
            this.Controls.Add(saveButton);

            CheckConfigFile();
            // Load the configuration file
            LoadConfigFile();
        }
        private void CheckConfigFile()
        {
            // Determine the path to the AppData folder
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDirectory = Path.Combine(appDataPath, "ZoomScreenSaver");
            // Create the directory if it doesn't exist
            if (!Directory.Exists(appDirectory))
            {
                Directory.CreateDirectory(appDirectory);
            }
            // Set the path to the configuration file
            configFilePath = Path.Combine(appDirectory, "ZoomScreenSaver.dll.config");
        }

        private void LoadConfigFile()
        {
            if (File.Exists(configFilePath))
            {
                ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = configFilePath;
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

                // Get the appSettings section
                AppSettingsSection appSettings = (AppSettingsSection)config.GetSection("appSettings");

                // Get the values from the appSettings section
                string photoPath = appSettings.Settings["PhotoPath"].Value;
                decimal zoomSpeed = decimal.Parse(appSettings.Settings["ZoomSpeed"].Value);

                // Set the values in the form
                photoPathTextBox.Text = photoPath;
                zoomSpeedNumericUpDown.Value = zoomSpeed;
            }else{
                File.Copy("ZoomScreenSaver.dll.config", configFilePath);
            }
        }

        private void SelectFolderButton_Click(object? sender, EventArgs? e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    photoPathTextBox.Text = folderBrowserDialog.SelectedPath;
                    //MessageBox.Show($"Selected Folder: {selectedPath}");
                }
            }
        }
        private void SaveButton_Click(object? sender, EventArgs e)
        {
             try
            {
                ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = configFilePath;
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
               
                var settings = config.AppSettings.Settings;
                if (settings["PhotoPath"] == null || string.IsNullOrEmpty(photoPathTextBox.Text))
                {
                    settings.Add("PhotoPath", "c:\\Pictures");
                }
                else
                {
                    settings["PhotoPath"].Value = photoPathTextBox.Text;
                }

                if (settings["ZoomSpeed"] == null || zoomSpeedNumericUpDown.Value == 0)
                {
                    settings.Add("ZoomSpeed", "0.01");
                }
                else
                {
                    settings["ZoomSpeed"].Value = zoomSpeedNumericUpDown.Value.ToString();
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ConfigurationErrorsException ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }

static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length > 0)
        {
            string firstArgument = args[0].ToLower().Trim();
            string? secondArgument=null;
            
            // Handle cases where arguments are separated by colon.
            // Examples: /c:1234567 or /P:1234567
            if (firstArgument.Length > 2)
            {
                secondArgument = firstArgument.Substring(3).Trim();
                firstArgument = firstArgument.Substring(0, 2);
            }
            else if (args.Length > 1)
                secondArgument = args[1];
    
            if (firstArgument == "/c")           // Configuration mode
            {
                Application.Run(new SettingsForm());
            
            }
            else if (firstArgument == "/p")      // Preview mode
            {
                Application.Run(new ScreenSaverForm());
            }
            else if (firstArgument == "/s")      // Full-screen mode
            {
                Application.Run(new ScreenSaverForm());
            } 
            else    // Undefined argument
            {
                MessageBox.Show("Sorry, but the command line argument \"" + firstArgument +
                    "\" is not valid.", "ScreenSaver",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            }
            else    // No arguments - treat like /c
            {
                //Application.Run(new SettingsForm());
                Application.Run(new ScreenSaverForm());
            }                 
        }
    }
}