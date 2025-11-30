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

namespace PPT_Juego_Servidor
{
    internal class Program
    {
        static TcpClient Jugador1;
        static TcpClient Jugador2;
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

                    // ¿Ya tenemos dos líneas? → Procesar mensaje completo
                    if (lineasPendientes.Count == 2)
                    {
                        string comando = lineasPendientes[0];
                        string argumentos = lineasPendientes[1];

                        switch (comando)
                        {
                            case "IniciarSesion":
                            {
                                string[] datosUsuario = BDconexion.obtenerDatos(
                                        $@"SELECT [NombreJugador]
                                                  ,[Contrasenia]
                                              FROM [PiedraPapelTijera1DB].[dbo].[Jugadores]"
                                );
                                if (datosUsuario.Length < 1)
                                {
                                    string respuesta = datosUsuario[0];
                                    byte[] data = Encoding.UTF8.GetBytes(respuesta);
                                    stream.Write(data, 0, data.Length);
                                    break;
                                }
                                else
                                {
                                    string respuesta = string.Join("|", datosUsuario) + "\n";
                                    byte[] data = Encoding.UTF8.GetBytes(respuesta);
                                    stream.Write(data, 0, data.Length);
                                    break;
                                }
                            }
                        }

                        lineasPendientes.Clear();
                    }
                }
            }
        }

    }
}
