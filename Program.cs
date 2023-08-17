using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceReference;
using WorkerQueueManagement.Models;
using WorkerQueueManagement.Utils;

public class Program
{
    private static IModel channel = null;
    private static IConfiguration Configuration = null;
    // private IConfiguration Configuration;
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo
            .File("./Logs/WorkerQueueManagement.out", Serilog.Events.LogEventLevel.Debug, "{Message:lj}{NewLine}", encoding: Encoding.UTF8)
            .CreateLogger();

        AppLog logApp = new AppLog();
        logApp.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff"));
        Utilerias.ImprimirLog(logApp, 0, "Iniciando servicio de consola Worker", "Debug");

        // CreateWebHostBuilder(args).Build().Run();
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        // var builder = new ConfigurationBuilder();
        // builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile($"appsettings.{environmentName}.json", optional: false, reloadOnChange: true);
        // Configuration = builder.Build();

        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();
        
        Console.WriteLine(Configuration.GetValue<string>("Logging:LogLevel:Default"));
        Console.WriteLine(Configuration.GetValue<string>("RabbitConfiguration:UserName"));

        ConnectionFactory connectionFactory = new ConnectionFactory();
        connectionFactory.HostName = Configuration.GetValue<string>("RabbitConfiguration:HostName");
        connectionFactory.Port = Configuration.GetValue<int>("RabbitConfiguration:Port");
        connectionFactory.UserName = Configuration.GetValue<string>("RabbitConfiguration:UserName");
        connectionFactory.Password = Configuration.GetValue<string>("RabbitConfiguration:Password");
        IConnection conexion = connectionFactory.CreateConnection();
        channel = conexion.CreateModel();
        var consumer = new EventingBasicConsumer(channel);
        var consumerAlumno = new EventingBasicConsumer(channel);
        consumer.Received += ConsumerMessageReceived;
        consumerAlumno.Received += ConsumerMessageReceivedAlumno;
        var consumerTag = channel.BasicConsume(Configuration.GetValue<string>("RabbitConfiguration:Queue"), true, consumer);
        var consumerAlumnoTag = channel.BasicConsume(Configuration.GetValue<string>("RabbitConfiguration:QueueAlumno"), true, consumerAlumno);
         Console.WriteLine("Precione una tecla para finalizar con la lectura de los mensajes");
        Console.ReadLine();
    }

    // public static IWebHostBuilder CreateWebHostBuilder(string[] args) => 
    //     WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
 
    // public static async void ConsumerMessageReceived(object? sender, BasicDeliverEventArgs e)
    // {
    //     string message = Encoding.UTF8.GetString(e.Body.ToArray());
    //     AspiranteRequest request = JsonSerializer.Deserialize<AspiranteRequest>(message);
    //     AspiranteResponse response = await ClientWebServiceAspirante(request);
    //     Console.WriteLine($"Se registro la solicitud con el numero de expediente {response.NoExpediente}");
    //     // await Task.Delay(100);
    //     // Console.WriteLine($"Recibiendo solicitud de {request.Apellido} {request.Nombre}");
    //     AppLog appLog = new AppLog();
    //     appLog.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff"));
    //     Utilerias.ImprimirLog(appLog, 201, $"Se registro la solicitud con el numero de expediente {response.NoExpediente}", "Information");
    // }
    public static async void ConsumerMessageReceived(object? sender, BasicDeliverEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Body.ToArray());
        AspiranteRequest request = JsonSerializer.Deserialize<AspiranteRequest>(message);
        AspiranteResponse response = await ClientWebServiceAspirante(request);
        if (response.StatusCode == 201 || response.StatusCode == 200)
        {
            HttpClient httpClient = new HttpClient();
            NotificationRequest notificationRequest = new NotificationRequest();
            notificationRequest.Email = request.Email;
            notificationRequest.Recipient = request.Email;
            notificationRequest.Subject = $"Generación de número de Expediente {response.NoExpediente}";
            notificationRequest.Body = $"http://localhost:4200/#/dashboard/update-identification/form?type=expediente&identificationId={response.NoExpediente}";
            notificationRequest.IdentificationId = response.NoExpediente;
            notificationRequest.Type = "Expediente";
            var json = JsonSerializer.Serialize(notificationRequest);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PostAsync("https://localhost:7207/kalum-notification/v1/notification", data);
            var result = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
        Console.WriteLine($"Se registro la solicitud con el número de expediente {response.NoExpediente}");
        AppLog appLog = new AppLog();
        appLog.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff"));
        Utilerias.ImprimirLog(appLog, 201, $"Se registro la solicitud con el número de expediente {response.NoExpediente}", "Information");
    }

    public static async void ConsumerMessageReceivedAlumno(object? sender, BasicDeliverEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Body.ToArray());
        string jsonMessage = "";
        string email = "";

        using (JsonDocument doc = JsonDocument.Parse(message))
        {
            // Obtener el valor de la propiedad "Email"
            email = doc.RootElement.GetProperty("Email").GetString();

            // Crear un nuevo objeto JsonDocument con la propiedad "Email" eliminada
            using (JsonDocument newDoc = RemoveProperty(doc.RootElement, "Email"))
            {
                // Convertir el nuevo objeto JsonDocument a JSON
                string modifiedJson = newDoc.RootElement.GetRawText();
                jsonMessage = modifiedJson;
                // Console.WriteLine(modifiedJson);
            }
        }       
        Console.WriteLine(message); 
        Console.WriteLine(jsonMessage);

        EnrollmentRequest request = JsonSerializer.Deserialize<EnrollmentRequest>(jsonMessage);
        EnrollmentResponse response = await ClientWebServiceAlumno(request);
        if (response.StatusCode == 201 || response.StatusCode == 200)
        {
            HttpClient httpClient = new HttpClient();
            NotificationRequest notificationRequest = new NotificationRequest();
            notificationRequest.Email = email;
            notificationRequest.Recipient = email;
            notificationRequest.Subject = $"Generación de número de Carné {response.Carne}";
            notificationRequest.Body = $"http://localhost:4200/#/dashboard/update-identification/form?type=carne&identificationId={response.Carne}";
            notificationRequest.IdentificationId = response.Carne;
            notificationRequest.Type = "carne";
            var json = JsonSerializer.Serialize(notificationRequest);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PostAsync("https://localhost:7207/kalum-notification/v1/notification", data);
            var result = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
        Console.WriteLine($"Se registro la solicitud con el número de carne {response.Carne}");
        AppLog appLog = new AppLog();
        appLog.ResponseTime = Convert.ToInt16(DateTime.Now.ToString("fff"));
        Utilerias.ImprimirLog(appLog, 201, $"Se registro la solicitud con el número de carne {response.Carne}", "Information");
    }

    public static async Task<AspiranteResponse> ClientWebServiceAspirante(AspiranteRequest request)
    {
        AspiranteResponse aspiranteResponse = null;
        var client = new EnrollmentServiceClient(EnrollmentServiceClient.EndpointConfiguration
        .BasicHttpBinding_IEnrollmentService_soap, $"{Configuration.GetValue<string>("WebServicesConfiguration:Protocol")}://{Configuration.GetValue<string>("WebServicesConfiguration:Host")}:{Configuration.GetValue<int>("WebServicesConfiguration:Port")}/{Configuration.GetValue<string>("WebServicesConfiguration:Path")}");

        var response = await client.CandidateRecordProcessAsync(request);
        aspiranteResponse = new AspiranteResponse()
        {
            StatusCode = response.Body.CandidateRecordProcessResult.StatusCode,
            Message = response.Body.CandidateRecordProcessResult.Message,
            NoExpediente = response.Body.CandidateRecordProcessResult.NoExpediente
        };

        return aspiranteResponse;
    }

    public static async Task<EnrollmentResponse> ClientWebServiceAlumno(EnrollmentRequest request)
    {
        EnrollmentResponse enrolmentResponse = null;
        var alumno = new EnrollmentServiceClient(EnrollmentServiceClient.EndpointConfiguration
            .BasicHttpBinding_IEnrollmentService_soap, $"{Configuration.GetValue<string>("WebServicesConfiguration:Protocol")}://{Configuration.GetValue<string>("WebServicesConfiguration:Host")}:{Configuration.GetValue<int>("WebServicesConfiguration:Port")}/{Configuration.GetValue<string>("WebServicesConfiguration:Path")}");

        var response = await alumno.EnrollmentProcessAsync(request);
        enrolmentResponse = new EnrollmentResponse()
        {
            StatusCode = response.Body.EnrollmentProcessResult.StatusCode,
            Message = response.Body.EnrollmentProcessResult.Message,
            Carne = response.Body.EnrollmentProcessResult.Carne
        };

        return enrolmentResponse;
    }

    public static JsonDocument RemoveProperty(JsonElement jsonElement, string propertyName)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();

                foreach (var property in jsonElement.EnumerateObject())
                {
                    if (property.Name != propertyName)
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            stream.Seek(0, System.IO.SeekOrigin.Begin);

            return JsonDocument.Parse(stream);
        }
    }
}
