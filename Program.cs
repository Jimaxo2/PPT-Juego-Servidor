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
        // Variables estáticas compartidas entre todos los hilos (Jugadores)
        static TcpClient Jugador1;
        static TcpClient Jugador2;
        static string NombreJ1 = null;
        static string NombreJ2 = null;
        static string JugadaJ1 = null;
        static string JugadaJ2 = null;
        static bool JuegoEnCurso = true;

        static void Main(string[] args)
        {
            // Inicia el servidor en el puerto 5000
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Servidor iniciado en puerto 5000...");

            while (true) // Bucle infinito para permitir múltiples partidas
            {
                Console.WriteLine("\n--- Iniciando nueva sala de espera ---");

                // Reinicia variables
                NombreJ1 = null;
                NombreJ2 = null;
                JugadaJ1 = null;
                JugadaJ2 = null;

                // Espera conexión del Jugador 1
                Jugador1 = server.AcceptTcpClient();
                Console.WriteLine("Jugador 1 conectado.");

                // Crea un hilo independiente para J1
                Thread AccionesJugador1 = new Thread(() => AtenderJugador(Jugador1, 1));
                AccionesJugador1.Start();

                // Espera conexión del Jugador 2
                Jugador2 = server.AcceptTcpClient();
                Console.WriteLine("Jugador 2 conectado.");

                // Crea un hilo independiente para J2
                Thread AccionesJugador2 = new Thread(() => AtenderJugador(Jugador2, 2));
                AccionesJugador2.Start();

                Console.WriteLine("Ambos jugadores conectados. Iniciando el juego...");

                // Mantiene el Main en espera mientras dure la partida
                while (AccionesJugador1.IsAlive || AccionesJugador2.IsAlive)
                {
                    Thread.Sleep(1000);
                }

                Console.WriteLine("Partida terminada. Reiniciando servidor para nuevos clientes...");
            }
        }

        private static void AtenderJugador(TcpClient jugador, int numeroJugador)
        {
            NetworkStream stream = null;
            try
            {
                stream = jugador.GetStream(); // Obtiene flujo de datos
            }
            catch
            {
                return; // Error al conectar
            }

            byte[] buffer = new byte[1024];
            StringBuilder acumulador = new StringBuilder();
            List<string> lineasPendientes = new List<string>();

            while (true) // Bucle de escucha de mensajes
            {
                int bytes = 0;

                try
                {
                    if (!stream.CanRead) break;

                    // Lee datos entrantes
                    bytes = stream.Read(buffer, 0, buffer.Length);

                    if (bytes == 0) // Cliente desconectado
                    {
                        Console.WriteLine($"Jugador {numeroJugador} cerró la conexión.");
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Conexión con Jugador {numeroJugador} perdida.");
                    break;
                }

                // Decodifica y acumula fragmentos de texto
                string recibido = Encoding.UTF8.GetString(buffer, 0, bytes);
                acumulador.Append(recibido);

                string contenido = acumulador.ToString();
                int indice;

                // Procesa mensajes completos (separados por salto de línea)
                while ((indice = contenido.IndexOf('\n')) >= 0)
                {
                    string linea = contenido.Substring(0, indice).TrimEnd('\r');
                    contenido = contenido.Substring(indice + 1);
                    acumulador.Clear();
                    acumulador.Append(contenido);

                    lineasPendientes.Add(linea);

                    // Si tiene comando + argumentos, ejecuta acción
                    if (lineasPendientes.Count == 2)
                    {
                        string comando = lineasPendientes[0];
                        string argumentos = lineasPendientes[1];

                        try
                        {
                            switch (comando)
                            {
                                case "IniciarSesion":
                                    {
                                        string[] partes = argumentos.Split('|');

                                        if (partes.Length < 2)
                                        {
                                            // Error de formato
                                            byte[] dataErr = Encoding.UTF8.GetBytes("Error|Formato incorrecto\n");
                                            stream.Write(dataErr, 0, dataErr.Length);
                                            break;
                                        }

                                        string usuario = partes[0];
                                        string contrasena = partes[1];

                                        // Valida credenciales en BD
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

                                        // Usuario no encontrado
                                        if (datosUsuario.Length == 1 && datosUsuario[0].StartsWith("Error"))
                                        {
                                            byte[] dataError = Encoding.UTF8.GetBytes("Error|Credenciales incorrectas\n");
                                            stream.Write(dataError, 0, dataError.Length);
                                            break;
                                        }

                                        // Usuario encontrado: envía datos
                                        string respuestaOK = string.Join("|", datosUsuario) + "\n";
                                        byte[] dataOK = Encoding.UTF8.GetBytes(respuestaOK);
                                        stream.Write(dataOK, 0, dataOK.Length);

                                        // Asigna nombre global
                                        if (numeroJugador == 1)
                                            NombreJ1 = datosUsuario[1].Trim();
                                        else
                                            NombreJ2 = datosUsuario[1].Trim();

                                        // Inicia lógica del juego
                                        IniciarCicloDeJuego(stream, numeroJugador);
                                        return; // Sale del hilo al terminar
                                    }

                                case "CrearCuenta":
                                    {
                                        string[] partes = argumentos.Split('|');
                                        if (partes.Length < 2) { /* Error formato */ break; }

                                        string usuario = partes[0];
                                        string contrasena = partes[1];

                                        // Inserta nuevo usuario en BD
                                        int filasAfectadas = BDconexion.ejecutarConsulta(
                                            $@"INSERT INTO [dbo].[Jugadores] ([NombreJugador],[Contrasenia])
                                              VALUES ('{usuario}','{contrasena}')"
                                            );

                                        if (filasAfectadas == 0)
                                        {
                                            byte[] dataError = Encoding.UTF8.GetBytes("Error|Credenciales incorrectas\n");
                                            stream.Write(dataError, 0, dataError.Length);
                                            break;
                                        }

                                        // Éxito al crear
                                        byte[] dataOK = Encoding.UTF8.GetBytes($"{usuario}\n");
                                        stream.Write(dataOK, 0, dataOK.Length);
                                        break;
                                    }
                            }
                        }
                        catch
                        {
                            break;
                        }

                        lineasPendientes.Clear();
                    }
                }
            }

            // Cierra recursos
            try { stream?.Close(); } catch { }
            try { jugador?.Close(); } catch { }
        }

        private static void IniciarCicloDeJuego(NetworkStream stream, int numeroJugador)
        {
            Console.WriteLine($"Jugador {numeroJugador} listo para jugar");

            // Espera a que ambos jugadores estén logueados
            while (NombreJ1 == null || NombreJ2 == null)
            {
                Thread.Sleep(500);
            }

            // Notifica inicio
            byte[] msgInicio = Encoding.UTF8.GetBytes("Mensaje|¡Comienza el juego!\n");
            stream.Write(msgInicio, 0, msgInicio.Length);

            // Reinicia jugadas
            if (numeroJugador == 1) JugadaJ1 = null;
            else JugadaJ2 = null;

            // Solicita jugada
            byte[] msgPedir = Encoding.UTF8.GetBytes("PedirJugada|Elige\n");
            try { stream.Write(msgPedir, 0, msgPedir.Length); } catch { return; }

            // Lee respuesta del jugador
            byte[] buffer = new byte[1024];
            int bytes = 0;
            try { bytes = stream.Read(buffer, 0, buffer.Length); } catch { }

            if (bytes > 0)
            {
                string respuesta = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                if (numeroJugador == 1) JugadaJ1 = respuesta;
                else JugadaJ2 = respuesta;

                Console.WriteLine($"Jugador {numeroJugador} tiró: {respuesta}");

                // Espera a que el oponente juegue
                while (JugadaJ1 == null || JugadaJ2 == null)
                {
                    Thread.Sleep(100);
                }

                // Calcula y envía resultado
                string resultado = CalcularGanador(JugadaJ1, JugadaJ2);
                byte[] msgRes = Encoding.UTF8.GetBytes($"Resultado|{resultado}\n");

                try { stream.Write(msgRes, 0, msgRes.Length); } catch { }

                // Intenta enviar resultado también al otro jugador
                try
                {
                    TcpClient otro = (numeroJugador == 1) ? Jugador2 : Jugador1;
                    if (otro != null && otro.Connected)
                    {
                        NetworkStream sOtro = otro.GetStream();
                        sOtro.Write(msgRes, 0, msgRes.Length);
                    }
                }
                catch { }
            }

            Thread.Sleep(3000); // Pausa final
            try { stream.Close(); } catch { } // Desconecta

            Console.WriteLine($"Jugador {numeroJugador} desconectado.");

            if (numeroJugador == 1) { if (Jugador1 != null) Jugador1.Close(); }
            else { if (Jugador2 != null) Jugador2.Close(); }
        }

        private static string CalcularGanador(string j1, string j2)
        {
            j1 = j1.ToUpper();
            j2 = j2.ToUpper();

            if (j1 == j2) return "Empate";

            // Lógica Piedra-Papel-Tijera
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