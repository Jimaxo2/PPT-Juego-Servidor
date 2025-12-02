using PPT_Juego_Cliente.Models;
using PPT_Juego_Servidor;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace PPT_Juego_Servidor
{
    internal class Program
    {
        static TcpClient Jugador1;
        static TcpClient Jugador2;
        static string NombreJ1 = null;
        static string NombreJ2 = null;
        static string JugadaJ1 = null;
        static string JugadaJ2 = null;
        static bool JuegoEnCurso = true;
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Servidor iniciado en puerto 5000...");

            while (true)
            {
                Console.WriteLine("\n--- Iniciando nueva sala de espera ---");

                NombreJ1 = null;
                NombreJ2 = null;
                JugadaJ1 = null;
                JugadaJ2 = null;

                Jugador1 = server.AcceptTcpClient();
                Console.WriteLine("Jugador 1 conectado.");

                Thread AccionesJugador1 = new Thread(() => AtenderJugador(Jugador1, 1));
                AccionesJugador1.Start();

                Jugador2 = server.AcceptTcpClient();
                Console.WriteLine("Jugador 2 conectado.");

                Thread AccionesJugador2 = new Thread(() => AtenderJugador(Jugador2, 2));
                AccionesJugador2.Start();

                Console.WriteLine("Ambos jugadores conectados. Iniciando el juego...");

                while (AccionesJugador1.IsAlive || AccionesJugador2.IsAlive)
                {
                    Thread.Sleep(1000);
                }

                Console.WriteLine("Partida terminada. Reiniciando servidor para nuevos clientes...");
            }
        }

        private static void AtenderJugador(TcpClient jugador, int numeroJugador)
        {
            NetworkStream stream = null; // Inicializar fuera para seguridad
            try
            {
                stream = jugador.GetStream();
            }
            catch
            {
                return; // Si no se puede obtener el stream, salimos
            }

            byte[] buffer = new byte[1024];
            StringBuilder acumulador = new StringBuilder();
            List<string> lineasPendientes = new List<string>();

            while (true)
            {
                int bytes = 0;

                // --- CORRECCIÓN AQUÍ ---
                try
                {
                    // Verificamos si podemos leer. Si el socket está cerrado, esto lanzará error.
                    if (!stream.CanRead) break;

                    bytes = stream.Read(buffer, 0, buffer.Length);

                    // Si Read devuelve 0, significa que el cliente cerró la conexión ordenadamente.
                    if (bytes == 0)
                    {
                        Console.WriteLine($"Jugador {numeroJugador} cerró la conexión.");
                        break;
                    }
                }
                catch (Exception)
                {
                    // Si ocurre un error (ej. desconexión forzosa), rompemos el bucle
                    Console.WriteLine($"Conexión con Jugador {numeroJugador} perdida.");
                    break;
                }
                // -----------------------

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

                    if (lineasPendientes.Count == 2)
                    {
                        string comando = lineasPendientes[0];
                        string argumentos = lineasPendientes[1];

                        // Ponemos un try-catch aquí también por si escribir falla
                        try
                        {
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
                                        string respuestaOK = string.Join("|", datosUsuario) + "\n";
                                        byte[] dataOK = Encoding.UTF8.GetBytes(respuestaOK);
                                        stream.Write(dataOK, 0, dataOK.Length);

                                        if (numeroJugador == 1)
                                        {
                                            NombreJ1 = datosUsuario[1].Trim();
                                        }
                                        else
                                        {
                                            NombreJ2 = datosUsuario[1].Trim();
                                        }

                                        IniciarCicloDeJuego(stream, numeroJugador);
                                        return; // Importante: Salir del hilo tras terminar el juego
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

                                        // CASO: NO SE PUDO CREAR
                                        if (filasAfectadas == 0)
                                        {
                                            string respuesta = "Error|Credenciales incorrectas\n";
                                            byte[] dataError = Encoding.UTF8.GetBytes(respuesta);
                                            stream.Write(dataError, 0, dataError.Length);
                                            break;
                                        }

                                        // CASO: EL USUARIO SE CREÓ → enviar confirmación
                                        string respuestaOK = $"{usuario}" + "\n";
                                        byte[] dataOK = Encoding.UTF8.GetBytes(respuestaOK);
                                        stream.Write(dataOK, 0, dataOK.Length);
                                        break;
                                    }
                            }
                        }
                        catch
                        {
                            break; // Si falla la lógica interna, salimos
                        }

                        lineasPendientes.Clear();
                    }
                }
            }

            // Limpieza final
            try { stream?.Close(); } catch { }
            try { jugador?.Close(); } catch { }
        }

        private static void IniciarCicloDeJuego(NetworkStream stream, int numeroJugador)
        {
            Console.WriteLine($"Jugador {numeroJugador} listo para jugar");

            while (NombreJ1 == null || NombreJ2 == null)
            {
                Thread.Sleep(500);
            }

            byte[] msgInicio = Encoding.UTF8.GetBytes("Mensaje|¡Comienza el juego!\n");
            stream.Write(msgInicio, 0, msgInicio.Length);

            if (numeroJugador == 1)
            {
                JugadaJ1 = null;
            }
            else
            {
                JugadaJ2 = null;
            }

            byte[] msgPedir = Encoding.UTF8.GetBytes("PedirJugada|Elige\n");
            try
            {
                stream.Write(msgPedir, 0, msgPedir.Length);
            }
            catch
            {
                return;
            }

            byte[] buffer = new byte[1024];
            int bytes = 0;

            try
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
            }
            catch
            {
            }

            if (bytes > 0)
            {
                string respuesta = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                if (numeroJugador == 1)
                {
                    JugadaJ1 = respuesta;
                }
                else
                {
                    JugadaJ2 = respuesta;
                }

                Console.WriteLine($"Jugador {numeroJugador} tiró: {respuesta}");

                while (JugadaJ1 == null || JugadaJ2 == null)
                {
                    Thread.Sleep(100);
                }

                string resultado = CalcularGanador(JugadaJ1, JugadaJ2);
                byte[] msgRes = Encoding.UTF8.GetBytes($"Resultado|{resultado}\n");

                // Intentar enviar a ambos jugadores (si están conectados)
                try
                {
                    // enviar al jugador actual
                    stream.Write(msgRes, 0, msgRes.Length);
                }
                catch { }

                try
                {
                    // enviar al otro jugador si existe
                    TcpClient otro = (numeroJugador == 1) ? Jugador2 : Jugador1;
                    if (otro != null && otro.Connected)
                    {
                        NetworkStream sOtro = otro.GetStream();
                        sOtro.Write(msgRes, 0, msgRes.Length);
                    }
                }
                catch { }

            }

            Thread.Sleep(3000);

            try
            {
                stream.Close();
            }
            catch { }


            Console.WriteLine($"Jugador {numeroJugador} desconectado.");

            if (numeroJugador == 1)
            {
                if (Jugador1 != null) Jugador1.Close();
            }
            else
            {
                if (Jugador2 != null) Jugador2.Close();
            }
        }

        private static string CalcularGanador(string j1, string j2)
        {
            j1 = j1.ToUpper();
            j2 = j2.ToUpper();

            if (j1 == j2)
            {
                return "Empate";
            }
            if ((j1 == "PIEDRA" && j2 == "TIJERA") ||
                (j1 == "PAPEL" && j2 == "PIEDRA") ||
                (j1 == "TIJERA" && j2 == "PAPEL"))
            {
                return $"GANADOR: {NombreJ1}";
            }
            else
            {
                return $"GANADOR: {NombreJ2}";
            }
        }
    }
}