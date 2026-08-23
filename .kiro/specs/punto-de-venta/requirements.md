# Requirements Document

## Introduction

Sistema de Punto de Venta (POS) desarrollado como aplicación web utilizando ASP.NET Core con Blazor, base de datos SQL Server y Entity Framework con enfoque Code First. El sistema permite la gestión de productos, ventas, usuarios y roles, incluyendo la generación de reportes exportables. La arquitectura sigue el principio de separación de responsabilidades mediante capas bien definidas.

## Glossary

- **Sistema_POS**: Aplicación web de Punto de Venta construida con Blazor y ASP.NET Core
- **Módulo_Autenticación**: Componente responsable de la validación de credenciales y gestión de sesiones
- **Módulo_Usuarios**: Componente responsable de las operaciones CRUD sobre la entidad Usuario
- **Módulo_Productos**: Componente responsable de las operaciones CRUD sobre la entidad Producto
- **Módulo_Ventas**: Componente responsable del registro y gestión de transacciones de venta
- **Módulo_Reportes**: Componente responsable de la generación y exportación de reportes
- **Administrador**: Rol con acceso completo a todas las funcionalidades del sistema
- **Vendedor**: Rol con acceso limitado a funciones operativas (ventas, consultas)
- **Usuario**: Entidad que representa una persona registrada en el sistema con credenciales de acceso
- **Producto**: Entidad que representa un artículo disponible para la venta
- **Categoría**: Entidad que clasifica los productos en grupos lógicos
- **Venta**: Entidad que representa una transacción comercial compuesta por uno o más detalles de venta
- **Detalle_Venta**: Entidad que representa una línea individual dentro de una venta (producto, cantidad, precio)
- **Módulo_Auditoría**: Componente responsable del registro automático y consulta del historial de operaciones realizadas en el sistema
- **Registro_Auditoría**: Entrada individual en el historial que documenta una operación específica realizada por un usuario
- **Modo_Offline**: Estado del sistema cuando no puede establecer comunicación con el servidor, operando con datos almacenados localmente en el navegador
- **AFIP_ARCA**: Administración Federal de Ingresos Públicos / Agencia de Recaudación y Control Aduanero, organismo fiscal argentino que autoriza comprobantes electrónicos
- **CAE**: Código de Autorización Electrónico, código numérico emitido por AFIP que valida un comprobante fiscal electrónico
- **Módulo_Facturación**: Componente responsable de la integración con AFIP/ARCA para la emisión y gestión de comprobantes electrónicos mediante Afip SDK
- **Módulo_Caja**: Componente responsable de la gestión de apertura, movimientos y cierre de caja registradora
- **Cierre_Caja**: Operación que finaliza el período operativo de una caja, calculando la diferencia entre el saldo esperado y el efectivo real contado
- **Medio_Pago**: Método configurable mediante el cual un cliente puede abonar una venta (efectivo, tarjeta, transferencia, etc.)
- **Pago_Mixto**: Modalidad de pago en la que una venta se abona utilizando dos o más medios de pago diferentes

## Requirements

### Requirement 1: Arquitectura y Estructura del Sistema

**User Story:** Como desarrollador, quiero que el sistema esté construido con arquitectura en capas y buenas prácticas, para que sea mantenible, escalable y cumpla con estándares de calidad de código.

#### Acceptance Criteria

1. THE Sistema_POS SHALL separar la lógica en al menos tres capas: presentación (Blazor), lógica de negocio (servicios) y acceso a datos (Entity Framework), donde cada capa reside en un proyecto o namespace independiente y las dependencias entre capas fluyen unidireccionalmente desde presentación hacia lógica de negocio y desde lógica de negocio hacia acceso a datos, sin referencias inversas
2. IF una excepción no controlada ocurre durante cualquier operación, THEN THE Sistema_POS SHALL interceptar la excepción, mostrar al usuario un mensaje que indique la acción que falló sin exponer stack traces, cadenas de conexión ni sentencias SQL, y preservar el estado de navegación actual del usuario
3. THE Sistema_POS SHALL utilizar inyección de dependencias para todos los servicios y repositorios entre capas, de modo que ninguna clase de una capa instancie directamente clases concretas de otra capa
4. THE Sistema_POS SHALL implementar el modelo de datos utilizando Entity Framework con enfoque Code First y migraciones, de modo que la base de datos pueda recrearse completamente a partir de las migraciones definidas en el código fuente

---

### Requirement 2: Modelo de Datos y Base de Datos

**User Story:** Como desarrollador, quiero un modelo de datos normalizado y bien definido, para que la información se almacene de forma íntegra y sin redundancias innecesarias.

#### Acceptance Criteria

