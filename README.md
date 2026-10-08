# HobbitCoder.TMTBucaramanga.Sheepfold

## Configuración local

Los secretos no van en el repositorio. En desarrollo se guardan con `dotnet user-secrets` en la WebAPI:

```powershell
$project = 'HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI'
dotnet user-secrets set 'ConnectionStrings:Default' 'Server=localhost;Port=5432;Database=TmtBucaramangaSheepfold;User Id=<usuario>;Password=<clave>' --project $project
```

En los demás ambientes se usan variables de entorno: `ConnectionStrings__Default` y `JwtSettings__Authority`.

### Junto con Identity y los frontends

En desarrollo, `JwtSettings:Authority` ya apunta a Identity (`https://localhost:7101`, en `appsettings.Development.json`).
Cada proyecto tiene sus propios puertos, user-secrets y base de datos, así que pueden correr a la vez:

| Proyecto | URL de desarrollo | Base de datos |
| --- | --- | --- |
| HobbitCoder.TMTBucaramanga.Identity | `https://localhost:7101` (perfil `https`) | `TmtBucaramangaIdentity` |
| hobbitcoder-tmtbucaramanga-identity (frontend) | `http://localhost:4300` (inicio de sesión y registro) | — |
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

### CORS

En producción el frontend vive en **el mismo dominio** que esta API (`https://sheepfold.tmtbucaramanga.com`): sus
llamadas son del mismo origen y el navegador no aplica CORS, así que no se permite ningún otro origen. En desarrollo
el frontend corre en `http://localhost:4200`, que sí es otro origen. Cada ambiente declara su política en su
`appsettings`:

| Ambiente | `AllowedOrigins` |
| --- | --- |
| Development | `http://localhost:4200` |
| Production | ninguno (mismo origen) |
| Base (cualquier otro) | ninguno |

En todos: métodos `GET`, `POST`, `PUT`, `PATCH`, `DELETE`; cabeceras `Authorization` y `Content-Type`; se exponen
`Location` y `WWW-Authenticate`; preflight en caché 10 minutos y **sin credenciales** (el token viaja en la cabecera
`Authorization`, no en cookies).

Cada origen se valida al arrancar: debe ser exactamente `esquema://host[:puerto]`, sin barra final ni ruta, `https`
fuera de desarrollo y nunca `*`. Si algún día el frontend se sirve desde otro dominio, agrégalo en
`appsettings.Production.json` o con `CorsSettings__AllowedOrigins__0`.

## Autenticación

Los JWT los emite **HobbitCoder.TMTBucaramanga.Identity**; esta API no genera tokens ni tiene claves para firmarlos.
Solo los valida: descarga las claves públicas del JWKS que Identity publica en
`{Authority}/.well-known/openid-configuration` y acepta únicamente tokens firmados con RS256.

Todos los endpoints de `TodoList` y `TodoItem` exigen un token de Identity emitido para esta API
(`aud` = `HobbitCoder.TMTBucaramanga.Sheepfold`); sin él responden **401**. Un token para otra API, de otro emisor
o firmado con otro algoritmo también recibe 401. `/health` es público. El usuario de auditoría (`CreatedBy`,
`UpdatedBy`, `CompletedBy`) es el claim `sub` del token.

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

`JwtSettings:Authority` ya viene en `appsettings.Production.json` (`https://identity.tmtbucaramanga.com`). Esta API no
tiene claves ni secretos de JWT.

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

Requieren **Docker** (PostgreSQL con Testcontainers). Verifican que cada endpoint exige token y que se rechazan los
tokens de otra audiencia, de otro emisor o firmados con HS256.

## Migraciones

Cada proveedor tiene sus migraciones en su proyecto `Persistence.*`. El proveedor se elige con `Persistence__Provider`:

```powershell
$env:Persistence__Provider = 'PostgreSQL'   # o 'SqlServer'
dotnet ef migrations add <Nombre> --project HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI
dotnet ef database update --project HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL --startup-project HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI
```

Un cambio de modelo requiere una migración para cada proveedor.
