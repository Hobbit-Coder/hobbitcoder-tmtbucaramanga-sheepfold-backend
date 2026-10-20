# HobbitCoder.TMTBucaramanga.Sheepfold

API ministerial de TMT (.NET 10): grupos y generaciones, asistentes, eventos, asistencia, discusión y archivos,
seguimientos pastorales, avisos, reportes, auditoría, registros retirados y configuración. Las cuentas y las
contraseñas viven en **HobbitCoder.TMTBucaramanga.Identity**; aquí solo se guarda el `sub` de cada usuario y su
alcance ministerial. El modelo de datos está en `docs/requerimiento/modelo-de-datos.md`.

## Configuración local

Los secretos no van en el repositorio. En desarrollo se guardan con `dotnet user-secrets` en la WebAPI:

```powershell
$project = 'HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI'
dotnet user-secrets set 'ConnectionStrings:Default' 'Server=localhost;Port=5432;Database=TmtBucaramangaSheepfold;User Id=<usuario>;Password=<clave>' --project $project
```

En los demás ambientes se usan variables de entorno (`ConnectionStrings__Default`, `IdentityDirectory__ClientSecret`,
`Storage__*`, `Messaging__*`); la lista completa está en `docs/requerimiento/despliegue-dokploy.md`.

### Junto con Identity y los frontends

En desarrollo, `JwtSettings:Authority` ya apunta a Identity (`https://localhost:7101`, en `appsettings.Development.json`).
Cada proyecto tiene sus propios puertos, user-secrets y base de datos, así que pueden correr a la vez:

| Proyecto | URL de desarrollo | Base de datos |
| --- | --- | --- |
| HobbitCoder.TMTBucaramanga.Identity | `https://localhost:7101` (perfil `https`) | `TmtBucaramangaIdentity` |
| hobbitcoder-tmtbucaramanga-identity (frontend) | `http://localhost:4300` (ingreso, primer ingreso, usuarios) | — |
| HobbitCoder.TMTBucaramanga.Sheepfold | `https://localhost:7291` / `http://localhost:5280` | `TmtBucaramangaSheepfold` |
| hobbitcoder-tmtbucaramanga-sheepfold (frontend) | `http://localhost:4200` (`ng serve`, reenvía `/api` a Sheepfold) | — |

El frontend de Sheepfold no pide credenciales: lleva al usuario al frontend de Identity y recibe tokens emitidos para
esta API (código de autorización con PKCE); su README describe el flujo completo.

1. Identity debe correr con su perfil `https`: Sheepfold descarga de ahí el discovery y las claves públicas.
2. El certificado HTTPS de desarrollo debe ser de confianza (`dotnet dev-certs https --trust`), o Sheepfold no podrá
   descargar el discovery.
3. Inicia sesión en Identity con `audience` = `HobbitCoder.TMTBucaramanga.Sheepfold` (`POST /api/auth/login`) y usa el
   `accessToken` como `Authorization: Bearer`. Con una sesión iniciada para otra API, `POST /api/auth/refresh` con esa
   `audience` entrega un token para Sheepfold. El archivo `.http` de la WebAPI tiene ambas peticiones.