1. THE Sistema_POS SHALL almacenar los datos en una base de datos relacional SQL Server utilizando Entity Framework Code First como estrategia de creación y migración del esquema
2. THE Sistema_POS SHALL definir claves primarias autogeneradas en todas las tablas y claves foráneas entre las siguientes relaciones: Venta referencia a Usuario, Detalle_Venta referencia a Venta, Detalle_Venta referencia a Producto, y Producto referencia a Categoría
3. THE Sistema_POS SHALL normalizar el esquema de datos hasta la tercera forma normal (3FN), asegurando que ningún atributo no clave dependa transitivamente de la clave primaria
4. THE Sistema_POS SHALL aplicar restricciones de integridad referencial con comportamiento RESTRICT ante eliminación en las relaciones Venta-Usuario y Detalle_Venta-Producto, y CASCADE ante eliminación en la relación Venta-Detalle_Venta
5. THE Sistema_POS SHALL aplicar las siguientes validaciones a nivel de entidad utilizando Data Annotations o Fluent API de Entity Framework: campos obligatorios marcados como NOT NULL (nombres, precios, cantidades, fechas), longitud máxima de 100 caracteres para campos de texto como nombre de producto y nombre de categoría, longitud máxima de 50 caracteres para nombre de usuario, y valores numéricos de precio y cantidad restringidos a ser mayores que cero
6. IF se intenta insertar o actualizar un registro que viola una restricción de integridad referencial o una validación de datos, THEN THE Sistema_POS SHALL rechazar la operación y retornar un mensaje de error indicando el campo y la restricción violada

---

### Requirement 3: Gestión de Usuarios (CRUD)

**User Story:** Como Administrador, quiero crear, consultar, modificar y eliminar usuarios del sistema, para mantener actualizado el registro de personas con acceso.

#### Acceptance Criteria

1. WHEN el Administrador solicita crear un usuario proporcionando nombre (entre 1 y 100 caracteres), correo electrónico válido (máximo 254 caracteres), contraseña (entre 8 y 50 caracteres) y rol (Administrador o Vendedor), THE Módulo_Usuarios SHALL registrar el usuario en la base de datos con estado activo y la contraseña almacenada de forma cifrada
2. WHEN el Administrador solicita listar usuarios, THE Módulo_Usuarios SHALL mostrar la lista paginada (máximo 20 registros por página) de usuarios activos con su nombre, correo electrónico y rol asignado, ordenados alfabéticamente por nombre
3. WHEN el Administrador solicita modificar un usuario activo existente, THE Módulo_Usuarios SHALL permitir actualizar el nombre, correo electrónico y rol del usuario en la base de datos
4. WHEN el Administrador solicita eliminar un usuario activo, THE Módulo_Usuarios SHALL realizar una eliminación lógica marcando el usuario como inactivo
5. WHEN el Administrador ingresa un término de búsqueda de al menos 1 carácter, THE Módulo_Usuarios SHALL filtrar y mostrar los usuarios activos cuyo nombre o correo electrónico contengan parcialmente el término ingresado
6. IF se intenta crear un usuario con un correo electrónico ya registrado en un usuario activo, THEN THE Módulo_Usuarios SHALL rechazar la operación y mostrar un mensaje indicando que el correo ya existe
7. IF el Administrador solicita modificar o eliminar un usuario que no existe o que se encuentra inactivo, THEN THE Módulo_Usuarios SHALL rechazar la operación y mostrar un mensaje indicando que el usuario no fue encontrado
8. IF el Administrador intenta eliminar su propia cuenta de usuario, THEN THE Módulo_Usuarios SHALL rechazar la operación y mostrar un mensaje indicando que no es posible eliminar el usuario con sesión activa
9. IF el Administrador solicita crear o modificar un usuario con datos que no cumplen las reglas de validación (nombre vacío o mayor a 100 caracteres, correo con formato inválido o mayor a 254 caracteres, contraseña menor a 8 o mayor a 50 caracteres), THEN THE Módulo_Usuarios SHALL rechazar la operación y mostrar un mensaje indicando los campos con error

---

### Requirement 4: Autenticación y Login

**User Story:** Como Usuario, quiero iniciar sesión con mis credenciales, para acceder a las funcionalidades del sistema según mi rol asignado.

#### Acceptance Criteria

1. WHEN un Usuario con estado Activo ingresa credenciales válidas (correo electrónico y contraseña), THE Módulo_Autenticación SHALL autenticar al usuario y redirigirlo al panel de Administrador si su rol es Administrador, o al panel de Vendedor si su rol es Vendedor, en un tiempo máximo de 3 segundos
2. IF un Usuario ingresa credenciales inválidas, THEN THE Módulo_Autenticación SHALL denegar el acceso y mostrar un mensaje indicando que las credenciales son incorrectas sin revelar cuál campo específico es erróneo
3. IF un Usuario con estado Inactivo ingresa credenciales válidas, THEN THE Módulo_Autenticación SHALL denegar el acceso y mostrar un mensaje indicando que la cuenta se encuentra deshabilitada
4. IF un Usuario acumula 5 intentos fallidos de inicio de sesión consecutivos, THEN THE Módulo_Autenticación SHALL bloquear temporalmente el acceso a esa cuenta durante 15 minutos y mostrar un mensaje indicando el bloqueo temporal
5. WHEN un Usuario autenticado solicita cerrar sesión, THE Módulo_Autenticación SHALL invalidar la sesión activa y redirigir a la pantalla de login
6. WHILE una sesión de usuario está activa sin interacción durante 30 minutos, THE Módulo_Autenticación SHALL expirar la sesión automáticamente y redirigir al usuario a la pantalla de login
7. THE Módulo_Autenticación SHALL almacenar las contraseñas utilizando un algoritmo de hash seguro (bcrypt o equivalente) sin almacenar texto plano

