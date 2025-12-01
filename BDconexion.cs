using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PPT_Juego_Servidor
{
    internal class BDconexion
    {
        // Conexion Jimmy
        public static SqlConnection conn = new SqlConnection("Server=MSI\\MSSQLSERVER02;Database=_NombreBaseDeDatos_;Integrated Security=True;TrustServerCertificate=True;");
       //Conexion Mari
        //public static SqlConnection conn = new SqlConnection( "Server=DESKTOP-RO62CP8\\MARILUBERSK;Database=PiedraPapelTijera1DB;Integrated Security= True;TrustServerCertificate=True;");
        
        public static int ejecutarConsulta(string consulta)
        {
            // Inicializamos la conexion
            int filasAfectadas = 0;

            try
            {
                // Abrimos la conexion
                conn.Open();

                // Inicializamos la conexion con la consulta en un comando
                SqlCommand comando = new SqlCommand(consulta, conn);

                // Definimos el comando como texto
                comando.CommandType = CommandType.Text;

                // Ejecutamos el comando y obtenemos el numero de filas afectadas
                filasAfectadas = comando.ExecuteNonQuery();

            }
            catch (SqlException e)
            {
                // Error a mostrar en caso de fallar
                MessageBox.Show("Error: " + e.Message);
            }
            finally
            {
                // En caso de que la conexion no este vacia, finalizamos y cerramos la conexion
                if (conn != null)
                {
                    conn.Close();
                }
            }

            return filasAfectadas;
        }

        public static string[] obtenerDatos(string consulta)
        {
            // Valor a devolver
            string[] strReturn;

            try
            {
                conn.Open();

                SqlCommand comando = new SqlCommand(consulta, conn);

                SqlDataReader reader = comando.ExecuteReader();

                reader.Read();

                strReturn = new string[reader.FieldCount];

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    strReturn[i] = reader.GetValue(i).ToString();
                }

                reader.Close();

            }
            catch (Exception e)
            {
                strReturn = new string[1];
                strReturn[0] = "Error: " + e.Message;
            }
            finally
            {
                conn?.Close();
            }

            return strReturn;
        }

    }
}
