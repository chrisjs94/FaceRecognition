using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Base64ImageViewer
{
    public partial class Base64ImageViewer : Form
    {
        private string base64Image { get; set; }
        public Base64ImageViewer(string base64Image)
        {
            InitializeComponent();
            this.base64Image = base64Image;
        }

        private void ImageViewer_Load(object sender, EventArgs e)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                pictureBox1.Image = Image.FromStream(ms, true);
            }
        }
    }
}
