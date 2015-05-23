# LinkupSharp #

LinkupSharp es una librería extensible y flexible, para intercomunicar aplicaciones de manera bidireccional.

Actualmente no está implementado, pero se planea tener librerías clientes para Java, para poder conectar aplicaciones Android, y JavaScript para poder conectar páginas web.

## ConnectionManager ##
Es el núcleo de la librería se encarga de esperar conexiones a través de distintos canales, gestionar las conexiones cliente, y gestionar el intercambio de paquetes entre ellos.

### Canales de comunicación ###
Se pueden configurar distintos canales en los cuales esperar conexiones entrantes a través de los métodos:

- `ConnectionManager.AddListener(IChannelListener)`
- `ConnectionManager.RemoveListener(IChannelListener)`

Los distintos IChannelListeners configurados se pueden visualizar a través de la colección:

- `ConnectionManager.Listeners`

La implementaciones de IChannelListener incluidas en el core son:

- `TcpChannelListener` para comunicaciones TCP estándar.
- `SslChannelListener` para comunicaciones TCP/SSL mediante un certificado.
- `WebChannelListener` que simula bidireccionalidad a través de HTTP requests GET y POST.

Además se está trabajando en una implementación `WebSocketChannelListener` y se planea realizar una implementación que funcione a través de POP3/SMTP `EmailChannelListener`

Cada una de las implementaciones de IChannelListener está acompañada de una implementación de IClientChannel que representa cada uno de los extremos conectados.
El objetivo de IChannelListener es la de reportar al ConnectionManager cada vez que un cliente se conecta a través de un evento, entregando un IClientChannel.
Así tenemos las implementaciones:

- `TcpClientChannel`
- `SslClientChannel`
- `WebClientChannel`

### Clientes conectados ###

El ConnectionManager encapsula cada IClientChannel recibido en una ClientConnection que representa la sesión cliente.
Lo primero que debe ocurrir es que la ClientConnection se autentifique para ser incluida en la colección de clientes:

- `ConnectionManager.Clients`

Hasta que esto ocurra, se mantendra en un estado pendiente de autentificación por un tiempo determinado (`ConnectionManager.AuthenticationTimeOut`) y no podrá enviar ni recibir paquetes.

Cuando un cliente se desconecta, el mismo se mantiene almacenado por un tiempo determinado (`ConnectionManager.InactivityTimeOut`), y es considerado como inactivo. Si el mismo se reconecta antes de ese período de tiempo, se restaurará la ClientConnection y volverá a un estado activo, en caso contrario se desechará y figurará como desconectado.

### Autenticadores de clientes ###
Se pueden configurar varios autenticadores de clientes formando una cadena de responsabilidad, mediante los métodos:

- `ConnectionManager.AddAuthenticator(IAuthenticator)`
- `ConnectionManager.RemoveAuthenticator(IAuthenticator)`

Asimismo se pueden visualizar los mismos en la colección:

- `ConnectionManager.Authenticators`

Cuando un ClientConnection envía su información de autenticación (`Credentials`), el mismo pasa por todos los autenticadores hasta que alguno lo considera válido, le setea un `AuthenticationContext`, y le informa al ConnectionManager que es un cliente válido.
En ese momento el ClientConnection lo comienza a considerar como un cliente conectado para la recepción y envío de paquetes.

La implementación que viene por defecto es `AnonymousAuthentication` que solo valida que `Credentials` no sea nulo, pero no valida passwords.
`Credentials` es una implementación base para cualquier extensión que se quiera realizar, que solo posee la propiedad Id para tener la identificación del cliente.
Finalmente la implementación base `AuthenticationContext` solo contiene el Id del cliente.

