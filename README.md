##  Colaboradores
<a href="https://github.com/anmaribaphomet"> @anmaribaphomet</a><br>
<a href="https://github.com/Jimaxo2"> @Jimaxo2</a><br>
<a href="https://github.com/IsHectron"> @IsHectron</a><br>
<a href="https://github.com/ahectpaul"> @ahectpaul</a><br>
<a href="https://github.com/SantaCruzVelarde"> @SantaCruzVelarde</a><br>

Proyecto desarrollado como una aplicación de escritorio cliente para el juego Piedra, Papel o Tijera en la materia  Desarrollo de Sistemas 3

# Servidor de videojuego

Para correr el servidor debe existir una base de datos hecha en SQL Server que almacene a los usuarios y registre las partidas, con ello puede ejecutarse y esperar a los clientes que se conecten por conexión tcp

<a href="https://github.com/Jimaxo2/PPT-Juego-Cliente" target="_blank">Link al cliente del juego</a>

#  PPT-Juego — Servidor

Componente servidor del sistema **PPT-Juego**, una aplicación multijugador de Piedra, Papel o Tijera desarrollada en C#. Su responsabilidad es administrar las conexiones de los clientes, validar las cuentas de usuario mediante una base de datos y coordinar la lógica de las partidas.

##  Responsabilidades del servidor

* **Gestión de conexiones TCP:** escucha solicitudes entrantes en el puerto `5000` y acepta las conexiones de los clientes.
* **Administración de jugadores:** mantiene las conexiones y los nombres de los dos participantes de cada partida.
* **Autenticación:** consulta la base de datos para validar las credenciales de los usuarios.
* **Registro de cuentas:** permite recibir solicitudes para crear nuevas cuentas de usuario.
* **Coordinación de partidas:** espera a que ambos jugadores estén conectados y listos para participar.
* **Recepción de jugadas:** obtiene la elección realizada por cada jugador.
* **Determinación del resultado:** compara las jugadas y establece si existe un ganador o un empate.
* **Distribución de resultados:** envía a ambos clientes las jugadas realizadas y el resultado de la partida.
* **Administración de sesiones:** cierra las conexiones al finalizar el enfrentamiento y vuelve a esperar nuevos clientes.

##  Tecnologías utilizadas

| Tecnología          | Función                                                            |
| ------------------- | ------------------------------------------------------------------ |
| C#                  | Implementación de la lógica del servidor.                          |
| .NET                | Entorno de ejecución de la aplicación.                             |
| TCP / `TcpListener` | Recepción y administración de conexiones entrantes.                |
| `TcpClient`         | Representación de las conexiones con los jugadores.                |
| `NetworkStream`     | Intercambio de mensajes entre el servidor y los clientes.          |
| `System.Threading`  | Ejecución de la atención de los jugadores en hilos independientes. |
| SQL Server          | Gestión de las cuentas almacenadas en la base de datos.            |
| ADO.NET             | Acceso a los datos mediante la clase de conexión del proyecto.     |

##  Estructura del proyecto