---

### Requirement 5: Recuperación de Contraseña

**User Story:** Como Usuario, quiero recuperar mi contraseña en caso de olvidarla, para poder volver a acceder al sistema sin intervención manual del administrador.

#### Acceptance Criteria

1. WHEN un Usuario solicita recuperación de contraseña proporcionando su correo electrónico registrado, THE Módulo_Autenticación SHALL generar un token de recuperación de un solo uso con una longitud mínima de 32 caracteres alfanuméricos, asociarlo al usuario correspondiente y registrar la fecha y hora de la solicitud
2. IF un Usuario solicita recuperación de contraseña proporcionando un correo electrónico que no se encuentra registrado en el sistema, THEN THE Módulo_Autenticación SHALL responder con el mismo mensaje de confirmación genérico que cuando el correo sí existe, sin revelar si la cuenta está registrada
3. WHEN un Usuario accede al enlace de recuperación con un token válido y no expirado, THE Módulo_Autenticación SHALL presentar un formulario para establecer una nueva contraseña que cumpla los mismos requisitos de complejidad definidos para el registro (mínimo 8 caracteres, al menos una letra mayúscula, una minúscula y un número)
4. IF un Usuario intenta usar un token de recuperación expirado, ya utilizado o con formato inválido, THEN THE Módulo_Autenticación SHALL rechazar la solicitud y mostrar un mensaje indicando que el enlace ya no es válido, sin especificar el motivo exacto del rechazo
5. WHEN un Usuario establece exitosamente una nueva contraseña mediante el enlace de recuperación, THE Módulo_Autenticación SHALL invalidar inmediatamente el token utilizado impidiendo su reutilización, y permitir el inicio de sesión con la nueva contraseña
6. THE Módulo_Autenticación SHALL expirar los tokens de recuperación tras 24 horas de su generación, rechazando cualquier intento de uso posterior a dicho periodo

---

### Requirement 6: Control de Acceso por Roles

**User Story:** Como Administrador, quiero que el sistema controle el acceso a funcionalidades según el rol del usuario, para garantizar que cada persona solo acceda a lo que le corresponde.

#### Acceptance Criteria

1. THE Sistema_POS SHALL asignar a cada usuario exactamente uno de los siguientes roles al momento de su creación: Administrador o Vendedor, donde únicamente un usuario con rol de Administrador puede asignar o modificar roles
2. WHILE un usuario tiene rol de Administrador, THE Sistema_POS SHALL permitir acceso a todas las funcionalidades del sistema incluyendo gestión de usuarios, gestión de productos, gestión de categorías, registro de ventas y visualización de reportes de todos los usuarios
3. WHILE un usuario tiene rol de Vendedor, THE Sistema_POS SHALL permitir acceso únicamente a las funciones de registro de ventas, consulta de productos (solo lectura), consulta de categorías (solo lectura) y visualización de reportes de ventas registradas por ese mismo usuario
4. IF un usuario intenta acceder a una funcionalidad no autorizada para su rol, THEN THE Sistema_POS SHALL denegar el acceso, mostrar un mensaje indicando permisos insuficientes y mantener al usuario en la última vista autorizada
5. THE Sistema_POS SHALL validar los permisos de rol tanto en la interfaz de usuario (ocultando opciones de menú no disponibles para el rol actual) como en el servidor (rechazando solicitudes no autorizadas y retornando una respuesta de acceso denegado)
6. IF un Administrador intenta cambiar el rol del único usuario con rol de Administrador existente en el sistema, THEN THE Sistema_POS SHALL rechazar la operación y mostrar un mensaje indicando que debe existir al menos un Administrador activo
7. IF un usuario no ha iniciado sesión e intenta acceder a cualquier funcionalidad del sistema, THEN THE Sistema_POS SHALL redirigir al usuario a la pantalla de inicio de sesión sin mostrar ninguna opción de menú del sistema

---

### Requirement 7: Gestión de Categorías

**User Story:** Como Administrador, quiero gestionar las categorías de productos, para organizar el inventario de forma lógica y facilitar las búsquedas.

#### Acceptance Criteria