| Clave | Descripción |
| --- | --- |
| `Persistence:Provider` | `PostgreSQL` (por defecto) o `SqlServer`. |
| `ConnectionStrings:Default` | Cadena de conexión del proveedor elegido. |
| `JwtSettings:Authority` | URL base pública de HobbitCoder.TMTBucaramanga.Identity, idéntica a su `Jwt:Issuer` (sin barra final). También es el emisor (`iss`) esperado. En desarrollo, `https://localhost:7101`; fuera de desarrollo debe ser `https`. La API no arranca sin ella. |
| `JwtSettings:Audience` | En `appsettings.json`: `HobbitCoder.TMTBucaramanga.Sheepfold`. Debe estar en `Jwt:Audiences` de Identity. |
| `CorsSettings:*` | Política CORS por ambiente, en los `appsettings`. Ver [CORS](#cors). |
| `IdentityDirectory:*` | Directorio de cuentas de Identity (nombres, roles, estado) con credenciales de cliente de servicio: `BaseUrl`, `ClientId` (`sheepfold-api`), `ClientSecret` (secreto, igual a `Jwt:ServiceClients` de Identity) y `CacheSeconds`. |
| `Storage:*` | Almacenamiento S3 compatible (Contabo) para adjuntos. Sin configurar, los archivos responden 503 y el resto funciona. Ver `docs/requerimiento/almacenamiento-s3.md`. |
| `Messaging:*` | WhatsApp para los avisos definidos: `Provider` `Evolution` (por defecto; `Evolution:Url`, `Evolution:Instance`, `Evolution:ApiKey`, `Evolution:SecondsBetweenMessages`) o `Meta` (`Meta:*`). Opcional: sin credenciales solo hay avisos internos. Ver `docs/requerimiento/whatsapp-evolution.md`. |
| `Notifications:ScheduleIntervalMinutes` | Cada cuánto se generan los avisos programados (60 min). |
| `Seeding:Enabled` | `true` carga al arrancar los datos del ministerio 2026. Ver [Carga inicial](#carga-inicial). |

### CORS

En producción el frontend de Sheepfold vive en **el mismo dominio** que esta API (`https://sheepfold.tmtbucaramanga.com`):
sus llamadas son del mismo origen y el navegador no aplica CORS. El único origen externo es el frontend de Identity,
cuyo editor de usuarios lee `GET /api/users/{userId}/responsibilities`. En desarrollo ambos frontends corren en otros
puertos. Cada ambiente declara su política en su `appsettings`:

| Ambiente | `AllowedOrigins` |
| --- | --- |
| Development | `http://localhost:4200`, `http://localhost:4300` |
| Production | `https://identity.tmtbucaramanga.com` |
| Base (cualquier otro) | ninguno |

En todos: métodos `GET`, `POST`, `PUT`, `PATCH`, `DELETE`; cabeceras `Authorization` y `Content-Type`; se exponen
`Location` y `WWW-Authenticate`; preflight en caché 10 minutos y **sin credenciales** (el token viaja en la cabecera
`Authorization`, no en cookies).

Cada origen se valida al arrancar: debe ser exactamente `esquema://host[:puerto]`, sin barra final ni ruta, `https`
fuera de desarrollo y nunca `*`. Si algún día el frontend se sirve desde otro dominio, agrégalo en
`appsettings.Production.json` o con `CorsSettings__AllowedOrigins__0`.

### Carga inicial

Con `Seeding:Enabled=true` (variable `Seeding__Enabled` o user-secrets), `SheepfoldSeeder` inserta al arrancar
`Infrastructure/Persistence/Seeding/sheepfold-seed.json`: la generación PAPÁS, el curso FACTER, 18 tipos de evento,
los 12 grupos de 2026 con sus líderes y coordinadores, 307 asistentes (46 sin grupo, visibles solo para directores)
con sus cursos y los padres de PAPÁS como contacto, y los 48 eventos del cronograma (finalizados los que ya pasaron).
Va en una sola transacción y solo una vez: si la generación PAPÁS existe no hace nada. Requisitos: la base migrada
y **Identity sembrado antes**, porque las responsabilidades apuntan a sus cuentas por Id. El JSON se genera desde
los Excel con `docs/resources/bases-grupos/generar-semillas.py` (`python -I`, requiere `openpyxl`).

## Autenticación

Los JWT los emite **HobbitCoder.TMTBucaramanga.Identity**; esta API no genera tokens ni tiene claves para firmarlos.
Solo los valida: descarga las claves públicas del JWKS que Identity publica en
`{Authority}/.well-known/openid-configuration` y acepta únicamente tokens firmados con RS256.

Todos los endpoints bajo `/api` exigen un token de Identity emitido para esta API
(`aud` = `HobbitCoder.TMTBucaramanga.Sheepfold`); sin él responden **401**. Un token para otra API, de otro emisor
o firmado con otro algoritmo también recibe 401. `/health` es público. El usuario de auditoría es el claim `sub`.

Además del token, cada operación pasa por `MinistryAccess` (`Application/Common/Security/`): el rol y la
responsabilidad vigente (director, coordinador de generación, líder o aprendiz de grupo) definen qué se puede ver y
hacer (**403**/**404** fuera del alcance). El administrador solo gestiona catálogos y configuración: no tiene acceso a
información ministerial ni pastoral. El contenido de los seguimientos solo lo reciben quienes la regla de
confidencialidad permite, y cada lectura queda auditada.

## Endpoints

| Recurso | Rutas principales |
| --- | --- |
| Contexto | `GET /api/me/context` (rol, alcance y capacidades para la interfaz) |
| Calendario | `GET /api/calendar` (eventos, seguimientos y fechas visibles en un rango) |
| Eventos | `GET/POST /api/events`, `GET /api/events/options`, `GET/PUT/DELETE /api/events/{id}`, `POST /api/events/{id}/status` |
| Asistencia | `GET/PUT /api/events/{id}/attendance` (dos marcas excluyentes; población según el grupo vigente al inicio del evento) |
| Discusión y archivos | `GET/POST /api/events/{id}/comments`, `POST /api/events/{id}/attachments`, `GET/DELETE /api/events/{id}/attachments/{attachmentId}`, `GET .../comments/{commentId}/attachments/{attachmentId}` |
| Asistentes | `GET/POST /api/attendees`, `GET/PUT/DELETE /api/attendees/{id}`, `history`, `activity`, `transfer`, `contacts`, `courses` |
| Grupos | `GET/POST /api/groups`, `GET/PUT/DELETE /api/groups/{id}`, `history`, `PUT /api/groups/{id}/team`, `GET /api/groups/team-candidates`, `GET /api/groups/renewal`, `POST /api/groups/renewal/assignments` |
| Seguimientos | `GET/POST /api/follow-ups`, `GET /api/follow-ups/subjects`, `GET/DELETE /api/follow-ups/{id}`, `PUT .../schedule`, `POST .../complete`, `PUT .../content` |
| Avisos | `GET /api/notifications` |
| Reportes | `GET /api/reports/{family}` |
| Auditoría | `GET /api/audit`, `GET /api/audit/{id}` |
| Retirados | `GET /api/retired`, `GET /api/retired/{type}/{id}/impact`, `POST /api/retired/{type}/{id}/restore`, `DELETE /api/retired/{type}/{id}` |
| Catálogos y configuración | `GET /api/catalogs`, `POST /api/catalogs/{kind}`, `PUT /api/catalogs/{kind}/{id}`, `GET/PUT /api/settings/indicators`, `GET/PUT /api/settings/notifications` |
| Responsabilidades | `GET /api/users/{userId}/responsibilities` (para el editor de usuarios de Identity) |

## Producción (Docker + Dokploy)

El `Dockerfile` de la raíz construye la imagen de la WebAPI. El contenedor escucha **HTTP en el puerto 8080**, corre
con un usuario sin privilegios y arranca en `Production`. El HTTPS de `sheepfold.tmtbucaramanga.com` lo termina el
Traefik de Dokploy con Let's Encrypt; la imagen respeta sus encabezados `X-Forwarded-*`.

**Aplicación en Dokploy**

- Origen: este repositorio. Tipo de build: `Dockerfile`, ruta `Dockerfile`.
- Dominio: `sheepfold.tmtbucaramanga.com` con ruta **`/api`** (sin quitar el prefijo), puerto del contenedor `8080`,
  HTTPS con Let's Encrypt. El frontend es otra aplicación de Dokploy con el mismo dominio y ruta `/`: Traefik envía
  `/api/...` a esta API y el resto al frontend, y así ambos comparten origen.
- Health check: `GET /health` en el puerto 8080 del contenedor. Por el dominio público esa ruta la sirve el frontend, porque solo `/api` llega a esta API.

**Variables de entorno**

| Variable | Valor |
| --- | --- |
| `ConnectionStrings__Default` | Cadena de conexión al PostgreSQL de producción (base `TmtBucaramangaSheepfold`). |
| `IdentityDirectory__ClientSecret` | Secreto del cliente de servicio `sheepfold-api` (el mismo de `Jwt__ServiceClients__0__Secret` en Identity). |
| `Storage__ServiceUrl`, `Storage__Region`, `Storage__Bucket`, `Storage__AccessKey`, `Storage__SecretKey` | Contabo Object Storage. Sin ellos, los archivos responden 503. |
| `Messaging__Evolution__Url`, `Messaging__Evolution__Instance`, `Messaging__Evolution__ApiKey` | Evolution API para avisos por WhatsApp (opcional). |

`JwtSettings:Authority`, `IdentityDirectory:BaseUrl` y CORS ya vienen en `appsettings.Production.json`. Esta API no
tiene claves para firmar JWT.

- **Identity primero.** Sheepfold descarga `https://identity.tmtbucaramanga.com/.well-known/openid-configuration` y
  el JWKS desde el contenedor, por HTTPS. Identity debe estar desplegado y el servidor debe poder resolver y llegar a
  su propio dominio; si no, ninguna petición autenticada pasa (401).
- **Migraciones.** El contenedor no las aplica. Genera el script idempotente y ejecútalo en producción antes de
  desplegar una versión con cambios de modelo:

```powershell
$env:Persistence__Provider = 'PostgreSQL'
dotnet ef migrations script --idempotent --output sheepfold.sql --project HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI
```

## Pruebas

```powershell
dotnet run --project HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests
```

Requieren **Docker** (PostgreSQL con Testcontainers). Cubren autenticación (token obligatorio, audiencia, emisor y
algoritmo), CORS y las reglas de negocio por módulo: `OrganizationTests` (grupos, traslados, renovación, asistentes,
contactos, cursos), `EventTests` (eventos, público, asistencia), `FollowUpTests` (confidencialidad) y
`OversightTests` (avisos, reportes, auditoría, retirados, configuración y archivos con un almacenamiento en memoria).

## Migraciones

Cada proveedor tiene sus migraciones en su proyecto `Persistence.*`. El proveedor se elige con `Persistence__Provider`:

```powershell
$env:Persistence__Provider = 'PostgreSQL'   # o 'SqlServer'
dotnet ef migrations add <Nombre> --project HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI
dotnet ef database update --project HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI
```

Un cambio de modelo requiere una migración para cada proveedor.

> **Atención:** `MinistryMvp` reemplaza el esquema de ejemplo y **elimina las tablas `TodoLists` y `TodoItems`**.
> Antes de aplicarla en un ambiente con datos, respalda la base (ver `docs/requerimiento/despliegue-dokploy.md`).
