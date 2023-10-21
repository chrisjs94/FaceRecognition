namespace Base64ImageViewer
{
    public partial class ImageViewer : Form
    {
        private string pathImage { get; set; }
        public ImageViewer(string pathImage)
        {
            InitializeComponent();

            this.pathImage = pathImage;
        }

        private void ImageViewer_Load(object sender, EventArgs e)
        {
            if (!File.Exists(pathImage))
                throw new Exception("El archivo no existe, verifique la ruta e intente nuevamente");

            pictureBox1.Image = Image.FromFile(pathImage);
            this.Text = Path.GetFileName(pathImage);
        }
    }
}