1. WHEN el Administrador solicita crear una categoría con un nombre de entre 3 y 50 caracteres que no exista previamente en el sistema, THE Módulo_Productos SHALL registrar la categoría con su nombre y descripción (máximo 200 caracteres, opcional) en la base de datos y confirmar la creación al usuario
2. WHEN el Administrador solicita listar categorías, THE Módulo_Productos SHALL mostrar todas las categorías registradas con su nombre y descripción, ordenadas alfabéticamente por nombre
3. WHEN el Administrador solicita modificar el nombre o la descripción de una categoría existente, THE Módulo_Productos SHALL actualizar los datos de la categoría aplicando las mismas reglas de validación que en la creación
4. WHEN el Administrador solicita eliminar una categoría que no tiene productos asociados, THE Módulo_Productos SHALL eliminar la categoría de la base de datos y confirmar la eliminación al usuario
5. IF se intenta eliminar una categoría con productos asociados, THEN THE Módulo_Productos SHALL rechazar la eliminación y mostrar un mensaje indicando la cantidad de productos vinculados a dicha categoría
6. IF el Administrador solicita crear o modificar una categoría con un nombre que ya existe en el sistema, THEN THE Módulo_Productos SHALL rechazar la operación y mostrar un mensaje indicando que el nombre de categoría ya está en uso

---

### Requirement 8: Gestión de Productos (CRUD)

**User Story:** Como Administrador, quiero crear, consultar, modificar y eliminar productos, para mantener actualizado el catálogo de artículos disponibles para la venta.

#### Acceptance Criteria

1. WHEN el Administrador solicita crear un producto con datos válidos (nombre de máximo 100 caracteres, descripción de máximo 500 caracteres, precio, stock inicial, categoría existente), THE Módulo_Productos SHALL registrar el producto en la base de datos asociado a la categoría indicada y con estado activo
2. WHEN un usuario solicita listar productos, THE Módulo_Productos SHALL mostrar únicamente los productos con estado activo, presentando su nombre, precio, stock disponible y categoría
3. WHEN el Administrador solicita modificar un producto activo, THE Módulo_Productos SHALL actualizar los campos editables (nombre, descripción, precio, stock, categoría) aplicando las mismas reglas de validación que en la creación
4. WHEN el Administrador solicita eliminar un producto, THE Módulo_Productos SHALL realizar una eliminación lógica marcando el producto como inactivo, de modo que deje de aparecer en listados y búsquedas
5. WHEN un usuario busca productos por nombre o categoría, THE Módulo_Productos SHALL filtrar los productos activos mediante coincidencia parcial (contiene) sin distinguir mayúsculas de minúsculas, y mostrar los resultados que coincidan
6. IF se intenta registrar o modificar un producto con precio menor a 0.01 o mayor a 999,999,999.99, THEN THE Módulo_Productos SHALL rechazar la operación y mostrar un mensaje de validación indicando el rango de precio permitido
7. IF se intenta registrar o modificar un producto con stock negativo, THEN THE Módulo_Productos SHALL rechazar la operación y mostrar un mensaje de validación indicando que el stock debe ser mayor o igual a cero
8. IF se intenta registrar un producto con un nombre que ya existe en estado activo dentro de la misma categoría, THEN THE Módulo_Productos SHALL rechazar la operación y mostrar un mensaje indicando que el producto ya existe en esa categoría
9. IF se intenta crear o modificar un producto referenciando una categoría inexistente o inactiva, THEN THE Módulo_Productos SHALL rechazar la operación y mostrar un mensaje indicando que la categoría seleccionada no es válida

---

### Requirement 9: Registro de Ventas

**User Story:** Como Vendedor, quiero registrar ventas seleccionando productos y cantidades, para documentar las transacciones comerciales realizadas.

#### Acceptance Criteria

1. WHEN un Vendedor inicia una nueva venta, THE Módulo_Ventas SHALL crear una transacción con la fecha actual y el vendedor asociado
2. WHEN un Vendedor agrega un producto a la venta indicando la cantidad (entero entre 1 y 10,000 unidades), THE Módulo_Ventas SHALL agregar el detalle de venta calculando el subtotal como precio unitario vigente del producto multiplicado por la cantidad indicada
3. WHEN un Vendedor confirma la venta, THE Módulo_Ventas SHALL registrar la venta y descontar el stock de cada producto vendido de forma atómica dentro de una única transacción de base de datos, de modo que todos los cambios se persistan juntos o ninguno se aplique
4. WHEN se agrega, elimina o modifica un detalle de la venta, THE Módulo_Ventas SHALL recalcular y mostrar el total de la venta como la suma de todos los subtotales de los detalles en un tiempo máximo de 1 segundo
5. IF un Vendedor intenta vender una cantidad mayor al stock disponible de un producto, THEN THE Módulo_Ventas SHALL rechazar la operación para ese producto y mostrar un mensaje indicando stock insuficiente
6. IF un Vendedor intenta confirmar una venta sin detalles de productos, THEN THE Módulo_Ventas SHALL rechazar la operación y mostrar un mensaje indicando que la venta debe tener al menos un producto
7. IF ocurre un error durante la confirmación de la venta (fallo de conexión o conflicto de concurrencia), THEN THE Módulo_Ventas SHALL revertir todos los cambios de la transacción, mantener la venta en estado editable con sus detalles intactos, y mostrar un mensaje indicando que la operación falló
8. WHEN un Vendedor elimina un producto de la venta antes de confirmarla, THE Módulo_Ventas SHALL remover el detalle correspondiente y recalcular el total de la venta
9. IF un Vendedor intenta agregar una cantidad menor a 1 o mayor a 10,000 unidades, THEN THE Módulo_Ventas SHALL rechazar la operación y mostrar un mensaje indicando que la cantidad debe estar entre 1 y 10,000

