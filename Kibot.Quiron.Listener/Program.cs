using Kibot.Quiron.Listener.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Kibot.Quiron.Listener
{
    public class Program
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .Build();

        private static readonly int _clientId = Configuration.GetValue<int>("ClientId");
        private static readonly string _connectionString = Configuration.GetValue<string>("ConnectionString");
        private static readonly string _hubAddress = Configuration.GetValue<string>("HubAddress");

        static void Main()
        {
            ConnectHub();
        }

        private static void ConnectHub()
        {
            try
            {
                var connection = new HubConnectionBuilder().WithUrl(_hubAddress).Build();

                connection.StartAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("There was an error opening the connection:{0}", task.Exception.GetBaseException());
                    }
                    else
                    {
                        Console.WriteLine($"Client: {_clientId}");
                        Console.WriteLine("Connection established");
                        Console.WriteLine($"Listening events from {_hubAddress}\n");
                    }
                }).Wait();

                connection.On<string>("received", citasRecibidas =>
                {
                    var citas = JsonConvert.DeserializeObject<List<ChatbotCita>>(citasRecibidas);
                    var citasCliente = citas.Where(x => x.IdCliente == _clientId).ToList();
                    Console.WriteLine($"Citas: {citasCliente.Count}");

                    foreach (var cita in citasCliente)
                    {
                        ActualizarCita(cita);
                    }
                });

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Se produjo un error en la conexión con SignalR: {ex.Message}");
                Console.ReadLine();
            }
        }

        static void ActualizarCita(ChatbotCita cita)
        {
            try
            {
                SqlConnection connection = new(_connectionString);
                connection.Open();

                string query = "ActualizarChatBotCita";
                SqlCommand cmd = new(query, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 9600
                };

                cmd.Parameters.Add(new SqlParameter("@NumeroCita", cita.NumeroCita));
                cmd.Parameters.Add(new SqlParameter("@Enviado", cita.Enviado));
                cmd.Parameters.Add(new SqlParameter("@FechaHoraEnvio", cita.FechaHoraEnvio));
                cmd.Parameters.Add(new SqlParameter("@FechaHoraRespuesta", cita.FechaHoraRespuesta));
                cmd.Parameters.Add(new SqlParameter("@Respuesta", cita.Respuesta));
                cmd.ExecuteNonQuery();

                connection.Close();

                Console.WriteLine($"Respuesta :: => [ cita: {Convert.ToInt32(cita.NumeroCita)}, paciente: {cita.NombresPaciente} - {cita.Celular}, respuesta: {cita.Respuesta} ]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Se produjo un error la actualización: {ex.Message}");
                Console.ReadLine();
            }
        }
    }
}
