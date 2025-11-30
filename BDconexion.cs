using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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

    }
}