---

### Requirement 10: Consulta de Ventas

**User Story:** Como Administrador, quiero consultar el historial de ventas con opciones de filtrado, para analizar las transacciones realizadas.

#### Acceptance Criteria

1. WHEN el Administrador solicita el historial de ventas, THE Módulo_Ventas SHALL mostrar la lista paginada de ventas registradas con fecha, vendedor, cantidad de productos y total, ordenadas por fecha descendente (más reciente primero)
2. WHEN el usuario filtra ventas por rango de fechas (fecha inicio y fecha fin), THE Módulo_Ventas SHALL mostrar únicamente las ventas realizadas dentro del período indicado inclusive
3. WHEN el usuario selecciona una venta específica, THE Módulo_Ventas SHALL mostrar el detalle completo incluyendo los productos vendidos, cantidades, precios unitarios y subtotales
4. WHILE un usuario tiene rol de Vendedor, THE Módulo_Ventas SHALL mostrar únicamente las ventas registradas por ese vendedor
5. IF el rango de fechas seleccionado no contiene ventas registradas, THEN THE Módulo_Ventas SHALL mostrar un mensaje indicando que no se encontraron ventas en el período seleccionado

---

### Requirement 11: Generación y Exportación de Reportes

**User Story:** Como Administrador, quiero generar reportes de ventas y productos exportables, para analizar el rendimiento del negocio y compartir información con terceros.

#### Acceptance Criteria

1. WHEN el Administrador selecciona una fecha de inicio y una fecha de fin (rango máximo de 365 días) y solicita un reporte de ventas, THE Módulo_Reportes SHALL generar un resumen que incluya el total monetario de ventas, la cantidad de transacciones realizadas y un desglose diario con el monto y número de transacciones por cada fecha dentro del rango
2. WHEN el Administrador solicita un reporte de productos más vendidos para un período seleccionado, THE Módulo_Reportes SHALL generar un listado de los 50 productos con mayor cantidad total de unidades vendidas, ordenados de mayor a menor, mostrando nombre del producto, categoría y cantidad total vendida
3. WHEN el Administrador solicita exportar un reporte visualizado en pantalla, THE Módulo_Reportes SHALL presentar las opciones de formato (PDF, Excel XLSX, CSV) y generar el archivo en el formato seleccionado por el usuario en un tiempo máximo de 30 segundos
4. WHEN el Módulo_Reportes genera un archivo de exportación exitosamente, THE Módulo_Reportes SHALL iniciar la descarga del archivo en el navegador del usuario con un nombre que incluya el tipo de reporte y el rango de fechas
5. WHEN el Administrador solicita un reporte de inventario, THE Módulo_Reportes SHALL generar un listado de todos los productos registrados mostrando nombre, categoría, stock actual, precio de venta y estado (activo/inactivo)
6. THE Módulo_Reportes SHALL mostrar los reportes en pantalla antes de permitir la exportación
7. IF el período seleccionado no contiene datos de ventas o productos vendidos, THEN THE Módulo_Reportes SHALL mostrar el reporte vacío con un mensaje indicando que no se encontraron resultados para el período seleccionado
8. IF la generación del archivo de exportación falla o excede los 30 segundos, THEN THE Módulo_Reportes SHALL mostrar un mensaje de error indicando que la exportación no pudo completarse y permitir al usuario reintentar la operación

---

### Requirement 12: Validaciones e Integridad de Datos

**User Story:** Como desarrollador, quiero que el sistema aplique validaciones tanto en cliente como en servidor, para garantizar la integridad de los datos almacenados.

#### Acceptance Criteria

