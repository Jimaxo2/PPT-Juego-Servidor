using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPT_Juego_Cliente.Models
{
    internal class Jugador
    {
        // Propiedades del jugador para su información y sus estadísticas
        public int IDJugador { get; set; }
        public string NombreJugador { get; set; }
        public int TotalPartidas { get; set; }
        public int PartidasGanadas { get; set; }
        public int PartidasEmpatadas { get; set; }
        public int PartidasPerdidas { get; set; }
        public int TasadeVictoria { get; set; }
    }
}
