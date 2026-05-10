# CineTraker 🎬

**CineTraker** es una plataforma integral para cinéfilos que permite gestionar un catálogo de películas, registrar reseñas personales y descubrir contenido a través de un innovador sistema de **Mapas de Recomendación** basados en grafos interactivos.

Este proyecto fue desarrollado como pieza central de mi formación en la carrera de **Ingeniería en Sistemas de Información (UTN-FRRo)**, aplicando patrones de arquitectura desacoplada, seguridad robusta y visualización de datos dinámica.

## 🚀 Características Principales

* **Catálogo Inteligente:** Exploración con *scroll* infinito y carga bajo demanda. Incluye búsqueda local y fallback automático a la API de **OMDb** para garantizar un catálogo siempre expandible.
* **Filtros Dinámicos:** Segmentación avanzada por género, puntuación IMDb, década de estreno y disponibilidad en plataformas de *streaming*.
* **Mapas de Recomendación (Killer Feature):** Generación de grafos interactivos utilizando **vis-network**. El sistema conecta películas por director y género, permitiendo al usuario expandir el grafo puntuando nodos en tiempo real.
* **Mis Expediciones:** Panel personal para retomar mapas de exploración guardados, persistiendo el estado del grafo de cada usuario.
* **Detalle con Streaming:** Integración con **Watchmode API** para mostrar disponibilidad de títulos en plataformas legales de Argentina.
* **Seguridad:** Sistema de autenticación y autorización basado en **ASP.NET Identity** y **JWT (JSON Web Tokens)** con persistencia en `LocalStorage`.
* **Panel de Administración:** Interfaz de gestión para sincronización masiva de datos desde fuentes externas con indicadores de progreso.

## 🛠️ Stack Tecnológico

| Capa | Tecnología |
| --- | --- |
| **Backend** | ASP.NET Core Web API 8 |
| **Frontend** | Blazor WebAssembly (.NET 8) |
| **Base de Datos** | SQL Server + Entity Framework Core (Code First) |
| **Seguridad** | JWT Bearer Authentication |
| **Visualización** | vis-network (JavaScript Interop) |
| **APIs Externas** | OMDb API & Watchmode API |
| **UI/UX** | Bootstrap 5 + Bootstrap Icons |

## 🏗️ Estructura del Proyecto

* `CineTraker/`: Servidor API, controladores, servicios de lógica de negocio y contexto de base de datos.
* `CineTraker.Client/`: Aplicación Blazor WASM, componentes de UI y servicios de consumo de API.
* `CineTraker.Shared/`: Modelos de datos y DTOs compartidos entre ambas capas para asegurar la consistencia.

## 📦 Instalación y Configuración

1. **Clonar el repositorio:**
```bash
git clone https://github.com/tu-usuario/CineTraker.git

```


2. **Configurar la Base de Datos:**
En `CineTraker/appsettings.json`, ajustá tu cadena de conexión:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR;Database=CineTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;"
}

```


3. **Actualizar la base de datos:**
```bash
dotnet ef database update --project CineTraker

```


4. **Ejecutar:**
Iniciá el proyecto `CineTraker` (Server) y la aplicación estará disponible en `http://localhost:5100`.

## 🔐 Lógica de Seguridad

El sistema implementa un `DelegatingHandler` en el cliente Blazor para interceptar las peticiones HTTP e inyectar automáticamente el token JWT almacenado. En el servidor, se utiliza middleware de autorización para validar los *claims* y roles (User/Admin) en cada *endpoint*.

## 🗺️ Funcionamiento de los Mapas

El mapa de recomendaciones es un motor dinámico. Al puntuar una película con más de 1 estrella, el backend calcula nuevas relaciones basadas en metadatos compartidos y expande el grafo. Cada snapshot se guarda en formato JSON, permitiendo una experiencia de exploración persistente y asincrónica.

## 🚧 Roadmap
* [ ] Despliegue productivo en **Azure App Service**.
* [ ] Soporte multiregión para disponibilidad de streaming.
* [ ] Estadísticas visuales de consumo cinematográfico por usuario.

## 👤 Autor

**Luca Conti** - Estudiante de 5to año de Ingeniería en Sistemas de Información.
**UTN Facultad Regional Rosario (UTN-FRRo)**