1. THE Sistema_POS SHALL validar todos los formularios tanto en el cliente (Blazor mediante EditForm y DataAnnotationsValidator) como en el servidor de forma independiente antes de persistir datos, de modo que la validación del servidor se ejecute aun si la validación del cliente es omitida o eludida
2. IF un usuario envía un formulario con campos obligatorios vacíos, THEN THE Sistema_POS SHALL rechazar la operación, resaltar visualmente cada campo faltante junto a un mensaje indicando que el campo es requerido, y preservar los datos ya ingresados por el usuario en los demás campos del formulario
3. IF un usuario envía datos que violan restricciones de formato (correo electrónico inválido, texto en campo numérico, valores monetarios fuera del rango 0.01 a 999,999,999.99, o cadenas que exceden la longitud máxima definida en el modelo de datos), THEN THE Sistema_POS SHALL rechazar la operación y mostrar junto al campo correspondiente un mensaje que indique la restricción específica violada
4. THE Sistema_POS SHALL utilizar transacciones de base de datos en operaciones que involucren múltiples tablas (registro de ventas con detalles y actualización de stock), con un tiempo límite de 30 segundos por transacción
5. IF ocurre un error durante una transacción de base de datos o se excede el tiempo límite de 30 segundos, THEN THE Sistema_POS SHALL revertir todos los cambios parciales, preservar los datos ingresados por el usuario en el formulario, y mostrar una notificación indicando que la operación no se completó y que el usuario puede reintentar


---

### Requirement 13: Historial de Auditoría de Operaciones

**User Story:** Como Administrador, quiero consultar un historial completo de todas las operaciones realizadas en el sistema, para llevar un control detallado de qué acciones se ejecutaron y quién las realizó.

#### Acceptance Criteria

1. THE Sistema_POS SHALL registrar automáticamente en una tabla de auditoría cada operación significativa realizada en el sistema, incluyendo: creación, modificación y eliminación de usuarios, productos y categorías, así como el registro y anulación de ventas
2. THE Sistema_POS SHALL almacenar para cada registro de auditoría los siguientes datos: identificador del usuario que realizó la acción, tipo de operación (creación, modificación, eliminación, venta), entidad afectada (usuario, producto, categoría, venta), identificador del registro afectado, fecha y hora exacta de la operación, y una descripción resumida del cambio realizado
3. WHEN el Administrador solicita consultar el historial de auditoría, THE Sistema_POS SHALL mostrar la lista paginada de registros de auditoría ordenados por fecha descendente (más reciente primero), mostrando fecha, usuario, tipo de operación, entidad afectada y descripción
4. WHEN el Administrador filtra el historial de auditoría por usuario, THE Sistema_POS SHALL mostrar únicamente los registros de operaciones realizadas por el usuario seleccionado
5. WHEN el Administrador filtra el historial de auditoría por rango de fechas, THE Sistema_POS SHALL mostrar únicamente los registros de operaciones realizadas dentro del período indicado inclusive
6. WHEN el Administrador filtra el historial por tipo de operación o entidad afectada, THE Sistema_POS SHALL mostrar únicamente los registros que coincidan con los criterios seleccionados
7. THE Sistema_POS SHALL impedir cualquier modificación o eliminación de los registros de auditoría desde la aplicación, garantizando que el historial sea de solo lectura e inmutable
8. WHILE un usuario tiene rol de Vendedor, THE Sistema_POS SHALL denegar el acceso al módulo de historial de auditoría
9. THE Sistema_POS SHALL registrar las operaciones de auditoría de forma asíncrona o en segundo plano, de modo que el registro de auditoría no afecte el tiempo de respuesta percibido por el usuario en la operación principal

---

### Requirement 14: Persistencia Offline y Sincronización

**User Story:** Como Vendedor, quiero que el sistema me permita seguir registrando ventas aunque se pierda la conexión con el servidor, para que mi trabajo no se detenga y los datos se sincronicen automáticamente cuando se restablezca la conexión.

#### Acceptance Criteria

1. THE Sistema_POS SHALL detectar la pérdida de conexión con el servidor y notificar al usuario mediante un indicador visual permanente en la interfaz que muestre el estado de conexión actual (conectado/desconectado)
2. WHILE el sistema se encuentra sin conexión al servidor, THE Sistema_POS SHALL permitir al Vendedor continuar registrando ventas almacenando los datos de forma local en el navegador utilizando IndexedDB o almacenamiento local equivalente
3. WHILE el sistema se encuentra sin conexión al servidor, THE Sistema_POS SHALL mantener disponible en caché local el catálogo de productos con sus precios y stock más recientes para permitir la selección de productos durante el registro de ventas
4. WHEN se restablece la conexión con el servidor, THE Sistema_POS SHALL sincronizar automáticamente todas las operaciones pendientes almacenadas localmente con la base de datos del servidor, en el orden cronológico en que fueron realizadas
5. IF durante la sincronización se detecta un conflicto (por ejemplo, stock insuficiente porque otro vendedor vendió el mismo producto mientras ambos estaban offline), THEN THE Sistema_POS SHALL marcar la operación conflictiva, notificar al Administrador del conflicto y presentar opciones de resolución
6. WHEN la sincronización de una operación pendiente se completa exitosamente, THE Sistema_POS SHALL eliminar la copia local de esa operación y actualizar el estado local con los datos confirmados del servidor
7. THE Sistema_POS SHALL mostrar un indicador del número de operaciones pendientes de sincronización mientras existan datos locales no sincronizados
8. IF el usuario cierra el navegador mientras hay operaciones pendientes de sincronización, THEN THE Sistema_POS SHALL preservar esas operaciones en el almacenamiento local del navegador y reanudar la sincronización automáticamente cuando el usuario vuelva a abrir el sistema
9. WHILE el sistema se encuentra en modo offline, THE Sistema_POS SHALL restringir las funcionalidades disponibles únicamente al registro de ventas y consulta del catálogo de productos en caché, deshabilitando operaciones que requieran datos actualizados del servidor (gestión de usuarios, reportes, auditoría)


