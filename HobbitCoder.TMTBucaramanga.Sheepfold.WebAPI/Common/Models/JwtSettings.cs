namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;

/// <summary>
/// Parámetros para validar los JWT que emite HobbitCoder.TMTBucaramanga.Identity, leídos de la
/// sección <see cref="SectionName"/>. Esta API no tiene ninguna clave para firmar: las claves
/// públicas se descargan del JWKS que publica <see cref="Authority"/>.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// URL base de Identity, donde expone <c>/.well-known/openid-configuration</c>. El claim
    /// <c>iss</c> de los tokens debe coincidir con el <c>issuer</c> que publica ese documento.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Valor esperado del claim <c>aud</c>: el identificador de esta API.</summary>
    public string Audience { get; set; } = string.Empty;
}
