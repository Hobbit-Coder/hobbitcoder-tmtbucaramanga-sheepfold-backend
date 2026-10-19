namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

/// <summary>
/// Envío automático de notificaciones por WhatsApp a destinatarios ya autorizados. No es un canal de
/// comunicación libre: solo recibe notificaciones generadas por la aplicación, sin contenido pastoral.
/// </summary>
public interface IWhatsAppNotificationSender
{
    /// <summary>Encola el envío; no bloquea la operación que originó la notificación.</summary>
    void Enqueue(WhatsAppNotification notification);
}
