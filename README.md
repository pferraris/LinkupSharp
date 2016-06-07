# LinkupSharp #

LinkupSharp es una plataforma de comunicación bidireccional de aplicaciones, diseñada para ser cómodamente extensible y flexible.

Agregalo a tu proyecto directamente desde [NuGet](https://www.nuget.org/packages/LinkupSharp).

Se está trabajando en una implementación cliente para JavaScript compatible con CommonJS, para conectar páginas web y servicios Node.JS: [LinkupJS](https://github.com/pferraris/LinkupJS)

## ConnectionManager ##

Utilizado para crear un objeto servidor. Se encarga de esperar conexiones pudiendo configurar distintos canales, gestionar las conexiones de clientes, y gestionar el intercambio de paquetes entre ellos. Además, soporta la utilización de módulos que son componentes a los cuales se los puede suscribir a determinados tipos de paquetes para manejar una determinada funcionalidad.

### Canales de comunicación ###

Existen dos interfaces que es necesario implementar para definir un canal:
- `IChannelListener`: Utilizada para esperar conexiones entrantes y lanzar el evento correspondiente al ConnectionManager.
- `IClientChannel`: Utilizada para la manipulación de cada uno de los extremos de la conexión.

El ConnectionManager posee una lista de `IChannelListener`, la cual se puede consultar desde la propiedad `Listeners`.
También posee métodos para agregar y remover IChannelListener: `AddListener` y `RemoveListener` respectivamente.

La implementaciones de IChannelListener incluidas en el core son:
- `TcpChannelListener` para comunicaciones TCP estándar o SSL.
- `WebChannelListener` que simula bidireccionalidad a través de requests HTTP/HTTPS (GET y POST).
- `WebSocketChannelListener` para comunicaciones mediante WebSockets, soportando protocolo ws y wss.

El objetivo de IChannelListener es la de reportar al ConnectionManager cada vez que un cliente se conecta a través de un evento, entregando un IClientChannel.

Cada una de las implementaciones de IChannelListener está acompañada de una implementación de IClientChannel que representa cada uno de los extremos conectados.
Por lo tanto, en el core contamos con las siguientes implementaciones:

- `TcpClientChannel`
- `WebClientChannel`
- `WebSocketClientChannel` / `WebSocketServerChannel`

### Clientes conectados ###

El ConnectionManager encapsula cada IClientChannel recibido en una ClientConnection que representa al cliente.
Las ClientConnection podrán autentificarse, realizando un SignIn, esto creará una nueva Session asignándoles un Token.
Al autenticarse, la ClientConnection será incluida en la lista de clientes:

- `ConnectionManager.Clients`

Si el cliente realiza un SignOut se eliminará la Session creada con anterioridad. Mientras tanto podrá utilizar el Token generado para restaurar la sesión en futuras conexiones mediante RestoreSession.


### Autenticadores de clientes ###
Se pueden configurar varios autenticadores los cuales intentarán autentificar a los clientes en el órden en que se agtreguen. Los métodos para agregar y quitar los mismos son:

- `ConnectionManager.AddAuthenticator(IAuthenticator)`
- `ConnectionManager.RemoveAuthenticator(IAuthenticator)`

Asimismo se puede obtener la lista mediante la propiedad:

- `ConnectionManager.Authenticators`

Cuando un ClientConnection envía su información de autenticación (`SignIn`), el mismo pasa por todos los autenticadores hasta que alguno lo considera válido, crea una nueva `Session` asignándole un Token, y le informa al ConnectionManager que es un cliente válido.
En ese momento el ClientConnection lo comienza a considerar como un cliente conectado para los broadcast.

La implementación que viene por defecto es `AnonymousAuthentication` que solo valida que `SignIn` no sea nulo, pero no valida passwords.
`SignIn` es una implementación base para cualquier extensión que se quiera realizar, que solo posee la propiedad Id para tener la identificación del cliente.
Finalmente la implementación base `Session` solo contiene el Id del cliente y su Token, pero puede ser extendida para contener más datos relacionados con la sesión del cliente.
