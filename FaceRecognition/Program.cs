// See https://aka.ms/new-console-template for more information
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceRecognition.Model;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;

List<FaceData> faceList;
VectorOfMat imageList = new VectorOfMat();
VectorOfInt labelList = new VectorOfInt();
//CascadeClassifier? haarCascade = null;
CascadeClassifier? faceClassifier = null;
EigenFaceRecognizer? recognizer = null;

Console.WriteLine("Testin face recognition");
FaceRecognition(Path.Combine(Directory.GetCurrentDirectory(), @"Images\image.jpg"));

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

void FaceRecognition(string imagePath, int page = 1, int take = 100)
{
    GetFacesList(page, take);

    if (imageList.Size != 0)
    {
        Image<Bgr, byte> emguImage = new Image<Bgr, byte>(imagePath);

        var faces = detectFacesInImage(emguImage);
        double umbralDistancia = 5000;

        foreach (var face in faces)
        {
            var detectedFace = emguImage.Copy(face).Convert<Gray, byte>().Resize(128, 150, Inter.Cubic);
            if (detectedFace != null)
            {
                emguImage.Draw(face, new Bgr(255, 255, 0), 2);
                emguImage.Save(Path.Combine(Directory.GetCurrentDirectory(), @"Images\" + Guid.NewGuid() + ".png"));

                MCvTermCriteria termCrit = new MCvTermCriteria(faceList.Count, 0.001);

                //Eigen Face Algorithm
                FaceRecognizer.PredictionResult result = (recognizer ?? throw new ArgumentNullException("No se ha encontrado Recognizer"))
                    .Predict(detectedFace);

                if (result.Distance == -1)
                {
                    Console.WriteLine("Unknown");
                    Console.WriteLine("Fin");
                }
                else if (result.Distance < umbralDistancia)
                {
                    Console.WriteLine(faceList[result.Label].PersonName);
                    Console.WriteLine("Fin");
                }
                else
                {
                    Console.WriteLine(result.Distance.ToString());
                    FaceRecognition(imagePath, page + 1, take);
                }
            }
            break;
        }
    }
    else
    {
        Console.WriteLine("No se han encontrado imagenes");
    }
}