---

### Requirement 15: Facturación Electrónica AFIP (Integración con Afip SDK)

**User Story:** Como Administrador, quiero que el sistema genere facturas electrónicas válidas ante AFIP/ARCA al confirmar una venta, para cumplir con las obligaciones fiscales y entregar comprobantes legales a los clientes.

#### Acceptance Criteria

1. THE Sistema_POS SHALL integrar la librería Afip SDK para .NET (paquete NuGet Afip.Net) para conectarse con los Web Services de facturación electrónica de AFIP/ARCA
2. THE Sistema_POS SHALL permitir configurar los parámetros de conexión con AFIP (CUIT del emisor, Access Token, certificado digital, clave privada y modo producción/desarrollo) desde una sección de configuración accesible únicamente por el Administrador
3. WHEN un Vendedor confirma una venta exitosamente, THE Sistema_POS SHALL solicitar automáticamente un CAE (Código de Autorización Electrónico) a AFIP mediante el método CreateNextVoucherAsync, enviando los datos del comprobante incluyendo punto de venta, tipo de comprobante, concepto, importes (total, neto gravado, IVA, exento, tributos), moneda y alícuotas de IVA
4. WHEN AFIP responde exitosamente con un CAE, THE Sistema_POS SHALL almacenar el CAE, la fecha de vencimiento del CAE y el número de comprobante asignado, asociándolos a la venta correspondiente en la base de datos
5. THE Sistema_POS SHALL soportar la emisión de los siguientes tipos de comprobantes según la condición fiscal del emisor: Factura A (tipo 1), Factura B (tipo 6) y Factura C (tipo 11), seleccionando automáticamente el tipo correcto según la condición IVA del receptor
6. WHEN el usuario solicita generar el PDF de una factura ya autorizada, THE Sistema_POS SHALL generar un comprobante en formato PDF que incluya los datos fiscales obligatorios: CAE, fecha de vencimiento del CAE, número de comprobante, datos del emisor, datos del receptor, detalle de productos, importes, alícuotas de IVA y código QR de verificación
7. IF la solicitud de CAE a AFIP falla (error de conexión, rechazo del comprobante o timeout), THEN THE Sistema_POS SHALL registrar la venta como pendiente de facturación, almacenar el error recibido, notificar al usuario que la factura no pudo emitirse, y permitir reintentar la emisión posteriormente
8. WHEN el Administrador accede a la sección de comprobantes pendientes de facturación, THE Sistema_POS SHALL mostrar la lista de ventas que no pudieron facturarse y permitir reintentar la emisión individual o masivamente
9. THE Sistema_POS SHALL permitir consultar la información de un comprobante ya emitido mediante el método GetVoucherInfoAsync para verificar su estado ante AFIP
10. THE Sistema_POS SHALL soportar modo desarrollo (utilizando CUIT de prueba 20409378472 sin certificado) y modo producción (con certificado digital propio), configurable por el Administrador
11. WHEN el sistema opera en modo offline y se registra una venta sin conexión, THE Sistema_POS SHALL marcar la venta como pendiente de facturación y emitir el comprobante electrónico automáticamente durante la sincronización cuando se restablezca la conexión con el servidor
12. THE Sistema_POS SHALL registrar en el historial de auditoría cada emisión de comprobante electrónico (exitosa o fallida) incluyendo el CAE obtenido, tipo de comprobante y número asignado

---

### Requirement 16: Gestión de Caja y Cierre de Caja

**User Story:** Como Vendedor, quiero abrir y cerrar la caja registrando los movimientos de dinero durante mi turno, para mantener un control preciso del efectivo y detectar diferencias al final de la jornada.

#### Acceptance Criteria

