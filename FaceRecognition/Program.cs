// See https://aka.ms/new-console-template for more information
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceRecognition.Model;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

List<FaceData> faceList;
VectorOfMat imageList = new VectorOfMat();
VectorOfInt labelList = new VectorOfInt();
//CascadeClassifier? haarCascade = null;
CascadeClassifier? faceClassifier = null;
EigenFaceRecognizer? recognizer = null;
Rectangle[]? faces = null;

Console.WriteLine("Testing face recognition");
initialize(Path.Combine(Directory.GetCurrentDirectory(), @"Images\image.jpg"));

Registro FetchDBImage(string dni)
{
    if (string.IsNullOrEmpty(dni))
        throw new ArgumentNullException("dni is null");

    string connectionString = "Data Source=localhost;Initial Catalog=dbFotos;Integrated Security=True";
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        try
        {
            Registro registro = new Registro();
            connection.Open();

            string query = $"SELECT cedula, Foto FROM Fotos WHERE cedula = '" + dni + "';";
            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string cedula = reader["cedula"].ToString();
                string imagenBase64 = reader["Foto"].ToString();

                registro = new Registro
                {
                    Cedula = cedula,
                    ImagenBase64 = imagenBase64
                };
            }

            reader.Close();

            return registro;
        }
        catch (Exception ex)
        {
            // Manejar la excepción según tus necesidades
            throw;
        }
    }
}

List<Registro> FetchDB(int page, int take)
{
    List<Registro> registros = new List<Registro>();
    //string connectionString = "Data Source=172.16.8.175;Initial Catalog=db_foto;User ID=UsrSistema;Password=F@c3Sys2022;TrustServerCertificate=True;";
    string connectionString = "Data Source=localhost;Initial Catalog=dbFotos;Integrated Security=True";
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        try
        {
            connection.Open();

            string query = $"SELECT cedula, Foto FROM Fotos order by cedula OFFSET {(page - 1) * take} ROWS FETCH NEXT {take} ROWS ONLY;";
            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string cedula = reader["cedula"].ToString();
                string imagenBase64 = reader["Foto"].ToString();

                if (cedula == "0011004940008N")
                    Console.WriteLine("Ya se obtuvo");

                registros.Add(new Registro
                {
                    Cedula = cedula,
                    ImagenBase64 = imagenBase64
                });
            }

            reader.Close();
        }
        catch (Exception ex)
        {
            // Manejar la excepción según tus necesidades
            Console.WriteLine("Error al obtener los registros: " + ex.Message);
        }
    }

    return registros;
}

Rectangle[] detectFacesInImage(Image<Bgr, byte> emguImage)
{
    //faceClassifier = new CascadeClassifier("Assets/haarcascade_frontalface_alt.xml");
    faceClassifier = new CascadeClassifier("Assets/haarcascade_frontalface_default.xml");
    //faceClassifier = new CascadeClassifier("Assets/haarcascade_frontalcatface.xml");

    var grayImage = emguImage.Convert<Gray, byte>();

    // Detectar caras utilizando el clasificador HaarCascade
    Rectangle[] faces = faceClassifier.DetectMultiScale(grayImage, 1.1, 4);

    return faces;
}

void GetFacesList(int page, int take)
{

    //haarCascade = new CascadeClassifier(Config.HaarCascadePath);
    faceList = FetchDB(page, take).Select(s =>
    {
        var bytes = Convert.FromBase64String(s.ImagenBase64);
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            // Leer la imagen desde el MemoryStream
            Bitmap bitmap = new Bitmap(ms);

            return new FaceData()
            {
                FaceImage = bitmap.ToImage<Gray, byte>().Resize(128, 150, Inter.Cubic),
                PersonName = s.Cedula
            };
        }
    }).ToList();

    int i = 0; imageList.Clear(); labelList.Clear();
    foreach (var face in faceList)
    {
        imageList.Push(face.FaceImage.Mat);
        labelList.Push(new[] { i++ });
    }

    recognizer = new EigenFaceRecognizer(labelList.Size);

    // Train recogniser
    if (imageList.Size > 0)
        recognizer.Train(imageList, labelList);
}

void initialize(string imagePath)
{
    Image<Bgr, byte> emguImage = new Image<Bgr, byte>(imagePath);
    if (faces == null)
        faces = detectFacesInImage(emguImage);

    if (faces.Length == 0)
    {
        Console.WriteLine("No se han detectado rostros en tu imagen");
        return;
    }

    int count = 1, selectedIndex = 0;
    foreach (var face in faces)
    {
        Console.WriteLine("Se ha detectado un rostro. Presiona " + count + " para procesar esta imagen.");
        count++;
    }

    selectedIndex = int.Parse(Console.ReadLine() ?? "0");
    var detectedFace = emguImage
                .Copy(faces[selectedIndex - 1])
                .Convert<Gray, byte>()
                .Resize(128, 150, Inter.Cubic);

    if (detectedFace != null)
    {
        string imageName = @"Images\" + Guid.NewGuid() + ".png";
        emguImage.Draw(faces[selectedIndex - 1], new Bgr(255, 255, 0), 2);
        emguImage.Save(Path.Combine(Directory.GetCurrentDirectory(), imageName));

        Process.Start(Directory.GetCurrentDirectory() + @"\ImageViewer.exe", "Image " + imageName);

        FaceRecognition(detectedFace);
    }

    Console.WriteLine("Fin del programa");
}

void FaceRecognition(Image<Gray, byte> emguImage, int page = 1, int take = 100)
{
    GetFacesList(page, take);
    double umbralDistancia = 4000;

    if (imageList.Size != 0)
    {
        MCvTermCriteria termCrit = new MCvTermCriteria(faceList.Count, 0.001);

        //Eigen Face Algorithm
        FaceRecognizer.PredictionResult result = (recognizer ?? throw new ArgumentNullException("No se ha encontrado Recognizer"))
            .Predict(emguImage);

        if (result.Distance == -1)
            Console.WriteLine("Unknown");
        else if (result.Distance < umbralDistancia)
        {
            try
            {
                Process.Start(Directory.GetCurrentDirectory() + @"\ImageViewer.exe", "Base64 " + FetchDBImage(faceList[result.Label].PersonName).ImagenBase64);
                Console.WriteLine(faceList[result.Label].PersonName);
            }
            catch { }
        }
        else
        {
            try
            {
                if (result.Distance < 5000)
                    Process.Start(Directory.GetCurrentDirectory() + @"\ImageViewer.exe", "Base64 " + FetchDBImage(faceList[result.Label].PersonName).ImagenBase64);
            }
            catch { }

            Console.WriteLine(result.Distance.ToString());
            FaceRecognition(emguImage, page + 1);
        }
    }
    else
    {
        Console.WriteLine("No se han encontrado imagenes");
    }
}