```text
PPT-Juego-Servidor/
├── Properties/
│   └── AssemblyInfo.cs
├── Models/
│   └── Jugador.cs
├── App.config
├── BDconexion.cs
├── packages.config
└── Program.cs

### Componentes principales

* **`Program.cs`:** punto de entrada y núcleo de la lógica del servidor. Inicia el servicio TCP, acepta conexiones, atiende a los jugadores, coordina las partidas y calcula los resultados.
* **`BDconexion.cs`:** componente destinado a las operaciones de conexión y consulta con la base de datos. En el código de `Program.cs` se utiliza para consultar credenciales y registrar cuentas.
* **`Models/Jugador.cs`:** archivo destinado al modelo de datos del jugador.
* **`App.config`:** archivo de configuración de la aplicación. La configuración concreta debe verificarse en el archivo del proyecto.
* **`packages.config`:** archivo de referencia para la administración de paquetes NuGet del proyecto.
* **`Properties/AssemblyInfo.cs`:** contiene información de ensamblado de la aplicación.

## Funcionamiento del servidor

El flujo general de ejecución es el siguiente:

1. **Inicio:** el servidor comienza a escuchar conexiones TCP en el puerto `5000`.
2. **Recepción de jugadores:** acepta las conexiones de dos clientes y crea un hilo de atención para cada uno.
3. **Autenticación:** recibe las credenciales y consulta la base de datos para comprobar si el usuario existe.
4. **Preparación de la partida:** espera a que ambos jugadores hayan iniciado sesión correctamente.
5. **Solicitud de jugadas:** envía a los clientes el comando que solicita una elección.
6. **Procesamiento:** recibe las jugadas y espera a que ambos participantes hayan realizado su elección.
7. **Envío de resultados:** comunica las elecciones de los jugadores y posteriormente el resultado final.
8. **Cierre y reinicio:** cierra las conexiones de la partida y vuelve al ciclo de espera para atender nuevos participantes.

##  Lógica de Piedra, Papel o Tijera

El método `CalcularGanador()` compara las jugadas de ambos participantes:

| Jugada | Gana contra |
| ------ | ----------- |
| Piedra | Tijera      |
| Papel  | Piedra      |
| Tijera | Papel       |

Si ambos jugadores eligen la misma opción, el resultado es un empate. En caso contrario, el servidor identifica al ganador y envía el resultado a los dos clientes.

##  Protocolo de comunicación

El servidor utiliza mensajes de texto codificados en UTF-8 y delimitados mediante saltos de línea. Para determinadas solicitudes, el protocolo separa el comando de sus argumentos en líneas consecutivas.

Entre los mensajes utilizados se encuentran:

| Mensaje             | Propósito                                                                    |
| ------------------- | ---------------------------------------------------------------------------- |
| `IniciarSesion`     | Solicita la validación de las credenciales.                                  |
| `CrearCuenta`       | Solicita el registro de una cuenta.                                          |
| `PedirJugada`       | Solicita una elección al jugador.                                            |
| `ResultadoCompleto` | Comunica los nombres y las jugadas de ambos participantes.                   |
| `Ganador`           | Comunica si la partida terminó en victoria o empate.                         |
| `Mensaje`           | Transmite información general sobre el estado de la partida.                 |
| `Error`             | Identifica respuestas relacionadas con errores de solicitud o autenticación. |

El cliente y el servidor deben mantener el mismo formato de mensajes para que el intercambio de información funcione correctamente.

##  Base de datos

El servidor utiliza una base de datos de SQL Server identificada en las consultas del código como `PiedraPapelTijera1DB`, dentro del esquema `dbo`.

La tabla principal identificada es `Jugadores`, de la cual se consultan campos como:

* `JugadorID`
* `NombreJugador`
* `Contrasenia`
* `TotalPartidas`
* `PartidasGanadas`
* `PartidasEmpatadas`
* `PartidasPerdidas`
* `TasaVictoria`

El código también contempla la inserción de nuevos usuarios en esta tabla.

**Nota:** para ejecutar el servidor correctamente, la base de datos debe existir y contar con la estructura esperada por las consultas. La configuración de conexión debe revisarse en `BDconexion.cs` y, si corresponde, en `App.config`.

### Requisitos previos

* Windows.
* Visual Studio con soporte para C#.
* Una versión de .NET compatible con el proyecto.
* SQL Server instalado y accesible.
* La base de datos `PiedraPapelTijera1DB` con la tabla `dbo.Jugadores`.
* El proyecto cliente PPT-Juego para participar en las partidas.

### Pasos

1. Clona el repositorio:

   ```bash
   git clone <URL_DEL_REPOSITORIO>
   ```

2. Abre el proyecto del servidor en Visual Studio.

3. Configura la conexión a SQL Server según los parámetros requeridos por `BDconexion.cs`.

4. Verifica que la base de datos y la tabla de jugadores estén disponibles.

5. Compila y ejecuta el proyecto.

6. Comprueba que la consola muestre el mensaje de inicio del servidor.

7. Inicia los clientes y verifica que puedan conectarse a la dirección y el puerto configurados.

### Configuración de red

El servidor utiliza la siguiente configuración en `Program.cs`:

```csharp
TcpListener server = new TcpListener(IPAddress.Any, 5000);
server.Start();
```

`IPAddress.Any` permite escuchar conexiones dirigidas a las interfaces de red locales del equipo. Los clientes deben conectarse a la dirección IP correspondiente del servidor utilizando el puerto `5000`.

Si los clientes se ejecutan en otros equipos, verifica que la red y el firewall permitan las conexiones entrantes a ese puerto.

##  Consideraciones

* La implementación actual coordina las partidas en grupos de dos jugadores.
* El servidor debe estar activo antes de iniciar una conexión desde el cliente.
* El cliente y el servidor deben utilizar un protocolo de comunicación compatible.
* La disponibilidad de la base de datos es necesaria para autenticar usuarios y registrar cuentas.
* Las operaciones con la base de datos deben protegerse mediante consultas parametrizadas para evitar inyecciones SQL. La implementación actual de las consultas mostradas en `Program.cs` utiliza interpolación de cadenas y debería reforzarse antes de utilizarse en un entorno real.

##  Estado del proyecto

El servidor implementa las funciones esenciales para conectar a dos jugadores, validar cuentas, coordinar sus elecciones y comunicar el resultado de una partida de Piedra, Papel o Tijera.

Su ejecución forma parte de un sistema cliente-servidor y requiere que el cliente, la base de datos y la configuración de red sean compatibles.