1. WHEN un Vendedor o Administrador solicita abrir la caja indicando el monto inicial en efectivo, THE Sistema_POS SHALL registrar la apertura de caja con la fecha y hora actual, el usuario que la abrió y el monto inicial declarado, siempre que no exista una caja abierta actualmente para ese punto de venta
2. IF un usuario intenta abrir una caja cuando ya existe una caja abierta para el mismo punto de venta, THEN THE Sistema_POS SHALL rechazar la operación y mostrar un mensaje indicando que debe cerrar la caja actual antes de abrir una nueva
3. WHILE la caja se encuentra abierta, THE Sistema_POS SHALL registrar automáticamente como ingreso de caja cada venta confirmada que se pague en efectivo, acumulando el monto al saldo esperado de la caja
4. WHEN un Administrador registra un retiro de efectivo de la caja indicando el monto y el motivo, THE Sistema_POS SHALL descontar el monto del saldo esperado y registrar el movimiento con fecha, hora, usuario y descripción
5. WHEN un Administrador registra un ingreso adicional de efectivo a la caja indicando el monto y el motivo, THE Sistema_POS SHALL sumar el monto al saldo esperado y registrar el movimiento con fecha, hora, usuario y descripción
6. WHEN un Vendedor o Administrador solicita cerrar la caja indicando el monto real contado en efectivo, THE Sistema_POS SHALL calcular la diferencia entre el saldo esperado (monto inicial + ingresos - retiros) y el monto real declarado, registrar la fecha y hora del cierre, y marcar la caja como cerrada
7. WHEN se completa un cierre de caja, THE Sistema_POS SHALL generar un resumen que incluya: monto de apertura, total de ventas en efectivo, total de retiros, total de ingresos adicionales, saldo esperado, monto real contado, diferencia (sobrante o faltante), y la lista detallada de todos los movimientos del período
8. THE Sistema_POS SHALL permitir al Administrador consultar el historial de cierres de caja anteriores con filtros por rango de fechas y por usuario que realizó el cierre
9. IF la diferencia entre el saldo esperado y el monto real contado supera un umbral configurable (por defecto $500), THEN THE Sistema_POS SHALL marcar el cierre como "con diferencia significativa" y notificar al Administrador
10. WHILE no existe una caja abierta para el punto de venta, THE Sistema_POS SHALL impedir el registro de nuevas ventas en efectivo y mostrar un mensaje indicando que se debe abrir la caja antes de operar
11. THE Sistema_POS SHALL registrar en el historial de auditoría cada apertura de caja, cierre de caja, retiro de efectivo e ingreso adicional

---

### Requirement 17: Gestión de Medios de Pago y Pago Mixto

**User Story:** Como Administrador, quiero configurar los medios de pago aceptados por el sistema y permitir que una venta se pague con múltiples métodos, para adaptarme a las necesidades del negocio y ofrecer flexibilidad a los clientes.

#### Acceptance Criteria

1. THE Sistema_POS SHALL incluir por defecto los siguientes medios de pago preconfigurados: Efectivo, Tarjeta de Débito, Tarjeta de Crédito y Transferencia Bancaria, cada uno con estado activo
2. WHEN el Administrador solicita crear un nuevo medio de pago proporcionando un nombre (entre 3 y 50 caracteres) que no exista previamente, THE Sistema_POS SHALL registrar el medio de pago en la base de datos con estado activo
3. WHEN el Administrador solicita modificar un medio de pago existente, THE Sistema_POS SHALL permitir actualizar el nombre del medio de pago aplicando las mismas reglas de validación que en la creación
4. WHEN el Administrador solicita desactivar un medio de pago, THE Sistema_POS SHALL marcarlo como inactivo de modo que deje de aparecer como opción disponible al registrar ventas, sin eliminar los registros históricos de pagos realizados con ese método
5. IF se intenta desactivar el medio de pago "Efectivo" mientras existe una caja abierta, THEN THE Sistema_POS SHALL rechazar la operación y mostrar un mensaje indicando que el medio de pago Efectivo no puede desactivarse mientras haya una caja operativa
6. WHEN un Vendedor confirma una venta, THE Sistema_POS SHALL requerir que el usuario especifique uno o más medios de pago activos con el monto asignado a cada uno, de modo que la suma de todos los montos sea igual al total de la venta
7. IF la suma de los montos asignados a los medios de pago no coincide con el total de la venta, THEN THE Sistema_POS SHALL rechazar la confirmación y mostrar un mensaje indicando la diferencia pendiente de asignar
8. WHEN una venta se paga parcial o totalmente en efectivo, THE Sistema_POS SHALL registrar únicamente el monto en efectivo como ingreso en la caja abierta (si existe), sin afectar el saldo de caja con montos de otros medios de pago
9. THE Sistema_POS SHALL almacenar para cada venta el desglose de pagos realizado, registrando el medio de pago utilizado y el monto asignado a cada uno, permitiendo consultar esta información en el detalle de la venta
10. WHEN el usuario consulta el detalle de una venta, THE Sistema_POS SHALL mostrar el desglose de medios de pago utilizados con el monto correspondiente a cada uno
11. THE Sistema_POS SHALL incluir en los reportes de ventas un desglose por medio de pago, mostrando el total recaudado por cada método en el período seleccionado
12. IF se intenta crear un medio de pago con un nombre que ya existe (activo o inactivo), THEN THE Sistema_POS SHALL rechazar la operación y mostrar un mensaje indicando que el nombre ya está en uso
