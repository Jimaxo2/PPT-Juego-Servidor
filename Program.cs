using PPT_Juego_Cliente.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PPT_Juego_Servidor;
using System.Windows.Forms.VisualStyles;

namespace PPT_Juego_Servidor
{
    internal class Program
    {
        static TcpClient Jugador1;
        static TcpClient Jugador2;

        static string eleccionJugador1 = null;
        static string eleccionJugador2 = null;
        static int idJugador1 = 0;
        static int idJugador2 = 0;
        static string nombreJugador1 = "";
        static string nombreJugador2 = "";
        static object lockElecciones = new object();

        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Servidor iniciado en puerto 5000...");

            Jugador1 = server.AcceptTcpClient();
            Console.WriteLine("Jugador 1 conectado.");

            Thread AccionesJugador1 = new Thread(() => AtenderJugador(Jugador1));
            AccionesJugador1.Start();

            Jugador2 = server.AcceptTcpClient();
            Console.WriteLine("Jugador 2 conectado.");

            Thread AccionesJugador2 = new Thread(() => AtenderJugador(Jugador2));
            AccionesJugador2.Start();

            Console.WriteLine("Ambos jugadores conectados. Iniciando el juego...");

        }

        private static void AtenderJugador(TcpClient jugador)
        {
            NetworkStream stream = jugador.GetStream();
            byte[] buffer = new byte[1024];
            StringBuilder acumulador = new StringBuilder();

            List<string> lineasPendientes = new List<string>();

            while (true)
            {
                int bytes = stream.Read(buffer, 0, buffer.Length);
                string recibido = Encoding.UTF8.GetString(buffer, 0, bytes);

                acumulador.Append(recibido);

                string contenido = acumulador.ToString();
                int indice;

                while ((indice = contenido.IndexOf('\n')) >= 0)
                {
                    string linea = contenido.Substring(0, indice).TrimEnd('\r');
                    contenido = contenido.Substring(indice + 1);
                    acumulador.Clear();
                    acumulador.Append(contenido);

                    lineasPendientes.Add(linea);

                    // ¿Ya tenemos dos líneas? -> Procesar mensaje completo
                    if (lineasPendientes.Count == 2)
                    {
                        string comando = lineasPendientes[0];
                        string argumentos = lineasPendientes[1];

                        switch (comando)
                        {
                            case "IniciarSesion":
                                {
                                    // argumentos = "Javier|Password12"
                                    string[] partes = argumentos.Split('|');

                                    // Validar formato correcto
                                    if (partes.Length < 2)
                                    {
                                        string respuesta = "Error|Formato incorrecto\n";
                                        byte[] dataErr = Encoding.UTF8.GetBytes(respuesta);
                                        stream.Write(dataErr, 0, dataErr.Length);
                                        break;
                                    }

                                    string usuario = partes[0];
                                    string contrasena = partes[1];

                                    // Consultar en la base de datos
                                    string[] datosUsuario = BDconexion.obtenerDatos(
                                        $@"SELECT [JugadorID]
                                              ,[NombreJugador]
                                              ,[Contrasenia]
                                              ,[TotalPartidas]
                                              ,[PartidasGanadas]
                                              ,[PartidasEmpatadas]
                                              ,[PartidasPerdidas]
                                              ,[TasaVictoria]
                                    FROM [PiedraPapelTijera1DB].[dbo].[Jugadores]
                                    WHERE [NombreJugador] = '{usuario}' 
                                      AND [Contrasenia] = '{contrasena}'"
                                    );

                                    // CASO: NO SE ENCONTRÓ EL USUARIO
                                    if (datosUsuario.Length == 1 && datosUsuario[0].StartsWith("Error"))
                                    {
                                        string respuesta = "Error|Credenciales incorrectas\n";
                                        byte[] dataError = Encoding.UTF8.GetBytes(respuesta);
                                        stream.Write(dataError, 0, dataError.Length);
                                        break;
                                    }

                                    // CASO: EL USUARIO EXISTE → enviar datos
                                    bool esJugador1 = (jugador == Jugador1);

                                    if (esJugador1)
                                    {
                                        idJugador1 = int.Parse(datosUsuario[0]);
                                        nombreJugador1 = datosUsuario[1];
                                    }
                                    else
                                    {
                                        idJugador2 = int.Parse(datosUsuario[0]);
                                        nombreJugador2 = datosUsuario[1];
                                    }

                                    string respuestaOK = string.Join("|", datosUsuario) + "\n";
                                    byte[] dataOK = Encoding.UTF8.GetBytes(respuestaOK);
                                    stream.Write(dataOK, 0, dataOK.Length);

                                    Console.WriteLine($"{(esJugador1 ? "Jugador 1" : "Jugador 2")} inició sesión: {usuario}");
                                    break;

                                }
                            case "CrearCuenta":
                                {
                                    // argumentos = "Javier|Password12"
                                    string[] partes = argumentos.Split('|');

                                    // Validar formato correcto
                                    if (partes.Length < 2)
                                    {
                                        string respuesta = "Error|Formato incorrecto\n";
                                        byte[] dataErr = Encoding.UTF8.GetBytes(respuesta);
                                        stream.Write(dataErr, 0, dataErr.Length);
                                        break;
                                    }

                                    string usuario = partes[0];
                                    string contrasena = partes[1];

                                    // Consultar en la base de datos
                                    int filasAfectadas = BDconexion.ejecutarConsulta(
                                        $@"INSERT INTO [dbo].[Jugadores]
                                               ([NombreJugador]
                                               ,[Contrasenia])
                                         VALUES
                                               ('{usuario}'
                                               ,'{contrasena}')"
                                    );

                                    // CASO: NO SE ENCONTRÓ EL USUARIO
                                    if (filasAfectadas == 0)
                                    {
                                        string respuesta = "Error|Credenciales incorrectas\n";
                                        byte[] dataError = Encoding.UTF8.GetBytes(respuesta);
                                        stream.Write(dataError, 0, dataError.Length);
                                        break;
                                    }

                                    // CASO: EL USUARIO EXISTE → enviar datos
                                    string respuestaOK = $"{usuario}" + "\n";
                                    byte[] dataOK = Encoding.UTF8.GetBytes(respuestaOK);
                                    stream.Write(dataOK, 0, dataOK.Length);

                                    break;
                                }
                            case "Eleccion":
                                {
                                    // argumentos = "Piedra" o "Papel" o "Tijera"
                                    string eleccion = argumentos.Trim();

                                    if (eleccion != "Piedra" && eleccion != "Papel" && eleccion != "Tijera")
                                    {
                                        string respuesta = "Error|Elección inválida\n";
                                        byte[] dataErr = Encoding.UTF8.GetBytes(respuesta);
                                        stream.Write(dataErr, 0, dataErr.Length);
                                        break;
                                    }

                                    lock (lockElecciones)
                                    {
                                        // Guardar la elección del jugador basado en su stream
                                        bool esJugador1 = (jugador == Jugador1);

                                        if (esJugador1)
                                        {
                                            eleccionJugador1 = eleccion;
                                            Console.WriteLine($"[{nombreJugador1}] eligió: {eleccion}");
                                        }
                                        else
                                        {
                                            eleccionJugador2 = eleccion;
                                            Console.WriteLine($"[{nombreJugador2}] eligió: {eleccion}");
                                        }

                                        // Verificar si ambos jugadores ya eligieron
                                        if (eleccionJugador1 != null && eleccionJugador2 != null)
                                        {
                                            Console.WriteLine("\n ESPERANDO RESULTADO");

                                            // Determinar el resultado
                                            string resultado = DeterminarGanador(eleccionJugador1, eleccionJugador2);
                                            Console.WriteLine($"Resultado: {resultado}");
                                            Console.WriteLine($"{nombreJugador1}: {eleccionJugador1} vs {nombreJugador2}: {eleccionJugador2}");

                                            // Registrar encuentro en BD
                                            int ganadorID = 0; // 0 = empate
                                            if (resultado == "Jugador1")
                                                ganadorID = idJugador1;
                                            else if (resultado == "Jugador2")
                                                ganadorID = idJugador2;

                                            RegistrarEncuentro(idJugador1, idJugador2, eleccionJugador1, eleccionJugador2, ganadorID);

                                            // Enviar resultado a ambos jugadores
                                            string mensajeResultado = $"Resultado\n{resultado}|{eleccionJugador1}|{eleccionJugador2}|{nombreJugador1}|{nombreJugador2}\n";

                                            byte[] dataJ1 = Encoding.UTF8.GetBytes(mensajeResultado);
                                            Jugador1.GetStream().Write(dataJ1, 0, dataJ1.Length);

                                            byte[] dataJ2 = Encoding.UTF8.GetBytes(mensajeResultado);
                                            Jugador2.GetStream().Write(dataJ2, 0, dataJ2.Length);

                                            Console.WriteLine("RESULTADO ENVIADO\n");

                                            // Reiniciar elecciones para la próxima ronda
                                            eleccionJugador1 = null;
                                            eleccionJugador2 = null;
                                        }
                                        else
                                        {
                                            // Confirmar recepción y esperar al otro jugador
                                            string respuesta = "Esperando|Esperando al otro jugador...\n";
                                            byte[] data = Encoding.UTF8.GetBytes(respuesta);
                                            stream.Write(data, 0, data.Length);
                                        }
                                    }
                                    break;
                                }
                        }

                        lineasPendientes.Clear();
                    }
                }
            }
        }
        private static string DeterminarGanador(string eleccion1, string eleccion2)
        {
            // Empate
            if (eleccion1 == eleccion2)
                return "Empate";

            // Jugador 1 gana
            if ((eleccion1 == "Piedra" && eleccion2 == "Tijera") ||
                (eleccion1 == "Papel" && eleccion2 == "Piedra") ||
                (eleccion1 == "Tijera" && eleccion2 == "Papel"))
            {
                return "Jugador1";
            }

            // Jugador 2 gana
            return "Jugador2";
        }

        private static void RegistrarEncuentro(int idJ1, int idJ2, string elecJ1, string elecJ2, int ganadorID)
        {
            try
            {
                string ganadorIDStr = (ganadorID == 0) ? "NULL" : ganadorID.ToString();

                string consulta = $@"INSERT INTO [dbo].[Encuentros]
                                   ([JugadorA]
                                   ,[JugadorB]
                                   ,[GanadorID]
                                   ,[EleccionJugadorA]
                                   ,[EleccionJugadorB])
                             VALUES
                                   ({idJ1}
                                   ,{idJ2}
                                   ,{ganadorIDStr}
                                   ,'{elecJ1}'
                                   ,'{elecJ2}')";

                int filasAfectadas = BDconexion.ejecutarConsulta(consulta);

                if (filasAfectadas > 0)
                    Console.WriteLine("Encuentro registrado en la base de datos");
                else
                    Console.WriteLine("Error al registrar el encuentro");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en RegistrarEncuentro: {ex.Message}");
            }
        }
    }
}
