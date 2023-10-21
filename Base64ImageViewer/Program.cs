namespace Base64ImageViewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            if (args.Length == 0)
            {
                throw new ArgumentException("No se han proporcionado los parametros requeridos");
            }
            else
            {
                if (args[0] == "Base64")
                    Application.Run(new Base64ImageViewer(args[1]));
                else
                    Application.Run(new ImageViewer(args[1]));
            }
        }
    }
}